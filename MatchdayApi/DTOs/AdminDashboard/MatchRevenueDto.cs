namespace MatchdayApi.DTOs.AdminDashboard;

public class MatchRevenueDto
{
    public int MatchId { get; set; }
    public DateTime MatchDateTime { get; set; }
    public string HomeTeamName { get; set; } = string.Empty;
    public string AwayTeamName { get; set; } = string.Empty;
    public int TicketsSold { get; set; }
    public decimal SeatRevenue { get; set; }
    public decimal FoodRevenue { get; set; }
    public decimal TotalRevenue { get; set; }
}
