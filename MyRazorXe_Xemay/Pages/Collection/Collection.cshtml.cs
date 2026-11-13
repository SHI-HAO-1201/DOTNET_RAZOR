using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyRazorXe_Xemay.Data;

namespace MyRazorXe_Xemay.Pages.Collection;

public sealed class CollectionModel(AppDbContext database) : PageModel
{
    public IReadOnlyList<Motorcycle> Motorcycles { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Motorcycles = await database.Motorcycles.AsNoTracking()
            .Where(motorcycle => motorcycle.IsActive)
            .OrderBy(motorcycle => motorcycle.Brand)
            .ThenBy(motorcycle => motorcycle.Name)
            .ToListAsync(cancellationToken);
    }

    public static string ImageUrl(string image)
    {
        if (Uri.TryCreate(image, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)) return image;
        if (image.StartsWith("photo-", StringComparison.Ordinal))
            return $"https://images.unsplash.com/{image}?auto=format&fit=crop&w=1000&q=85";
        return "/images/motorcycle-placeholder.svg";
    }
}
