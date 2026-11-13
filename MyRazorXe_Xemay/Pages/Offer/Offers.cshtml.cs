using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyRazorXe_Xemay.Data;

namespace MyRazorXe_Xemay.Pages.Offer;

public sealed class OffersModel(AppDbContext database) : PageModel
{
    public IReadOnlyList<Promotion> Promotions { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        Promotions = await database.Promotions.AsNoTracking()
            .Where(promotion => promotion.IsActive && promotion.StartsAtUtc.Date <= today
                && promotion.EndsAtUtc.Date >= today
                && (promotion.UsageLimit == 0 || promotion.UsedCount < promotion.UsageLimit))
            .OrderBy(promotion => promotion.EndsAtUtc)
            .ToListAsync(cancellationToken);
    }
}
