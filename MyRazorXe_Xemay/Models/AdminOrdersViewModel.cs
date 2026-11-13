using MyRazorXe_Xemay.Data;

namespace MyRazorXe_Xemay.Models;

public sealed class AdminOrdersViewModel
{
    public required AdminStatisticsViewModel Statistics { get; init; }
    public required IReadOnlyList<MotorcycleOrder> Orders { get; init; }
}
