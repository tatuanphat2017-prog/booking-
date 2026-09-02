using MatchdayApi.Models;

namespace MatchdayApi.Services;

public interface ITicketService
{
    /// <summary>
    /// Sinh vé (Ticket) cho từng ghế (BookingSeat) trong 1 Booking đã thanh toán — mỗi ghế 1 vé riêng,
    /// mỗi vé 1 TicketCode + QrCodeData duy nhất. Idempotent: ghế nào đã có Ticket rồi thì bỏ qua,
    /// không tạo trùng (an toàn khi gọi lại nhiều lần, vd cả VnPayReturn lẫn VnPayIpn cùng trigger).
    /// Trả về toàn bộ danh sách Ticket của Booking đó (gồm cả vé mới tạo lẫn vé đã có sẵn).
    /// </summary>
    Task<List<Ticket>> GenerateTicketsForBookingAsync(int bookingId);
}
