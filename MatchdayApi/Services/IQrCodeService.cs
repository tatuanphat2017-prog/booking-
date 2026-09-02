namespace MatchdayApi.Services;

/// <summary>Sinh ảnh QR (PNG) từ 1 chuỗi nội dung bất kỳ — dùng cho vé điện tử (task #14).</summary>
public interface IQrCodeService
{
    byte[] GeneratePngBytes(string content);
}
