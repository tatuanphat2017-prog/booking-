using QRCoder;

namespace MatchdayApi.Services;

public class QrCodeService : IQrCodeService
{
    public byte[] GeneratePngBytes(string content)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data);
        return png.GetGraphic(10); // 10px/module — đủ nét để camera điện thoại quét
    }
}
