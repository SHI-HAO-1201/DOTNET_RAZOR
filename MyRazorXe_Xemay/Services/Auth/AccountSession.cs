using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace MyRazorXe_Xemay.Services.Auth;

public static class AccountSession
{
    public static Task SignInAsync(HttpContext context, AccountRecord account, bool persistent = true)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, account.Id),
            new Claim(ClaimTypes.Name, account.FullName),
            new Claim(ClaimTypes.Email, account.Email),
            new Claim(ClaimTypes.MobilePhone, account.Phone),
            new Claim(ClaimTypes.StreetAddress, account.Address),
            new Claim("created_utc", account.CreatedUtc.ToString("O"))
        };
        var adminEmail = context.RequestServices.GetRequiredService<IConfiguration>()["Auth:AdminEmail"];
        if (account.IsAdmin || (!string.IsNullOrWhiteSpace(adminEmail) && account.Email.Equals(adminEmail.Trim(), StringComparison.OrdinalIgnoreCase)))
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var properties = new AuthenticationProperties
        {
            IsPersistent = persistent,
            AllowRefresh = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        };
        return context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);
    }
}
