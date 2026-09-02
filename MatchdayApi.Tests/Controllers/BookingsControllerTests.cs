using MatchdayApi.Controllers;
using MatchdayApi.DTOs.Refunds;
using MatchdayApi.Enums;
using MatchdayApi.Models;
using Microsoft.EntityFrameworkCore;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace MatchdayApi.Tests.Controllers;

public class BookingsControllerTests
{
    private const int OwnerId = 2;

    private static async Task<Booking> SeedBookingAsync(
        MatchdayApi.Data.AppDbContext db,
        BookingStatus status,
        int matchId = 3,
        int userId = OwnerId)
    {
        var booking = new Booking
        {
            BookingCode = "BK260101888888",
            Status = status,
            UserId = userId,
            MatchId = matchId,
            TotalAmount = 150000,
            BookingSeats = { new BookingSeat { SeatId = 1, MatchId = matchId, Price = 150000 } }
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        return booking;
    }

    // ---------- Cancel ----------

    [Fact]
    public async Task Cancel_ShouldCancelBookingAndReleaseSeats_WhenPending()
    {
        await using var db = TestHelpers.CreateDbContext();
        var booking = await SeedBookingAsync(db, BookingStatus.Pending);

        var hubMock = TestHelpers.CreateMockHub(out var clientProxyMock);
        var controller = new BookingsController(db, hubMock.Object);
        TestHelpers.SetCurrentUser(controller, OwnerId);

        var result = await controller.Cancel(booking.Id);

        Assert.IsType<NoContentResult>(result);

        var reloaded = await db.Bookings.Include(b => b.BookingSeats).FirstAsync(b => b.Id == booking.Id);
        Assert.Equal(BookingStatus.Cancelled, reloaded.Status);
        Assert.Empty(reloaded.BookingSeats);

        clientProxyMock.Verify(
            p => p.SendCoreAsync("SeatReleased", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Cancel_ShouldReturnBadRequest_WhenBookingNotPending()
    {
        await using var db = TestHelpers.CreateDbContext();
        var booking = await SeedBookingAsync(db, BookingStatus.Paid);

        var hubMock = TestHelpers.CreateMockHub(out _);
        var controller = new BookingsController(db, hubMock.Object);
        TestHelpers.SetCurrentUser(controller, OwnerId);

        var result = await controller.Cancel(booking.Id);

        Assert.IsType<BadRequestObjectResult>(result);

        var reloaded = await db.Bookings.FindAsync(booking.Id);
        Assert.Equal(BookingStatus.Paid, reloaded!.Status); // không bị đổi trạng thái
    }

    [Fact]
    public async Task Cancel_ShouldReturnForbid_WhenCallerIsNotOwner()
    {
        await using var db = TestHelpers.CreateDbContext();
        var booking = await SeedBookingAsync(db, BookingStatus.Pending, userId: OwnerId);

        var hubMock = TestHelpers.CreateMockHub(out _);
        var controller = new BookingsController(db, hubMock.Object);
        TestHelpers.SetCurrentUser(controller, userId: 999); // không phải chủ đơn

        var result = await controller.Cancel(booking.Id);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Cancel_ShouldReturnNotFound_WhenBookingDoesNotExist()
    {
        await using var db = TestHelpers.CreateDbContext();
        var hubMock = TestHelpers.CreateMockHub(out _);
        var controller = new BookingsController(db, hubMock.Object);
        TestHelpers.SetCurrentUser(controller, OwnerId);

        var result = await controller.Cancel(999999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ---------- RequestRefund ----------

    [Fact]
        public async Task RequestRefund_ShouldCreatePendingRefund_WhenBookingPaidAndMatchNotFinished()
    {
        await using var db = TestHelpers.CreateDbContext();
        // matchId = 3 (Upcoming, chưa kết thúc) — có sẵn trong dữ liệu mẫu.
        var booking = await SeedBookingAsync(db, BookingStatus.Paid, matchId: 3);

        var hubMock = TestHelpers.CreateMockHub(out _);
        var controller = new BookingsController(db, hubMock.Object);
        TestHelpers.SetCurrentUser(controller, OwnerId);

        var result = await controller.RequestRefund(booking.Id, new RefundRequestDto { Reason = "Bận việc đột xuất" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<RefundDto>(ok.Value);
        Assert.Equal(RefundStatus.Pending, dto.Status);

        Assert.Equal(1, await db.Refunds.CountAsync(r => r.BookingId == booking.Id));
    }

    [Fact]
    public async Task RequestRefund_ShouldReturnBadRequest_WhenAlreadyHasPendingRefund()
    {
        await using var db = TestHelpers.CreateDbContext();
        var booking = await SeedBookingAsync(db, BookingStatus.Paid, matchId: 3);
        db.Refunds.Add(new Refund
        {
            BookingId = booking.Id,
            Reason = "Yêu cầu trước đó",
            Status = RefundStatus.Pending,
            Amount = booking.TotalAmount
        });
        await db.SaveChangesAsync();

        var hubMock = TestHelpers.CreateMockHub(out _);
        var controller = new BookingsController(db, hubMock.Object);
        TestHelpers.SetCurrentUser(controller, OwnerId);

        var result = await controller.RequestRefund(booking.Id, new RefundRequestDto { Reason = "Yêu cầu lần 2" });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        // Không được tạo thêm bản ghi Refund thứ 2.
        Assert.Equal(1, await db.Refunds.CountAsync(r => r.BookingId == booking.Id));
    }

    [Fact]
    public async Task RequestRefund_ShouldReturnBadRequest_WhenMatchAlreadyFinished()
    {
        await using var db = TestHelpers.CreateDbContext();
        // matchId = 1 đã Finished trong dữ liệu mẫu.
        var booking = await SeedBookingAsync(db, BookingStatus.Paid, matchId: 1);

        var hubMock = TestHelpers.CreateMockHub(out _);
        var controller = new BookingsController(db, hubMock.Object);
        TestHelpers.SetCurrentUser(controller, OwnerId);

        var result = await controller.RequestRefund(booking.Id, new RefundRequestDto { Reason = "Không đi được" });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task RequestRefund_ShouldReturnBadRequest_WhenBookingNotPaid()
    {
        await using var db = TestHelpers.CreateDbContext();
        var booking = await SeedBookingAsync(db, BookingStatus.Pending, matchId: 3);

        var hubMock = TestHelpers.CreateMockHub(out _);
        var controller = new BookingsController(db, hubMock.Object);
        TestHelpers.SetCurrentUser(controller, OwnerId);

        var result = await controller.RequestRefund(booking.Id, new RefundRequestDto { Reason = "Test" });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}
