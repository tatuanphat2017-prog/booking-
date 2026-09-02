using System.Security.Claims;
using MatchdayApi.Data;
using MatchdayApi.Hubs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace MatchdayApi.Tests;

/// <summary>
/// Hàm dùng chung cho các bài test: tạo AppDbContext chạy trên EF Core InMemory (mỗi lần gọi là 1
/// database rỗng riêng biệt, không ảnh hưởng lẫn nhau giữa các test), giả lập user đăng nhập cho
/// Controller, và giả lập IHubContext&lt;SeatSelectionHub&gt; bằng Moq (không cần SignalR thật chạy
/// khi test).
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Tạo 1 AppDbContext mới trên InMemory database (tên ngẫu nhiên = độc lập hoàn toàn giữa các test).
    /// Lưu ý: dữ liệu mẫu khai báo bằng HasData() trong SeedData.cs (Roles, Admin, Teams, Players,
    /// Seats, Matches, FoodItems...) VẪN được EF Core tự nạp sẵn vào đây, vì HasData là 1 phần của
    /// model, áp dụng cho mọi provider (kể cả InMemory) — không cần tự seed lại trong test.
    /// </summary>
    public static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options);

        // QUAN TRỌNG: với InMemory provider, dữ liệu khai báo qua HasData() (SeedData.cs) chỉ thực sự
        // được nạp vào database khi gọi EnsureCreated() — khác với SQL Server (nạp qua Migration).
        // Thiếu dòng này thì Teams/Matches/Seats/Players... coi như KHÔNG TỒN TẠI trong test,
        // dù code test có set MatchId=3, SeatId=1... đúng theo SeedData.cs.
        context.Database.EnsureCreated();

        return context;
    }

    /// <summary>Gán ClaimsPrincipal (đóng vai user đã đăng nhập) cho 1 Controller, để test được các API có [Authorize].</summary>
    public static void SetCurrentUser(ControllerBase controller, int userId, string role = "User")
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    /// <summary>
    /// Giả lập IHubContext&lt;SeatSelectionHub&gt; bằng Moq — cho phép controller gọi
    /// _hub.Clients.Group(...).SendAsync(...) bình thường mà không cần kết nối SignalR thật.
    /// Trả kèm Mock của IClientProxy để test có thể Verify() số lần / tham số đã gọi SendAsync.
    /// </summary>
    public static Mock<IHubContext<SeatSelectionHub>> CreateMockHub(out Mock<IClientProxy> clientProxyMock)
    {
        clientProxyMock = new Mock<IClientProxy>();
        clientProxyMock
            .Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var clientsMock = new Mock<IHubClients>();
        clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(clientProxyMock.Object);

        var hubContextMock = new Mock<IHubContext<SeatSelectionHub>>();
        hubContextMock.Setup(h => h.Clients).Returns(clientsMock.Object);

        return hubContextMock;
    }
}
