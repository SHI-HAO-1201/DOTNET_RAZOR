using MyRazorXe_Xemay.Data;

namespace MyRazorXe_Xemay.Models;

public sealed class AdminStaffViewModel
{
    public required AdminStatisticsViewModel Statistics { get; init; }
    public required IReadOnlyList<StaffMember> Members { get; init; }
}
