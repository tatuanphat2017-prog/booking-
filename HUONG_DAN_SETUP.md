# Hướng dẫn setup MatchdayApi trên máy bạn (Visual Studio 2022)

## 1. Mở project
- Giải nén / copy thư mục `MatchdayApi` vào máy.
- Mở Visual Studio 2022 → **File > Open > Project/Solution** → chọn file `MatchdayApi/MatchdayApi.csproj`.
- Visual Studio sẽ tự hỏi tạo solution — đồng ý là được, không cần tự tạo `.sln` tay.

## 2. Cấu hình chuỗi kết nối SQL Server
File `appsettings.Development.json` (đã có sẵn, **không** nằm trong Git vì đã thêm vào `.gitignore`) đang để:

```json
"DefaultConnection": "Server=localhost;Database=MatchdayDb;Trusted_Connection=True;TrustServerCertificate=True;"
```

- Nếu bạn dùng SQL Server LocalDB/Express trên máy, sửa `Server=localhost` thành đúng instance của bạn (vd `Server=.\SQLEXPRESS` hoặc `Server=(localdb)\MSSQLLocalDB`).
- Không cần sửa `appsettings.json` (file chính, có trong Git) — để trống là đúng, tránh lộ thông tin thật khi push code.

## 3. Cài EF Core CLI tool (nếu máy chưa có)
Mở Package Manager Console hoặc Terminal trong VS, chạy 1 lần:

```
dotnet tool install --global dotnet-ef
```

## 4. Tạo migration đầu tiên và tạo database

Trong thư mục `MatchdayApi` (chứa file .csproj), chạy:

```
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Sau bước này, SQL Server sẽ có database `MatchdayDb` với đầy đủ 21 bảng theo đúng ERD, và **tự động có sẵn dữ liệu mẫu** (4 đội bóng, 2 sân, 360 ghế, 40 cầu thủ, 4 trận đấu, kết quả/đội hình/chỉ số của trận đã kết thúc, menu đồ ăn) — vì mình đã đưa dữ liệu mẫu vào `Data/SeedData.cs` bằng `HasData()`, migration sẽ tự sinh câu lệnh INSERT cho toàn bộ dữ liệu này.

## 5. Chạy thử

```
dotnet run
```

Mở `https://localhost:5001/swagger` để xem Swagger UI — hiện tại chưa có Controller nào (sẽ làm ở task #9, #10) nên Swagger sẽ trống, nhưng project phải chạy lên không lỗi là đạt yêu cầu của task #8.

## Cấu trúc thư mục

```
MatchdayApi/
├── Program.cs              # Cấu hình app, EF Core, Swagger, CORS
├── appsettings.json         # Config chính (không chứa secret)
├── appsettings.Development.json  # Chuỗi kết nối local (gitignore)
├── Enums/                   # MatchStatus, BookingStatus, PaymentStatus, RefundStatus
├── Models/                  # 21 entity class theo đúng ERD
└── Data/
    ├── AppDbContext.cs      # DbContext + cấu hình quan hệ/ràng buộc (Fluent API)
    └── SeedData.cs          # Dữ liệu mẫu (HasData) — tự nạp khi migrate
```

## Việc cần làm tiếp (không nằm trong task #8)
- Task #12: SignalR Hub cho real-time chọn ghế
- Task #13: Tích hợp VNPay (nhớ dùng **User Secrets**, không hardcode `vnp_HashSecret` vào appsettings)

## Task #9 — Auth JWT + phân quyền Admin/User

### 1. Thêm cấu hình Jwt vào `appsettings.Development.json`
File này đã có `ConnectionStrings`, giờ thêm khối `Jwt` vào cùng cấp:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "Jwt": {
    "Key": "tA8qzmkObyKgzT5AjfSMnumCV96PUB04yn538RE5xToOko74Oz4zHmOdGoa89uV5",
    "Issuer": "MatchdayApi",
    "Audience": "MatchdayApiClient",
    "ExpiryMinutes": 120
  }
}
```

`Jwt:Key` này chỉ dùng để chạy thử ở máy bạn (dev), không commit lên Git vì file này đã bị `.gitignore`. Khi deploy thật lên MonsterASP (task #26), phải đổi qua key khác và cấu hình qua Environment Variables/User Secrets, không copy nguyên key này lên production.

### 2. Restore package mới (JWT + BCrypt)
Trong Package Manager Console:

```
Update-Package -reinstall
```

Hoặc đơn giản là Build lại (Ctrl+Shift+B) — Visual Studio sẽ tự restore 2 package mới thêm vào `.csproj`: `Microsoft.AspNetCore.Authentication.JwtBearer` và `BCrypt.Net-Next`.

### 3. Tạo migration mới cho tài khoản Admin mặc định
Đã seed sẵn 1 tài khoản Admin trong `SeedData.cs`:
- Email: `admin@matchday.local`
- Mật khẩu: `Admin@123`

Chạy trong Package Manager Console:

```
Add-Migration AddAuthAndAdminUser
Update-Database
```

### 4. Test bằng Swagger
- Chạy app (F5) → mở `/swagger`.
- Gọi `POST /api/auth/login` với `email: admin@matchday.local`, `password: Admin@123` → nhận về JWT token.
- Bấm nút **Authorize** ở góc trên Swagger UI, nhập `Bearer {token vừa nhận}`.
- Gọi thử `GET /api/auth/me` → phải trả về đúng thông tin + role "Admin".
- Gọi thử `GET /api/auth/admin-only` → phải thành công (200) vì đang đăng nhập Admin.
- Đăng ký 1 tài khoản mới qua `POST /api/auth/register` (role tự động là "User"), login bằng tài khoản đó, gọi lại `GET /api/auth/admin-only` → phải bị từ chối (403 Forbidden) vì không phải Admin.

## Task #10 — CRUD Teams/Players/Matches/Stadiums/Seats

Không cần thêm package hay migration gì — chỉ thêm Controller + DTO mới, dùng lại `AppDbContext` sẵn có. Build lại (Ctrl+Shift+B) là chạy được ngay.

### Nguyên tắc phân quyền áp dụng cho cả 5 nhóm entity
- `GET` (xem danh sách / xem chi tiết): **công khai**, không cần đăng nhập — vì khách vãng lai cũng cần xem lịch thi đấu, đội bóng trước khi đăng nhập đặt vé.
- `POST` / `PUT` / `DELETE` (thêm/sửa/xóa): chỉ **Admin** mới gọi được (`[Authorize(Roles = "Admin")]`).

### Danh sách endpoint mới

**Stadiums** (`/api/Stadiums`): GET, GET/{id}, POST, PUT/{id}, DELETE/{id}

**Teams** (`/api/Teams`): GET, GET/{id}, POST, PUT/{id}, DELETE/{id} — POST/PUT có kiểm tra `HomeStadiumId` tồn tại.

**Players** (`/api/Players`): GET (có thể lọc `?teamId=`), GET/{id}, POST, PUT/{id}, DELETE/{id} — POST/PUT kiểm tra `TeamId` tồn tại.

**SeatBlocks** (`/api/SeatBlocks`): GET (có thể lọc `?stadiumId=`), GET/{id}, POST, PUT/{id}, DELETE/{id}.

**Seats** (`/api/Seats`): GET (nên lọc `?seatBlockId=` vì mỗi khối có hàng chục ghế), GET/{id}, POST, PUT/{id}, DELETE/{id} — chặn trùng ghế (cùng khối + cùng hàng + cùng số).

**Matches** (`/api/Matches`): GET (có thể lọc `?status=Upcoming` / `Live` / `Finished`), GET/{id}, POST, PUT/{id}, DELETE/{id} — kiểm tra đội nhà ≠ đội khách, các Id đội/sân phải tồn tại.
Kèm 2 endpoint quản lý giá vé theo khối ghế của từng trận:
- `GET /api/Matches/{matchId}/ticket-prices` — công khai, xem giá vé từng khối ghế của trận đó.
- `POST /api/Matches/{matchId}/ticket-prices` — Admin, đặt/cập nhật giá cho 1 khối ghế (body: `{ "seatBlockId": 1, "price": 200000 }`).

Tất cả thao tác xóa đều bắt lỗi khi entity đang bị tham chiếu (vd xóa Team đang có Player) và trả về `409 Conflict` kèm thông báo tiếng Việt thay vì để lỗi 500 khó hiểu.

### Test nhanh bằng Swagger
1. `GET /api/Teams` → phải thấy 4 đội bóng mẫu, không cần Authorize.
2. `GET /api/Matches?status=Upcoming` → phải thấy 2 trận sắp diễn ra (Match3, Match4 trong seed data).
3. Đăng nhập Admin, Authorize token → `POST /api/Teams` tạo 1 đội mới → phải trả 201 Created.
4. Không Authorize (hoặc Authorize bằng token User) → thử `POST /api/Teams` → phải bị 401/403.
5. `POST /api/Matches/1/ticket-prices` với body `{ "seatBlockId": 1, "price": 180000 }` (Admin) → `GET /api/Matches/1/ticket-prices` → phải thấy giá vừa đặt.

## Task #11 — Dashboard user: trận sắp diễn ra & đã kết thúc

Thêm `DashboardController` — khác với `/api/Matches` (CRUD thô cho Admin), controller này trả sẵn dữ liệu đã gộp/định dạng để frontend trang chủ dùng ngay, không cần tự ráp từ nhiều API. Toàn bộ **công khai**, không cần đăng nhập.

### Endpoint mới
- `GET /api/Dashboard/upcoming?take=20` — danh sách trận Sắp diễn ra + Đang diễn ra, sắp xếp gần nhất trước. Mỗi trận kèm logo 2 đội, tên sân, và **giá vé thấp nhất/cao nhất** (lấy từ giá đã đặt ở task #10, null nếu Admin chưa đặt giá).
- `GET /api/Dashboard/finished?take=20` — danh sách trận Đã kết thúc, mới đá gần đây lên trước, kèm tỷ số (null nếu đã đánh dấu Finished nhưng chưa nhập kết quả).
- `GET /api/Dashboard/matches/{id}` — chi tiết 1 trận khi user bấm vào từ Dashboard: đầy đủ thông tin đội/sân + giá vé theo từng khối ghế (nếu còn bán) + kết quả chi tiết (tỷ số, kiểm soát bóng, sút, thẻ, phạt góc — nếu đã có).

### Test nhanh bằng Swagger
1. `GET /api/Dashboard/upcoming` → phải thấy Match3 (Gia Định vs Bến Nghé) và Match4 (Sông Hàn vs Sơn Trà), `minTicketPrice`/`maxTicketPrice` đang là `null` vì chưa đặt giá cho 2 trận này.
2. Đăng nhập Admin → `POST /api/Matches/3/ticket-prices` với `{ "seatBlockId": 1, "price": 150000 }` và `{ "seatBlockId": 3, "price": 350000 }` (gọi 2 lần, đổi seatBlockId) → gọi lại `GET /api/Dashboard/upcoming` → Match3 phải hiện `minTicketPrice: 150000`, `maxTicketPrice: 350000`.
3. `GET /api/Dashboard/finished` → phải thấy Match1 (Gia Định vs Sông Hàn) với tỷ số đúng theo seed data (2-1).
4. `GET /api/Dashboard/matches/1` → phải thấy đầy đủ `result` (tỷ số, possession, shots, thẻ, phạt góc) vì Match1 đã Finished và có seed `MatchResult`.
5. `GET /api/Dashboard/matches/3` → vì Match3 chưa đá nên `result` phải là `null`, còn `ticketPrices` phải thấy 2 dòng vừa đặt ở bước 2.

## Task #12 — Chọn ghế real-time (SignalR) + tạo Booking

### Những gì đã thêm
- **`Hubs/SeatSelectionHub.cs`** — SignalR Hub tại đường dẫn `/hubs/seat-selection`. Client gọi `JoinMatch(matchId)` để vào phòng của 1 trận, `HoldSeat(matchId, seatId)` để giữ tạm 1 ghế (5 phút), `ReleaseSeat(matchId, seatId)` để nhả ghế. Server phát các sự kiện `SeatHeld`, `SeatReleased`, `SeatBooked`, `HoldConfirmed`, `HoldRejected` cho client.
- **`Services/SeatHoldCleanupService.cs`** — chạy nền, cứ 15 giây quét 1 lần để tự nhả các ghế bị giữ quá 5 phút mà chưa chốt đặt vé (báo real-time cho mọi người luôn).
- **`GET /api/Matches/{matchId}/seat-map`** — sơ đồ toàn bộ ghế của trận (Available/Held/Booked + giá vé), dùng để vẽ màn hình chọn ghế lúc mới tải trang.
- **`BookingsController`** (`/api/Bookings`, yêu cầu đăng nhập):
  - `POST /api/Bookings` — chốt đặt vé cho các ghế mình đang giữ qua Hub (body: `{ "matchId": 3, "seatIds": [1,2] }`). Kiểm tra ghế còn giữ hợp lệ, chưa bị đặt trùng, đã có giá vé — rồi tạo `Booking` + `BookingSeat`, xóa `SeatHold`, báo real-time `SeatBooked`.
  - `GET /api/Bookings/mine` — danh sách đơn của chính mình.
  - `GET /api/Bookings/{id}` — chi tiết 1 đơn (chỉ chủ đơn hoặc Admin xem được).
- **`wwwroot/seat-test.html`** — trang test trực quan, mở song song 2 tab để thấy ghế đồng bộ real-time giữa 2 người dùng.

### Cách test bằng trang seat-test.html (khuyên dùng — trực quan nhất)
1. Chạy app (F5), mở trình duyệt tới `https://localhost:{port}/seat-test.html` (thay `{port}` bằng port hiện tại, ví dụ 53602).
2. Lấy JWT token: gọi `POST /api/auth/login` bằng Swagger (`/swagger`) với tài khoản Admin hoặc tài khoản User bạn đã đăng ký ở task #9 → copy token (không kèm chữ "Bearer").
3. Dán token vào ô "JWT Token" trên trang test, để Match Id = 3 (đã có giá vé từ task #11), bấm **"Kết nối + Tải sơ đồ ghế"**.
4. Mở thêm 1 tab/trình duyệt khác (hoặc cửa sổ ẩn danh), lặp lại bước 2-3 nhưng đăng nhập bằng **tài khoản khác** (vd tài khoản `test2@matchday.local` đã đăng ký ở task #10).
5. Ở tab 1, bấm vào 1 ghế còn trống (màu xám) → ghế chuyển màu xanh dương (mình đang giữ) — đồng thời tab 2 phải thấy đúng ghế đó chuyển màu cam ("người khác đang giữ") gần như ngay lập tức, không cần tải lại trang.
6. Ở tab 2, thử bấm vào đúng ghế tab 1 đang giữ → phải không bấm được (bị khóa, xem log "HoldRejected").
7. Ở tab 1, bấm lại ghế đang giữ để nhả ra → tab 2 phải thấy ghế trở lại màu xám (trống) real-time.
8. Ở tab 1, chọn 1-2 ghế rồi bấm **"Đặt vé (Checkout)"** → phải báo "Đặt vé thành công" kèm mã đơn, đồng thời tab 2 thấy ghế đó chuyển màu xám đậm (đã bán — Booked), không bấm chọn được nữa.
9. (Test cơ chế tự nhả ghế hết hạn) Giữ 1 ghế rồi không đặt vé, đợi hơn 5 phút → ghế đó phải tự động nhả về trạng thái trống ở cả 2 tab mà không cần thao tác gì thêm.

### Test bằng Swagger (chỉ test được phần REST, không test được real-time)
1. `GET /api/Matches/3/seat-map` (không cần Authorize) → phải thấy toàn bộ ghế của sân, seat block có giá đã đặt ở task #11 hiện đúng `price`.
2. Vì Swagger không gọi được SignalR, để test `POST /api/Bookings` cần tạo `SeatHold` trước — cách nhanh nhất vẫn là dùng trang `seat-test.html` ở trên để giữ ghế, sau đó dùng đúng token đó gọi `POST /api/Bookings` trong Swagger cũng được (vì SeatHold đã được lưu trong DB).

## Task #13 — Tích hợp thanh toán VNPay sandbox

### Những gì đã thêm
- **`Services/VnPayLibrary.cs`** — hàm build URL + ký/kiểm tra chữ ký HMAC-SHA512 theo đúng chuẩn VNPay.
- **`Services/VnPayService.cs`** — dùng `VnPayLibrary` để tạo URL thanh toán cho 1 Booking, và validate dữ liệu VNPay trả về.
- **`PaymentsController`** (`/api/Payments`):
  - `POST /api/Payments/vnpay/create/{bookingId}` — (đăng nhập, chủ đơn) sinh URL thanh toán VNPay cho đơn đang Pending.
  - `GET /api/Payments/vnpay-return` — VNPay redirect trình duyệt user về đây sau khi thanh toán, hiện trang kết quả (thành công/thất bại) và cập nhật `Payment`/`Booking`.
  - `GET /api/Payments/vnpay-ipn` — VNPay server gọi thẳng vào để xác nhận (chỉ hoạt động khi app có domain public, tức từ task #26 trở đi — chạy `localhost` thì VNPay không gọi vào được, nhưng Return URL vẫn test được bình thường).

### 1. Cấu hình VNPay qua User Secrets (KHÔNG đưa vào appsettings)
Mở Package Manager Console tại project `MatchdayApi`, chạy:

```
dotnet user-secrets init
dotnet user-secrets set "VnPay:TmnCode" "<mã Terminal ID của bạn>"
dotnet user-secrets set "VnPay:HashSecret" "<Secret Key của bạn>"
```

Đây chính là mã TMN Code và Secret Key bạn đã lấy được lúc đăng ký tài khoản VNPay sandbox ở task #4 (trong email VNPay gửi, hoặc trong trang quản trị sandbox). Không paste 2 giá trị này vào bất kỳ file nào trong project — User Secrets lưu ở ngoài project, trên máy bạn, không bao giờ bị commit lên Git.

### 2. Test luồng thanh toán
1. Đăng nhập, tạo 1 Booking mới qua trang `seat-test.html` (task #12) — hoặc dùng lại 1 booking Pending cũ nếu còn.
2. Trong Swagger, Authorize bằng đúng token của chủ đơn đó → gọi `POST /api/Payments/vnpay/create/{bookingId}` → phải trả về `{ "paymentUrl": "https://sandbox.vnpayment.vn/..." }`.
3. Copy `paymentUrl` đó, dán vào 1 tab trình duyệt mới → phải thấy trang thanh toán VNPay sandbox (giống lúc bạn test sandbox ở task #4).
4. Chọn ngân hàng test (NCB) → nhập thông tin thẻ test theo tài liệu VNPay sandbox → hoàn tất thanh toán.
5. VNPay sẽ tự động redirect trình duyệt bạn về lại `https://localhost:{port}/api/Payments/vnpay-return` → phải thấy trang "Thanh toán thành công!" màu xanh, kèm đúng mã đơn và số tiền.
6. Gọi `GET /api/Bookings/{id}` (đúng Id booking vừa thanh toán) trong Swagger → `status` phải đã chuyển thành `"Paid"`.
7. (Test trường hợp hủy) Tạo thêm 1 booking khác, lặp lại bước 2-3, nhưng ở trang VNPay bấm **Hủy giao dịch** thay vì thanh toán → phải bị redirect về trang "Thanh toán không thành công" (màu đỏ), và booking đó vẫn giữ `status: "Pending"` (không bị đổi thành Paid).

## Task #13b — Chế độ giả lập VNPay (Mock mode)

Trường hợp chưa có (hoặc tạm thời mất) Mã website/Chuỗi bí mật VNPay sandbox thật, vẫn có thể test toàn bộ luồng đặt vé → thanh toán → vé bằng **chế độ giả lập**, không cần tài khoản VNPay nào cả.

### Những gì đã thêm
- **`Services/MockVnPayService.cs`** — bản giả của `IVnPayService`, không gọi VNPay thật, trả về link tới trang tự host `vnpay-mock.html`.
- **`wwwroot/vnpay-mock.html`** — trang mô phỏng màn hình thanh toán, có 2 nút "Giả lập thành công" / "Giả lập thất bại", tự tạo ra kết quả redirect đúng định dạng VNPay để `PaymentsController` xử lý y hệt như thật.
- **`appsettings.json`** — thêm `"VnPay": { "Mode": "Mock", ... }`. `Program.cs` đọc giá trị này: `Mode = "Mock"` (mặc định) dùng `MockVnPayService`, `Mode = "Real"` dùng `VnPayService` thật (cần TmnCode/HashSecret qua User Secrets như mục trên).

### Cách test (không cần cấu hình gì thêm — Mock là mặc định)
1. Đăng nhập, tạo 1 Booking mới qua `seat-test.html` (task #12).
2. Trong Swagger, Authorize bằng token chủ đơn → gọi `POST /api/Payments/vnpay/create/{bookingId}` → `paymentUrl` trả về giờ sẽ có dạng `https://localhost:{port}/vnpay-mock.html?...` (không phải link sandbox.vnpayment.vn).
3. Copy `paymentUrl`, mở tab mới → thấy trang "Mô phỏng cổng thanh toán" (tông màu tối/xanh giống các trang khác của project).
4. Bấm **"✅ Giả lập thanh toán THÀNH CÔNG"** → được redirect về trang "Thanh toán thành công!" màu xanh.
5. Gọi `GET /api/Bookings/{id}` → `status` phải đã chuyển `"Paid"`.
6. Test thêm trường hợp thất bại: tạo booking khác, lặp lại bước 2-3, lần này bấm **"❌ Giả lập thanh toán THẤT BẠI / Hủy"** → phải thấy trang lỗi màu đỏ, và booking vẫn giữ `status: "Pending"`.

### Khi nào chuyển sang VNPay thật
Khi có Mã website + Chuỗi bí mật thật (lấy lại được, hoặc đăng ký tài khoản sandbox mới): làm theo mục "1. Cấu hình VNPay qua User Secrets" ở trên để set `VnPay:TmnCode`/`VnPay:HashSecret`, sau đó đổi `"VnPay:Mode"` từ `"Mock"` thành `"Real"` trong `appsettings.Development.json` (không sửa `appsettings.json` gốc — file đó vẫn nên giữ `"Mode": "Mock"` làm mặc định an toàn khi người khác clone repo về mà chưa có tài khoản VNPay). Không cần sửa code nào khác — `Program.cs` tự chọn đúng service theo giá trị này.

## Task #14 — Sinh vé QR + email xác nhận

### Những gì đã thêm
- Package **QRCoder** (sinh ảnh QR PNG, offline, không cần internet/API key).
- **`Services/QrCodeService.cs`** — sinh ảnh QR (PNG bytes) từ 1 chuỗi bất kỳ.
- **`Services/TicketService.cs`** — khi Booking chuyển `Paid`, tự tạo 1 `Ticket` (vé điện tử) cho MỖI ghế trong đơn, mỗi vé có `TicketCode` riêng (dạng `<MãĐơn>-<Ghế>`, vd `BK260830185214447-A3`). An toàn khi gọi lại nhiều lần (không tạo trùng vé).
- **`Services/IEmailService.cs`** + 2 bản triển khai:
  - **`MockEmailService`** (mặc định) — không gửi email thật, chỉ lưu thành file HTML (kèm ảnh QR nhúng sẵn) tại `wwwroot/sent-emails/<MãĐơn>.html`.
  - **`SmtpEmailService`** — gửi email thật qua SMTP (vd Gmail), chỉ dùng khi `Email:Mode` = `"Real"`.
- **`Controllers/TicketsController`** (`/api/Tickets`):
  - `GET /api/Tickets/booking/{bookingId}` — danh sách vé của 1 đơn (chủ đơn/Admin).
  - `GET /api/Tickets/{ticketId}/qr` — tải ảnh QR (PNG) của 1 vé (chủ đơn/Admin).
- **`PaymentsController`** — khi thanh toán chuyển `Paid` (cả ở `VnPayReturn` lẫn `VnPayIpn`), tự gọi `TicketService` sinh vé + `EmailService` gửi email xác nhận.
- **`appsettings.json`** — thêm `"Email": { "Mode": "Mock", ... }` (mặc định Mock, giống VnPay:Mode).

### Cách test (Mock — không cần cấu hình gì thêm)
1. Lặp lại luồng thanh toán ở task #13b (tạo booking → tạo link thanh toán → bấm "Giả lập thành công").
2. Sau khi thấy trang "Thanh toán thành công!", vào Swagger gọi `GET /api/Tickets/booking/{bookingId}` (đúng id booking vừa thanh toán) → phải thấy danh sách vé, mỗi vé có `ticketCode` riêng theo từng ghế.
3. Copy `id` của 1 vé, gọi `GET /api/Tickets/{ticketId}/qr` trong Swagger → bấm "Download file" (hoặc mở link trực tiếp trên trình duyệt) → phải ra 1 ảnh QR.
4. Mở trình duyệt vào `https://localhost:{port}/sent-emails/{bookingCode}.html` (thay đúng port + mã đơn) → phải thấy 1 trang mô phỏng email xác nhận, có đủ thông tin trận đấu + từng vé kèm ảnh QR.

### Khi nào chuyển sang gửi email thật (Gmail)
1. Vào Gmail → bật xác minh 2 bước → tạo **App Password** (mật khẩu ứng dụng riêng, KHÔNG phải mật khẩu Gmail thường — Google không cho SmtpClient dùng mật khẩu thường).
2. Chạy trong Package Manager Console tại project `MatchdayApi`:
```
dotnet user-secrets set "Email:Username" "<email Gmail của bạn>"
dotnet user-secrets set "Email:Password" "<App Password 16 ký tự>"
```
3. Đổi `"Email:Mode"` từ `"Mock"` thành `"Real"` trong `appsettings.Development.json` (không sửa `appsettings.json` gốc). Test lại luồng thanh toán — lần này email sẽ được gửi thật tới hộp thư của user đặt vé.

## Task #16 — Module menu đồ ăn/thức uống

Không cần migration mới — bảng `FoodItems` và dữ liệu mẫu (6 món: Bắp rang bơ, Nước ngọt, Combo, Xúc xích, Bia lon, Nước suối) đã có sẵn từ task #7. Chỉ thêm Controller + DTO.

### Endpoint mới (`/api/FoodItems`)
- `GET /api/FoodItems?onlyAvailable=true` — công khai. `onlyAvailable=true` chỉ lấy món đang bán (dùng cho khách chọn đồ ăn); để trống/`false` lấy tất cả kể cả món đã ngừng bán (dùng cho trang quản lý Admin).
- `GET /api/FoodItems/{id}` — công khai.
- `POST` / `PUT /{id}` — chỉ Admin.
- `DELETE /{id}` — chỉ Admin. Nếu món đã từng được đặt trong 1 đơn vé nào đó, DB sẽ chặn xóa (409 Conflict) — lúc đó nên `PUT` để set `isAvailable: false` (ngừng bán) thay vì xóa hẳn, tránh làm hỏng lịch sử đơn hàng cũ.

### Test nhanh bằng Swagger
1. `GET /api/FoodItems` (không cần đăng nhập) → phải thấy 6 món mẫu.
2. `GET /api/FoodItems?onlyAvailable=true` → vẫn 6 món (vì mặc định `IsAvailable = true`).
3. Đăng nhập Admin → `PUT /api/FoodItems/6` với body `{ "name": "Nước suối", "price": 15000, "isAvailable": false }` → `GET /api/FoodItems?onlyAvailable=true` → phải chỉ còn 5 món (Nước suối biến mất khỏi danh sách "đang bán").
4. `POST /api/FoodItems` thêm 1 món mới (Admin) → phải trả 201 Created.

## Task #17 — Thêm đồ ăn vào đơn, thanh toán chung với vé

Không cần migration mới. Chỉ thêm 1 endpoint mới + mở rộng `BookingDto` có sẵn.

### Endpoint mới
- `PUT /api/Bookings/{id}/food-items` — chỉ chủ đơn, chỉ áp dụng khi đơn còn `Pending`. Body:
```json
{ "items": [ { "foodItemId": 1, "quantity": 2 }, { "foodItemId": 3, "quantity": 1 } ] }
```
Gọi lại nhiều lần sẽ **THAY THẾ toàn bộ** giỏ đồ ăn cũ (không cộng dồn) — gửi `"items": []` để xóa hết đồ ăn đã chọn. Tự tính lại `Booking.TotalAmount = tiền ghế + tiền đồ ăn`.

`BookingDto` (trả về từ `GET /api/Bookings/{id}`, `GET /api/Bookings/mine`, và chính endpoint này) giờ có thêm:
- `seatsAmount`, `foodAmount` — tách riêng tiền vé / tiền đồ ăn để hiển thị breakdown.
- `foodItems: [{ foodItemId, name, quantity, unitPrice, subtotal }]`.

**Không cần sửa `PaymentsController`** — vì `POST /api/Payments/vnpay/create/{bookingId}` đã dùng thẳng `booking.TotalAmount`, nên tiền đồ ăn tự động được cộng vào số tiền thanh toán VNPay.

### Test nhanh bằng Swagger
1. Tạo 1 Booking mới qua `seat-test.html` (như các task trước) → lấy `bookingId`.
2. `PUT /api/Bookings/{bookingId}/food-items` với body ở trên (dùng đúng `foodItemId` có trong `GET /api/FoodItems`) → response phải thấy `foodAmount` > 0 và `totalAmount` = tiền ghế + tiền đồ ăn.
3. `GET /api/Bookings/{bookingId}` → xác nhận `foodItems` hiện đúng danh sách vừa đặt.
4. `POST /api/Payments/vnpay/create/{bookingId}` → mở `paymentUrl` (trang giả lập) → phải thấy đúng **tổng tiền đã gồm cả đồ ăn**.
5. Test thay thế giỏ hàng: gọi lại `PUT .../food-items` với danh sách khác (hoặc `"items": []`) → `totalAmount` phải cập nhật lại đúng.
6. Test lỗi: gọi `PUT .../food-items` sau khi đơn đã `Paid` → phải bị `400 Bad Request`.

## Task #18 — Demo Giai đoạn 2 (đồ ăn/thức uống)

Test lại luồng: đặt ghế → `PUT .../food-items` thêm đồ ăn → thanh toán → vé + email. Phát hiện thiếu sót: email xác nhận (cả Mock lẫn Real) lúc đó chưa hiện danh sách đồ ăn đã đặt, dù tổng tiền đã đúng — đã vá lại `MockEmailService.cs` và `SmtpEmailService.cs`: thêm dòng "Tiền vé" / "Tiền đồ ăn/thức uống" tách riêng trong phần tổng, và 1 khối liệt kê từng món (tên x số lượng = thành tiền) — chỉ hiện khối này nếu đơn có đặt đồ ăn.

### Test lại (nên tạo booking MỚI để thấy email bản đã vá — booking cũ trả về email cũ không tự cập nhật)
1. Tạo booking mới (seat-test.html) → `PUT /api/Bookings/{id}/food-items` thêm 1-2 món → `POST /api/Payments/vnpay/create/{id}` → thanh toán giả lập thành công.
2. Mở `https://localhost:{port}/sent-emails/{bookingCode}.html` → phải thấy dòng "Tiền vé", "Tiền đồ ăn/thức uống" tách riêng, và khối "Đồ ăn/thức uống đã đặt" liệt kê đúng từng món + thành tiền, bên cạnh vé QR như cũ.

## Task #19 — Module hủy vé/hoàn tiền

Không cần migration mới — bảng `Refunds` + enum `RefundStatus` (Pending/Approved/Rejected) và `BookingStatus.Cancelled`/`Refunded` đã có sẵn từ ERD ban đầu (task #6/#8).

### Endpoint mới

**`BookingsController`** (`/api/Bookings`):
- `POST /api/Bookings/{id}/cancel` — chủ đơn, chỉ áp dụng khi đơn đang `Pending` (chưa thanh toán). Hủy ngay lập tức, giải phóng ghế để người khác đặt lại, báo real-time qua SignalR (`SeatReleased`) cho ai đang xem sơ đồ ghế trận đó.
- `POST /api/Bookings/{id}/refund-request` — chủ đơn, chỉ áp dụng khi đơn đã `Paid` và trận đấu chưa kết thúc. Body: `{ "reason": "..." }`. KHÔNG hoàn tiền/giải phóng ghế ngay — chỉ tạo 1 yêu cầu `Refund` ở trạng thái `Pending`, chờ Admin duyệt.

**`RefundsController`** (`/api/Refunds`):
- `GET /api/Refunds/mine` — danh sách yêu cầu hoàn tiền của chính mình.
- `GET /api/Refunds?status=Pending` — Admin, xem tất cả yêu cầu (lọc theo trạng thái nếu muốn).
- `POST /api/Refunds/{id}/approve` — Admin duyệt: đơn chuyển `Refunded`, ghế được giải phóng thật (xóa `BookingSeat`, tự cascade xóa `Ticket` liên quan), báo real-time `SeatReleased`.
- `POST /api/Refunds/{id}/reject` — Admin từ chối: đơn giữ nguyên `Paid`, không đổi gì về ghế.

Lưu ý: hệ thống KHÔNG tự động hoàn tiền qua VNPay (sandbox demo không hỗ trợ hoàn tiền tự động) — Admin duyệt xong thì tự chuyển khoản lại cho khách thủ công ngoài hệ thống, hệ thống chỉ ghi nhận trạng thái.

### Test nhanh bằng Swagger
1. **Hủy đơn Pending**: tạo booking mới (chưa thanh toán) → `POST /api/Bookings/{id}/cancel` → `GET /api/Bookings/{id}` phải thấy `status: "Cancelled"` → mở `seat-map` của trận đó, ghế vừa hủy phải về lại `"Available"`.
2. **Yêu cầu hoàn tiền đơn đã Paid**: dùng 1 booking đã thanh toán → `POST /api/Bookings/{id}/refund-request` với body `{ "reason": "Bận việc đột xuất không đi xem được" }` → phải trả về `Refund` với `status: "Pending"`.
3. Đăng nhập Admin → `GET /api/Refunds?status=Pending` → phải thấy yêu cầu vừa tạo.
4. `POST /api/Refunds/{id}/approve` (dùng id của Refund, không phải Booking) → `GET /api/Bookings/{bookingId}` phải thấy `status: "Refunded"` → `seat-map` phải thấy ghế đó về lại `"Available"`.
5. Test từ chối: tạo thêm 1 yêu cầu khác → `POST /api/Refunds/{id}/reject` → booking phải vẫn giữ `"Paid"`.
6. Test chặn trùng: gửi 2 lần `refund-request` liên tiếp cho cùng 1 booking (lần 2 khi lần 1 còn Pending) → lần 2 phải bị `400 Bad Request`.

## Task #20 — Module đội hình + chỉ số cầu thủ

Model DB (Player, MatchTeamLineup, LineupPlayer, MatchResult, PlayerMatchStat) đã có sẵn từ lúc thiết
kế ERD ban đầu — task này chỉ thêm Controller + DTO, **không cần migration mới**, chỉ cần Build lại.

Các endpoint mới, tất cả nằm trong `MatchesController` (Swagger nhóm "Matches"):

### 1. Đội hình ra sân
- `GET /api/Matches/{matchId}/lineup` — công khai. Trả về đội hình 2 đội (`home`, `away`), đội nào
  chưa xếp thì trả `null`.
- `PUT /api/Matches/{matchId}/lineup/{teamId}` — Admin. `teamId` phải là đội nhà hoặc đội khách của
  trận. Body:
  ```json
  {
    "formation": "4-3-3",
    "players": [
      { "playerId": 1, "positionX": 50, "positionY": 95, "isStarting": true },
      { "playerId": 2, "positionX": 20, "positionY": 75, "isStarting": true }
    ]
  }
  ```
  Gọi lại nhiều lần sẽ **thay thế toàn bộ** đội hình cũ của đội đó (không cộng dồn). Tối đa 11 cầu
  thủ đá chính (`isStarting: true`), cầu thủ phải thuộc đúng đội `teamId`.

### 2. Kết quả trận đấu
- `GET /api/Matches/{matchId}/result` — công khai. Trả `404` nếu trận chưa có kết quả.
- `PUT /api/Matches/{matchId}/result` — Admin. Body gồm tỷ số + thống kê:
  ```json
  {
    "homeScore": 2, "awayScore": 1,
    "possessionHome": 55, "possessionAway": 45,
    "shotsHome": 10, "shotsAway": 7,
    "yellowCardsHome": 2, "yellowCardsAway": 3,
    "redCardsHome": 0, "redCardsAway": 0,
    "cornersHome": 5, "cornersAway": 4
  }
  ```
  Gọi lại sẽ ghi đè kết quả cũ (dùng khi cập nhật tỷ số theo thời gian thực hoặc sửa sai).

### 3. Chỉ số cầu thủ theo trận
- `GET /api/Matches/{matchId}/player-stats?teamId=` — công khai, `teamId` để lọc theo đội (bỏ trống
  = lấy cả 2 đội).
- `PUT /api/Matches/{matchId}/player-stats` — Admin. Body dạng danh sách, mỗi cầu thủ 1 dòng:
  ```json
  {
    "stats": [
      { "playerId": 1, "goals": 1, "assists": 0, "yellowCards": 0, "redCards": 0, "minutesPlayed": 90, "rating": 8.2 },
      { "playerId": 5, "goals": 0, "assists": 1, "yellowCards": 1, "redCards": 0, "minutesPlayed": 90, "rating": 7.5 }
    ]
  }
  ```
  Cầu thủ đã có chỉ số ở trận này thì bị ghi đè, chưa có thì tạo mới.

### Test nhanh (Swagger, cần token Admin cho các API PUT)
1. Lấy `teamId`, `playerId` có sẵn: `GET /api/Teams`, `GET /api/Players?teamId=...`.
2. Chọn 1 `matchId` bất kỳ: `GET /api/Matches`.
3. `PUT /api/Matches/{matchId}/lineup/{teamId}` cho đội nhà, rồi cho đội khách (2 lần gọi, 2 teamId
   khác nhau) → `GET /api/Matches/{matchId}/lineup` phải thấy đủ `home` và `away`.
4. `PUT /api/Matches/{matchId}/result` → `GET /api/Matches/{matchId}/result` phải khớp dữ liệu.
5. `PUT /api/Matches/{matchId}/player-stats` với vài cầu thủ → `GET /api/Matches/{matchId}/player-stats`
   phải thấy đúng, sắp theo số bàn thắng giảm dần.
6. Test validate: gọi lineup với `playerId` không thuộc `teamId` → phải bị `400`; gọi với 12 cầu thủ
   `isStarting: true` → phải bị `400`.

## Task #22 — Admin dashboard thống kê

Thêm Controller mới `AdminDashboardController` (route `/api/AdminDashboard`), toàn bộ endpoint
**chỉ Admin** mới gọi được (`[Authorize(Roles = "Admin")]` áp cho cả controller). Không cần migration
mới, chỉ dùng dữ liệu đã có sẵn (Bookings, Payments, BookingSeats, BookingFoodItems, Refunds, Tickets).

Quy ước: "doanh thu" trong toàn bộ các API này chỉ tính đơn đang ở trạng thái **Paid** — đơn đã
Cancelled/Refunded không tính vào doanh thu (vì tiền đã/sẽ được hoàn lại).

### 1. Tổng quan
`GET /api/AdminDashboard/overview` — trả về:
- `totalRevenue`, `seatRevenue`, `foodRevenue`: doanh thu tổng/vé/đồ ăn.
- `totalBookings`, `pendingBookings`, `paidBookings`, `cancelledBookings`, `refundedBookings`: số đơn
  theo từng trạng thái.
- `totalTicketsSold`: số vé đang hiệu lực (đơn Paid).
- `pendingRefundRequests`, `approvedRefunds`, `rejectedRefunds`, `totalRefundedAmount`: số liệu hoàn tiền.

### 2. Doanh thu theo trận đấu
`GET /api/AdminDashboard/revenue-by-match?take=20` — danh sách từng trận (chỉ tính trận đã có đơn
Paid), sắp theo doanh thu giảm dần: số vé bán, doanh thu vé, doanh thu đồ ăn, tổng doanh thu.

### 3. Doanh thu theo ngày
`GET /api/AdminDashboard/revenue-by-day?days=30` — doanh thu từng ngày trong N ngày gần nhất (dựa
trên thời điểm thanh toán thành công `Payment.PaidAt`), dùng để vẽ biểu đồ theo thời gian.

### 4. Món ăn/thức uống bán chạy
`GET /api/AdminDashboard/top-food-items?take=10` — sắp theo doanh thu giảm dần, gồm số lượng bán +
doanh thu từng món.

### Test nhanh (Swagger, cần token Admin)
1. Login Admin (`admin@matchday.local` / `Admin@123`), Authorize.
2. `GET /overview` → so khớp thủ công: đếm số đơn Paid hiện có, cộng `TotalAmount` lại xem có khớp
   `totalRevenue` không.
3. `GET /revenue-by-match` → xem trận đã test (matchId=3) có xuất hiện với số vé + doanh thu hợp lý.
4. `GET /top-food-items` → xem món đã đặt trước đó (vd "Combo Bắp + Nước", "Xúc xích nướng") có lên
   danh sách với đúng số lượng.
5. Test phân quyền: gọi bất kỳ endpoint nào bằng token **User thường** (không phải Admin) → phải bị
   `403 Forbidden`.

## Task #23 — Unit test (xUnit + Moq)

Đây là 1 **project mới** (`MatchdayApi.Tests`), nằm NGANG HÀNG với project `MatchdayApi` (cùng cấp
trong solution), không phải file thêm vào project cũ — nên cần thêm thủ công vào solution 1 lần.

### Công cụ dùng
- **xUnit** — framework viết test.
- **Moq** — giả lập (mock) `IHubContext<SeatSelectionHub>` (SignalR) khi test các API có broadcast
  real-time, không cần chạy SignalR thật.
- **EF Core InMemory** — giả lập database trong bộ nhớ cho mỗi test (không đụng SQL Server thật, mỗi
  test có 1 database riêng biệt, không ảnh hưởng lẫn nhau). Dữ liệu mẫu khai báo trong `SeedData.cs`
  (Admin, Teams, Players, Seats, Matches, FoodItems...) vẫn tự có sẵn trong mỗi InMemory database vì
  đó là 1 phần cấu hình model (`HasData`), không phải data thật trong SQL Server.

### Các file test (28 test case, chia theo controller/service)
- `Services/JwtTokenServiceTests.cs` — sinh token đúng claims (email, role), báo lỗi khi thiếu cấu hình Jwt:Key.
- `Services/QrCodeServiceTests.cs` — sinh ảnh PNG hợp lệ, nội dung khác nhau ra ảnh khác nhau.
- `Services/MockVnPayServiceTests.cs` — tạo đúng URL thanh toán giả lập, đọc đúng kết quả thành công/thất bại.
- `Services/TicketServiceTests.cs` — sinh đúng số vé theo số ghế, gọi lại không tạo trùng vé (idempotent).
- `Controllers/BookingsControllerTests.cs` — hủy vé chỉ khi Pending, chặn người không phải chủ đơn,
  gửi yêu cầu hoàn tiền chỉ khi Paid + trận chưa kết thúc + chưa có yêu cầu Pending khác.
- `Controllers/RefundsControllerTests.cs` — Admin duyệt thì giải phóng ghế + đổi Refunded, từ chối
  thì giữ nguyên Paid, không xử lý được yêu cầu đã xử lý rồi.
- `Controllers/MatchesControllerLineupTests.cs` — xếp đội hình hợp lệ, chặn cầu thủ sai đội, chặn đội
  không thi đấu trong trận, chặn quá 11 người đá chính, gọi lại thì thay thế đội hình cũ.
- `Controllers/AdminDashboardControllerTests.cs` — số liệu tổng quan tính đúng theo dữ liệu tự dựng.

### Cách thêm vào solution (làm 1 lần)

**Bước 1:** Giải nén zip, đặt thư mục `MatchdayApi.Tests` NGANG HÀNG với thư mục `MatchdayApi` (tức
là `D:\MatchdayApi\MatchdayApi.Tests\`, cùng cấp với `D:\MatchdayApi\MatchdayApi\`).

**Bước 2:** Trong Visual Studio, chuột phải vào **Solution 'MatchdayApi'** (dòng trên cùng Solution
Explorer) → **Add** → **Existing Project...** → chọn file `MatchdayApi.Tests\MatchdayApi.Tests.csproj`.

**Bước 3:** Build lại toàn bộ Solution (Ctrl+Shift+B) — NuGet sẽ tự tải các gói xUnit/Moq/EFCore.InMemory.

**Bước 4:** Mở **Test Explorer** (menu Test → Test Explorer, hoặc Ctrl+E, T) → bấm **Run All Tests**
(hoặc nút play ở đầu cửa sổ) → tất cả test phải chuyển xanh (Passed).

Nếu có test đỏ (Failed), chụp màn hình thông báo lỗi cụ thể của Test Explorer gửi lại để mình sửa.
