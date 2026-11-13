using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyRazorXe_Xemay.Data;

namespace MyRazorXe_Xemay.Pages.Account;

[Authorize]
public sealed class OrdersModel(AppDbContext database) : PageModel
{
    public IReadOnlyList<MotorcycleOrder> Orders { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email)) return;

        Orders = await database.Orders.AsNoTracking()
            .Where(order => order.Customer.Email == email)
            .Include(order => order.Items)
            .OrderByDescending(order => order.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public static string StatusLabel(string status) => status switch
    {
        "Pending" => "Chờ xác nhận",
        "Processing" => "Đang xử lý",
        "Shipping" => "Đang giao",
        "Completed" => "Hoàn tất",
        "Cancelled" => "Đã hủy",
        _ => status
    };
}
