namespace MatchdayApi.DTOs.AdminDashboard;

/// <summary>Số liệu tổng quan cho Admin — task #22.</summary>
public class AdminOverviewDto
{
    /// <summary>Tổng doanh thu hiện tại (chỉ tính đơn đang Paid — đơn Cancelled/Refunded không tính).</summary>
    public decimal TotalRevenue { get; set; }
    public decimal SeatRevenue { get; set; }
    public decimal FoodRevenue { get; set; }

    public int TotalBookings { get; set; }
    public int PendingBookings { get; set; }
    public int PaidBookings { get; set; }
    public int CancelledBookings { get; set; }
    public int RefundedBookings { get; set; }

    /// <summary>Số vé đang có hiệu lực (đơn Paid) — vé của đơn đã hủy/hoàn tiền không tính vào đây.</summary>
    public int TotalTicketsSold { get; set; }

    public int PendingRefundRequests { get; set; }
    public int ApprovedRefunds { get; set; }
    public int RejectedRefunds { get; set; }
    public decimal TotalRefundedAmount { get; set; }
}
