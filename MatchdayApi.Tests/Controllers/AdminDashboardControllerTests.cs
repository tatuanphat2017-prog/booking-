using MatchdayApi.Controllers;
using MatchdayApi.DTOs.AdminDashboard;
using MatchdayApi.Enums;
using MatchdayApi.Models;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace MatchdayApi.Tests.Controllers;

/// <summary>Test số liệu tổng quan (task #22) — tự dựng dữ liệu để biết chính xác kết quả mong đợi.</summary>
public class AdminDashboardControllerTests
{
    [Fact]
    public async Task GetOverview_ShouldCalculateRevenueAndCounts_Correctly()
    {
        await using var db = TestHelpers.CreateDbContext();

        // Đơn Paid: 80.000đ tiền ghế + 20.000đ tiền đồ ăn = 100.000đ.
        var paidBooking = new Booking
        {
            BookingCode = "BK-PAID-1",
            Status = BookingStatus.Paid,
            UserId = 1,
            MatchId = 3,
            TotalAmount = 100000,
            BookingSeats = { new BookingSeat { SeatId = 1, MatchId = 3, Price = 80000 } },
            FoodItems = { new BookingFoodItem { FoodItemId = 1, Quantity = 2, UnitPrice = 10000 } }
        };
        db.Bookings.Add(paidBooking);
        await db.SaveChangesAsync();

        db.Tickets.Add(new Ticket
        {
            BookingSeatId = paidBooking.BookingSeats.First().Id,
            TicketCode = "BK-PAID-1-A1",
            QrCodeData = "BK-PAID-1-A1"
        });

        db.Bookings.Add(new Booking { BookingCode = "BK-PENDING-1", Status = BookingStatus.Pending, UserId = 1, MatchId = 3, TotalAmount = 50000 });
        db.Bookings.Add(new Booking { BookingCode = "BK-CANCELLED-1", Status = BookingStatus.Cancelled, UserId = 1, MatchId = 3, TotalAmount = 50000 });
        db.Bookings.Add(new Booking { BookingCode = "BK-REFUNDED-1", Status = BookingStatus.Refunded, UserId = 1, MatchId = 3, TotalAmount = 50000 });

        db.Refunds.Add(new Refund { BookingId = paidBooking.Id, Reason = "x", Status = RefundStatus.Approved, Amount = 50000, ProcessedAt = DateTime.UtcNow });
        db.Refunds.Add(new Refund { BookingId = paidBooking.Id, Reason = "y", Status = RefundStatus.Rejected, Amount = 30000, ProcessedAt = DateTime.UtcNow });

        await db.SaveChangesAsync();

        var controller = new AdminDashboardController(db);

        var result = await controller.GetOverview();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<AdminOverviewDto>(ok.Value);

        Assert.Equal(100000, dto.TotalRevenue);
        Assert.Equal(80000, dto.SeatRevenue);
        Assert.Equal(20000, dto.FoodRevenue);
        Assert.Equal(4, dto.TotalBookings);
        Assert.Equal(1, dto.PendingBookings);
        Assert.Equal(1, dto.PaidBookings);
        Assert.Equal(1, dto.CancelledBookings);
        Assert.Equal(1, dto.RefundedBookings);
        Assert.Equal(1, dto.TotalTicketsSold);
        Assert.Equal(0, dto.PendingRefundRequests);
        Assert.Equal(1, dto.ApprovedRefunds);
        Assert.Equal(1, dto.RejectedRefunds);
        Assert.Equal(50000, dto.TotalRefundedAmount);
    }

    [Fact]
    public async Task GetOverview_ShouldReturnAllZero_WhenNoBookingsExist()
    {
        await using var db = TestHelpers.CreateDbContext();
        var controller = new AdminDashboardController(db);

        var result = await controller.GetOverview();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<AdminOverviewDto>(ok.Value);

        Assert.Equal(0, dto.TotalRevenue);
        Assert.Equal(0, dto.TotalBookings);
        Assert.Equal(0, dto.TotalTicketsSold);
    }
}
