# Matchday — App đặt vé xem bóng đá trực tiếp

Ứng dụng đặt vé xem các trận bóng đá trực tiếp tại sân vận động: xem lịch thi đấu, chọn ghế theo thời gian thực, mua thêm đồ ăn/thức uống, thanh toán online, nhận vé điện tử QR. Có thêm dashboard thể thao hiển thị kết quả, đội hình và chỉ số cầu thủ sau mỗi trận.

> Dự án cá nhân phục vụ học tập / portfolio (CV). Dữ liệu đội bóng, cầu thủ trong app là dữ liệu demo.

## Tính năng chính

- Đăng ký / đăng nhập, phân quyền Admin - User (JWT)
- Dashboard: trận sắp diễn ra, đang diễn ra, đã kết thúc
- Đặt vé: chọn hạng ghế, chọn chỗ ngồi real-time (khóa ghế khi đang chọn)
- Mua đồ ăn/thức uống kèm vé
- Thanh toán online (VNPay sandbox)
- Vé điện tử kèm mã QR, check-in tại cổng
- Hủy vé / yêu cầu hoàn tiền
- Trang kết quả trận đấu: tỷ số, đội hình ra sân, chỉ số cầu thủ, thống kê trận
- Trang quản trị (Admin): quản lý đội bóng, cầu thủ, trận đấu, sân, menu đồ ăn, đơn hàng, thống kê doanh thu

## Tech stack

**Backend:** ASP.NET Core Web API (C#) · SQL Server + EF Core (Code First, Migrations) · JWT Bearer · SignalR (real-time khóa ghế) · AutoMapper · FluentValidation · Swashbuckle (Swagger) · Serilog

**Thanh toán:** VNPay Sandbox

**Frontend:** HTML / CSS / JS thuần, gọi API qua fetch

**Testing / CI:** xUnit + Moq · GitHub Actions

**Deploy:** MonsterASP

## Trạng thái

🚧 Đang phát triển — xem tiến độ chi tiết trong task list của dự án.


- [ ] Giai đoạn 1 — MVP đặt vé (auth, chọn ghế real-time, thanh toán, vé QR)
- [ ] Giai đoạn 2 — Module đồ ăn / thức uống
- [ ] Giai đoạn 3 — Module thể thao (đội hình, chỉ số cầu thủ) + hủy vé/hoàn tiền
- [ ] Hoàn thiện — thống kê admin, unit test, CI, deploy

## Cách chạy (sẽ cập nhật khi có code)

```bash
# clone
git clone https://github.com/tatuanphat2017-prog/booking-.git

# cấu hình chuỗi kết nối SQL Server trong appsettings.json
# chạy migration
dotnet ef database update

# chạy project
dotnet run
```

## Tác giả

Tạ Tuấn Phát — Sinh viên CNTT, chuyên ngành Công nghệ Phần mềm, HUTECH
