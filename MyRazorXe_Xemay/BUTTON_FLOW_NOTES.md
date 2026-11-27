# BUTTON_FLOW — bản đồ nút, trang, API và dữ liệu

Tài liệu này mô tả luồng theo mã hiện có. “API” ở đây là các HTTP endpoint được khai báo trong `Program.cs`; Razor Page form cũng có thể gọi handler server trực tiếp mà không qua `/api/...`.

## Cách lần theo một nút

1. Tìm markup của nút hoặc form trong `Pages/**/*.cshtml` (tìm `asp-page`, `method`, `id`, `class`, `data-*`).
2. Với link `asp-page`, mở Page đích. GET thường chạy `OnGet[Async]` trong file `.cshtml.cs` cùng tên. POST chạy `OnPost[Handler][Async]` trong PageModel cùng trang.
3. Với `id`/`data-*`, tìm selector trong `wwwroot/js/*.js`. JavaScript có thể chỉ cập nhật giao diện/localStorage hoặc gửi `fetch()`.
4. Nếu thấy `/api/...`, tìm `MapGet`, `MapPost`, `MapPut`, `MapPatch`, `MapDelete` trong `Program.cs`; tiếp tục lần sang model, service và `Data/AppDbContext.cs` để thấy chỗ đọc/ghi SQL.

## Các file nền tảng và ý nghĩa tên

| File/thư mục | Ý nghĩa và vai trò |
|---|---|
| `Program.cs` | Điểm khởi động ứng dụng: đăng ký dịch vụ, SQL Server/EF Core, cookie đăng nhập, phân quyền, middleware, toàn bộ API và `MapRazorPages()`. API hiện được định nghĩa trực tiếp trong file này. |
| `appsettings.json` | Cấu hình mặc định không bí mật: log, host và các cài đặt chung. Connection string có thể được ghi đè bởi User Secrets hoặc biến môi trường. |
| `appsettings.Development.json` | Cấu hình bổ sung khi chạy môi trường Development, ví dụ bật lỗi chi tiết. |
| `MyRazorXe_Xemay.csproj` | File project .NET: framework `net10.0`, nullable, User Secrets ID và các package EF Core SQL Server/Design. |
| `BUTTON_FLOW_NOTES.md` | Tài liệu bản đồ nút và luồng trang/API này. |
| `README.md` | Hướng dẫn tổng quan ứng dụng, tài khoản và lệnh tạo admin. |
| `Data/` | Mô hình dữ liệu, EF Core DbContext, factory, khởi tạo và ghi chú database. |
| `Migrations/` | Lịch sử thay đổi schema SQL do EF Core quản lý; migration mới xóa bảng xe legacy `Motorcycles` sau khi xác nhận mã xe đều có trong `XeMays`. |
| `Models/` | Input/view model phục vụ form và API, nhất là dữ liệu quản trị/đánh giá/liên hệ. |
| `Services/Auth/` | Logic tài khoản, phiên đăng nhập và kho thông tin tài khoản. |
| `Pages/` | Razor Pages: `.cshtml` là giao diện/markup; `.cshtml.cs` là PageModel xử lý request khi có. |
| `Pages/Shared/_Layout.cshtml` | Khung giao diện dùng chung: menu, liên kết tài khoản/giỏ hàng, footer và các script toàn site. |
| `Pages/_ViewStart.cshtml` | Chọn layout dùng chung cho Razor Pages. |
| `Pages/_ViewImports.cshtml` | Import namespace/tag helper cho các trang Razor. |
| `wwwroot/` | Tài nguyên trình duyệt: JavaScript, CSS, ảnh và thư viện tĩnh. |
| `wwwroot/js/site.js` | Tương tác dùng chung, giỏ hàng localStorage, tải catalog từ API và xử lý một số thao tác giao diện. |
| `wwwroot/js/admin.js` | Tải dữ liệu admin và bắt sự kiện các nút quản trị; gọi `/api/admin/...`. |
| `wwwroot/js/collection.js` | Tìm/lọc/sắp xếp danh sách bộ sưu tập phía trình duyệt. |
| `wwwroot/js/checkout.js` | Tương tác giao diện trang checkout; việc đặt đơn thật do Razor POST xử lý. |
| `wwwroot/js/password-toggle.js` | Nút hiện/ẩn mật khẩu, chỉ đổi kiểu hiển thị input. |
| `wwwroot/js/localize.js` | Bổ sung định dạng/hiển thị phía trình duyệt, gồm xử lý ảnh lỗi. |


### Từng file dữ liệu, model và xác thực

| File | Ý nghĩa / liên quan tới luồng |
|---|---|
| `Data/AppDbContext.cs` | Khai báo EF Core DbSet và mapping giữa các entity với bảng SQL; API và PageModel inject file này để đọc/ghi. |
| `Data/AppDbContextFactory.cs` | Tạo DbContext cho công cụ design-time như EF migrations. |
| `Data/DatabaseInitializer.cs` | Logic khởi tạo dữ liệu ban đầu nếu được gọi; khác với migration schema. |
| `Data/Motorcycle.cs` | Entity một xe/sản phẩm; catalog, admin, giỏ và checkout dùng ID/giá/tồn kho của xe. |
| `Data/MotorcycleOrder.cs` | Entity đơn hàng; trạng thái, tổng tiền, khách hàng và chi tiết đơn. |
| `Data/MotorcycleOrderItem.cs` | Một dòng xe/số lượng/giá trong đơn; dùng để tính và hoàn/trừ tồn kho. |
| `Data/Customer.cs` | Entity khách hàng, liên kết với đơn và hồ sơ. |
| `Data/Promotion.cs` | Entity mã/chương trình ưu đãi; admin quản lý và checkout kiểm tra. |
| `Data/StoreSetting.cs` | Cặp key/value cài đặt cửa hàng; API admin settings đọc/ghi. |
| `Data/MotorcycleReview.cs` | Entity đánh giá xe; hiện có model dữ liệu nhưng không thấy nút/API review được nối trong luồng admin đang dùng. |
| `Data/ProductContactRequest.cs` | Entity yêu cầu tư vấn/liên hệ; hiện form contact chưa lưu yêu cầu. |
| `Data/StaffMember.cs` | Entity nhân viên; không phải luồng nút storefront/admin đang được nối ở các API đã rà soát. |
| `Data/README.md` | Ghi chú schema, database dùng chung, migration và cấu hình kết nối. |
| `Data/Use-CodeFirst.ps1`, `Data/Use-DatabaseFirst.ps1` | Script chuyển/chọn quy trình EF; chỉ chạy khi chủ động đổi cách quản lý schema. |
| `Models/AdminProductInput.cs` | Input tạo/sửa sản phẩm từ dialog admin gửi đến POST products. |
| `Models/AdminCustomerInput.cs` | Input tạo/sửa khách hàng qua POST customers. |
| `Models/AdminPromotionInput.cs` | Dữ liệu tạo chương trình ưu đãi. |
| `Models/StoreSettingsInput.cs` | Kiểu input cài đặt cửa hàng (endpoint hiện nhận dictionary key/value). |
| `Models/AdminStatisticsViewModel.cs` | Dữ liệu thống kê dashboard admin. |
| `Models/AdminOrdersViewModel.cs`, `AdminCustomersViewModel.cs`, `AdminPromotionsViewModel.cs`, `AdminReviewsViewModel.cs`, `AdminStaffViewModel.cs` | View model danh sách quản trị theo loại dữ liệu tương ứng. |
| `Models/AdminStaffInput.cs` | Dữ liệu đầu vào nhân viên; chưa thấy endpoint admin staff tương ứng đang được map. |
| `Models/ReviewInput.cs` | Dữ liệu gửi đánh giá; chưa thấy endpoint review được map trong luồng hiện tại. |
| `Models/ProductContactRequestInput.cs` | Dữ liệu yêu cầu liên hệ; cần được nối vào handler/API để form contact thật sự lưu dữ liệu. |
| `Models/ErrorViewModel.cs` | Dữ liệu phụ trợ trang lỗi. |
| `Services/Auth/AccountStore.cs` | Đăng ký/xác thực/cập nhật tài khoản; PageModel và auth API gọi service này. |
| `Services/Auth/AccountRecord.cs` | Cấu trúc bản ghi tài khoản lưu trong kho tài khoản. |
| `Services/Auth/AccountRequests.cs` | Request/response model cho API tài khoản và thông tin tài khoản trả về. |
| `Services/Auth/AccountResult.cs` | Kết quả thành công/thất bại từ thao tác tài khoản. |
| `Services/Auth/AccountSession.cs` | Tạo/xóa phiên cookie và claims người dùng. |
| `Services/Auth/SharedAdminStore.cs` | Kho tài khoản admin dùng chung/cấu hình admin. |
| `Services/Auth/AdminAccountCommand.cs` | Xử lý lệnh dòng lệnh `--create-admin`, không phải nút trên trang. |
| `Migrations/*` | Các file migration và model snapshot mô tả schema theo từng phiên bản. Migration `RemoveLegacyMotorcycles` xóa bảng sản phẩm cũ `Motorcycles`; app tiếp tục đọc/ghi bảng dùng chung `XeMays`. |


### Quy ước tên trang và handler

- `Pages/Collection/Collection.cshtml` ánh xạ trang bộ sưu tập; `Collection.cshtml.cs` là PageModel. Tương tự với `Offers`, `Checkout`, `Login`…
- Tên thư mục thường nhóm trang theo chức năng (`Account`, `Admin`, `Cart`); tên file là route/page cuối. `Index.cshtml` thường là trang chính của nhóm.
- Hậu tố `Input` trong `Models/` thường là dữ liệu đầu vào; `ViewModel` là dữ liệu chuẩn bị để render trang.
- Các entity trong `Data/` là đối tượng dữ liệu lưu/đọc bằng EF Core; `AppDbContext` nối entity với database.
- Tên hàm `OnGetAsync` nghĩa là xử lý HTTP GET bất đồng bộ; `OnPostAsync` là xử lý HTTP POST bất đồng bộ. Hậu tố `Async` không tạo ra route mới.

## Nút điều hướng giữa các trang

Các liên kết chính ở `Pages/Shared/_Layout.cshtml` được lặp ở menu điều hướng/footer. Bấm link `asp-page` sẽ điều hướng trình duyệt sang trang Razor, không phải gọi API JSON.

| Nút/link | Tệp khai báo → đích | Luồng |
|---|---|---|
| Logo/trang chủ | Layout → `Pages/Dashboard/Index.cshtml` | GET trang chủ; PageModel `Index.cshtml.cs` lấy dữ liệu trang từ database rồi Razor render. |
| Bộ sưu tập | Layout, trang chủ, bài tạp chí → `Pages/Collection/Collection.cshtml` | GET → `Collection.cshtml.cs` đọc xe đang hoạt động từ `AppDbContext` → render danh sách. Tìm kiếm/lọc/sắp xếp do `collection.js` thực hiện trên dữ liệu đã render. |
| Ưu đãi | Layout/trang chủ → `Pages/Offer/Offers.cshtml` | GET → `Offers.cshtml.cs` đọc chương trình ưu đãi → render; nút sao chép mã chỉ chạy JS. |
| Tạp chí | Layout → `Pages/Megazine/Megazine.cshtml` | Điều hướng tới trang tạp chí (tên thư mục trong source viết `Megazine`). |
| Showroom | Layout/trang chủ/tạp chí → `Pages/Showroom/Showroom.cshtml` | Chỉ điều hướng và render trang; không có API đặt lịch được nối bởi các link này. |
| Liên hệ | Layout, ưu đãi, bộ sưu tập → `Pages/Contact/Contact.cshtml` | Điều hướng trang. Form liên hệ hiện là giao diện demo, chưa gửi/lưu yêu cầu qua API. |
| Giỏ hàng | Layout → `Pages/Cart/Cart.cshtml` | Trang đọc giỏ từ `localStorage` bằng `site.js`; chưa có đơn hàng trong database ở bước này. |
| Đăng nhập/đăng ký | Layout/trang tài khoản → `/Account/Login`, `/Account/Register` | Mở form tương ứng; POST form xử lý bởi PageModel. |
| Hồ sơ/đơn hàng của tôi | Layout (khi đã đăng nhập) → `/Account/Profile`, `/Account/Orders` | GET lấy dữ liệu người dùng hiện tại; hồ sơ có POST cập nhật. |
| Đăng xuất | Layout/hồ sơ → POST `/Account/Logout` | Form POST → `Logout.cshtml.cs.OnPostAsync()` → xóa cookie xác thực → chuyển hướng. |
| Admin | Liên kết quản trị ở layout (nếu được render) → `/Admin` | Yêu cầu vai trò Admin. `Admin.cshtml.cs.OnGetAsync()` nạp khung/trang ban đầu; thao tác quản trị sau đó do `admin.js` gọi API. |

## Giỏ hàng và đặt đơn

| Nút/thao tác | Nơi khai báo/bắt sự kiện | Luồng đầy đủ |
|---|---|---|
| Thêm xe vào giỏ | Nút `.add-cart` tại `Pages/Collection/Collection.cshtml`; `wwwroot/js/site.js` | JS lấy `data-id/name/brand/category/engine/image/price`, lưu/cập nhật giỏ trong `localStorage`. Không gọi API tạo đơn và chưa ghi đơn SQL. |
| Tăng/giảm/xóa dòng | Nút `data-action` được render ở giao diện giỏ; `site.js` | Cập nhật số lượng hoặc xóa dòng trong `localStorage`, sau đó vẽ lại tổng tiền. |
| Tiến hành thanh toán | Giao diện `Pages/Cart/Cart.cshtml` và handler giao diện trong `site.js` | Chuyển trình duyệt tới `/Cart/Checkout`; đây là điều hướng trang, không phải API. |
| Gửi đơn | Form `#checkoutForm` trong `Pages/Cart/Checkout.cshtml` | POST → `Pages/Cart/Checkout.cshtml.cs.OnPostAsync()` → đọc giỏ gửi lên, kiểm tra sản phẩm/tồn kho/mã ưu đãi, tạo/cập nhật khách hàng và đơn/chi tiết, trừ tồn kho, lưu transaction bằng EF Core → trả kết quả đặt đơn. |
| Theo dõi đơn | Link sau đặt hàng hoặc menu tài khoản → `/Account/Orders` | `Orders.cshtml.cs.OnGetAsync()` lọc đơn theo người dùng đang đăng nhập. |

## Tài khoản và xác thực

| Thao tác | Tệp giao diện → xử lý | Kết quả |
|---|---|---|
| Đăng ký | `Pages/Account/Register.cshtml` POST → `Register.cshtml.cs.OnPostAsync()` | Kiểm tra input, tạo tài khoản qua `AccountStore`, đăng nhập bằng cookie; có thể gọi service/kho tài khoản thay vì ghi vào bảng tài khoản EF. |
| Đăng nhập | `Pages/Account/Login.cshtml` POST → `Login.cshtml.cs.OnPostAsync()` | `AccountStore` xác thực → tạo cookie → điều hướng theo vai trò/đích. |
| Hiện/ẩn mật khẩu | Nút `data-password-toggle` → `password-toggle.js` | Đổi `type=password/text`; không gửi request. |
| Quên mật khẩu | `Pages/Account/ForgotPassword.cshtml` | Hiện trang demo; chưa có luồng gửi email/reset mật khẩu thật. |
| Sửa hồ sơ | `Pages/Account/Profile.cshtml` POST → `Profile.cshtml.cs.OnPostAsync()` | Cập nhật hồ sơ cho user hiện tại. |
| Đăng xuất | Form POST `/Account/Logout` → `Logout.cshtml.cs` | Xóa cookie/phiên và redirect. |
| API tài khoản | `Program.cs`, nhóm `/api/auth` | `POST /register`, `POST /login`, `GET /me`, `POST /logout`; API dùng cookie. `/me` và `/logout` yêu cầu đăng nhập. Giới hạn POST bằng policy `account`. |

## Admin: nút nào gọi API nào

Giao diện nằm trong `Pages/Admin/Admin.cshtml`; `Admin.cshtml.cs` chuẩn bị trang ban đầu. `wwwroot/js/admin.js` tải dữ liệu, gắn sự kiện, gửi JSON; endpoint thực tế nằm trong `Program.cs`. Các endpoint admin yêu cầu role `Admin`. Thành công thường gọi lại `loadAll()` để tải mới các bảng.

| Nút/điều khiển | Selector/JS | HTTP endpoint | Tác dụng phía server |
|---|---|---|---|
| Chọn tab Tổng quan/Sản phẩm/Đơn hàng/Khách hàng/Ưu đãi/Nội dung/Báo cáo/Cài đặt | `[data-panel]` trong `Admin.cshtml`; `admin.js` | Không gọi API | Chỉ đổi panel đang hiển thị trong cùng trang. |
| Lối tắt “Xem danh sách”, quản lý đơn/ưu đãi/báo cáo | `[data-goto]` | Không gọi API trực tiếp | Kích hoạt tab tương ứng. |
| Tải ban đầu/refresh dữ liệu quản trị | `loadAll()` trong `admin.js` | GET `/api/admin/dashboard`, `/api/admin/products`, `/api/admin/orders`, `/api/admin/customers`, `/api/admin/promotions`, `/api/admin/settings` | Đọc thống kê, danh sách, cài đặt từ `AppDbContext`. |
| Thêm sản phẩm | `[data-open-product]` mở dialog; gửi form trong `admin.js` | POST `/api/admin/products` | Validate input, kiểm tra mã trùng, thêm xe vào `Motorcycles`, lưu SQL. |
| Sửa sản phẩm | `[data-edit]` → điền dialog → lưu | GET `/api/admin/products/{id}` nếu chưa có trong state; POST `/api/admin/products` với `Id` | POST cùng endpoint dùng `Id` để cập nhật sản phẩm hiện có. |
| Ẩn/hiện sản phẩm | `[data-toggle-product]` | POST `/api/admin/products` | Cập nhật `IsActive` và các trường sản phẩm. API `DELETE /api/admin/products/{id}` cũng tồn tại nhưng giao diện hiện dùng luồng POST để bật/tắt. |
| Tải CSV sản phẩm | `#exportProducts` | GET `/api/admin/products/export` | Tạo và trả file CSV từ dữ liệu SQL. |
| Tìm sản phẩm | `#productSearch` | Không gọi API | Lọc các dòng DOM đã tải. |
| Cập nhật trạng thái đơn | `select[data-order-status]` change | PUT `/api/admin/orders/{id}/status` | Validate trạng thái; transaction cập nhật đơn. Hủy đơn hoàn tồn kho; mở lại đơn đã hủy kiểm tra/trừ tồn kho. |
| Xóa đơn | `[data-delete-order]` | DELETE `/api/admin/orders/{id}` | Transaction hoàn kho nếu đơn chưa hủy, rồi xóa đơn. |
| Tìm/lọc đơn | `#orderSearch`, `#orderFilter` | Không gọi API | Lọc danh sách DOM trong trình duyệt. |
| Thêm/sửa khách hàng | `#addCustomer`, `#customerForm`, `[data-edit-customer]` | POST `/api/admin/customers` | `Id=0` tạo; `Id>0` sửa; kiểm tra email trùng và validate trước khi lưu. Form dùng `method=dialog`, JS bắt submit để gửi fetch. |
| Xóa khách hàng | `[data-delete-customer]` | DELETE `/api/admin/customers/{id}` | Xóa nếu khách chưa có đơn; nếu có lịch sử trả conflict. |
| Tạo ưu đãi | `#addOffer` | POST `/api/admin/promotions` | Validate rồi thêm promotion vào SQL. |
| Tạm dừng/kích hoạt ưu đãi | `[data-toggle-promotion]` | PATCH `/api/admin/promotions/{id}` | Cập nhật `IsActive`. |
| Lưu nội dung/cài đặt | `#saveContent`, `#saveSettings` | PUT `/api/admin/settings` | Chỉ nhận một số key cho phép (`BrandName`, `HomeMessage`, `SupportEmail`, `SupportPhone`, `Currency`), lưu `StoreSettings`. |
| Xuất báo cáo | `#exportReport` | Không có endpoint export report riêng | Tạo/tải CSV từ dữ liệu đã nạp phía trình duyệt. |
| Dialog đóng/hủy/lưu | `form method="dialog"` | Không tự POST tới Razor Page | Browser điều khiển dialog; `admin.js` ngăn submit lưu và gửi API tương ứng. |

### API storefront

- `GET /api/storefront/catalog` trong `Program.cs`: đọc xe đang hoạt động, trả JSON `{id,name,brand,category,engine,image,price}`. `site.js` dùng endpoint này để đồng bộ dữ liệu catalog ở phía trình duyệt; trang Razor cũng có PageModel đọc dữ liệu ban đầu.
- API đọc catalog không yêu cầu role Admin. Các API admin được bảo vệ bằng policy role `Admin`.

## Các trang và PageModel hiện có

| Page | PageModel | Ý nghĩa |
|---|---|---|
| `Pages/Dashboard/Index.cshtml` | `Index.cshtml.cs` | Trang chủ/dashboard người mua. |
| `Pages/Collection/Collection.cshtml` | `Collection.cshtml.cs` | Danh mục xe; dữ liệu ban đầu từ database. |
| `Pages/Offer/Offers.cshtml` | `Offers.cshtml.cs` | Danh sách ưu đãi. |
| `Pages/Cart/Cart.cshtml` | Không có | View giỏ phía client; giỏ lưu trong localStorage. |
| `Pages/Cart/Checkout.cshtml` | `Checkout.cshtml.cs` | Form tạo đơn hàng thực. |
| `Pages/Account/Login.cshtml` | `Login.cshtml.cs` | Đăng nhập form. |
| `Pages/Account/Register.cshtml` | `Register.cshtml.cs` | Đăng ký form. |
| `Pages/Account/Profile.cshtml` | `Profile.cshtml.cs` | Xem/cập nhật hồ sơ. |
| `Pages/Account/Orders.cshtml` | `Orders.cshtml.cs` | Xem đơn hàng cá nhân. |
| `Pages/Account/Logout.cshtml` | `Logout.cshtml.cs` | Kết thúc phiên đăng nhập. |
| `Pages/Account/ForgotPassword.cshtml`, `AccessDenied.cshtml` | Không có PageModel | Trang tĩnh/demo quên mật khẩu và từ chối truy cập. |
| `Pages/Admin/Admin.cshtml` | `Admin.cshtml.cs` | Khung quản trị và dữ liệu khởi tạo; hành động quản trị chính gọi API. |
| `Pages/Megazine/Megazine.cshtml`, `Pages/Showroom/Showroom.cshtml`, `Pages/Contact/Contact.cshtml` | Không có | Trang nội dung tĩnh; contact hiện chưa lưu form lên server. |
| `Pages/Privacy.cshtml` | `Privacy.cshtml.cs` | Trang quyền riêng tư. |
| `Pages/Error.cshtml` | `Error.cshtml.cs` | Trang lỗi. |

## Luồng khởi động và kết nối database

1. `dotnet watch`/`dotnet run` khởi chạy `Program.cs`.
2. App đọc `ConnectionStrings:DefaultConnection` theo thứ tự cấu hình ASP.NET Core (biến môi trường/User Secrets có thể ghi đè `appsettings*.json`).
3. `AddDbContext<AppDbContext>(...UseSqlServer(...))` đăng ký EF Core.
4. Sau `builder.Build()`, ứng dụng gọi `Database.MigrateAsync()` để áp dụng migration chưa chạy.
5. Middleware xác thực/phân quyền chạy trước API và Razor Pages. `MapRazorPages()` ánh xạ các `.cshtml`; các `Map...` trong `Program.cs` ánh xạ API.
6. Trình duyệt mở URL cụ thể; URL quyết định Page đầu tiên được tải. `dotnet watch` không tự chọn trang.

### Cấu hình SQL Server cho project này

Project có User Secrets ID trong `MyRazorXe_Xemay.csproj`. Connection string cục bộ đã được lưu trong User Secrets, không cần ghi mật khẩu vào Git:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=MotoSpeedDb;User Id=sa;Password=<mat-khau-sa>;Encrypt=True;TrustServerCertificate=True" --project MyRazorXe_Xemay.csproj
```

Nếu chạy project ở máy/môi trường khác, đặt lại secret hoặc cấu hình biến môi trường `ConnectionStrings__DefaultConnection`. Tài khoản `sa` phải được bật trong SQL Server và password phải khớp với cấu hình máy chủ; `TrustServerCertificate=True` phù hợp kết nối phát triển cục bộ. Không commit thông tin mật khẩu vào `appsettings.json`.

## Ghi chú cần biết khi chỉnh luồng

- Giỏ hiện ở `localStorage`; nếu đổi trình duyệt/thiết bị thì giỏ không tự đồng bộ. Đơn chỉ tạo khi POST checkout thành công.
- Form liên hệ và quên mật khẩu chưa có xử lý API thật. Muốn triển khai thì cần thêm input model, handler/endpoint và nơi lưu/gửi dữ liệu.
- Các link tạp chí trên trang chủ đã được đồng bộ thành `/Megazine/Megazine` theo đúng thư mục/file `Pages/Megazine/Megazine.cshtml`.
- Để thêm nút mới: markup `.cshtml` → chọn Page handler nếu submit trang hoặc selector JS nếu cần tương tác → thêm endpoint trong `Program.cs` nếu cần API → cập nhật model/DbContext/migration khi schema thay đổi → cập nhật bảng tương ứng trong tài liệu này.


## Bảng vị trí từng nút trên web với đường dẫn đầy đủ

Gốc project là `D:\Hào Lê code\DOTNET\RAZOR\DOTNET_RAZOR\MyRazorXe_Xemay\`. Mỗi ô dưới đây ghi đường dẫn đầy đủ tới file chứa nút; nếu nút được JavaScript tạo động, mình ghi cả file chứa phần tử và file tạo/bắt sự kiện.

| Nút/thao tác | File chứa nút (đường dẫn đầy đủ) | Luồng xử lý tiếp theo |
|---|---|---|
| Logo, Trang chủ, Bộ sưu tập, Ưu đãi, Tạp chí, Showroom, Liên hệ, Giỏ hàng, link tài khoản và Đăng xuất | `D:\Hào Lê code\DOTNET\RAZOR\DOTNET_RAZOR\MyRazorXe_Xemay\Pages\Shared\_Layout.cshtml` | Link `asp-page` điều hướng sang Razor Page; Đăng xuất POST tới `/Account/Logout` → `Pages\Account\Logout.cshtml.cs`. |
| Mở/đóng menu điện thoại | `D:\Hào Lê code\DOTNET\RAZOR\DOTNET_RAZOR\MyRazorXe_Xemay\Pages\Shared\_Layout.cshtml` | `wwwroot\js\site.js` bắt nút menu và đổi trạng thái hiển thị; không gọi server. |
| Nút “Khám phá bộ sưu tập”, “Xem xe”, “Xem ưu đãi”, bài tạp chí trên trang chủ | `D:\Hào Lê code\DOTNET\RAZOR\DOTNET_RAZOR\MyRazorXe_Xemay\Pages\Dashboard\Index.cshtml` | Các link `asp-page` chuyển sang Collection, Showroom, Offer hoặc Megazine; không phải API. |
| Tìm/lọc/sắp xếp xe | `D:\Hào Lê code\DOTNET\RAZOR\DOTNET_RAZOR\MyRazorXe_Xemay\Pages\Collection\Collection.cshtml` | `wwwroot\js\collection.js` lọc/sắp xếp các xe đã render ở trình duyệt; tải danh sách ban đầu do `Collection.cshtml.cs.OnGetAsync()` đọc SQL. |
| Nút “TƯ VẤN” trên từng xe | `D:\Hào Lê code\DOTNET\RAZOR\DOTNET_RAZOR\MyRazorXe_Xemay\Pages\Collection\Collection.cshtml` | Chuyển sang `Pages\Contact\Contact.cshtml`; form liên hệ hiện demo, chưa lưu database. |
| Nút “Thêm xe vào giỏ” ở bộ sưu tập | `D:\Hào Lê code\DOTNET\RAZOR\DOTNET_RAZOR\MyRazorXe_Xemay\Pages\Collection\Collection.cshtml` | `wwwroot\js\site.js` bắt `.add-cart`, ghi xe vào `localStorage`; chưa tạo đơn và chưa gọi API ghi SQL. |
| Tăng/giảm số lượng, xóa xe khỏi giỏ | Vùng giỏ trong `D:\Hào Lê code\DOTNET\RAZOR\DOTNET_RAZOR\MyRazorXe_Xemay\Pages\Cart\Cart.cshtml`; các nút được tạo động tại `D:\Hào Lê code\DOTNET\RAZOR\DOTNET_RAZOR\MyRazorXe_Xemay\wwwroot\js\site.js` | `site.js` cập nhật `localStorage`, tính lại tổng tiền và render giỏ; không ghi đơn. |
| Nút “Tiến hành đặt hàng” | `D:\Hào Lê code\DOTNET\RAZOR\DOTNET_RAZOR\MyRazorXe_Xemay\Pages\Cart\Cart.cshtml` | `site.js` chuyển tới `/Cart/Checkout`. |
| Nút “Gửi đơn đặt xe” | `D:\Hào Lê code\DOTNET\RAZOR\DOTNET_RAZOR\MyRazorXe_Xemay\Pages\Cart\Checkout.cshtml` | POST → `Pages\Cart\Checkout.cshtml.cs.OnPostAsync()` → kiểm tra sản phẩm, tồn kho, khuyến mãi; ghi khách hàng/đơn/chi tiết và cập nhật tồn kho qua EF Core. `checkout.js` chuẩn bị phần hiển thị giỏ trước khi gửi. |
| Nút Đăng nhập và nút hiện/ẩn mật khẩu | `D:\Hào Lê code\DOTNET\RAZOR\DOTNET_RAZOR\MyRazorXe_Xemay\Pages\Account\Login.cshtml` | Form POST → `Login.cshtml.cs.OnPostAsync()`; nút hiện mật khẩu gọi `wwwroot\js\password-toggle.js`, chỉ đổi cách hiển thị input. |
| Nút Đăng ký và hiện/ẩn mật khẩu | `D:\Hào Lê code\DOTNET\RAZOR\DOTNET_RAZOR\MyRazorXe_Xemay\Pages\Account\Register.cshtml` | POST → `Register.cshtml.cs.OnPostAsync()` → `AccountStore`; nút hiện mật khẩu gọi `password-toggle.js`. |
| Nút “Lưu thay đổi” hồ sơ và Đăng xuất | `D:\Hào Lê code\DOTNET\RAZOR\DOTNET_RAZOR\MyRazorXe_Xemay\Pages\Account\Profile.cshtml` | POST hồ sơ → `Profile.cshtml.cs.OnPostAsync()`; form đăng xuất POST → `Logout.cshtml.cs`. |
| Nút “Gửi yêu cầu khôi phục” | `D:\Hào Lê code\DOTNET\RAZOR\DOTNET_RAZOR\MyRazorXe_Xemay\Pages\Account\ForgotPassword.cshtml` | JavaScript `wwwroot\js\site.js` chặn submit và hiện thông báo demo; chưa có API gửi email/reset mật khẩu. |
| Nút gửi form Liên hệ | `D:\Hào Lê code\DOTNET\RAZOR\DOTNET_RAZOR\MyRazorXe_Xemay\Pages\Contact\Contact.cshtml` | `site.js` chặn submit và báo demo; chưa lưu SQL. |
| Nút sao chép mã ưu đãi | `D:\Hào Lê code\DOTNET\RAZOR\DOTNET_RAZOR\MyRazorXe_Xemay\Pages\Offer\Offers.cshtml` | `site.js` bắt `.copy-voucher`, chép mã vào clipboard; không gọi server. |
| Các tab quản trị Tổng quan/Sản phẩm/Đơn hàng/Khách hàng/Ưu đãi/Nội dung/Báo cáo/Cài đặt | `D:\Hào Lê code\DOTNET\RAZOR\DOTNET_RAZOR\MyRazorXe_Xemay\Pages\Admin\Admin.cshtml` | `wwwroot\js\admin.js` đổi panel trong cùng trang; không chuyển Page. |
| Nút “Thêm sản phẩm”, mở/sửa dialog, nút Lưu sản phẩm | Phần dialog và nút tĩnh nằm ở `D:\Hào Lê code\DOTNET\RAZOR\DOTNET_RAZOR\MyRazorXe_Xemay\Pages\Admin\Admin.cshtml`; các nút sửa/ẩn trong bảng được tạo tại `D:\Hào Lê code\DOTNET\RAZOR\DOTNET_RAZOR\MyRazorXe_Xemay\wwwroot\js\admin.js` | `admin.js` gửi POST `/api/admin/products` trong `Program.cs`; `Id=0` thêm mới, `Id>0` cập nhật xe trong SQL. Đây là nút quản trị lưu sản phẩm thật. |
| Nút “Tải danh sách CSV” sản phẩm | `D:\Hào Lê code\DOTNET\RAZOR\DOTNET_RAZOR\MyRazorXe_Xemay\Pages\Admin\Admin.cshtml` | `admin.js` điều hướng GET `/api/admin/products/export` → endpoint trong `Program.cs` trả CSV. |
| Nút Thêm/Sửa/Xóa khách hàng | Nút mở dialog nằm tại `D:\Hào Lê code\DOTNET\RAZOR\DOTNET_RAZOR\MyRazorXe_Xemay\Pages\Admin\Admin.cshtml`; nút Sửa/Xóa từng dòng được tạo tại `D:\Hào Lê code\DOTNET\RAZOR\DOTNET_RAZOR\MyRazorXe_Xemay\wwwroot\js\admin.js` | `admin.js` gọi POST `/api/admin/customers` để thêm/sửa và DELETE `/api/admin/customers/{id}` để xóa; endpoint trong `Program.cs`. |
| Nút Tạo/Tạm dừng ưu đãi | Nút Tạo trong `D:\Hào Lê code\DOTNET\RAZOR\DOTNET_RAZOR\MyRazorXe_Xemay\Pages\Admin\Admin.cshtml`; nút trạng thái trong card được tạo ở `D:\Hào Lê code\DOTNET\RAZOR\DOTNET_RAZOR\MyRazorXe_Xemay\wwwroot\js\admin.js` | POST `/api/admin/promotions` hoặc PATCH `/api/admin/promotions/{id}` trong `Program.cs`. |
| Nút Cập nhật trạng thái/Xóa đơn | `D:\Hào Lê code\DOTNET\RAZOR\DOTNET_RAZOR\MyRazorXe_Xemay\Pages\Admin\Admin.cshtml` (vùng bảng); hàng và nút được render trong `D:\Hào Lê code\DOTNET\RAZOR\DOTNET_RAZOR\MyRazorXe_Xemay\wwwroot\js\admin.js` | PUT `/api/admin/orders/{id}/status` hoặc DELETE `/api/admin/orders/{id}` trong `Program.cs`; cập nhật trạng thái và tồn kho. |
| Nút “Lưu nội dung” và “Lưu cài đặt” | `D:\Hào Lê code\DOTNET\RAZOR\DOTNET_RAZOR\MyRazorXe_Xemay\Pages\Admin\Admin.cshtml` | `admin.js` gửi PUT `/api/admin/settings` → `Program.cs` lưu `StoreSettings`. |
| Nút “Tải báo cáo CSV” | `D:\Hào Lê code\DOTNET\RAZOR\DOTNET_RAZOR\MyRazorXe_Xemay\Pages\Admin\Admin.cshtml` | `admin.js` tạo/tải CSV từ dữ liệu đã nạp trên trình duyệt; hiện không có endpoint export report riêng. |

> **Phân biệt nhanh:** “Thêm xe vào giỏ” nằm tại `Pages\Collection\Collection.cshtml` và chỉ lưu localStorage. “Thêm sản phẩm vào kho” nằm ở dialog admin tại `Pages\Admin\Admin.cshtml`, được xử lý bởi `wwwroot\js\admin.js` → POST `/api/admin/products` → `Program.cs` → SQL.
