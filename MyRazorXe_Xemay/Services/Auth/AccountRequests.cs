namespace MyRazorXe_Xemay.Services.Auth;

public sealed record RegisterAccountRequest(string FullName, string Email, string Phone, string Password);
public sealed record LoginAccountRequest(string Email, string Password);
public sealed record AccountSummary(string Id, string FullName, string Email, string Phone, DateTimeOffset CreatedUtc)
{
    public static AccountSummary From(AccountRecord account) => new(account.Id, account.FullName, account.Email, account.Phone, account.CreatedUtc);
}
