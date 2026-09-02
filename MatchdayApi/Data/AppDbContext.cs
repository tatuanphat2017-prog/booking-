using Microsoft.EntityFrameworkCore;
using MatchdayApi.Models;

namespace MatchdayApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Stadium> Stadiums => Set<Stadium>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<SeatBlock> SeatBlocks => Set<SeatBlock>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<MatchTicketPrice> MatchTicketPrices => Set<MatchTicketPrice>();
    public DbSet<MatchResult> MatchResults => Set<MatchResult>();
    public DbSet<MatchTeamLineup> MatchTeamLineups => Set<MatchTeamLineup>();
    public DbSet<LineupPlayer> LineupPlayers => Set<LineupPlayer>();
    public DbSet<PlayerMatchStat> PlayerMatchStats => Set<PlayerMatchStat>();
    public DbSet<SeatHold> SeatHolds => Set<SeatHold>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingSeat> BookingSeats => Set<BookingSeat>();
    public DbSet<FoodItem> FoodItems => Set<FoodItem>();
    public DbSet<BookingFoodItem> BookingFoodItems => Set<BookingFoodItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Refund> Refunds => Set<Refund>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---------- Tài khoản ----------
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .Property(u => u.Email)
            .HasMaxLength(256)
            .IsRequired();

        // ---------- Đội bóng & sân ----------
        modelBuilder.Entity<Team>()
            .HasOne(t => t.HomeStadium)
            .WithMany(s => s.Teams)
            .HasForeignKey(t => t.HomeStadiumId)
            .OnDelete(DeleteBehavior.SetNull);

        // ---------- Trận đấu ----------
        // Match tham chiếu Team 2 lần (Home/Away) + Stadium 1 lần => nếu để Cascade mặc định,
        // SQL Server sẽ báo lỗi "may cause cycles or multiple cascade paths". Đặt Restrict để
        // tránh lỗi này (muốn xóa 1 Team/Stadium thì phải xóa/migrate Match liên quan trước).
        modelBuilder.Entity<Match>()
            .HasOne(m => m.HomeTeam)
            .WithMany(t => t.HomeMatches)
            .HasForeignKey(m => m.HomeTeamId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Match>()
            .HasOne(m => m.AwayTeam)
            .WithMany(t => t.AwayMatches)
            .HasForeignKey(m => m.AwayTeamId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Match>()
            .HasOne(m => m.Stadium)
            .WithMany(s => s.Matches)
            .HasForeignKey(m => m.StadiumId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MatchResult>()
            .HasOne(r => r.Match)
            .WithOne(m => m.Result)
            .HasForeignKey<MatchResult>(r => r.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MatchResult>()
            .HasIndex(r => r.MatchId)
            .IsUnique();

        // ---------- Đội hình & chỉ số cầu thủ ----------
        modelBuilder.Entity<MatchTeamLineup>()
            .HasOne(l => l.Match)
            .WithMany(m => m.Lineups)
            .HasForeignKey(l => l.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MatchTeamLineup>()
            .HasOne(l => l.Team)
            .WithMany(t => t.Lineups)
            .HasForeignKey(l => l.TeamId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PlayerMatchStat>()
            .HasOne(s => s.Match)
            .WithMany(m => m.PlayerStats)
            .HasForeignKey(s => s.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PlayerMatchStat>()
            .HasOne(s => s.Player)
            .WithMany(p => p.MatchStats)
            .HasForeignKey(s => s.PlayerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PlayerMatchStat>()
            .Property(s => s.Rating)
            .HasColumnType("decimal(3,1)"); // vd 8.4

        // ---------- Ghế & giữ ghế real-time ----------
        modelBuilder.Entity<SeatBlock>()
            .Property(sb => sb.BasePrice)
            .HasColumnType("decimal(12,0)");

        modelBuilder.Entity<MatchTicketPrice>()
            .Property(p => p.Price)
            .HasColumnType("decimal(12,0)");

        // Unique (SeatId, MatchId): 1 ghế chỉ được giữ tạm 1 lần / trận tại 1 thời điểm.
        // Ứng dụng phải xóa SeatHold hết hạn (ExpireAt < now) trước khi tạo hold mới cho ghế đó.
        modelBuilder.Entity<SeatHold>()
            .HasIndex(h => new { h.SeatId, h.MatchId })
            .IsUnique();

        modelBuilder.Entity<SeatHold>()
            .HasOne(h => h.Match)
            .WithMany(m => m.SeatHolds)
            .HasForeignKey(h => h.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SeatHold>()
            .HasOne(h => h.Seat)
            .WithMany(s => s.SeatHolds)
            .HasForeignKey(h => h.SeatId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SeatHold>()
            .HasOne(h => h.User)
            .WithMany(u => u.SeatHolds)
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ---------- Đặt vé ----------
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.User)
            .WithMany(u => u.Bookings)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Match)
            .WithMany(m => m.Bookings)
            .HasForeignKey(b => b.MatchId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Booking>()
            .Property(b => b.TotalAmount)
            .HasColumnType("decimal(12,0)");

        modelBuilder.Entity<Booking>()
            .HasIndex(b => b.BookingCode)
            .IsUnique();

        // Unique (SeatId, MatchId) trên BookingSeat: chặn 1 ghế bị bán trùng trong cùng 1 trận.
        // MatchId ở đây là bản sao (denormalize) từ Booking.MatchId, PHẢI gán khi tạo BookingSeat.
        modelBuilder.Entity<BookingSeat>()
            .HasIndex(bs => new { bs.SeatId, bs.MatchId })
            .IsUnique();

        modelBuilder.Entity<BookingSeat>()
            .HasOne(bs => bs.Booking)
            .WithMany(b => b.BookingSeats)
            .HasForeignKey(bs => bs.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BookingSeat>()
            .HasOne(bs => bs.Seat)
            .WithMany(s => s.BookingSeats)
            .HasForeignKey(bs => bs.SeatId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BookingSeat>()
            .Property(bs => bs.Price)
            .HasColumnType("decimal(12,0)");

        // ---------- Đồ ăn / thức uống ----------
        modelBuilder.Entity<FoodItem>()
            .Property(f => f.Price)
            .HasColumnType("decimal(12,0)");

        modelBuilder.Entity<BookingFoodItem>()
            .HasOne(bf => bf.Booking)
            .WithMany(b => b.FoodItems)
            .HasForeignKey(bf => bf.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BookingFoodItem>()
            .HasOne(bf => bf.FoodItem)
            .WithMany(f => f.BookingFoodItems)
            .HasForeignKey(bf => bf.FoodItemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BookingFoodItem>()
            .Property(bf => bf.UnitPrice)
            .HasColumnType("decimal(12,0)");

        // ---------- Thanh toán / Vé / Hoàn tiền ----------
        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Booking)
            .WithMany(b => b.Payments)
            .HasForeignKey(p => p.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Payment>()
            .Property(p => p.Amount)
            .HasColumnType("decimal(12,0)");

        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.BookingSeat)
            .WithOne(bs => bs.Ticket)
            .HasForeignKey<Ticket>(t => t.BookingSeatId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Ticket>()
            .HasIndex(t => t.BookingSeatId)
            .IsUnique();

        modelBuilder.Entity<Ticket>()
            .HasIndex(t => t.TicketCode)
            .IsUnique();

        modelBuilder.Entity<Refund>()
            .HasOne(r => r.Booking)
            .WithMany(b => b.Refunds)
            .HasForeignKey(r => r.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Refund>()
            .Property(r => r.Amount)
            .HasColumnType("decimal(12,0)");

        // ---------- Dữ liệu mẫu (task #7) ----------
        SeedData.Seed(modelBuilder);
    }
}
