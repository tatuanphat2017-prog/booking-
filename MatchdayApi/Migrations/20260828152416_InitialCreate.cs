using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MatchdayApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FoodItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(12,0)", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Stadiums",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stadiums", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SeatBlocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BasePrice = table.Column<decimal>(type: "decimal(12,0)", nullable: false),
                    StadiumId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeatBlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeatBlocks_Stadiums_StadiumId",
                        column: x => x.StadiumId,
                        principalTable: "Stadiums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShortName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HomeStadiumId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teams_Stadiums_HomeStadiumId",
                        column: x => x.HomeStadiumId,
                        principalTable: "Stadiums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Seats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RowLabel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SeatNumber = table.Column<int>(type: "int", nullable: false),
                    SeatBlockId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Seats_SeatBlocks_SeatBlockId",
                        column: x => x.SeatBlockId,
                        principalTable: "SeatBlocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Matches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MatchDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    HomeTeamId = table.Column<int>(type: "int", nullable: false),
                    AwayTeamId = table.Column<int>(type: "int", nullable: false),
                    StadiumId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Matches_Stadiums_StadiumId",
                        column: x => x.StadiumId,
                        principalTable: "Stadiums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Matches_Teams_AwayTeamId",
                        column: x => x.AwayTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Matches_Teams_HomeTeamId",
                        column: x => x.HomeTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JerseyNumber = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PhotoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TeamId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Players_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(12,0)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    MatchId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bookings_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bookings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MatchResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HomeScore = table.Column<int>(type: "int", nullable: false),
                    AwayScore = table.Column<int>(type: "int", nullable: false),
                    PossessionHome = table.Column<int>(type: "int", nullable: false),
                    PossessionAway = table.Column<int>(type: "int", nullable: false),
                    ShotsHome = table.Column<int>(type: "int", nullable: false),
                    ShotsAway = table.Column<int>(type: "int", nullable: false),
                    YellowCardsHome = table.Column<int>(type: "int", nullable: false),
                    YellowCardsAway = table.Column<int>(type: "int", nullable: false),
                    RedCardsHome = table.Column<int>(type: "int", nullable: false),
                    RedCardsAway = table.Column<int>(type: "int", nullable: false),
                    CornersHome = table.Column<int>(type: "int", nullable: false),
                    CornersAway = table.Column<int>(type: "int", nullable: false),
                    MatchId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchResults_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MatchTeamLineups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Formation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MatchId = table.Column<int>(type: "int", nullable: false),
                    TeamId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchTeamLineups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchTeamLineups_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MatchTeamLineups_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MatchTicketPrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Price = table.Column<decimal>(type: "decimal(12,0)", nullable: false),
                    MatchId = table.Column<int>(type: "int", nullable: false),
                    SeatBlockId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchTicketPrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchTicketPrices_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MatchTicketPrices_SeatBlocks_SeatBlockId",
                        column: x => x.SeatBlockId,
                        principalTable: "SeatBlocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SeatHolds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConnectionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpireAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MatchId = table.Column<int>(type: "int", nullable: false),
                    SeatId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeatHolds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeatHolds_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeatHolds_Seats_SeatId",
                        column: x => x.SeatId,
                        principalTable: "Seats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SeatHolds_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlayerMatchStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Goals = table.Column<int>(type: "int", nullable: false),
                    Assists = table.Column<int>(type: "int", nullable: false),
                    YellowCards = table.Column<int>(type: "int", nullable: false),
                    RedCards = table.Column<int>(type: "int", nullable: false),
                    MinutesPlayed = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<decimal>(type: "decimal(3,1)", nullable: false),
                    MatchId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerMatchStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerMatchStats_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerMatchStats_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BookingFoodItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(12,0)", nullable: false),
                    BookingId = table.Column<int>(type: "int", nullable: false),
                    FoodItemId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingFoodItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingFoodItems_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookingFoodItems_FoodItems_FoodItemId",
                        column: x => x.FoodItemId,
                        principalTable: "FoodItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BookingSeats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Price = table.Column<decimal>(type: "decimal(12,0)", nullable: false),
                    BookingId = table.Column<int>(type: "int", nullable: false),
                    SeatId = table.Column<int>(type: "int", nullable: false),
                    MatchId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingSeats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingSeats_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookingSeats_Seats_SeatId",
                        column: x => x.SeatId,
                        principalTable: "Seats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Amount = table.Column<decimal>(type: "decimal(12,0)", nullable: false),
                    Method = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VnpTransactionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BookingId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Refunds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(12,0)", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BookingId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Refunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Refunds_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LineupPlayers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PositionX = table.Column<double>(type: "float", nullable: false),
                    PositionY = table.Column<double>(type: "float", nullable: false),
                    IsStarting = table.Column<bool>(type: "bit", nullable: false),
                    MatchTeamLineupId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineupPlayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LineupPlayers_MatchTeamLineups_MatchTeamLineupId",
                        column: x => x.MatchTeamLineupId,
                        principalTable: "MatchTeamLineups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LineupPlayers_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tickets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TicketCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    QrCodeData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsCheckedIn = table.Column<bool>(type: "bit", nullable: false),
                    CheckedInAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BookingSeatId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tickets_BookingSeats_BookingSeatId",
                        column: x => x.BookingSeatId,
                        principalTable: "BookingSeats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "FoodItems",
                columns: new[] { "Id", "IsAvailable", "Name", "Price" },
                values: new object[,]
                {
                    { 1, true, "Bắp rang bơ", 30000m },
                    { 2, true, "Nước ngọt (lon)", 20000m },
                    { 3, true, "Combo Bắp + Nước", 45000m },
                    { 4, true, "Xúc xích nướng", 25000m },
                    { 5, true, "Bia lon", 35000m },
                    { 6, true, "Nước suối", 15000m }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Admin" },
                    { 2, "User" }
                });

            migrationBuilder.InsertData(
                table: "Stadiums",
                columns: new[] { "Id", "Address", "Capacity", "City", "Name" },
                values: new object[,]
                {
                    { 1, "123 Đường Đào Duy Từ", 25000, "TP. Hồ Chí Minh", "Sân Thống Nhất Mới" },
                    { 2, "45 Đường Trần Phú", 20000, "Đà Nẵng", "Sân Hòa Bình" }
                });

            migrationBuilder.InsertData(
                table: "SeatBlocks",
                columns: new[] { "Id", "BasePrice", "Name", "StadiumId" },
                values: new object[,]
                {
                    { 1, 150000m, "Khối A (Thường)", 1 },
                    { 2, 120000m, "Khối B (Thường)", 1 },
                    { 3, 350000m, "Khối VIP", 1 },
                    { 4, 150000m, "Khối A (Thường)", 2 },
                    { 5, 120000m, "Khối B (Thường)", 2 },
                    { 6, 350000m, "Khối VIP", 2 }
                });

            migrationBuilder.InsertData(
                table: "Teams",
                columns: new[] { "Id", "HomeStadiumId", "LogoUrl", "Name", "ShortName" },
                values: new object[,]
                {
                    { 1, 1, null, "Gia Định FC", "GDF" },
                    { 2, 2, null, "Sông Hàn FC", "SHN" },
                    { 3, 1, null, "Bến Nghé FC", "BNG" },
                    { 4, 2, null, "Sơn Trà FC", "STR" }
                });

            migrationBuilder.InsertData(
                table: "Matches",
                columns: new[] { "Id", "AwayTeamId", "HomeTeamId", "MatchDateTime", "StadiumId", "Status" },
                values: new object[,]
                {
                    { 1, 2, 1, new DateTime(2026, 8, 5, 19, 0, 0, 0, DateTimeKind.Unspecified), 1, 2 },
                    { 2, 4, 3, new DateTime(2026, 8, 11, 19, 0, 0, 0, DateTimeKind.Unspecified), 1, 1 },
                    { 3, 3, 1, new DateTime(2026, 8, 15, 20, 0, 0, 0, DateTimeKind.Unspecified), 1, 0 },
                    { 4, 4, 2, new DateTime(2026, 8, 17, 20, 0, 0, 0, DateTimeKind.Unspecified), 2, 0 }
                });

            migrationBuilder.InsertData(
                table: "Players",
                columns: new[] { "Id", "DateOfBirth", "FullName", "JerseyNumber", "PhotoUrl", "Position", "TeamId" },
                values: new object[,]
                {
                    { 1, new DateTime(1998, 3, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), "Nguyễn Văn An", 1, null, "GK", 1 },
                    { 2, new DateTime(1997, 6, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "Trần Minh Bảo", 4, null, "CB", 1 },
                    { 3, new DateTime(1996, 11, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), "Lê Hoàng Cường", 5, null, "CB", 1 },
                    { 4, new DateTime(1999, 1, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Phạm Quốc Duy", 2, null, "RB", 1 },
                    { 5, new DateTime(1998, 9, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), "Hoàng Văn Em", 3, null, "LB", 1 },
                    { 6, new DateTime(1997, 4, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "Vũ Đình Phúc", 6, null, "CM", 1 },
                    { 7, new DateTime(1998, 12, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Đặng Thành Giang", 8, null, "CM", 1 },
                    { 8, new DateTime(1996, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), "Bùi Văn Hùng", 10, null, "CM", 1 },
                    { 9, new DateTime(1999, 3, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "Ngô Minh Khoa", 7, null, "LW", 1 },
                    { 10, new DateTime(1997, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), "Đỗ Xuân Long", 9, null, "ST", 1 },
                    { 11, new DateTime(1998, 5, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), "Trịnh Công Minh", 11, null, "RW", 1 },
                    { 12, new DateTime(2000, 1, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), "Lý Thành Nam", 12, null, "GK", 1 },
                    { 13, new DateTime(1999, 2, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), "Phan Anh Tuấn", 14, null, "CB", 1 },
                    { 14, new DateTime(2000, 6, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), "Đinh Văn Sơn", 16, null, "ST", 1 },
                    { 15, new DateTime(1997, 5, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), "Nguyễn Hữu Phát", 1, null, "GK", 2 },
                    { 16, new DateTime(1998, 8, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), "Trần Đức Anh", 2, null, "RB", 2 },
                    { 17, new DateTime(1996, 10, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), "Lê Văn Bình", 5, null, "CB", 2 },
                    { 18, new DateTime(1997, 12, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), "Phạm Ngọc Cường", 4, null, "CB", 2 },
                    { 19, new DateTime(1998, 2, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), "Hoàng Trọng Đạt", 3, null, "LB", 2 },
                    { 20, new DateTime(1997, 7, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), "Vũ Thế Hiển", 6, null, "CM", 2 },
                    { 21, new DateTime(1998, 11, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), "Đặng Công Khánh", 8, null, "CM", 2 },
                    { 22, new DateTime(1996, 4, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), "Bùi Quang Linh", 10, null, "CM", 2 },
                    { 23, new DateTime(1999, 9, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), "Ngô Bảo Long", 7, null, "LW", 2 },
                    { 24, new DateTime(1997, 1, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "Đỗ Tiến Mạnh", 9, null, "ST", 2 },
                    { 25, new DateTime(1998, 6, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), "Trịnh Xuân Nghĩa", 11, null, "RW", 2 },
                    { 26, new DateTime(1999, 10, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), "Lý Công Phúc", 12, null, "GK", 2 },
                    { 27, new DateTime(2000, 3, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), "Phan Đình Quang", 14, null, "CB", 2 },
                    { 28, new DateTime(1999, 12, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), "Đinh Bảo Sơn", 16, null, "ST", 2 },
                    { 29, null, "Nguyễn Anh Kiệt", 1, null, "GK", 3 },
                    { 30, null, "Trần Bảo Long", 9, null, "ST", 3 },
                    { 31, null, "Lê Minh Nhật", 7, null, "LW", 3 },
                    { 32, null, "Phạm Gia Huy", 5, null, "CB", 3 },
                    { 33, null, "Hoàng Nhật Nam", 10, null, "CM", 3 },
                    { 34, null, "Vũ Đăng Khoa", 11, null, "RW", 3 },
                    { 35, null, "Đỗ Hoàng Việt", 1, null, "GK", 4 },
                    { 36, null, "Ngô Xuân Bách", 9, null, "ST", 4 },
                    { 37, null, "Bùi Tấn Phát", 7, null, "LW", 4 },
                    { 38, null, "Đặng Minh Tú", 5, null, "CB", 4 },
                    { 39, null, "Lý Gia Bảo", 10, null, "CM", 4 },
                    { 40, null, "Trịnh Anh Duy", 11, null, "RW", 4 }
                });

            migrationBuilder.InsertData(
                table: "Seats",
                columns: new[] { "Id", "RowLabel", "SeatBlockId", "SeatNumber" },
                values: new object[,]
                {
                    { 1, "A", 1, 1 },
                    { 2, "A", 1, 2 },
                    { 3, "A", 1, 3 },
                    { 4, "A", 1, 4 },
                    { 5, "A", 1, 5 },
                    { 6, "A", 1, 6 },
                    { 7, "A", 1, 7 },
                    { 8, "A", 1, 8 },
                    { 9, "A", 1, 9 },
                    { 10, "A", 1, 10 },
                    { 11, "B", 1, 1 },
                    { 12, "B", 1, 2 },
                    { 13, "B", 1, 3 },
                    { 14, "B", 1, 4 },
                    { 15, "B", 1, 5 },
                    { 16, "B", 1, 6 },
                    { 17, "B", 1, 7 },
                    { 18, "B", 1, 8 },
                    { 19, "B", 1, 9 },
                    { 20, "B", 1, 10 },
                    { 21, "C", 1, 1 },
                    { 22, "C", 1, 2 },
                    { 23, "C", 1, 3 },
                    { 24, "C", 1, 4 },
                    { 25, "C", 1, 5 },
                    { 26, "C", 1, 6 },
                    { 27, "C", 1, 7 },
                    { 28, "C", 1, 8 },
                    { 29, "C", 1, 9 },
                    { 30, "C", 1, 10 },
                    { 31, "D", 1, 1 },
                    { 32, "D", 1, 2 },
                    { 33, "D", 1, 3 },
                    { 34, "D", 1, 4 },
                    { 35, "D", 1, 5 },
                    { 36, "D", 1, 6 },
                    { 37, "D", 1, 7 },
                    { 38, "D", 1, 8 },
                    { 39, "D", 1, 9 },
                    { 40, "D", 1, 10 },
                    { 41, "E", 1, 1 },
                    { 42, "E", 1, 2 },
                    { 43, "E", 1, 3 },
                    { 44, "E", 1, 4 },
                    { 45, "E", 1, 5 },
                    { 46, "E", 1, 6 },
                    { 47, "E", 1, 7 },
                    { 48, "E", 1, 8 },
                    { 49, "E", 1, 9 },
                    { 50, "E", 1, 10 },
                    { 51, "F", 1, 1 },
                    { 52, "F", 1, 2 },
                    { 53, "F", 1, 3 },
                    { 54, "F", 1, 4 },
                    { 55, "F", 1, 5 },
                    { 56, "F", 1, 6 },
                    { 57, "F", 1, 7 },
                    { 58, "F", 1, 8 },
                    { 59, "F", 1, 9 },
                    { 60, "F", 1, 10 },
                    { 61, "A", 2, 1 },
                    { 62, "A", 2, 2 },
                    { 63, "A", 2, 3 },
                    { 64, "A", 2, 4 },
                    { 65, "A", 2, 5 },
                    { 66, "A", 2, 6 },
                    { 67, "A", 2, 7 },
                    { 68, "A", 2, 8 },
                    { 69, "A", 2, 9 },
                    { 70, "A", 2, 10 },
                    { 71, "B", 2, 1 },
                    { 72, "B", 2, 2 },
                    { 73, "B", 2, 3 },
                    { 74, "B", 2, 4 },
                    { 75, "B", 2, 5 },
                    { 76, "B", 2, 6 },
                    { 77, "B", 2, 7 },
                    { 78, "B", 2, 8 },
                    { 79, "B", 2, 9 },
                    { 80, "B", 2, 10 },
                    { 81, "C", 2, 1 },
                    { 82, "C", 2, 2 },
                    { 83, "C", 2, 3 },
                    { 84, "C", 2, 4 },
                    { 85, "C", 2, 5 },
                    { 86, "C", 2, 6 },
                    { 87, "C", 2, 7 },
                    { 88, "C", 2, 8 },
                    { 89, "C", 2, 9 },
                    { 90, "C", 2, 10 },
                    { 91, "D", 2, 1 },
                    { 92, "D", 2, 2 },
                    { 93, "D", 2, 3 },
                    { 94, "D", 2, 4 },
                    { 95, "D", 2, 5 },
                    { 96, "D", 2, 6 },
                    { 97, "D", 2, 7 },
                    { 98, "D", 2, 8 },
                    { 99, "D", 2, 9 },
                    { 100, "D", 2, 10 },
                    { 101, "E", 2, 1 },
                    { 102, "E", 2, 2 },
                    { 103, "E", 2, 3 },
                    { 104, "E", 2, 4 },
                    { 105, "E", 2, 5 },
                    { 106, "E", 2, 6 },
                    { 107, "E", 2, 7 },
                    { 108, "E", 2, 8 },
                    { 109, "E", 2, 9 },
                    { 110, "E", 2, 10 },
                    { 111, "F", 2, 1 },
                    { 112, "F", 2, 2 },
                    { 113, "F", 2, 3 },
                    { 114, "F", 2, 4 },
                    { 115, "F", 2, 5 },
                    { 116, "F", 2, 6 },
                    { 117, "F", 2, 7 },
                    { 118, "F", 2, 8 },
                    { 119, "F", 2, 9 },
                    { 120, "F", 2, 10 },
                    { 121, "A", 3, 1 },
                    { 122, "A", 3, 2 },
                    { 123, "A", 3, 3 },
                    { 124, "A", 3, 4 },
                    { 125, "A", 3, 5 },
                    { 126, "A", 3, 6 },
                    { 127, "A", 3, 7 },
                    { 128, "A", 3, 8 },
                    { 129, "A", 3, 9 },
                    { 130, "A", 3, 10 },
                    { 131, "B", 3, 1 },
                    { 132, "B", 3, 2 },
                    { 133, "B", 3, 3 },
                    { 134, "B", 3, 4 },
                    { 135, "B", 3, 5 },
                    { 136, "B", 3, 6 },
                    { 137, "B", 3, 7 },
                    { 138, "B", 3, 8 },
                    { 139, "B", 3, 9 },
                    { 140, "B", 3, 10 },
                    { 141, "C", 3, 1 },
                    { 142, "C", 3, 2 },
                    { 143, "C", 3, 3 },
                    { 144, "C", 3, 4 },
                    { 145, "C", 3, 5 },
                    { 146, "C", 3, 6 },
                    { 147, "C", 3, 7 },
                    { 148, "C", 3, 8 },
                    { 149, "C", 3, 9 },
                    { 150, "C", 3, 10 },
                    { 151, "D", 3, 1 },
                    { 152, "D", 3, 2 },
                    { 153, "D", 3, 3 },
                    { 154, "D", 3, 4 },
                    { 155, "D", 3, 5 },
                    { 156, "D", 3, 6 },
                    { 157, "D", 3, 7 },
                    { 158, "D", 3, 8 },
                    { 159, "D", 3, 9 },
                    { 160, "D", 3, 10 },
                    { 161, "E", 3, 1 },
                    { 162, "E", 3, 2 },
                    { 163, "E", 3, 3 },
                    { 164, "E", 3, 4 },
                    { 165, "E", 3, 5 },
                    { 166, "E", 3, 6 },
                    { 167, "E", 3, 7 },
                    { 168, "E", 3, 8 },
                    { 169, "E", 3, 9 },
                    { 170, "E", 3, 10 },
                    { 171, "F", 3, 1 },
                    { 172, "F", 3, 2 },
                    { 173, "F", 3, 3 },
                    { 174, "F", 3, 4 },
                    { 175, "F", 3, 5 },
                    { 176, "F", 3, 6 },
                    { 177, "F", 3, 7 },
                    { 178, "F", 3, 8 },
                    { 179, "F", 3, 9 },
                    { 180, "F", 3, 10 },
                    { 181, "A", 4, 1 },
                    { 182, "A", 4, 2 },
                    { 183, "A", 4, 3 },
                    { 184, "A", 4, 4 },
                    { 185, "A", 4, 5 },
                    { 186, "A", 4, 6 },
                    { 187, "A", 4, 7 },
                    { 188, "A", 4, 8 },
                    { 189, "A", 4, 9 },
                    { 190, "A", 4, 10 },
                    { 191, "B", 4, 1 },
                    { 192, "B", 4, 2 },
                    { 193, "B", 4, 3 },
                    { 194, "B", 4, 4 },
                    { 195, "B", 4, 5 },
                    { 196, "B", 4, 6 },
                    { 197, "B", 4, 7 },
                    { 198, "B", 4, 8 },
                    { 199, "B", 4, 9 },
                    { 200, "B", 4, 10 },
                    { 201, "C", 4, 1 },
                    { 202, "C", 4, 2 },
                    { 203, "C", 4, 3 },
                    { 204, "C", 4, 4 },
                    { 205, "C", 4, 5 },
                    { 206, "C", 4, 6 },
                    { 207, "C", 4, 7 },
                    { 208, "C", 4, 8 },
                    { 209, "C", 4, 9 },
                    { 210, "C", 4, 10 },
                    { 211, "D", 4, 1 },
                    { 212, "D", 4, 2 },
                    { 213, "D", 4, 3 },
                    { 214, "D", 4, 4 },
                    { 215, "D", 4, 5 },
                    { 216, "D", 4, 6 },
                    { 217, "D", 4, 7 },
                    { 218, "D", 4, 8 },
                    { 219, "D", 4, 9 },
                    { 220, "D", 4, 10 },
                    { 221, "E", 4, 1 },
                    { 222, "E", 4, 2 },
                    { 223, "E", 4, 3 },
                    { 224, "E", 4, 4 },
                    { 225, "E", 4, 5 },
                    { 226, "E", 4, 6 },
                    { 227, "E", 4, 7 },
                    { 228, "E", 4, 8 },
                    { 229, "E", 4, 9 },
                    { 230, "E", 4, 10 },
                    { 231, "F", 4, 1 },
                    { 232, "F", 4, 2 },
                    { 233, "F", 4, 3 },
                    { 234, "F", 4, 4 },
                    { 235, "F", 4, 5 },
                    { 236, "F", 4, 6 },
                    { 237, "F", 4, 7 },
                    { 238, "F", 4, 8 },
                    { 239, "F", 4, 9 },
                    { 240, "F", 4, 10 },
                    { 241, "A", 5, 1 },
                    { 242, "A", 5, 2 },
                    { 243, "A", 5, 3 },
                    { 244, "A", 5, 4 },
                    { 245, "A", 5, 5 },
                    { 246, "A", 5, 6 },
                    { 247, "A", 5, 7 },
                    { 248, "A", 5, 8 },
                    { 249, "A", 5, 9 },
                    { 250, "A", 5, 10 },
                    { 251, "B", 5, 1 },
                    { 252, "B", 5, 2 },
                    { 253, "B", 5, 3 },
                    { 254, "B", 5, 4 },
                    { 255, "B", 5, 5 },
                    { 256, "B", 5, 6 },
                    { 257, "B", 5, 7 },
                    { 258, "B", 5, 8 },
                    { 259, "B", 5, 9 },
                    { 260, "B", 5, 10 },
                    { 261, "C", 5, 1 },
                    { 262, "C", 5, 2 },
                    { 263, "C", 5, 3 },
                    { 264, "C", 5, 4 },
                    { 265, "C", 5, 5 },
                    { 266, "C", 5, 6 },
                    { 267, "C", 5, 7 },
                    { 268, "C", 5, 8 },
                    { 269, "C", 5, 9 },
                    { 270, "C", 5, 10 },
                    { 271, "D", 5, 1 },
                    { 272, "D", 5, 2 },
                    { 273, "D", 5, 3 },
                    { 274, "D", 5, 4 },
                    { 275, "D", 5, 5 },
                    { 276, "D", 5, 6 },
                    { 277, "D", 5, 7 },
                    { 278, "D", 5, 8 },
                    { 279, "D", 5, 9 },
                    { 280, "D", 5, 10 },
                    { 281, "E", 5, 1 },
                    { 282, "E", 5, 2 },
                    { 283, "E", 5, 3 },
                    { 284, "E", 5, 4 },
                    { 285, "E", 5, 5 },
                    { 286, "E", 5, 6 },
                    { 287, "E", 5, 7 },
                    { 288, "E", 5, 8 },
                    { 289, "E", 5, 9 },
                    { 290, "E", 5, 10 },
                    { 291, "F", 5, 1 },
                    { 292, "F", 5, 2 },
                    { 293, "F", 5, 3 },
                    { 294, "F", 5, 4 },
                    { 295, "F", 5, 5 },
                    { 296, "F", 5, 6 },
                    { 297, "F", 5, 7 },
                    { 298, "F", 5, 8 },
                    { 299, "F", 5, 9 },
                    { 300, "F", 5, 10 },
                    { 301, "A", 6, 1 },
                    { 302, "A", 6, 2 },
                    { 303, "A", 6, 3 },
                    { 304, "A", 6, 4 },
                    { 305, "A", 6, 5 },
                    { 306, "A", 6, 6 },
                    { 307, "A", 6, 7 },
                    { 308, "A", 6, 8 },
                    { 309, "A", 6, 9 },
                    { 310, "A", 6, 10 },
                    { 311, "B", 6, 1 },
                    { 312, "B", 6, 2 },
                    { 313, "B", 6, 3 },
                    { 314, "B", 6, 4 },
                    { 315, "B", 6, 5 },
                    { 316, "B", 6, 6 },
                    { 317, "B", 6, 7 },
                    { 318, "B", 6, 8 },
                    { 319, "B", 6, 9 },
                    { 320, "B", 6, 10 },
                    { 321, "C", 6, 1 },
                    { 322, "C", 6, 2 },
                    { 323, "C", 6, 3 },
                    { 324, "C", 6, 4 },
                    { 325, "C", 6, 5 },
                    { 326, "C", 6, 6 },
                    { 327, "C", 6, 7 },
                    { 328, "C", 6, 8 },
                    { 329, "C", 6, 9 },
                    { 330, "C", 6, 10 },
                    { 331, "D", 6, 1 },
                    { 332, "D", 6, 2 },
                    { 333, "D", 6, 3 },
                    { 334, "D", 6, 4 },
                    { 335, "D", 6, 5 },
                    { 336, "D", 6, 6 },
                    { 337, "D", 6, 7 },
                    { 338, "D", 6, 8 },
                    { 339, "D", 6, 9 },
                    { 340, "D", 6, 10 },
                    { 341, "E", 6, 1 },
                    { 342, "E", 6, 2 },
                    { 343, "E", 6, 3 },
                    { 344, "E", 6, 4 },
                    { 345, "E", 6, 5 },
                    { 346, "E", 6, 6 },
                    { 347, "E", 6, 7 },
                    { 348, "E", 6, 8 },
                    { 349, "E", 6, 9 },
                    { 350, "E", 6, 10 },
                    { 351, "F", 6, 1 },
                    { 352, "F", 6, 2 },
                    { 353, "F", 6, 3 },
                    { 354, "F", 6, 4 },
                    { 355, "F", 6, 5 },
                    { 356, "F", 6, 6 },
                    { 357, "F", 6, 7 },
                    { 358, "F", 6, 8 },
                    { 359, "F", 6, 9 },
                    { 360, "F", 6, 10 }
                });

            migrationBuilder.InsertData(
                table: "MatchResults",
                columns: new[] { "Id", "AwayScore", "CornersAway", "CornersHome", "HomeScore", "MatchId", "PossessionAway", "PossessionHome", "RedCardsAway", "RedCardsHome", "ShotsAway", "ShotsHome", "YellowCardsAway", "YellowCardsHome" },
                values: new object[,]
                {
                    { 1, 1, 4, 6, 2, 1, 45, 55, 0, 0, 8, 12, 1, 2 },
                    { 2, 0, 2, 3, 1, 2, 48, 52, 0, 0, 5, 7, 0, 1 }
                });

            migrationBuilder.InsertData(
                table: "MatchTeamLineups",
                columns: new[] { "Id", "Formation", "MatchId", "TeamId" },
                values: new object[,]
                {
                    { 1, "4-3-3", 1, 1 },
                    { 2, "4-3-3", 1, 2 }
                });

            migrationBuilder.InsertData(
                table: "PlayerMatchStats",
                columns: new[] { "Id", "Assists", "Goals", "MatchId", "MinutesPlayed", "PlayerId", "Rating", "RedCards", "YellowCards" },
                values: new object[,]
                {
                    { 1, 0, 0, 1, 90, 1, 6.8m, 0, 0 },
                    { 2, 0, 0, 1, 90, 5, 6.5m, 0, 0 },
                    { 3, 0, 0, 1, 90, 2, 7.0m, 0, 0 },
                    { 4, 0, 0, 1, 90, 3, 6.9m, 0, 1 },
                    { 5, 0, 0, 1, 90, 4, 6.7m, 0, 0 },
                    { 6, 0, 0, 1, 90, 6, 7.1m, 0, 1 },
                    { 7, 0, 0, 1, 90, 7, 7.3m, 0, 0 },
                    { 8, 1, 0, 1, 90, 8, 6.8m, 0, 0 },
                    { 9, 0, 1, 1, 90, 9, 8.0m, 0, 0 },
                    { 10, 0, 1, 1, 90, 10, 8.4m, 0, 0 },
                    { 11, 1, 0, 1, 90, 11, 7.0m, 0, 0 },
                    { 12, 0, 0, 1, 90, 15, 6.0m, 0, 0 },
                    { 13, 0, 0, 1, 90, 19, 6.4m, 0, 0 },
                    { 14, 0, 0, 1, 90, 17, 6.2m, 0, 0 },
                    { 15, 0, 0, 1, 90, 18, 6.3m, 0, 0 },
                    { 16, 0, 0, 1, 90, 16, 6.5m, 0, 0 },
                    { 17, 0, 0, 1, 90, 20, 6.8m, 0, 0 },
                    { 18, 0, 0, 1, 90, 21, 6.6m, 0, 1 },
                    { 19, 0, 0, 1, 90, 22, 6.5m, 0, 0 },
                    { 20, 0, 0, 1, 90, 23, 6.7m, 0, 0 },
                    { 21, 0, 1, 1, 90, 24, 7.6m, 0, 0 },
                    { 22, 0, 0, 1, 90, 25, 6.4m, 0, 0 }
                });

            migrationBuilder.InsertData(
                table: "LineupPlayers",
                columns: new[] { "Id", "IsStarting", "MatchTeamLineupId", "PlayerId", "PositionX", "PositionY" },
                values: new object[,]
                {
                    { 1, true, 1, 1, 10.0, 50.0 },
                    { 2, true, 1, 5, 25.0, 20.0 },
                    { 3, true, 1, 2, 25.0, 42.0 },
                    { 4, true, 1, 3, 25.0, 58.0 },
                    { 5, true, 1, 4, 25.0, 82.0 },
                    { 6, true, 1, 6, 45.0, 30.0 },
                    { 7, true, 1, 7, 45.0, 50.0 },
                    { 8, true, 1, 8, 45.0, 70.0 },
                    { 9, true, 1, 9, 62.0, 25.0 },
                    { 10, true, 1, 10, 62.0, 50.0 },
                    { 11, true, 1, 11, 62.0, 75.0 },
                    { 12, true, 2, 15, 10.0, 50.0 },
                    { 13, true, 2, 19, 25.0, 20.0 },
                    { 14, true, 2, 17, 25.0, 42.0 },
                    { 15, true, 2, 18, 25.0, 58.0 },
                    { 16, true, 2, 16, 25.0, 82.0 },
                    { 17, true, 2, 20, 45.0, 30.0 },
                    { 18, true, 2, 21, 45.0, 50.0 },
                    { 19, true, 2, 22, 45.0, 70.0 },
                    { 20, true, 2, 23, 62.0, 25.0 },
                    { 21, true, 2, 24, 62.0, 50.0 },
                    { 22, true, 2, 25, 62.0, 75.0 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingFoodItems_BookingId",
                table: "BookingFoodItems",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingFoodItems_FoodItemId",
                table: "BookingFoodItems",
                column: "FoodItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_BookingCode",
                table: "Bookings",
                column: "BookingCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_MatchId",
                table: "Bookings",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_UserId",
                table: "Bookings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingSeats_BookingId",
                table: "BookingSeats",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingSeats_SeatId_MatchId",
                table: "BookingSeats",
                columns: new[] { "SeatId", "MatchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LineupPlayers_MatchTeamLineupId",
                table: "LineupPlayers",
                column: "MatchTeamLineupId");

            migrationBuilder.CreateIndex(
                name: "IX_LineupPlayers_PlayerId",
                table: "LineupPlayers",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_AwayTeamId",
                table: "Matches",
                column: "AwayTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_HomeTeamId",
                table: "Matches",
                column: "HomeTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_StadiumId",
                table: "Matches",
                column: "StadiumId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchResults_MatchId",
                table: "MatchResults",
                column: "MatchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchTeamLineups_MatchId",
                table: "MatchTeamLineups",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchTeamLineups_TeamId",
                table: "MatchTeamLineups",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchTicketPrices_MatchId",
                table: "MatchTicketPrices",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchTicketPrices_SeatBlockId",
                table: "MatchTicketPrices",
                column: "SeatBlockId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_BookingId",
                table: "Payments",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerMatchStats_MatchId",
                table: "PlayerMatchStats",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerMatchStats_PlayerId",
                table: "PlayerMatchStats",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_TeamId",
                table: "Players",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_BookingId",
                table: "Refunds",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_SeatBlocks_StadiumId",
                table: "SeatBlocks",
                column: "StadiumId");

            migrationBuilder.CreateIndex(
                name: "IX_SeatHolds_MatchId",
                table: "SeatHolds",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_SeatHolds_SeatId_MatchId",
                table: "SeatHolds",
                columns: new[] { "SeatId", "MatchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeatHolds_UserId",
                table: "SeatHolds",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Seats_SeatBlockId",
                table: "Seats",
                column: "SeatBlockId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_HomeStadiumId",
                table: "Teams",
                column: "HomeStadiumId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_BookingSeatId",
                table: "Tickets",
                column: "BookingSeatId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_TicketCode",
                table: "Tickets",
                column: "TicketCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingFoodItems");

            migrationBuilder.DropTable(
                name: "LineupPlayers");

            migrationBuilder.DropTable(
                name: "MatchResults");

            migrationBuilder.DropTable(
                name: "MatchTicketPrices");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "PlayerMatchStats");

            migrationBuilder.DropTable(
                name: "Refunds");

            migrationBuilder.DropTable(
                name: "SeatHolds");

            migrationBuilder.DropTable(
                name: "Tickets");

            migrationBuilder.DropTable(
                name: "FoodItems");

            migrationBuilder.DropTable(
                name: "MatchTeamLineups");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "BookingSeats");

            migrationBuilder.DropTable(
                name: "Bookings");

            migrationBuilder.DropTable(
                name: "Seats");

            migrationBuilder.DropTable(
                name: "Matches");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "SeatBlocks");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Stadiums");
        }
    }
}
