using MatchdayApi.Enums;
using MatchdayApi.Models;
using Microsoft.EntityFrameworkCore;

namespace MatchdayApi.Data;

/// <summary>
/// Dữ liệu mẫu (giống hệt file sample_data.xlsx đã chuẩn bị ở task #7), nạp qua HasData()
/// nên sẽ tự động có trong migration đầu tiên — không cần chạy script insert tay.
/// </summary>
public static class SeedData
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        // ---------- Roles ----------
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "Admin" },
            new Role { Id = 2, Name = "User" }
        );

        // ---------- Tài khoản Admin mặc định (task #9) ----------
        // Email: admin@matchday.local / Mật khẩu: Admin@123
        // Hash BCrypt được tính sẵn (không hash lúc runtime vì HasData() chỉ chạy lúc migration).
        // Đổi mật khẩu này sau khi có màn hình đổi mật khẩu, không dùng khi deploy thật.
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                FullName = "Quản trị viên",
                Email = "admin@matchday.local",
                PasswordHash = "$2b$11$QbGXPSCjgBrKb46nyvEQzOUNjKWaAD8oje32j6O6C6Nnq7P2OLxSW",
                PhoneNumber = null,
                IsActive = true,
                CreatedAt = new DateTime(2026, 8, 28, 0, 0, 0, DateTimeKind.Utc),
                RoleId = 1
            }
        );

        // ---------- Stadiums ----------
        modelBuilder.Entity<Stadium>().HasData(
            new Stadium { Id = 1, Name = "Sân Thống Nhất Mới", Address = "123 Đường Đào Duy Từ", City = "TP. Hồ Chí Minh", Capacity = 25000 },
            new Stadium { Id = 2, Name = "Sân Hòa Bình", Address = "45 Đường Trần Phú", City = "Đà Nẵng", Capacity = 20000 }
        );

        // ---------- Teams ----------
        modelBuilder.Entity<Team>().HasData(
            new Team { Id = 1, Name = "Gia Định FC", ShortName = "GDF", HomeStadiumId = 1 },
            new Team { Id = 2, Name = "Sông Hàn FC", ShortName = "SHN", HomeStadiumId = 2 },
            new Team { Id = 3, Name = "Bến Nghé FC", ShortName = "BNG", HomeStadiumId = 1 },
            new Team { Id = 4, Name = "Sơn Trà FC", ShortName = "STR", HomeStadiumId = 2 }
        );

        // ---------- SeatBlocks (6 khối: 3 khối x 2 sân) ----------
        modelBuilder.Entity<SeatBlock>().HasData(
            new SeatBlock { Id = 1, StadiumId = 1, Name = "Khối A (Thường)", BasePrice = 150000 },
            new SeatBlock { Id = 2, StadiumId = 1, Name = "Khối B (Thường)", BasePrice = 120000 },
            new SeatBlock { Id = 3, StadiumId = 1, Name = "Khối VIP", BasePrice = 350000 },
            new SeatBlock { Id = 4, StadiumId = 2, Name = "Khối A (Thường)", BasePrice = 150000 },
            new SeatBlock { Id = 5, StadiumId = 2, Name = "Khối B (Thường)", BasePrice = 120000 },
            new SeatBlock { Id = 6, StadiumId = 2, Name = "Khối VIP", BasePrice = 350000 }
        );

        // ---------- Seats: 6 khối x 6 hàng (A-F) x 10 ghế = 360 ghế ----------
        var seats = new List<Seat>();
        var seatId = 1;
        for (var seatBlockId = 1; seatBlockId <= 6; seatBlockId++)
        {
            foreach (var rowLetter in "ABCDEF")
            {
                for (var seatNumber = 1; seatNumber <= 10; seatNumber++)
                {
                    seats.Add(new Seat
                    {
                        Id = seatId++,
                        SeatBlockId = seatBlockId,
                        RowLabel = rowLetter.ToString(),
                        SeatNumber = seatNumber
                    });
                }
            }
        }
        modelBuilder.Entity<Seat>().HasData(seats);

        // ---------- Players ----------
        // SGS -> Gia Định FC (TeamId=1), TXD -> Sông Hàn FC (TeamId=2): đủ đội hình chính + dự bị.
        // BNG (Id=3) & STR (Id=4): roster rút gọn, chưa cần đội hình/chỉ số chi tiết.
        modelBuilder.Entity<Player>().HasData(
            // Gia Định FC (TeamId = 1)
            new Player { Id = 1, TeamId = 1, FullName = "Nguyễn Văn An", JerseyNumber = 1, Position = "GK", DateOfBirth = new DateTime(1998, 3, 12) },
            new Player { Id = 2, TeamId = 1, FullName = "Trần Minh Bảo", JerseyNumber = 4, Position = "CB", DateOfBirth = new DateTime(1997, 6, 20) },
            new Player { Id = 3, TeamId = 1, FullName = "Lê Hoàng Cường", JerseyNumber = 5, Position = "CB", DateOfBirth = new DateTime(1996, 11, 2) },
            new Player { Id = 4, TeamId = 1, FullName = "Phạm Quốc Duy", JerseyNumber = 2, Position = "RB", DateOfBirth = new DateTime(1999, 1, 15) },
            new Player { Id = 5, TeamId = 1, FullName = "Hoàng Văn Em", JerseyNumber = 3, Position = "LB", DateOfBirth = new DateTime(1998, 9, 8) },
            new Player { Id = 6, TeamId = 1, FullName = "Vũ Đình Phúc", JerseyNumber = 6, Position = "CM", DateOfBirth = new DateTime(1997, 4, 25) },
            new Player { Id = 7, TeamId = 1, FullName = "Đặng Thành Giang", JerseyNumber = 8, Position = "CM", DateOfBirth = new DateTime(1998, 12, 1) },
            new Player { Id = 8, TeamId = 1, FullName = "Bùi Văn Hùng", JerseyNumber = 10, Position = "CM", DateOfBirth = new DateTime(1996, 7, 19) },
            new Player { Id = 9, TeamId = 1, FullName = "Ngô Minh Khoa", JerseyNumber = 7, Position = "LW", DateOfBirth = new DateTime(1999, 3, 30) },
            new Player { Id = 10, TeamId = 1, FullName = "Đỗ Xuân Long", JerseyNumber = 9, Position = "ST", DateOfBirth = new DateTime(1997, 8, 14) },
            new Player { Id = 11, TeamId = 1, FullName = "Trịnh Công Minh", JerseyNumber = 11, Position = "RW", DateOfBirth = new DateTime(1998, 5, 22) },
            new Player { Id = 12, TeamId = 1, FullName = "Lý Thành Nam", JerseyNumber = 12, Position = "GK", DateOfBirth = new DateTime(2000, 1, 10) },
            new Player { Id = 13, TeamId = 1, FullName = "Phan Anh Tuấn", JerseyNumber = 14, Position = "CB", DateOfBirth = new DateTime(1999, 2, 18) },
            new Player { Id = 14, TeamId = 1, FullName = "Đinh Văn Sơn", JerseyNumber = 16, Position = "ST", DateOfBirth = new DateTime(2000, 6, 27) },

            // Sông Hàn FC (TeamId = 2)
            new Player { Id = 15, TeamId = 2, FullName = "Nguyễn Hữu Phát", JerseyNumber = 1, Position = "GK", DateOfBirth = new DateTime(1997, 5, 11) },
            new Player { Id = 16, TeamId = 2, FullName = "Trần Đức Anh", JerseyNumber = 2, Position = "RB", DateOfBirth = new DateTime(1998, 8, 9) },
            new Player { Id = 17, TeamId = 2, FullName = "Lê Văn Bình", JerseyNumber = 5, Position = "CB", DateOfBirth = new DateTime(1996, 10, 3) },
            new Player { Id = 18, TeamId = 2, FullName = "Phạm Ngọc Cường", JerseyNumber = 4, Position = "CB", DateOfBirth = new DateTime(1997, 12, 21) },
            new Player { Id = 19, TeamId = 2, FullName = "Hoàng Trọng Đạt", JerseyNumber = 3, Position = "LB", DateOfBirth = new DateTime(1998, 2, 14) },
            new Player { Id = 20, TeamId = 2, FullName = "Vũ Thế Hiển", JerseyNumber = 6, Position = "CM", DateOfBirth = new DateTime(1997, 7, 7) },
            new Player { Id = 21, TeamId = 2, FullName = "Đặng Công Khánh", JerseyNumber = 8, Position = "CM", DateOfBirth = new DateTime(1998, 11, 19) },
            new Player { Id = 22, TeamId = 2, FullName = "Bùi Quang Linh", JerseyNumber = 10, Position = "CM", DateOfBirth = new DateTime(1996, 4, 28) },
            new Player { Id = 23, TeamId = 2, FullName = "Ngô Bảo Long", JerseyNumber = 7, Position = "LW", DateOfBirth = new DateTime(1999, 9, 16) },
            new Player { Id = 24, TeamId = 2, FullName = "Đỗ Tiến Mạnh", JerseyNumber = 9, Position = "ST", DateOfBirth = new DateTime(1997, 1, 25) },
            new Player { Id = 25, TeamId = 2, FullName = "Trịnh Xuân Nghĩa", JerseyNumber = 11, Position = "RW", DateOfBirth = new DateTime(1998, 6, 13) },
            new Player { Id = 26, TeamId = 2, FullName = "Lý Công Phúc", JerseyNumber = 12, Position = "GK", DateOfBirth = new DateTime(1999, 10, 5) },
            new Player { Id = 27, TeamId = 2, FullName = "Phan Đình Quang", JerseyNumber = 14, Position = "CB", DateOfBirth = new DateTime(2000, 3, 8) },
            new Player { Id = 28, TeamId = 2, FullName = "Đinh Bảo Sơn", JerseyNumber = 16, Position = "ST", DateOfBirth = new DateTime(1999, 12, 2) },

            // Bến Nghé FC (TeamId = 3) - roster rút gọn
            new Player { Id = 29, TeamId = 3, FullName = "Nguyễn Anh Kiệt", JerseyNumber = 1, Position = "GK" },
            new Player { Id = 30, TeamId = 3, FullName = "Trần Bảo Long", JerseyNumber = 9, Position = "ST" },
            new Player { Id = 31, TeamId = 3, FullName = "Lê Minh Nhật", JerseyNumber = 7, Position = "LW" },
            new Player { Id = 32, TeamId = 3, FullName = "Phạm Gia Huy", JerseyNumber = 5, Position = "CB" },
            new Player { Id = 33, TeamId = 3, FullName = "Hoàng Nhật Nam", JerseyNumber = 10, Position = "CM" },
            new Player { Id = 34, TeamId = 3, FullName = "Vũ Đăng Khoa", JerseyNumber = 11, Position = "RW" },

            // Sơn Trà FC (TeamId = 4) - roster rút gọn
            new Player { Id = 35, TeamId = 4, FullName = "Đỗ Hoàng Việt", JerseyNumber = 1, Position = "GK" },
            new Player { Id = 36, TeamId = 4, FullName = "Ngô Xuân Bách", JerseyNumber = 9, Position = "ST" },
            new Player { Id = 37, TeamId = 4, FullName = "Bùi Tấn Phát", JerseyNumber = 7, Position = "LW" },
            new Player { Id = 38, TeamId = 4, FullName = "Đặng Minh Tú", JerseyNumber = 5, Position = "CB" },
            new Player { Id = 39, TeamId = 4, FullName = "Lý Gia Bảo", JerseyNumber = 10, Position = "CM" },
            new Player { Id = 40, TeamId = 4, FullName = "Trịnh Anh Duy", JerseyNumber = 11, Position = "RW" }
        );

        // ---------- Matches ----------
        modelBuilder.Entity<Match>().HasData(
            new Match { Id = 1, HomeTeamId = 1, AwayTeamId = 2, StadiumId = 1, MatchDateTime = new DateTime(2026, 8, 5, 19, 0, 0), Status = MatchStatus.Finished },
            new Match { Id = 2, HomeTeamId = 3, AwayTeamId = 4, StadiumId = 1, MatchDateTime = new DateTime(2026, 8, 11, 19, 0, 0), Status = MatchStatus.Live },
            new Match { Id = 3, HomeTeamId = 1, AwayTeamId = 3, StadiumId = 1, MatchDateTime = new DateTime(2026, 8, 15, 20, 0, 0), Status = MatchStatus.Upcoming },
            new Match { Id = 4, HomeTeamId = 2, AwayTeamId = 4, StadiumId = 2, MatchDateTime = new DateTime(2026, 8, 17, 20, 0, 0), Status = MatchStatus.Upcoming }
        );

        // ---------- MatchResults ----------
        modelBuilder.Entity<MatchResult>().HasData(
            new MatchResult
            {
                Id = 1, MatchId = 1, HomeScore = 2, AwayScore = 1,
                PossessionHome = 55, PossessionAway = 45, ShotsHome = 12, ShotsAway = 8,
                YellowCardsHome = 2, YellowCardsAway = 1, RedCardsHome = 0, RedCardsAway = 0,
                CornersHome = 6, CornersAway = 4
            },
            new MatchResult
            {
                Id = 2, MatchId = 2, HomeScore = 1, AwayScore = 0,
                PossessionHome = 52, PossessionAway = 48, ShotsHome = 7, ShotsAway = 5,
                YellowCardsHome = 1, YellowCardsAway = 0, RedCardsHome = 0, RedCardsAway = 0,
                CornersHome = 3, CornersAway = 2
            }
        );

        // ---------- MatchTeamLineups (chỉ trận #1 - đã kết thúc) ----------
        modelBuilder.Entity<MatchTeamLineup>().HasData(
            new MatchTeamLineup { Id = 1, MatchId = 1, TeamId = 1, Formation = "4-3-3" },
            new MatchTeamLineup { Id = 2, MatchId = 1, TeamId = 2, Formation = "4-3-3" }
        );

        // ---------- LineupPlayers: tọa độ khớp với sơ đồ sân trong wireframe.html ----------
        modelBuilder.Entity<LineupPlayer>().HasData(
            // Gia Định FC (MatchTeamLineupId = 1)
            new LineupPlayer { Id = 1, MatchTeamLineupId = 1, PlayerId = 1, PositionX = 10, PositionY = 50, IsStarting = true },
            new LineupPlayer { Id = 2, MatchTeamLineupId = 1, PlayerId = 5, PositionX = 25, PositionY = 20, IsStarting = true },
            new LineupPlayer { Id = 3, MatchTeamLineupId = 1, PlayerId = 2, PositionX = 25, PositionY = 42, IsStarting = true },
            new LineupPlayer { Id = 4, MatchTeamLineupId = 1, PlayerId = 3, PositionX = 25, PositionY = 58, IsStarting = true },
            new LineupPlayer { Id = 5, MatchTeamLineupId = 1, PlayerId = 4, PositionX = 25, PositionY = 82, IsStarting = true },
            new LineupPlayer { Id = 6, MatchTeamLineupId = 1, PlayerId = 6, PositionX = 45, PositionY = 30, IsStarting = true },
            new LineupPlayer { Id = 7, MatchTeamLineupId = 1, PlayerId = 7, PositionX = 45, PositionY = 50, IsStarting = true },
            new LineupPlayer { Id = 8, MatchTeamLineupId = 1, PlayerId = 8, PositionX = 45, PositionY = 70, IsStarting = true },
            new LineupPlayer { Id = 9, MatchTeamLineupId = 1, PlayerId = 9, PositionX = 62, PositionY = 25, IsStarting = true },
            new LineupPlayer { Id = 10, MatchTeamLineupId = 1, PlayerId = 10, PositionX = 62, PositionY = 50, IsStarting = true },
            new LineupPlayer { Id = 11, MatchTeamLineupId = 1, PlayerId = 11, PositionX = 62, PositionY = 75, IsStarting = true },

            // Sông Hàn FC (MatchTeamLineupId = 2)
            new LineupPlayer { Id = 12, MatchTeamLineupId = 2, PlayerId = 15, PositionX = 10, PositionY = 50, IsStarting = true },
            new LineupPlayer { Id = 13, MatchTeamLineupId = 2, PlayerId = 19, PositionX = 25, PositionY = 20, IsStarting = true },
            new LineupPlayer { Id = 14, MatchTeamLineupId = 2, PlayerId = 17, PositionX = 25, PositionY = 42, IsStarting = true },
            new LineupPlayer { Id = 15, MatchTeamLineupId = 2, PlayerId = 18, PositionX = 25, PositionY = 58, IsStarting = true },
            new LineupPlayer { Id = 16, MatchTeamLineupId = 2, PlayerId = 16, PositionX = 25, PositionY = 82, IsStarting = true },
            new LineupPlayer { Id = 17, MatchTeamLineupId = 2, PlayerId = 20, PositionX = 45, PositionY = 30, IsStarting = true },
            new LineupPlayer { Id = 18, MatchTeamLineupId = 2, PlayerId = 21, PositionX = 45, PositionY = 50, IsStarting = true },
            new LineupPlayer { Id = 19, MatchTeamLineupId = 2, PlayerId = 22, PositionX = 45, PositionY = 70, IsStarting = true },
            new LineupPlayer { Id = 20, MatchTeamLineupId = 2, PlayerId = 23, PositionX = 62, PositionY = 25, IsStarting = true },
            new LineupPlayer { Id = 21, MatchTeamLineupId = 2, PlayerId = 24, PositionX = 62, PositionY = 50, IsStarting = true },
            new LineupPlayer { Id = 22, MatchTeamLineupId = 2, PlayerId = 25, PositionX = 62, PositionY = 75, IsStarting = true }
        );

        // ---------- PlayerMatchStats (trận #1: Gia Định 2-1 Sông Hàn) ----------
        modelBuilder.Entity<PlayerMatchStat>().HasData(
            new PlayerMatchStat { Id = 1, MatchId = 1, PlayerId = 1, MinutesPlayed = 90, Rating = 6.8m },
            new PlayerMatchStat { Id = 2, MatchId = 1, PlayerId = 5, MinutesPlayed = 90, Rating = 6.5m },
            new PlayerMatchStat { Id = 3, MatchId = 1, PlayerId = 2, MinutesPlayed = 90, Rating = 7.0m },
            new PlayerMatchStat { Id = 4, MatchId = 1, PlayerId = 3, MinutesPlayed = 90, Rating = 6.9m, YellowCards = 1 },
            new PlayerMatchStat { Id = 5, MatchId = 1, PlayerId = 4, MinutesPlayed = 90, Rating = 6.7m },
            new PlayerMatchStat { Id = 6, MatchId = 1, PlayerId = 6, MinutesPlayed = 90, Rating = 7.1m, YellowCards = 1 },
            new PlayerMatchStat { Id = 7, MatchId = 1, PlayerId = 7, MinutesPlayed = 90, Rating = 7.3m },
            new PlayerMatchStat { Id = 8, MatchId = 1, PlayerId = 8, MinutesPlayed = 90, Rating = 6.8m, Assists = 1 },
            new PlayerMatchStat { Id = 9, MatchId = 1, PlayerId = 9, MinutesPlayed = 90, Rating = 8.0m, Goals = 1 },
            new PlayerMatchStat { Id = 10, MatchId = 1, PlayerId = 10, MinutesPlayed = 90, Rating = 8.4m, Goals = 1 },
            new PlayerMatchStat { Id = 11, MatchId = 1, PlayerId = 11, MinutesPlayed = 90, Rating = 7.0m, Assists = 1 },
            new PlayerMatchStat { Id = 12, MatchId = 1, PlayerId = 15, MinutesPlayed = 90, Rating = 6.0m },
            new PlayerMatchStat { Id = 13, MatchId = 1, PlayerId = 19, MinutesPlayed = 90, Rating = 6.4m },
            new PlayerMatchStat { Id = 14, MatchId = 1, PlayerId = 17, MinutesPlayed = 90, Rating = 6.2m },
            new PlayerMatchStat { Id = 15, MatchId = 1, PlayerId = 18, MinutesPlayed = 90, Rating = 6.3m },
            new PlayerMatchStat { Id = 16, MatchId = 1, PlayerId = 16, MinutesPlayed = 90, Rating = 6.5m },
            new PlayerMatchStat { Id = 17, MatchId = 1, PlayerId = 20, MinutesPlayed = 90, Rating = 6.8m },
            new PlayerMatchStat { Id = 18, MatchId = 1, PlayerId = 21, MinutesPlayed = 90, Rating = 6.6m, YellowCards = 1 },
            new PlayerMatchStat { Id = 19, MatchId = 1, PlayerId = 22, MinutesPlayed = 90, Rating = 6.5m },
            new PlayerMatchStat { Id = 20, MatchId = 1, PlayerId = 23, MinutesPlayed = 90, Rating = 6.7m },
            new PlayerMatchStat { Id = 21, MatchId = 1, PlayerId = 24, MinutesPlayed = 90, Rating = 7.6m, Goals = 1 },
            new PlayerMatchStat { Id = 22, MatchId = 1, PlayerId = 25, MinutesPlayed = 90, Rating = 6.4m }
        );

        // ---------- FoodItems ----------
        modelBuilder.Entity<FoodItem>().HasData(
            new FoodItem { Id = 1, Name = "Bắp rang bơ", Price = 30000, IsAvailable = true },
            new FoodItem { Id = 2, Name = "Nước ngọt (lon)", Price = 20000, IsAvailable = true },
            new FoodItem { Id = 3, Name = "Combo Bắp + Nước", Price = 45000, IsAvailable = true },
            new FoodItem { Id = 4, Name = "Xúc xích nướng", Price = 25000, IsAvailable = true },
            new FoodItem { Id = 5, Name = "Bia lon", Price = 35000, IsAvailable = true },
            new FoodItem { Id = 6, Name = "Nước suối", Price = 15000, IsAvailable = true }
        );
    }
}
