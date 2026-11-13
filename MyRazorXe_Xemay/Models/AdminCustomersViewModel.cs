using MyRazorXe_Xemay.Data;

namespace MyRazorXe_Xemay.Models;

public sealed class AdminCustomersViewModel
{
    public required AdminStatisticsViewModel Statistics { get; init; }
    public required IReadOnlyList<Customer> Customers { get; init; }
}
