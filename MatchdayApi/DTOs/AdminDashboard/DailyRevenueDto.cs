namespace MatchdayApi.DTOs.AdminDashboard;

public class DailyRevenueDto
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public int PaymentCount { get; set; }
}
