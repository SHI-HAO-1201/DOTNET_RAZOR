using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using MyRazorXe_Xemay.Services.Auth;

namespace MyRazorXe_Xemay.Pages.Account;

[EnableRateLimiting("account")]
public sealed class LoginModel(AccountStore accounts, SharedAdminStore admins, IConfiguration configuration) : PageModel
{
    [BindProperty, Required(ErrorMessage = "Vui lòng nhập địa chỉ email."), EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]
    public string Email { get; set; } = string.Empty;

    [BindProperty, Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    public string Password { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; private set; }

    public IActionResult OnGet()
    {
        if (User.IsInRole("Admin")) return Redirect("/Admin");
        if (User.Identity?.IsAuthenticated == true) return RedirectToPage("/Account/Profile");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Page();
        var admin = await admins.AuthenticateAsync(Email, Password, cancellationToken);
        if (admin is not null)
        {
            await AccountSession.SignInAsync(HttpContext, admin);
            return Redirect("/Admin");
        }

        var result = await accounts.AuthenticateAsync(Email, Password, cancellationToken);
        if (!result.Succeeded) { ErrorMessage = result.Error; return Page(); }

        await AccountSession.SignInAsync(HttpContext, result.Account!);
        var adminEmail = configuration["Auth:AdminEmail"];
        if (result.Account!.IsAdmin || (!string.IsNullOrWhiteSpace(adminEmail) && result.Account.Email.Equals(adminEmail.Trim(), StringComparison.OrdinalIgnoreCase)))
            return Redirect("/Admin");
        if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl)) return LocalRedirect(ReturnUrl);
        return RedirectToPage("/Account/Profile");
    }
}
