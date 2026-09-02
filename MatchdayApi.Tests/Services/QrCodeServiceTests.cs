using MatchdayApi.Services;
using Xunit;

namespace MatchdayApi.Tests.Services;

public class QrCodeServiceTests
{
    // 8 byte đầu tiên của mọi file PNG hợp lệ (theo chuẩn định dạng PNG).
    private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    [Fact]
    public void GeneratePngBytes_ShouldReturnValidPng_ForNonEmptyContent()
    {
        var service = new QrCodeService();

        var bytes = service.GeneratePngBytes("BK260101000000001-A1");

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 8, "Ảnh QR sinh ra quá nhỏ, có thể bị lỗi.");
        Assert.Equal(PngSignature, bytes.Take(8).ToArray());
    }

    [Fact]
    public void GeneratePngBytes_ShouldProduceDifferentBytes_ForDifferentContent()
    {
        var service = new QrCodeService();

        var bytes1 = service.GeneratePngBytes("TICKET-A1");
        var bytes2 = service.GeneratePngBytes("TICKET-B2");

        Assert.NotEqual(bytes1, bytes2);
    }
}
