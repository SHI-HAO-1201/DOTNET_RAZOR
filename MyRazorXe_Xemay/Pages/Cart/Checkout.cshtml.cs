using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyRazorXe_Xemay.Data;

namespace MyRazorXe_Xemay.Pages.Cart;

public sealed class CheckoutModel(AppDbContext database) : PageModel
{
    [BindProperty, Required(ErrorMessage = "Vui lòng nhập họ tên."), StringLength(120, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [BindProperty, Required(ErrorMessage = "Vui lòng nhập số điện thoại."), Phone(ErrorMessage = "Số điện thoại không hợp lệ."), StringLength(30)]
    public string Phone { get; set; } = string.Empty;

    [BindProperty, Required(ErrorMessage = "Vui lòng nhập email."), EmailAddress(ErrorMessage = "Email không hợp lệ."), StringLength(320)]
    public string Email { get; set; } = string.Empty;

    [BindProperty, Required(ErrorMessage = "Vui lòng nhập địa chỉ nhận xe."), StringLength(500)]
    public string Address { get; set; } = string.Empty;

    [BindProperty, Required]
    public string PaymentMethod { get; set; } = "consult";

    [BindProperty, StringLength(32)]
    public string? PromotionCode { get; set; }

    [BindProperty]
    public string CartJson { get; set; } = "[]";

    public string? OrderNumber { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        FullName = User.Identity?.Name ?? string.Empty;
        Email = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
        Phone = User.FindFirst(ClaimTypes.MobilePhone)?.Value ?? string.Empty;
        Address = User.FindFirst(ClaimTypes.StreetAddress)?.Value ?? string.Empty;
        await LoadPaymentAvailabilityAsync(cancellationToken);
        OrderNumber = TempData["OrderPlaced"] as string;
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await LoadPaymentAvailabilityAsync(cancellationToken);
        FullName = FullName.Trim();
        Phone = Phone.Trim();
        Email = Email.Trim().ToLowerInvariant();
        Address = Address.Trim();
        PromotionCode = PromotionCode?.Trim().ToUpperInvariant();
        if (User.Identity?.IsAuthenticated == true && User.FindFirstValue(ClaimTypes.Email) is { Length: > 0 } accountEmail)
        {
            Email = accountEmail.Trim().ToLowerInvariant();
            ModelState.Remove(nameof(Email));
        }

        if (PaymentMethod is not ("bank" or "consult") || !IsPaymentEnabled(PaymentMethod))
            ModelState.AddModelError(nameof(PaymentMethod), "Phương thức thanh toán này hiện không khả dụng.");

        List<CheckoutCartItem>? cart;
        try
        {
            cart = JsonSerializer.Deserialize<List<CheckoutCartItem>>(CartJson ?? "[]", new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException)
        {
            cart = null;
        }

        if (cart is null || cart.Count == 0)
            ModelState.AddModelError(string.Empty, "Giỏ hàng đang trống hoặc không hợp lệ.");

        Dictionary<int, int>? quantities = null;
        if (cart is not null)
        {
            quantities = [];
            foreach (var item in cart)
            {
                if (item is null || item.Id < 1 || item.Quantity < 1)
                {
                    ModelState.AddModelError(string.Empty, "Giỏ hàng có sản phẩm hoặc số lượng không hợp lệ.");
                    continue;
                }

                if (!quantities.TryAdd(item.Id, item.Quantity))
                {
                    if (quantities.TryGetValue(item.Id, out var current))
                    {
                        try
                        {
                            quantities[item.Id] = checked(current + item.Quantity);
                        }
                        catch (OverflowException)
                        {
                            ModelState.AddModelError(string.Empty, "Số lượng trong giỏ hàng vượt quá giới hạn.");
                        }
                    }
                }
            }
        }

        if (!ModelState.IsValid || quantities is null)
            return Page();

        await using var transaction = await database.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var products = await database.Motorcycles
            .Where(product => quantities.Keys.Contains(product.Id) && product.IsActive
                && product.StockQuantity > 0)
            .ToDictionaryAsync(product => product.Id, cancellationToken);
        if (products.Count != quantities.Count)
        {
            ModelState.AddModelError(string.Empty, "Một số mẫu xe không còn được bán. Hãy quay lại giỏ hàng để cập nhật.");
            return Page();
        }

        foreach (var (productId, quantity) in quantities)
        {
            if (products[productId].StockQuantity < quantity)
                ModelState.AddModelError(string.Empty, $"{products[productId].Name} hiện không đủ hàng để đặt mua.");
        }
        if (!ModelState.IsValid)
            return Page();

        Promotion? promotion = null;
        if (!string.IsNullOrWhiteSpace(PromotionCode))
        {
            promotion = await database.Promotions.FirstOrDefaultAsync(item => item.Code == PromotionCode, cancellationToken);
            var today = DateTime.UtcNow.Date;
            if (promotion is null || !promotion.IsActive || promotion.StartsAtUtc.Date > today
                || promotion.EndsAtUtc.Date < today
                || promotion.UsageLimit > 0 && promotion.UsedCount >= promotion.UsageLimit)
            {
                ModelState.AddModelError(nameof(PromotionCode), "Mã khuyến mãi không hợp lệ, hết hạn hoặc đã hết lượt sử dụng.");
                return Page();
            }
        }

        var customer = await database.Customers.FirstOrDefaultAsync(record => record.Email == Email, cancellationToken);
        if (customer is null)
        {
            customer = new Customer { FullName = FullName, Email = Email, Phone = Phone, Address = Address };
            database.Customers.Add(customer);
        }
        else
        {
            customer.FullName = FullName;
            customer.Phone = Phone;
            customer.Address = Address;
        }

        var order = new MotorcycleOrder
        {
            OrderNumber = $"MH-{DateTime.UtcNow:yyMMddHHmmss}-{Guid.NewGuid():N}"[..21].ToUpperInvariant(),
            Customer = customer,
            TotalAmount = 0,
            PromotionCode = promotion?.Code,
            ShippingAddress = Address,
            PaymentMethod = PaymentMethod == "bank" ? "Chuyển khoản" : "Nhân viên tư vấn",
            Status = "Pending"
        };

        try
        {
            foreach (var (productId, quantity) in quantities)
            {
                var product = products[productId];
                product.StockQuantity -= quantity;
                order.Items.Add(new MotorcycleOrderItem
                {
                    MotorcycleId = product.Id,
                    MotorcycleName = product.Name,
                    UnitPrice = product.Price,
                    Quantity = quantity
                });
                order.TotalAmount = checked(order.TotalAmount + checked(product.Price * quantity));
            }
        }
        catch (OverflowException)
        {
            ModelState.AddModelError(string.Empty, "Tổng giá trị đơn hàng vượt quá giới hạn.");
            return Page();
        }

        if (promotion is not null)
        {
            order.DiscountAmount = Math.Min(order.TotalAmount, promotion.DiscountAmount);
            order.TotalAmount -= order.DiscountAmount;
            promotion.UsedCount++;
        }

        database.Orders.Add(order);
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        TempData["OrderPlaced"] = order.OrderNumber;
        return RedirectToPage();
    }

    private async Task LoadPaymentAvailabilityAsync(CancellationToken cancellationToken)
    {
        var settings = await database.StoreSettings.AsNoTracking()
            .ToDictionaryAsync(setting => setting.Key, setting => setting.Value, cancellationToken);
        ViewData["BankTransferEnabled"] = ReadSetting(settings, "BankTransfer", true);
        ViewData["PayOnDeliveryEnabled"] = ReadSetting(settings, "PayOnDelivery", true);
    }

    private bool IsPaymentEnabled(string paymentMethod)
    {
        var key = paymentMethod == "bank" ? "BankTransferEnabled" : "PayOnDeliveryEnabled";
        return ViewData[key] is not false;
    }

    private static bool ReadSetting(IReadOnlyDictionary<string, string> settings, string key, bool defaultValue) =>
        !settings.TryGetValue(key, out var value) || !bool.TryParse(value, out var enabled) ? defaultValue : enabled;

    private sealed record CheckoutCartItem(int Id, int Quantity);
}
