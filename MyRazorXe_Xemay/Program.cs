using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using MyRazorXe_Xemay.Services.Auth;
using MyRazorXe_Xemay.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Hãy cấu hình ConnectionStrings:DefaultConnection trong appsettings.json.");
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddSingleton<SharedAdminStore>();

if (args.Contains("--create-admin", StringComparer.OrdinalIgnoreCase))
{
    var admins = new SharedAdminStore(builder.Configuration);
    Environment.ExitCode = await AdminAccountCommand.RunAsync(admins);
    return;
}

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddRazorPages();
builder.Services.AddSingleton<AccountStore>();
var keyDirectory = builder.Configuration["Auth:KeysDirectory"] ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys");
builder.Services.AddDataProtection()
    .SetApplicationName("MOTOHUB")
    .PersistKeysToFileSystem(new DirectoryInfo(keyDirectory));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "Motohub.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("account", context =>
    {
        if (!HttpMethods.IsPost(context.Request.Method)) return RateLimitPartition.GetNoLimiter("read-only");
        return RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 8,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
var authApi = app.MapGroup("/api/auth").RequireRateLimiting("account");
authApi.MapPost("/register", async (RegisterAccountRequest request, AccountStore accounts, HttpContext context, CancellationToken cancellationToken) =>
{
    var result = await accounts.RegisterAsync(request.FullName, request.Email, request.Phone, request.Password, cancellationToken);
    if (!result.Succeeded) return Results.ValidationProblem(new Dictionary<string, string[]> { ["account"] = [result.Error!] });
    await AccountSession.SignInAsync(context, result.Account!);
    return Results.Created("/api/auth/me", AccountSummary.From(result.Account!));
});
authApi.MapPost("/login", async (LoginAccountRequest request, AccountStore accounts, HttpContext context, CancellationToken cancellationToken) =>
{
    var result = await accounts.AuthenticateAsync(request.Email, request.Password, cancellationToken);
    if (!result.Succeeded) return Results.Problem(result.Error, statusCode: StatusCodes.Status401Unauthorized);
    await AccountSession.SignInAsync(context, result.Account!);
    return Results.Ok(AccountSummary.From(result.Account!));
});
authApi.MapGet("/me", async (ClaimsPrincipal user, AccountStore accounts, CancellationToken cancellationToken) =>
{
    var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (id is null) return Results.Unauthorized();
    var account = await accounts.FindByIdAsync(id, cancellationToken);
    return account is null ? Results.Unauthorized() : Results.Ok(AccountSummary.From(account));
}).RequireAuthorization();
authApi.MapPost("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.NoContent();
}).RequireAuthorization();
app.MapRazorPages()
   .WithStaticAssets();

app.MapGet("/api/storefront/catalog", async (AppDbContext database, CancellationToken cancellationToken) =>
    await database.Motorcycles.AsNoTracking()
        .Where(item => item.IsActive)
        .OrderBy(item => item.Name)
        .Select(item => new
        {
            id = item.Id,
            name = item.Name,
            brand = item.Brand,
            category = item.Type,
            engine = item.Engine,
            image = item.Image,
            price = item.Price
        })
        .ToListAsync(cancellationToken));

var adminApi = app.MapGroup("/api/admin/products").RequireAuthorization(policy => policy.RequireRole("Admin"));
adminApi.MapGet("/", async (AppDbContext database, CancellationToken cancellationToken) =>
    await database.Motorcycles.AsNoTracking().OrderByDescending(item => item.Id)
        .Select(item => new { id = item.Id, name = item.Name, brand = item.Brand, category = item.Type, price = item.Price, stock = item.StockQuantity, code = item.Code, image = item.Image, engine = item.Engine, isActive = item.IsActive, availabilityStatus = item.StockQuantity > 0 ? MotorcycleAvailabilityStatus.ForSale : MotorcycleAvailabilityStatus.Incoming })
        .ToListAsync(cancellationToken));
adminApi.MapGet("/export", async (AppDbContext database, CancellationToken cancellationToken) =>
{
    var products = await database.Motorcycles.AsNoTracking().OrderBy(product => product.Name).ToListAsync(cancellationToken);
    var csv = new System.Text.StringBuilder("Id,Code,Name,Brand,Type,Price,StockQuantity,IsActive\r\n");
    foreach (var product in products)
        csv.AppendLine($"{product.Id},{Csv(product.Code)},{Csv(product.Name)},{Csv(product.Brand)},{Csv(product.Type)},{product.Price},{product.StockQuantity},{product.IsActive}");

    var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
    return Results.File(bytes, "text/csv; charset=utf-8", "motohub-products.csv");
});
adminApi.MapGet("/{id:int}", async (int id, AppDbContext database, CancellationToken cancellationToken) =>
{
    var product = await database.Motorcycles.AsNoTracking().Where(item => item.Id == id)
        .Select(item => new { id = item.Id, name = item.Name, brand = item.Brand, category = item.Type, price = item.Price, stock = item.StockQuantity, code = item.Code, image = item.Image, engine = item.Engine, isActive = item.IsActive, availabilityStatus = item.StockQuantity > 0 ? MotorcycleAvailabilityStatus.ForSale : MotorcycleAvailabilityStatus.Incoming })
        .FirstOrDefaultAsync(cancellationToken);
    return product is null ? Results.NotFound() : Results.Ok(product);
});
adminApi.MapPost("/", async (MyRazorXe_Xemay.Models.AdminProductInput input, AppDbContext database, CancellationToken cancellationToken) =>
{
    input.Name = input.Name.Trim();
    input.Brand = input.Brand.Trim();
    input.Type = input.Type.Trim();
    input.Engine = input.Engine.Trim();
    input.Image = input.Image.Trim();
    input.Code = input.Code.Trim().ToUpperInvariant();

    var validationResults = new List<ValidationResult>();
    if (!Validator.TryValidateObject(input, new ValidationContext(input), validationResults, validateAllProperties: true))
    {
        var errors = validationResults
            .SelectMany(result => result.MemberNames.DefaultIfEmpty("product")
                .Select(member => new { member, message = result.ErrorMessage ?? "Thông tin không hợp lệ." }))
            .GroupBy(error => error.member)
            .ToDictionary(group => group.Key, group => group.Select(error => error.message).ToArray());
        return Results.ValidationProblem(errors);
    }

    if (input.AvailabilityStatus is not (MotorcycleAvailabilityStatus.ForSale or MotorcycleAvailabilityStatus.Incoming))
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["availabilityStatus"] = ["Trạng thái bán xe không hợp lệ."] });
    if (input.Id < 0)
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["id"] = ["Sản phẩm không hợp lệ."] });
    if (input.AvailabilityStatus == MotorcycleAvailabilityStatus.ForSale && input.StockQuantity < 1)
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["stockQuantity"] = ["Xe đang bán phải có ít nhất 1 xe trong kho."] });
    if (await database.Motorcycles.AnyAsync(item => item.Code == input.Code && item.Id != input.Id, cancellationToken))
        return Results.Conflict(new { message = "Mã xe đã được sử dụng." });

    var product = input.Id > 0 ? await database.Motorcycles.FindAsync([input.Id], cancellationToken) : null;
    if (input.Id > 0 && product is null) return Results.NotFound();
    product ??= new Motorcycle();
    product.Name = input.Name.Trim(); product.Brand = input.Brand.Trim(); product.Type = input.Type.Trim();
    product.Price = input.Price; product.Engine = input.Engine.Trim();
    product.Image = input.Image.Trim(); product.Code = input.Code.Trim();
    product.StockQuantity = input.StockQuantity;
    product.IsActive = input.IsActive;
    if (input.AvailabilityStatus != MotorcycleAvailabilityStatus.ForSale)
        product.StockQuantity = 0;
    if (input.Id == 0)
    {
        database.Entry(product).Property<decimal>("GiaNiemYet").CurrentValue = input.Price;
        database.Motorcycles.Add(product);
    }
    try { await database.SaveChangesAsync(cancellationToken); }
    catch (DbUpdateException) { return Results.Conflict(new { message = "Mã xe đã được sử dụng hoặc dữ liệu không hợp lệ." }); }
    return Results.Ok(new { id = product.Id });
});
adminApi.MapDelete("/{id:int}", async (int id, AppDbContext database, CancellationToken cancellationToken) =>
{
    var product = await database.Motorcycles.FindAsync([id], cancellationToken);
    if (product is null) return Results.NotFound();
    product.IsActive = false;
    await database.SaveChangesAsync(cancellationToken);
    return Results.NoContent();
});
var dashboardApi = app.MapGroup("/api/admin").RequireAuthorization(policy => policy.RequireRole("Admin"));
dashboardApi.MapGet("/dashboard", async (AppDbContext database, CancellationToken cancellationToken) =>
{
    var completedOrders = database.Orders.AsNoTracking().Where(order => order.Status == "Completed");
    var revenue = await completedOrders.SumAsync(order => (long?)order.TotalAmount, cancellationToken) ?? 0;
    var completedCount = await completedOrders.CountAsync(cancellationToken);
    var recentOrders = await database.Orders.AsNoTracking().OrderByDescending(order => order.CreatedAtUtc).Take(5)
        .Select(order => new { order.Id, order.OrderNumber, customer = order.Customer.FullName, order.CreatedAtUtc, order.TotalAmount, order.Status })
        .ToListAsync(cancellationToken);
    return Results.Ok(new
    {
        productCount = await database.Motorcycles.CountAsync(cancellationToken),
        orderCount = await database.Orders.CountAsync(cancellationToken),
        customerCount = await database.Customers.CountAsync(cancellationToken),
        revenue,
        completedCount,
        averageOrderValue = completedCount == 0 ? 0 : revenue / completedCount,
        recentOrders
    });
});
dashboardApi.MapGet("/orders", async (AppDbContext database, CancellationToken cancellationToken) =>
    await database.Orders.AsNoTracking().OrderByDescending(order => order.CreatedAtUtc)
        .Select(order => new { order.Id, order.OrderNumber, customer = order.Customer.FullName, order.CreatedAtUtc, order.TotalAmount, order.Status })
        .ToListAsync(cancellationToken));
dashboardApi.MapDelete("/orders/{id:int}", async (int id, AppDbContext database, CancellationToken cancellationToken) =>
{
    await using var transaction = await database.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
    var order = await database.Orders.Include(item => item.Items).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    if (order is null) return Results.NotFound();

    if (order.Status != "Cancelled")
    {
        foreach (var item in order.Items)
        {
            var product = await database.Motorcycles.FindAsync([item.MotorcycleId], cancellationToken);
            if (product is null) return Results.Conflict(new { message = "Không tìm thấy sản phẩm trong đơn hàng." });
            if (product.StockQuantity > int.MaxValue - item.Quantity)
                return Results.Conflict(new { message = "Không thể hoàn tồn kho do vượt quá giới hạn." });
            product.StockQuantity += item.Quantity;
        }
    }

    database.Orders.Remove(order);
    await database.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
    return Results.NoContent();
});
dashboardApi.MapPut("/orders/{id:int}/status", async (int id, AdminOrderStatusInput input, AppDbContext database, CancellationToken cancellationToken) =>
{
    if (!new[] { "Pending", "Processing", "Shipping", "Completed", "Cancelled" }.Contains(input.Status, StringComparer.Ordinal))
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Trạng thái đơn hàng không hợp lệ."] });
    await using var transaction = await database.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
    var order = await database.Orders.Include(item => item.Items).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    if (order is null) return Results.NotFound();
    if (input.Status == "Cancelled" && order.Status != "Cancelled")
    {
        foreach (var item in order.Items)
        {
            var product = await database.Motorcycles.FindAsync([item.MotorcycleId], cancellationToken);
            if (product is null) return Results.Conflict(new { message = "Không tìm thấy sản phẩm trong đơn hàng." });
            if (product.StockQuantity > int.MaxValue - item.Quantity)
                return Results.Conflict(new { message = "Không thể hoàn tồn kho do vượt quá giới hạn." });
            product.StockQuantity += item.Quantity;
        }
    }
    else if (order.Status == "Cancelled" && input.Status != "Cancelled")
    {
        foreach (var item in order.Items)
        {
            var product = await database.Motorcycles.FindAsync([item.MotorcycleId], cancellationToken);
            if (product is null || product.StockQuantity < item.Quantity)
                return Results.Conflict(new { message = "Không đủ tồn kho để mở lại đơn hàng này." });
        }
        foreach (var item in order.Items)
        {
            var product = await database.Motorcycles.FindAsync([item.MotorcycleId], cancellationToken);
            product!.StockQuantity -= item.Quantity;
        }
    }

    order.Status = input.Status;
    await database.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
    return Results.NoContent();
});
dashboardApi.MapGet("/customers", async (AppDbContext database, CancellationToken cancellationToken) =>
    await database.Customers.AsNoTracking().OrderBy(customer => customer.FullName)
        .Select(customer => new { customer.Id, name = customer.FullName, customer.Email, customer.Phone, customer.Address, orderCount = customer.Orders.Count(), spent = customer.Orders.Where(order => order.Status == "Completed").Sum(order => (long?)order.TotalAmount) ?? 0 })
        .ToListAsync(cancellationToken));
dashboardApi.MapPost("/customers", async (MyRazorXe_Xemay.Models.AdminCustomerInput input, AppDbContext database, CancellationToken cancellationToken) =>
{
    input.FullName = input.FullName.Trim();
    input.Email = input.Email.Trim().ToLowerInvariant();
    input.Phone = input.Phone.Trim();
    input.Address = input.Address.Trim();
    var validationResults = new List<ValidationResult>();
    if (!Validator.TryValidateObject(input, new ValidationContext(input), validationResults, validateAllProperties: true))
    {
        var errors = validationResults
            .SelectMany(result => result.MemberNames.DefaultIfEmpty("customer")
                .Select(member => new { member, message = result.ErrorMessage ?? "Thông tin không hợp lệ." }))
            .GroupBy(error => error.member)
            .ToDictionary(group => group.Key, group => group.Select(error => error.message).ToArray());
        return Results.ValidationProblem(errors);
    }

    if (input.Id < 0)
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["id"] = ["Khách hàng không hợp lệ."] });
    if (await database.Customers.AnyAsync(customer => customer.Email == input.Email && customer.Id != input.Id, cancellationToken))
        return Results.Conflict(new { message = "Email này đã được sử dụng." });

    Customer customer;
    if (input.Id == 0)
    {
        customer = new Customer { FullName = input.FullName, Email = input.Email, Phone = input.Phone, Address = input.Address };
        database.Customers.Add(customer);
    }
    else
    {
        var existingCustomer = await database.Customers.FindAsync([input.Id], cancellationToken);
        if (existingCustomer is null) return Results.NotFound();
        customer = existingCustomer;
        customer.FullName = input.FullName;
        customer.Email = input.Email;
        customer.Phone = input.Phone;
        customer.Address = input.Address;
    }

    await database.SaveChangesAsync(cancellationToken);
    return Results.Ok(new { customer.Id });
});
dashboardApi.MapDelete("/customers/{id:int}", async (int id, AppDbContext database, CancellationToken cancellationToken) =>
{
    var customer = await database.Customers.FindAsync([id], cancellationToken);
    if (customer is null) return Results.NotFound();
    if (await database.Orders.AnyAsync(order => order.CustomerId == id, cancellationToken))
        return Results.Conflict(new { message = "Không thể xóa khách hàng có lịch sử đơn hàng." });

    database.Customers.Remove(customer);
    await database.SaveChangesAsync(cancellationToken);
    return Results.NoContent();
});
dashboardApi.MapGet("/promotions", async (AppDbContext database, CancellationToken cancellationToken) =>
    await database.Promotions.AsNoTracking().OrderByDescending(promotion => promotion.Id)
        .Select(promotion => new { promotion.Id, promotion.Name, promotion.Code, promotion.Description, promotion.DiscountAmount, promotion.UsageLimit, promotion.UsedCount, promotion.StartsAtUtc, promotion.EndsAtUtc, promotion.IsActive })
        .ToListAsync(cancellationToken));
dashboardApi.MapPost("/promotions", async (AdminPromotionInput input, AppDbContext database, CancellationToken cancellationToken) =>
{
    if (!Validator.TryValidateObject(input, new ValidationContext(input), new List<ValidationResult>(), true) || input.EndsAtUtc <= input.StartsAtUtc)
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["promotion"] = ["Thông tin ưu đãi chưa hợp lệ."] });
    var promotion = new Promotion { Name = input.Name.Trim(), Code = input.Code.Trim().ToUpperInvariant(), Description = input.Description.Trim(), DiscountAmount = input.DiscountAmount, UsageLimit = input.UsageLimit, StartsAtUtc = input.StartsAtUtc, EndsAtUtc = input.EndsAtUtc, IsActive = true };
    database.Promotions.Add(promotion);
    try { await database.SaveChangesAsync(cancellationToken); }
    catch (DbUpdateException) { return Results.Conflict(new { message = "Mã ưu đãi đã tồn tại hoặc thông tin không hợp lệ." }); }
    return Results.Created($"/api/admin/promotions/{promotion.Id}", new { promotion.Id });
});
dashboardApi.MapPatch("/promotions/{id:int}", async (int id, AdminPromotionActiveInput input, AppDbContext database, CancellationToken cancellationToken) =>
{
    var promotion = await database.Promotions.FindAsync([id], cancellationToken);
    if (promotion is null) return Results.NotFound();
    promotion.IsActive = input.IsActive;
    await database.SaveChangesAsync(cancellationToken);
    return Results.NoContent();
});
dashboardApi.MapGet("/settings", async (AppDbContext database, CancellationToken cancellationToken) =>
    await database.StoreSettings.AsNoTracking().ToDictionaryAsync(item => item.Key, item => item.Value, cancellationToken));
dashboardApi.MapPut("/settings", async (Dictionary<string, string> values, AppDbContext database, CancellationToken cancellationToken) =>
{
    var allowed = new HashSet<string>(["BrandName", "HomeMessage", "SupportEmail", "SupportPhone", "Currency"], StringComparer.Ordinal);
    if (values.Keys.Any(key => !allowed.Contains(key)) || values.Values.Any(value => value.Length > 500))
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["settings"] = ["Thông tin cài đặt không hợp lệ."] });
    foreach (var (key, value) in values)
    {
        var setting = await database.StoreSettings.FindAsync([key], cancellationToken);
        if (setting is null) database.StoreSettings.Add(new StoreSetting { Key = key, Value = value.Trim() });
        else setting.Value = value.Trim();
    }
    await database.SaveChangesAsync(cancellationToken);
    return Results.NoContent();
});

app.Run();

static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

public sealed record AdminOrderStatusInput([Required] string Status);
public sealed record AdminPromotionActiveInput(bool IsActive);
public sealed class AdminPromotionInput
{
    [Required, StringLength(120)] public string Name { get; init; } = string.Empty;
    [Required, StringLength(32)] public string Code { get; init; } = string.Empty;
    [StringLength(500)] public string Description { get; init; } = string.Empty;
    [Range(0, long.MaxValue)] public long DiscountAmount { get; init; }
    [Range(1, int.MaxValue)] public int UsageLimit { get; init; } = 100;
    public DateTime StartsAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime EndsAtUtc { get; init; } = DateTime.UtcNow.AddMonths(1);
}
