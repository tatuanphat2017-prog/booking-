using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MatchdayApi.Data;
using MatchdayApi.Hubs;
using MatchdayApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- Services ----------
// Serialize enum (MatchStatus, BookingStatus...) thành chữ (vd "Upcoming") thay vì số (0)
// trong JSON request/response, và cả trong dropdown filter của Swagger UI — dễ đọc, dễ test hơn.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Cho phép nhập "Bearer {token}" trong Swagger UI để test API cần Authorize.
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Nhập token dạng: Bearer {token}"
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// VnPay:Mode = "Mock" (mặc định) -> dùng MockVnPayService, không cần TmnCode/HashSecret thật,
// test được toàn bộ luồng thanh toán ngay trên máy. Đổi thành "Real" (kèm User Secrets thật) khi
// đã có tài khoản VNPay sandbox để chuyển sang gọi VNPay thật — không cần sửa code nào khác.
builder.Services.AddScoped<IVnPayService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var mode = config["VnPay:Mode"] ?? "Mock";
    return mode.Equals("Real", StringComparison.OrdinalIgnoreCase)
        ? new VnPayService(config)
        : new MockVnPayService();
});

// ---------- QR + Vé điện tử + Email xác nhận (task #14) ----------
builder.Services.AddScoped<IQrCodeService, QrCodeService>();
builder.Services.AddScoped<ITicketService, TicketService>();

// Email:Mode = "Mock" (mặc định) -> ghi email ra file HTML trong wwwroot/sent-emails/, không cần
// tài khoản SMTP thật. Đổi thành "Real" (kèm Email:Username/Password thật qua User Secrets) để gửi
// email thật qua SMTP (vd Gmail) — không cần sửa code nào khác.
builder.Services.AddScoped<IEmailService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var mode = config["Email:Mode"] ?? "Mock";
    var db = sp.GetRequiredService<AppDbContext>();
    var qr = sp.GetRequiredService<IQrCodeService>();
    return mode.Equals("Real", StringComparison.OrdinalIgnoreCase)
        ? new SmtpEmailService(db, qr, config)
        : new MockEmailService(db, qr, sp.GetRequiredService<IWebHostEnvironment>());
});

// ---------- SignalR (task #12 — chọn ghế real-time) ----------
builder.Services.AddSignalR();
builder.Services.AddHostedService<SeatHoldCleanupService>();

// ---------- JWT Authentication ----------
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"];
if (!string.IsNullOrEmpty(jwtKey))
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"] ?? "MatchdayApi",
            ValidAudience = jwtSection["Audience"] ?? "MatchdayApiClient",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        // SignalR (trình duyệt qua WebSocket) không tự gắn header Authorization được,
        // nên phải cho phép nhận JWT qua query string "access_token" riêng cho đường dẫn /hubs/...
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
}
builder.Services.AddAuthorization();

// CORS: cho phép frontend (HTML/CSS/JS thuần) gọi API từ domain khác lúc dev.
// Khi deploy thật, nên đổi AllowAnyOrigin() thành domain cụ thể của frontend.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// ---------- Middleware pipeline ----------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles(); // phục vụ trang test SignalR tại wwwroot/seat-test.html
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<SeatSelectionHub>("/hubs/seat-selection");

app.Run();
