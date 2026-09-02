using MatchdayApi.Controllers;
using MatchdayApi.DTOs.Matches;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace MatchdayApi.Tests.Controllers;

/// <summary>
/// Test phần đội hình ra sân (task #20) của MatchesController. Dùng thẳng dữ liệu mẫu có sẵn
/// (SeedData.cs): trận Id=3 là Gia Định FC (TeamId=1, có cầu thủ Id 1-14) gặp Bến Nghé FC
/// (TeamId=3, có cầu thủ Id 29-34) — chưa Finished, chưa có đội hình.
/// </summary>
public class MatchesControllerLineupTests
{
    private const int MatchId = 3;
    private const int HomeTeamId = 1;
    private const int AwayTeamId = 3;

    private static SetLineupDto BuildDto(string formation, params (int playerId, bool starting)[] players) => new()
    {
        Formation = formation,
        Players = players.Select(p => new SetLineupPlayerDto
        {
            PlayerId = p.playerId,
            PositionX = 50,
            PositionY = 50,
            IsStarting = p.starting
        }).ToList()
    };

    [Fact]
    public async Task SetLineup_ShouldSucceed_WhenPlayersBelongToTeamInMatch()
    {
        await using var db = TestHelpers.CreateDbContext();
        var controller = new MatchesController(db);

        var result = await controller.SetLineup(MatchId, HomeTeamId, BuildDto("4-3-3", (1, true), (4, true)));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<TeamLineupDto>(ok.Value);
        Assert.Equal("4-3-3", dto.Formation);
        Assert.Equal(2, dto.Players.Count);
    }

    [Fact]
    public async Task SetLineup_ShouldReturnBadRequest_WhenPlayerNotInTeam()
    {
        await using var db = TestHelpers.CreateDbContext();
        var controller = new MatchesController(db);

        // Cầu thủ Id=29 thuộc Bến Nghé FC (TeamId=3), không phải Gia Định FC (TeamId=1).
        var result = await controller.SetLineup(MatchId, HomeTeamId, BuildDto("4-3-3", (29, true)));

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task SetLineup_ShouldReturnBadRequest_WhenTeamNotPlayingInMatch()
    {
        await using var db = TestHelpers.CreateDbContext();
        var controller = new MatchesController(db);

        // TeamId=2 (Sông Hàn FC) không thi đấu trong trận Id=3 (Gia Định vs Bến Nghé).
        var result = await controller.SetLineup(MatchId, teamId: 2, BuildDto("4-3-3", (15, true)));

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task SetLineup_ShouldReturnBadRequest_WhenMoreThan11StartersGiven()
    {
        await using var db = TestHelpers.CreateDbContext();
        var controller = new MatchesController(db);

        // Gia Định FC (TeamId=1) có cầu thủ Id 1..14 trong dữ liệu mẫu — lấy 12 người, tất cả đá chính.
        var players = Enumerable.Range(1, 12).Select(id => (id, true)).ToArray();

        var result = await controller.SetLineup(MatchId, HomeTeamId, BuildDto("4-3-3", players));

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task SetLineup_ShouldReturnBadRequest_WhenPlayerListEmpty()
    {
        await using var db = TestHelpers.CreateDbContext();
        var controller = new MatchesController(db);

        var result = await controller.SetLineup(MatchId, HomeTeamId, BuildDto("4-3-3"));

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task SetLineup_ShouldReplaceOldLineup_WhenCalledTwice()
    {
        await using var db = TestHelpers.CreateDbContext();
        var controller = new MatchesController(db);

        await controller.SetLineup(MatchId, HomeTeamId, BuildDto("4-3-3", (1, true), (4, true)));
        var secondResult = await controller.SetLineup(MatchId, HomeTeamId, BuildDto("4-4-2", (5, true)));

        var ok = Assert.IsType<OkObjectResult>(secondResult.Result);
        var dto = Assert.IsType<TeamLineupDto>(ok.Value);

        Assert.Equal("4-4-2", dto.Formation);
        Assert.Single(dto.Players);
        Assert.Equal(5, dto.Players[0].PlayerId);
    }

    [Fact]
    public async Task SetLineup_ShouldReturnNotFound_WhenMatchDoesNotExist()
    {
        await using var db = TestHelpers.CreateDbContext();
        var controller = new MatchesController(db);

        var result = await controller.SetLineup(999999, HomeTeamId, BuildDto("4-3-3", (1, true)));

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }
}
