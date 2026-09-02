namespace MatchdayApi.Models;

/// <summary>Ghế cụ thể thuộc 1 khối ghế</summary>
public class Seat
{
    public int Id { get; set; }
    public string RowLabel { get; set; } = string.Empty; // A, B, C...
    public int SeatNumber { get; set; }

    public int SeatBlockId { get; set; }
    public SeatBlock SeatBlock { get; set; } = null!;

    public ICollection<SeatHold> SeatHolds { get; set; } = new List<SeatHold>();
    public ICollection<BookingSeat> BookingSeats { get; set; } = new List<BookingSeat>();
}
