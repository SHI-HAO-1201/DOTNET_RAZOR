namespace MyRazorXe_Xemay.Services.Auth;

public sealed class AccountRecord
{
    public required string Id { get; init; }
    public required string FullName { get; set; }
    public required string Email { get; init; }
    public required string Phone { get; set; }
    public string Address { get; set; } = string.Empty;
    public required string PasswordHash { get; set; }
    public DateTimeOffset CreatedUtc { get; init; }
    public bool IsAdmin { get; set; }
}
