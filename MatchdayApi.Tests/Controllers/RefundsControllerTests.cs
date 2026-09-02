using MatchdayApi.Controllers;
using MatchdayApi.Enums;
using MatchdayApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace MatchdayApi.Tests.Controllers;

public class RefundsControllerTests
{
    private static async Task<(Booking booking, Refund refund)> SeedPaidBookingWithPendingRefundAsync(
        MatchdayApi.Data.AppDbContext db)
    {
        var booking = new Booking
        {
            BookingCode = "BK260101777777",
            Status = BookingStatus.Paid,
            UserId = 2,
            MatchId = 3,
            TotalAmount = 150000,
            BookingSeats = { new BookingSeat { SeatId = 1, MatchId = 3, Price = 150000 } }
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        var refund = new Refund
        {
            BookingId = booking.Id,
            Reason = "Test hoàn tiền",
            Status = RefundStatus.Pending,
            Amount = booking.TotalAmount
        };
        db.Refunds.Add(refund);
        await db.SaveChangesAsync();

        return (booking, refund);
    }

    [Fact]
    public async Task Approve_ShouldRefundBookingAndReleaseSeats_WhenPending()
    {
        await using var db = TestHelpers.CreateDbContext();
        var (booking, refund) = await SeedPaidBookingWithPendingRefundAsync(db);

        var hubMock = TestHelpers.CreateMockHub(out var clientProxyMock);
        var controller = new RefundsController(db, hubMock.Object);
        TestHelpers.SetCurrentUser(controller, userId: 1, role: "Admin");

        var result = await controller.Approve(refund.Id);

        Assert.IsType<NoContentResult>(result);

        var reloadedRefund = await db.Refunds.FindAsync(refund.Id);
        Assert.Equal(RefundStatus.Approved, reloadedRefund!.Status);
        Assert.NotNull(reloadedRefund.ProcessedAt);

        var reloadedBooking = await db.Bookings.Include(b => b.BookingSeats).FirstAsync(b => b.Id == booking.Id);
        Assert.Equal(BookingStatus.Refunded, reloadedBooking.Status);
        Assert.Empty(reloadedBooking.BookingSeats); // ghế phải được giải phóng

        clientProxyMock.Verify(
            p => p.SendCoreAsync("SeatReleased", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Approve_ShouldReturnBadRequest_WhenRefundAlreadyProcessed()
    {
        await using var db = TestHelpers.CreateDbContext();
        var (_, refund) = await SeedPaidBookingWithPendingRefundAsync(db);
        refund.Status = RefundStatus.Approved;
        refund.ProcessedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var hubMock = TestHelpers.CreateMockHub(out var clientProxyMock);
        var controller = new RefundsController(db, hubMock.Object);
        TestHelpers.SetCurrentUser(controller, userId: 1, role: "Admin");

        var result = await controller.Approve(refund.Id);

        Assert.IsType<BadRequestObjectResult>(result);
        clientProxyMock.Verify(
            p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Never); // không được broadcast thêm lần nào vì bị chặn sớm
    }

    [Fact]
    public async Task Reject_ShouldSetRejected_AndKeepBookingAndSeatsUntouched()
    {
        await using var db = TestHelpers.CreateDbContext();
        var (booking, refund) = await SeedPaidBookingWithPendingRefundAsync(db);

        var hubMock = TestHelpers.CreateMockHub(out var clientProxyMock);
        var controller = new RefundsController(db, hubMock.Object);
        TestHelpers.SetCurrentUser(controller, userId: 1, role: "Admin");

        var result = await controller.Reject(refund.Id);

        Assert.IsType<NoContentResult>(result);

        var reloadedRefund = await db.Refunds.FindAsync(refund.Id);
        Assert.Equal(RefundStatus.Rejected, reloadedRefund!.Status);

        var reloadedBooking = await db.Bookings.Include(b => b.BookingSeats).FirstAsync(b => b.Id == booking.Id);
        Assert.Equal(BookingStatus.Paid, reloadedBooking.Status); // không đổi
        Assert.Single(reloadedBooking.BookingSeats); // ghế vẫn còn nguyên

        clientProxyMock.Verify(
            p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Never); // reject không giải phóng ghế nên không broadcast
    }

    [Fact]
    public async Task Approve_ShouldReturnNotFound_WhenRefundDoesNotExist()
    {
        await using var db = TestHelpers.CreateDbContext();
        var hubMock = TestHelpers.CreateMockHub(out _);
        var controller = new RefundsController(db, hubMock.Object);
        TestHelpers.SetCurrentUser(controller, userId: 1, role: "Admin");

        var result = await controller.Approve(999999);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
