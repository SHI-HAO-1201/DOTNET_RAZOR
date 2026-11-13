# Cơ sở dữ liệu MotoSpeedDb

`AppDbContext`, các migration và model xe dùng Entity Framework Core với SQL
Server. Cả MyRazorXe_Xemay, MyMvcApp và BlazorXe_may đang cấu hình cùng
database `MotoSpeedDb` trên máy chủ `localhost`; thông tin đăng nhập nằm trong
User Secrets, không nằm trong source code.

## Migrations và an toàn dữ liệu

Khi ứng dụng khởi động, EF Core gọi `Database.MigrateAsync()`. EF chỉ áp dụng
migration chưa có trong `__EFMigrationsHistory`; MyRazor đã được khởi động và
xác nhận database hiện có đủ migration, không cần tạo lại database. Không dùng
`EnsureDeleted`, `EnsureCreated` hoặc xóa database để cập nhật schema.

Để kiểm tra cấu hình EF trước khi chạy `dotnet ef`, xem User Secrets của project
hoặc chạy `dotnet user-secrets list`. Không đưa mật khẩu vào `appsettings.json`.

## Dữ liệu xe và ảnh

- MVC, Razor và Blazor cùng đọc/ghi trực tiếp `dbo.XeMays` trong `MotoSpeedDb`.
  Ba project hiện được cấu hình cùng máy chủ và database qua User Secrets.
- MVC/Razor ánh xạ các trường sản phẩm sang schema Blazor: tên, hãng, loại,
  giá bán, mô tả (`MoTa`), ảnh, mã xe, tồn kho và trạng thái kinh doanh.
  Giá trong hai ứng dụng cũ là số nguyên VND; giá bán có phần thập phân được
  làm tròn khi đọc. Trạng thái còn hàng/đang nhập được suy ra từ tồn kho.
- `Power` và `Gear` không tồn tại trong `XeMays`; màn hình quản trị MVC/Razor
  không còn cho sửa các trường này. Trường mô tả lưu vào `MoTa`.
- Migration `UseSharedXeMays` nhập các mã xe còn thiếu, chuyển khóa ngoại của
  đơn hàng/đánh giá/yêu cầu liên hệ theo mã xe và bỏ trigger đồng bộ hai bảng.
  Migration giữ nguyên `dbo.Motorcycles` làm dữ liệu lưu trữ cũ; các ứng dụng
  không còn đọc hoặc ghi bảng đó.
- Nhập URL ảnh ở trang quản trị. Database lưu URL trong `HinhAnh`; nếu dùng ảnh
  cục bộ, lưu file trong `wwwroot/images` và đường dẫn `/images/ten-anh.jpg`.

## Tạo tài khoản quản trị dùng chung

Từ thư mục từng project, chạy:

```powershell
dotnet run --no-launch-profile -- --create-admin
```

Nhập cùng email và mật khẩu để tạo/cập nhật một tài khoản trong các bảng
`AspNetUsers`, `AspNetRoles` và `AspNetUserRoles`. Tài khoản được gán cả vai trò
`Admin` và `QuanTriVien`, dùng đăng nhập được ở cả ba ứng dụng.

## Tạo migration khi cần đổi schema

Chỉ tạo migration sau khi thay đổi model EF:

```powershell
dotnet ef migrations add TenMigration
dotnet ef database update
```

Kiểm tra connection string đang trỏ đúng `MotoSpeedDb` trước khi chạy lệnh.
