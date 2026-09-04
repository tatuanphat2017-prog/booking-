# Matchday — App đặt vé xem bóng đá trực tiếp

Ứng dụng đặt vé xem các trận bóng đá trực tiếp tại sân vận động: xem lịch thi đấu, chọn ghế theo thời gian thực, mua thêm đồ ăn/thức uống, thanh toán online qua VNPay, nhận vé điện tử QR. Có thêm dashboard thể thao hiển thị kết quả, đội hình ra sân và chỉ số cầu thủ sau mỗi trận, cùng 1 trang quản trị đầy đủ cho Admin.

> Dự án cá nhân phục vụ học tập / portfolio (CV) — Tạ Tuấn Phát, sinh viên CNTT chuyên ngành Công nghệ Phần mềm, HUTECH. Dữ liệu đội bóng, cầu thủ, trận đấu trong app là dữ liệu demo.

## Tính năng chính

### Tài khoản & phân quyền
- Đăng ký, đăng nhập bằng email/mật khẩu, cấp JWT token (mật khẩu băm bằng BCrypt)
- Phân quyền 2 vai trò: **User** (khách đặt vé) và **Admin** (quản trị toàn hệ thống)
- Trang hồ sơ tài khoản xem thông tin cá nhân, vai trò, ngày tham gia

### Trang chủ & lịch thi đấu
- Banner carousel lớn ở đầu trang cho các trận nổi bật sắp diễn ra/đang diễn ra (tự chuyển slide, có mũi tên/dot điều hướng, đếm ngược tới giờ bóng lăn, tạm dừng khi rê chuột vào)
- Dải "Đội bóng nổi bật" cuộn ngang, bấm vào 1 đội sẽ tự lọc lịch thi đấu của đội đó
- Danh sách trận sắp diễn ra + trận đã kết thúc, tìm theo tên đội bóng
- Bảng điện tử (scoreboard ticker) chạy chữ kiểu sân vận động thật

### Đặt vé & chọn ghế real-time
- Sơ đồ ghế theo từng khối (khán đài), hiển thị đúng giá vé từng khối
- Chọn/nhả ghế **real-time qua SignalR** (`SeatSelectionHub`) — nhiều người xem cùng 1 trận sẽ thấy ngay ghế nào vừa bị người khác giữ/nhả/mua, tránh 2 người cùng đặt trùng 1 ghế
- Ghế tự động nhả nếu giữ quá lâu không thanh toán (`SeatHoldCleanupService` chạy nền dọn ghế hết hạn)
- Mua kèm đồ ăn/thức uống ngay trong lúc đặt vé, chỉnh số lượng trực tiếp
- Thanh toán online qua **VNPay** (xem chi tiết luồng ở mục riêng bên dưới) — hỗ trợ cả chế độ giả lập (Mock, không cần tài khoản VNPay) lẫn chế độ thật (Real, đã kết nối tài khoản sandbox VNPay)
- Sau khi thanh toán thành công: tự sinh **vé điện tử kèm mã QR** cho từng ghế, gửi email xác nhận (giả lập hoặc gửi thật qua SMTP)

### Vé của tôi
- Xem danh sách đơn đã đặt, trạng thái (Chờ thanh toán / Đã thanh toán / Đã hủy / Đã hoàn tiền)
- Xem vé điện tử, tải ảnh mã QR
- Hủy đơn chưa thanh toán (nhả ghế ngay lập tức)
- Gửi yêu cầu hoàn tiền cho đơn đã thanh toán (chờ Admin duyệt)

### Trang trận đấu chi tiết
- Trận sắp/đang diễn ra: hiển thị đội hình dự kiến/ra sân (nếu Admin đã xếp), sơ đồ chọn ghế, giỏ đồ ăn, thanh toán
- Trận đã kết thúc: tỷ số, thống kê trận (kiểm soát bóng, dứt điểm, phạt góc, thẻ vàng/đỏ), đội hình ra sân 2 đội trên sân vẽ bằng tọa độ thật, danh sách cầu thủ nổi bật (ghi bàn/kiến tạo/điểm đánh giá cao)

### Trang quản trị (Admin)
- **Tổng quan**: doanh thu, số đơn theo trạng thái, số vé đã bán, số yêu cầu hoàn tiền, biểu đồ doanh thu theo ngày, doanh thu theo từng trận, món đồ ăn bán chạy nhất
- **Trận đấu**: tạo/sửa/xóa trận, đặt giá vé theo từng khối ghế, xếp đội hình ra sân (chọn sơ đồ chiến thuật có sẵn hoặc tự đặt vị trí từng cầu thủ trên sân), nhập kết quả + thống kê trận, nhập chỉ số từng cầu thủ
- **Đồ ăn**: quản lý menu đồ ăn/thức uống, bật/tắt trạng thái đang bán
- **Hoàn tiền**: xem danh sách yêu cầu hoàn tiền, duyệt (tự nhả ghế) hoặc từ chối
- **Dữ liệu nền**: quản lý sân vận động, đội bóng, cầu thủ, khối ghế + số lượng ghế từng khối

### Thiết kế giao diện
- Theme tối + vàng đồng, style "vé xem bóng đá thật" (thẻ trận có dáng cuống vé xé, viền đứt nét, vết khoét tròn)
- Chuyển trang mượt bằng View Transitions API
- Responsive cơ bản, tôn trọng `prefers-reduced-motion`

## Luồng thanh toán VNPay (chi tiết)

1. Đặt vé xong → `Booking` tạo ở trạng thái `Pending`, ghế đã chốt tạm cho người đặt.
2. Frontend gọi `POST /api/Payments/vnpay/create/{bookingId}` → server ký (bằng `HashSecret`) và trả về URL thanh toán VNPay.
3. Trình duyệt chuyển sang trang VNPay (sandbox thật hoặc trang giả lập nội bộ, tùy `VnPay:Mode`), người dùng nhập thẻ test/OTP.
4. VNPay báo kết quả về theo 2 đường song song:
   - **Return URL** (`GET /api/Payments/vnpay-return`) — trình duyệt người dùng được đưa về đây, hiển thị màn kết quả ngay.
   - **IPN** (`GET /api/Payments/vnpay-ipn`) — server VNPay tự gọi thẳng vào server (đáng tin hơn vì không phụ thuộc trình duyệt người dùng); **chỉ hoạt động khi server có domain public**, không chạy được khi test trên `localhost`.
5. Thanh toán thành công → `Booking` chuyển `Paid` → tự sinh vé QR (`TicketService`) + gửi email xác nhận, chống sinh vé trùng nếu cả Return lẫn IPN cùng báo về.

Công tắc `VnPay:Mode` trong `appsettings.json` chọn `Mock` (dùng `MockVnPayService`, tự dựng trang giả lập, không cần tài khoản VNPay — mặc định khi chưa cấu hình) hoặc `Real` (dùng `VnPayService`, gọi đúng API VNPay sandbox thật, cần `TmnCode`/`HashSecret` lưu qua User Secrets, không hardcode vào file cấu hình).

## Tech stack

**Backend:** ASP.NET Core 8 Web API (C#) · SQL Server + EF Core 8 (Code First, Migrations) · JWT Bearer Authentication · BCrypt.Net (băm mật khẩu) · SignalR (real-time khóa ghế) · QRCoder (sinh mã QR vé) · Swashbuckle (Swagger)

**Thanh toán:** VNPay (chế độ Mock nội bộ để test, hoặc Real qua tài khoản sandbox VNPay thật)

**Email:** Gửi giả lập (Mock, lưu file HTML mô phỏng email) hoặc gửi thật qua SMTP

**Frontend:** HTML / CSS / JS thuần (không framework), gọi API qua `fetch`, SignalR JS client cho real-time chọn ghế

**Testing:** xUnit + Moq — unit test cho các controller/service quan trọng (đặt vé, hoàn tiền, thống kê admin, đội hình, JWT, VNPay mock, sinh QR, sinh vé)

**CI:** GitHub Actions — tự động build + chạy test mỗi khi push/tạo Pull Request vào nhánh `main`

**Deploy:** MonsterASP (chưa triển khai — đang chạy local để phát triển/demo)

## Danh sách API chính (nhóm theo chức năng)

| Nhóm | Endpoint tiêu biểu | Ghi chú |
|---|---|---|
| Auth | `POST /api/Auth/register`, `/login`, `GET /api/Auth/me` | Đăng ký/đăng nhập, lấy hồ sơ |
| Dashboard | `GET /api/Dashboard/upcoming`, `/finished`, `/matches/{id}` | Dữ liệu gộp sẵn cho trang chủ/chi tiết trận, công khai |
| Trận đấu | `GET/POST/PUT/DELETE /api/Matches`, `/{id}/seat-map`, `/{id}/lineup`, `/{id}/result`, `/{id}/player-stats` | CRUD trận, sơ đồ ghế, đội hình, kết quả, chỉ số cầu thủ |
| Đặt vé | `POST /api/Bookings`, `GET /api/Bookings/mine`, `PUT /{id}/food-items`, `POST /{id}/cancel`, `POST /{id}/refund-request` | Chốt đơn từ ghế đang giữ, sửa giỏ đồ ăn, hủy/yêu cầu hoàn tiền |
| Thanh toán | `POST /api/Payments/vnpay/create/{bookingId}`, `GET /vnpay-return`, `GET /vnpay-ipn` | Luồng VNPay như mô tả ở trên |
| Vé điện tử | `GET /api/Tickets/booking/{bookingId}`, `/{ticketId}/qr` | Xem vé + tải ảnh QR |
| Hoàn tiền | `GET /api/Refunds/mine`, `GET /api/Refunds` (Admin), `POST /{id}/approve`, `/reject` | User xem của mình, Admin duyệt/từ chối |
| Đồ ăn | `GET/POST/PUT/DELETE /api/FoodItems` | Menu đồ ăn/thức uống |
| Dữ liệu nền | `/api/Teams`, `/api/Players`, `/api/Stadiums`, `/api/SeatBlocks`, `/api/Seats` | CRUD, hầu hết GET công khai, ghi phải Admin |
| Thống kê Admin | `GET /api/AdminDashboard/overview`, `/revenue-by-match`, `/revenue-by-day`, `/top-food-items` | Chỉ Admin |

Toàn bộ endpoint đọc dữ liệu công khai (lịch thi đấu, đội bóng, cầu thủ...) đều không cần đăng nhập; các endpoint tạo/sửa/xóa dữ liệu nền và quản trị đều yêu cầu `[Authorize(Roles = "Admin")]`.

## Trạng thái

Đã hoàn thành đầy đủ luồng nghiệp vụ chính: đăng ký/đăng nhập → xem lịch thi đấu → chọn ghế real-time → mua đồ ăn kèm → thanh toán VNPay (đã test cả Mock lẫn Real qua sandbox thật) → nhận vé QR → xem/hủy vé → yêu cầu và duyệt hoàn tiền → trang quản trị đầy đủ 5 khu vực → giao diện trang chủ có hero carousel + dải đội bóng nổi bật.

Còn lại trước khi triển khai chính thức:
- [ ] Deploy lên MonsterASP (đổi biến môi trường cho chuỗi kết nối, JWT key, VNPay TmnCode/HashSecret — không đưa giá trị thật vào file cấu hình commit lên Git)
- [ ] Cấu hình IPN URL thật (cần domain public, không chạy được trên localhost)
- [ ] Rà lại toàn diện responsive trên di động cho các trang admin (bảng dữ liệu nhiều cột)

## Cách chạy

```bash
# clone
git clone https://github.com/tatuanphat2017-prog/booking-.git
cd booking-/MatchdayApi/MatchdayApi

# cấu hình chuỗi kết nối SQL Server trong appsettings.json (ConnectionStrings:DefaultConnection)
# và JWT key (Jwt:Key) — 2 giá trị này nên để trống trong file, set qua User Secrets khi chạy local:
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<chuỗi kết nối SQL Server của bạn>"
dotnet user-secrets set "Jwt:Key" "<chuỗi bí mật bất kỳ, đủ dài>"

# nếu muốn bật thanh toán VNPay thật (mặc định đang chạy Mock, không cần bước này):
dotnet user-secrets set "VnPay:TmnCode" "<TmnCode từ tài khoản merchant sandbox VNPay>"
dotnet user-secrets set "VnPay:HashSecret" "<HashSecret từ tài khoản merchant sandbox VNPay>"
# rồi đổi "VnPay:Mode" thành "Real" trong appsettings.json

# chạy migration để tạo database
dotnet ef database update

# chạy project
dotnet run
```

Frontend nằm sẵn trong `wwwroot/`, được serve trực tiếp bởi chính API (mở `https://localhost:<port>/index.html` sau khi chạy `dotnet run`).

## Chạy test

```bash
dotnet test MatchdayApi.Tests/MatchdayApi.Tests.csproj
```

## Tác giả

Tạ Tuấn Phát — Sinh viên CNTT, chuyên ngành Công nghệ Phần mềm, HUTECH
