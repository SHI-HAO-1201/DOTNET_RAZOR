# Ghi chú luồng nút và điều hướng — Razor Pages

Razor Pages dùng link `asp-page` để chuyển sang Page khác; form `method="post"` gửi về `OnPostAsync` trong PageModel nếu PageModel có handler. Một số nút gọi JavaScript, rồi JavaScript mới gọi API trong `Program.cs`. Nút giao diện không mặc định đồng nghĩa có PageModel xử lý.

## Điều hướng chung và mua hàng

| Nút/thao tác | Đích/handler | File tiếp theo |
|---|---|---|
| Logo/trang chủ | `/` | `Pages/Dashboard/Index.cshtml` và `Index.cshtml.cs` |
| Bộ sưu tập | `/Collection/Collection` | `Pages/Collection/Collection.cshtml` và `Collection.cshtml.cs` (đọc danh mục xe) |
| Ưu đãi | `/Offer/Offers` | `Pages/Offer/Offers.cshtml` và `Offers.cshtml.cs` |
| Tạp chí / showroom / liên hệ | `asp-page` | `Pages/Megazine/Megazine.cshtml`, `Pages/Showroom/Showroom.cshtml`, `Pages/Contact/Contact.cshtml` |
| Nút menu trên điện thoại | `#menuToggle` | JavaScript `wwwroot/js/site.js`, mở/đóng menu; không gọi server |
| Giỏ hàng | `/Cart` | `Pages/Cart/Cart.cshtml` |
| Thêm xe vào giỏ | `.add-cart` | `wwwroot/js/site.js`, lưu giỏ trong `localStorage`; không ghi đơn vào database |
| Tăng/giảm số lượng hoặc xóa dòng giỏ | `button[data-action]` được tạo bởi JS | `wwwroot/js/site.js`, cập nhật `localStorage` |
| Tiến hành thanh toán | nút `#checkout` | `wwwroot/js/site.js` chuyển tới `/Checkout` |
| Gửi đơn | POST form checkout | `Pages/Cart/Checkout.cshtml.cs` → `OnPostAsync`: kiểm tra giỏ, tồn kho, khuyến mãi; tạo khách hàng/đơn/chi tiết, cập nhật tồn kho và lưu bằng EF Core |
| Theo dõi đơn sau khi đặt | `/Account/Orders` | `Pages/Account/Orders.cshtml.cs` → `OnGetAsync` |
| Tư vấn xe | link từ bộ sưu tập | dẫn tới `Pages/Contact/Contact.cshtml`; form liên hệ hiện được JS chặn và chỉ hiện kết quả demo, chưa lưu database |
| Sao chép mã ưu đãi | `.copy-voucher` | `wwwroot/js/site.js`, chép mã vào clipboard; không gọi server |

## Tài khoản

- **Đăng ký:** `Pages/Account/Register.cshtml` POST → `Pages/Account/Register.cshtml.cs` `OnPostAsync` → `AccountStore` → database.
- **Đăng nhập:** `Pages/Account/Login.cshtml` POST → `Pages/Account/Login.cshtml.cs` `OnPostAsync` → xác thực, tạo cookie đăng nhập.
- **Hiện/ẩn mật khẩu:** nút `data-password-toggle` trong Login/Register → `wwwroot/js/password-toggle.js`; chỉ đổi hiển thị input.
- **Quên mật khẩu:** `Pages/Account/ForgotPassword.cshtml` form không có PageModel POST; `wwwroot/js/site.js` chặn submit và hiện thông báo demo, chưa gửi email hoặc đổi mật khẩu.
- **Hồ sơ:** `Pages/Account/Profile.cshtml` POST → `Profile.cshtml.cs` `OnPostAsync`, cập nhật hồ sơ database.
- **Đơn hàng của tôi:** link `/Account/Orders` → `Orders.cshtml.cs` `OnGetAsync` lọc theo người dùng hiện tại.
- **Đăng xuất:** form POST `/Account/Logout` → `Pages/Account/Logout.cshtml.cs` `OnPostAsync` → xóa cookie rồi redirect.
- Link đăng nhập/đăng ký/hồ sơ/đơn hàng ở header/footer nằm trong `Pages/Shared/_Layout.cshtml`.

## Admin — nút trên giao diện và API đích

`Pages/Admin/Admin.cshtml.cs` chỉ xử lý `OnGetAsync` để tải trang và số liệu cơ bản. Các tương tác admin còn lại chạy qua JavaScript `wwwroot/js/admin.js` và các API được khai báo trực tiếp trong `Program.cs`; chúng không đi qua PageModel tương ứng.

| Nút/giao diện | JavaScript / API | Điểm xử lý server |
|---|---|---|
| Chọn Tổng quan/Sản phẩm/Đơn hàng/Khách hàng/Ưu đãi/Nội dung/Báo cáo/Cài đặt | `[data-panel]`, `[data-goto]` | `wwwroot/js/admin.js`, chỉ chuyển panel trong cùng `Pages/Admin/Admin.cshtml` |
| Thêm/sửa/lưu/ẩn-hiện sản phẩm | `[data-open-product]`, `[data-edit]`, `[data-toggle-product]` | `admin.js` → `/api/admin/products` GET/POST/DELETE; endpoint ở `Program.cs` trong nhóm `adminApi` |
| Tải CSV sản phẩm | `#exportProducts` | `admin.js` điều hướng `/api/admin/products/export` → endpoint CSV trong `Program.cs` |
| Đơn gần đây, danh sách và báo cáo | `loadAll()` | `admin.js` gọi `/api/admin/dashboard` và `/api/admin/orders` GET → `Program.cs` |
| Đổi trạng thái đơn | `select[data-order-status]` change | `admin.js` PUT `/api/admin/orders/{id}/status` → `Program.cs`; cập nhật đơn và tồn kho |
| Xóa đơn | `[data-delete-order]` | `admin.js` DELETE `/api/admin/orders/{id}` → `Program.cs`; hoàn kho nếu cần rồi xóa |
| Thêm/sửa khách hàng | `#addCustomer`, submit `#customerForm`, `[data-edit-customer]` | `admin.js` POST `/api/admin/customers` → `Program.cs` |
| Xóa khách hàng | `[data-delete-customer]` | `admin.js` DELETE `/api/admin/customers/{id}` → `Program.cs` |
| Tạo ưu đãi | `#addOffer` | `admin.js` POST `/api/admin/promotions` → `Program.cs` |
| Bật/tạm dừng ưu đãi | `[data-toggle-promotion]` | `admin.js` PATCH `/api/admin/promotions/{id}` → `Program.cs` |
| Lưu nội dung/cài đặt | `#saveContent`, `#saveSettings` | `admin.js` PUT `/api/admin/settings` → `Program.cs` |
| Tải CSV báo cáo | `#exportReport` | `admin.js`; hiện tạo/tải báo cáo từ dữ liệu đã nạp phía trình duyệt, không có endpoint export report riêng |
| Tìm/lọc sản phẩm và đơn | `#productSearch`, `#orderSearch`, `#orderFilter` | `admin.js`, lọc DOM phía trình duyệt |
| Đóng/hủy dialog, lưu dialog | `method="dialog"` | Trình duyệt đóng dialog; lưu sản phẩm/khách hàng được bắt trong `admin.js` rồi gọi API, không POST đến Razor Page |

Link vào admin `/Admin` ở `Pages/Shared/_Layout.cshtml` yêu cầu vai trò Admin. Các endpoint `/api/admin/...` cũng yêu cầu quyền Admin. Link đăng xuất ở layout POST tới Razor Page Logout.

## Cách lần theo nút trong IDE

1. Tìm nút hoặc form trong file `.cshtml`.
2. Nếu có `asp-page`, mở Page được chỉ định; nếu form POST mặc định, tìm `OnPostAsync` trong `.cshtml.cs` cùng tên.
3. Nếu nút có `id`, class hoặc `data-*`, tìm selector đó trong `wwwroot/js/*.js`.
4. Nếu JavaScript gọi `/api/...`, tìm `MapGet/MapPost/MapPut/MapPatch/MapDelete` tương ứng trong `Program.cs`.

## Khi chạy `dotnet watch`: thứ tự khởi động

Chạy lệnh từ thư mục `MyRazorXe_Xemay`:

```powershell
dotnet watch
```

1. `dotnet watch` build và theo dõi project; entry point top-level đầu tiên của ứng dụng là `Program.cs`.
2. `Program.cs` đọc connection string, đăng ký `AppDbContext`, Razor Pages, account services, cookie authentication, authorization và rate limiter.
3. Sau `builder.Build()`, app tạo scope và gọi `AppDbContext.Database.MigrateAsync()` để áp dụng migration còn thiếu.
4. App cấu hình middleware rồi map API auth, API storefront/admin và `MapRazorPages()`.
5. Browser GET `/` → Page `/` là `Pages/Dashboard/Index.cshtml` (có `Index.cshtml.cs`) → `Pages/_ViewStart.cshtml` chọn `Pages/Shared/_Layout.cshtml`.
6. Layout render header/footer, nội dung Page tại `@RenderBody()` và nạp `wwwroot/js/site.js` cùng các script theo trang.

`dotnet watch` không chọn Page đầu tiên; URL mà browser mở quyết định Page/handler được chạy. File khởi động server là `Program.cs`.

## Ví dụ: bấm “Bộ sưu tập”

`Pages/Shared/_Layout.cshtml` có link `asp-page="/Collection/Collection"`. Razor Pages tạo URL theo route `/Collection` khai báo đầu `Pages/Collection/Collection.cshtml` → request GET chạy `Pages/Collection/Collection.cshtml.cs` → `OnGetAsync()` đọc danh sách xe từ `AppDbContext` → trả model cho `Collection.cshtml` render → `_ViewStart` áp layout chung.

Tại trang này:

- Bấm **TƯ VẤN** trên một xe → mở `/Contact` (`Pages/Contact/Contact.cshtml`). Form liên hệ hiện do `wwwroot/js/site.js` chặn và chỉ báo demo.
- Bấm **thêm giỏ** → `wwwroot/js/site.js`, ghi lựa chọn vào `localStorage`; chưa tạo đơn.
- Bấm **Giỏ hàng** trên layout → route `/Cart` → `Pages/Cart/Cart.cshtml`; bấm **Tiến hành thanh toán** → JS điều hướng `/Checkout`.
- Bấm **Gửi đơn đặt xe** → POST vào `Pages/Cart/Checkout.cshtml.cs` `OnPostAsync()` → kiểm tra và ghi đơn xuống SQL Server.

## Ví dụ luồng tài khoản và admin

- Đăng nhập: `/Account/Login` → `Login.cshtml` POST → `Login.cshtml.cs.OnPostAsync()` → xác thực/cookie. Đăng ký tương tự ở `/Account/Register` → `Register.cshtml.cs.OnPostAsync()`.
- Mở admin: link `/Admin` → `Pages/Admin/Admin.cshtml`; `Admin.cshtml.cs.OnGetAsync()` tải trang. Nút trong admin được bắt bởi `wwwroot/js/admin.js`; thao tác gọi API `Program.cs` (`/api/admin/...`), không chuyển sang một PageModel admin khác.

## File đầu tiên, nút và nơi chứa chức năng

### Khi chạy ứng dụng

- File C# khởi động server là `Program.cs`: đăng ký dịch vụ, cấu hình `AppDbContext`/xác thực, áp dụng migration, cấu hình middleware và gọi `MapRazorPages()`/`Run()`.
- Browser mở `/` thì route gốc hiển thị `Pages/Dashboard/Index.cshtml` cùng PageModel `Pages/Dashboard/Index.cshtml.cs`. URL được mở quyết định Page đầu tiên được render; `dotnet watch` tự nó không chọn một trang.
- `Pages/_ViewStart.cshtml` chọn `Pages/Shared/_Layout.cshtml`. Layout chứa menu và nạp JavaScript dùng chung; JavaScript theo trang được nạp ở Page hoặc layout tương ứng.

### Nút được khai báo ở đâu, chạy tới đâu?

Razor Pages khai báo nút/link trong `.cshtml`. Link `asp-page` điều hướng tới Page; form POST thường gọi `OnPost...` trong `.cshtml.cs`; nút có `id`, class hoặc `data-*` có thể được JavaScript bắt và gọi API.

| Ví dụ | Nơi khai báo | Handler/đích và nơi có logic |
|---|---|---|
| Tìm kiếm trong bộ sưu tập | `Pages/Collection/Collection.cshtml`, ô `#searchInput` và lựa chọn `#sortPrice` | `wwwroot/js/collection.js`, hàm `render()` lọc theo tên và sắp xếp các thẻ đã render trong browser; không gửi truy vấn tìm kiếm về server. |
| Tải dữ liệu bộ sưu tập ban đầu | Route `/Collection`, khai báo trong `Pages/Collection/Collection.cshtml` | GET chạy `Pages/Collection/Collection.cshtml.cs` → `OnGetAsync()` đọc dữ liệu qua `AppDbContext`; view render dữ liệu đó. |
| Thêm xe vào giỏ | Nút `.add-cart` trong trang collection | `wwwroot/js/site.js` bắt click và lưu giỏ trong `localStorage`; chưa ghi đơn vào SQL. |
| Gửi đơn đặt xe | Form checkout trong `Pages/Cart/Checkout.cshtml` | POST chạy `Pages/Cart/Checkout.cshtml.cs` → `OnPostAsync()`; kiểm tra giỏ/tồn kho/khuyến mãi, tạo đơn và chi tiết, rồi lưu qua EF Core. |
| Tìm/lọc sản phẩm hoặc đơn trong admin | `Pages/Admin/Admin.cshtml`, các ô `#productSearch`, `#orderSearch`, `#orderFilter` | `wwwroot/js/admin.js`, hàm `filterProducts()`/`filterOrders()` lọc DOM phía browser. Các thao tác lưu/xóa khác trong cùng JS gọi API `/api/admin/...`; endpoint tương ứng được khai báo trong `Program.cs`. |

Muốn lần theo thao tác: tìm markup trong `.cshtml` → xem `asp-page`, `method`, `handler`, `id` hoặc `data-*` → tìm handler `OnGet...`/`OnPost...` trong PageModel hoặc selector trong `wwwroot/js/*.js` → nếu JS gọi `/api/...`, tìm `MapGet/MapPost/MapPut/MapPatch/MapDelete` trong `Program.cs` → theo tiếp service/`AppDbContext` để thấy chỗ đọc/ghi database.

