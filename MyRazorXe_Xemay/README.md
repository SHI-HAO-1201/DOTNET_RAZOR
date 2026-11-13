# MOTOHUB

## Tài khoản và API

Tài khoản được lưu trong `App_Data/accounts.json`; mật khẩu được băm bằng ASP.NET Core `PasswordHasher`. Cookie xác thực có thời hạn 8 giờ. Thư mục chứa tài khoản và khóa Data Protection đã được thêm vào `.gitignore`.

Có thể cấu hình các đường dẫn và email quản trị bằng biến môi trường:

- `Auth__UsersFile`: đường dẫn tệp tài khoản.
- `Auth__KeysDirectory`: thư mục lưu khóa cookie.
- `Auth__AdminEmail`: email được cấp vai trò quản trị khi đăng ký/đăng nhập.

API dùng cookie xác thực:

- `POST /api/auth/register` — JSON: `fullName`, `email`, `phone`, `password`.
- `POST /api/auth/login` — JSON: `email`, `password`.
- `GET /api/auth/me` — trả hồ sơ của phiên hiện tại; cần đăng nhập.
- `POST /api/auth/logout` — kết thúc phiên; cần đăng nhập.

Đây là kho JSON phù hợp chạy một máy/đơn vị triển khai. Khi đưa lên môi trường nhiều máy hoặc cần sao lưu, nên chuyển `AccountStore` sang cơ sở dữ liệu dùng chung.

## Tạo tài khoản quản trị

Trong PowerShell, mở thư mục dự án và chạy:

```powershell
dotnet run --no-launch-profile -- --create-admin
```

Nhập họ tên, email và mật khẩu khi được hỏi. Mật khẩu không hiển thị khi nhập. Lệnh tạo tài khoản quản trị mới hoặc đặt lại mật khẩu và cấp quyền quản trị cho tài khoản cùng email đã có. Sau đó chạy ứng dụng như bình thường, đăng nhập bằng tài khoản đó và hệ thống sẽ chuyển thẳng tới `/Admin`.
