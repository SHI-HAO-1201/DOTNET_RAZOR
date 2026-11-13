namespace MyRazorXe_Xemay.Services.Auth;

public sealed record AccountResult(AccountRecord? Account, string? Error)
{
    public bool Succeeded => Account is not null;
}
