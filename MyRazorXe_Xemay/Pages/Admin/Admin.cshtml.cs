using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MyRazorXe_Xemay.Data;

namespace MyRazorXe_Xemay.Pages.Admin;

[Authorize(Roles = "Admin")]
public sealed class AdminModel(AppDbContext database) : PageModel
{
    public IReadOnlyList<Motorcycle> Products { get; private set; } = [];

    public int OrderCount { get; private set; }

    public int CustomerCount { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Products = await database.Motorcycles.AsNoTracking()
            .OrderByDescending(product => product.Id)
            .ToListAsync(cancellationToken);
        OrderCount = await database.Orders.CountAsync(cancellationToken);
        CustomerCount = await database.Customers.CountAsync(cancellationToken);
    }
}
