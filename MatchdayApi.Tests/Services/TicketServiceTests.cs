using MatchdayApi.Enums;
using MatchdayApi.Models;
using MatchdayApi.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MatchdayApi.Tests.Services;

public class TicketServiceTests
{
    /// <summary>
    /// Tạo 1 Booking Paid với 2 ghế, dùng seatId=1 và seatId=2 (đã có sẵn từ dữ liệu mẫu SeedData —
    /// seat 1 = hàng A số 1, seat 2 = hàng A số 2, cùng thuộc Khối A sân Thống Nhất Mới).
    /// </summary>
    private static async Task<int> SeedBookingWithTwoSeatsAsync(MatchdayApi.Data.AppDbContext db)
    {
        var booking = new Booking
        {
            BookingCode = "BK260101999999",
            Status = BookingStatus.Paid,
            UserId = 1,
            MatchId = 3,
            TotalAmount = 300000,
            BookingSeats =
            {
                new BookingSeat { SeatId = 1, MatchId = 3, Price = 150000 },
                new BookingSeat { SeatId = 2, MatchId = 3, Price = 150000 }
            }
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        return booking.Id;
    }

    [Fact]
    public async Task GenerateTicketsForBookingAsync_ShouldCreateOneTicketPerSeat()
    {
        await using var db = TestHelpers.CreateDbContext();
        var bookingId = await SeedBookingWithTwoSeatsAsync(db);
        var service = new TicketService(db);

        var tickets = await service.GenerateTicketsForBookingAsync(bookingId);

        Assert.Equal(2, tickets.Count);
        Assert.Equal(2, await db.Tickets.CountAsync());
        Assert.Contains(tickets, t => t.TicketCode == "BK260101999999-A1");
        Assert.Contains(tickets, t => t.TicketCode == "BK260101999999-A2");
    }

    [Fact]
    public async Task GenerateTicketsForBookingAsync_ShouldBeIdempotent_WhenCalledTwice()
    {
        await using var db = TestHelpers.CreateDbContext();
        var bookingId = await SeedBookingWithTwoSeatsAsync(db);
        var service = new TicketService(db);

        await service.GenerateTicketsForBookingAsync(bookingId);
        var secondCallResult = await service.GenerateTicketsForBookingAsync(bookingId);

        // Gọi lần 2 không được tạo thêm vé trùng — vẫn đúng 2 vé như lần đầu.
        Assert.Equal(2, secondCallResult.Count);
        Assert.Equal(2, await db.Tickets.CountAsync());
    }

    [Fact]
    public async Task GenerateTicketsForBookingAsync_ShouldReturnEmptyList_WhenBookingNotFound()
    {
        await using var db = TestHelpers.CreateDbContext();
        var service = new TicketService(db);

        var tickets = await service.GenerateTicketsForBookingAsync(999999);

        Assert.Empty(tickets);
    }
}
