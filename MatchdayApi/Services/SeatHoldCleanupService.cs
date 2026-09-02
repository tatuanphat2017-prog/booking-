using MatchdayApi.Data;
using MatchdayApi.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace MatchdayApi.Services;

/// <summary>
/// Background service quét định kỳ (mỗi 15 giây) để dọn các SeatHold đã hết hạn (quá 5 phút
/// không đặt vé) mà chính người giữ không chủ động nhả (ví dụ đứng yên không thao tác gì).
/// Mỗi ghế được dọn sẽ được báo real-time (SeatReleased) cho tất cả client đang xem trận đó,
/// để họ thấy ghế chuyển lại thành "còn trống" mà không cần tải lại trang.
/// </summary>
public class SeatHoldCleanupService : BackgroundService
{
    private static readonly TimeSpan ScanInterval = TimeSpan.FromSeconds(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<SeatSelectionHub> _hubContext;
    private readonly ILogger<SeatHoldCleanupService> _logger;

    public SeatHoldCleanupService(
        IServiceScopeFactory scopeFactory,
        IHubContext<SeatSelectionHub> hubContext,
        ILogger<SeatHoldCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredHoldsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                // Không để 1 lần quét lỗi làm crash toàn bộ service — log lại rồi thử tiếp ở vòng sau.
                _logger.LogError(ex, "Lỗi khi dọn SeatHold hết hạn.");
            }

            await Task.Delay(ScanInterval, stoppingToken);
        }
    }

    private async Task CleanupExpiredHoldsAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;
        var expiredHolds = await db.SeatHolds
            .Where(h => h.ExpireAt <= now)
            .ToListAsync(stoppingToken);

        if (expiredHolds.Count == 0) return;

        db.SeatHolds.RemoveRange(expiredHolds);
        await db.SaveChangesAsync(stoppingToken);

        foreach (var hold in expiredHolds)
        {
            await _hubContext.Clients
                .Group(SeatSelectionHub.GroupName(hold.MatchId))
                .SendAsync("SeatReleased", hold.SeatId, cancellationToken: stoppingToken);
        }
    }
}
