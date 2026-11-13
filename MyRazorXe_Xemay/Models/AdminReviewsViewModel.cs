using MyRazorXe_Xemay.Data;

namespace MyRazorXe_Xemay.Models;

public sealed class AdminReviewsViewModel
{
    public required AdminStatisticsViewModel Statistics { get; init; }
    public required IReadOnlyList<MotorcycleReview> Reviews { get; init; }
    public decimal AverageRating { get; init; }
    public int FiveStarCount { get; init; }
}
