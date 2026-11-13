using MyRazorXe_Xemay.Data;

namespace MyRazorXe_Xemay.Models;

public sealed class AdminPromotionsViewModel
{
    public required AdminStatisticsViewModel Statistics { get; init; }
    public required IReadOnlyList<Promotion> Promotions { get; init; }
}
