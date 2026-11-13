using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using MyRazorXe_Xemay.Services.Auth;

namespace MyRazorXe_Xemay.Pages.Account;

[EnableRateLimiting("account")]
public sealed class RegisterModel(AccountStore accounts) : PageModel
{
    [BindProperty, Required(ErrorMessage = "Vui lòng nhập họ tên."), StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên cần từ 2 đến 100 ký tự.")]
    public string FullName { get; set; } = string.Empty;

    [BindProperty, Required(ErrorMessage = "Vui lòng nhập địa chỉ email."), EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]
    public string Email { get; set; } = string.Empty;

    [BindProperty, Required(ErrorMessage = "Vui lòng nhập số điện thoại."), Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string Phone { get; set; } = string.Empty;

    [BindProperty, Required(ErrorMessage = "Vui lòng nhập mật khẩu."), StringLength(256, MinimumLength = 8, ErrorMessage = "Mật khẩu cần từ 8 đến 256 ký tự.")]
    public string Password { get; set; } = string.Empty;

    [BindProperty, Required(ErrorMessage = "Vui lòng nhập lại mật khẩu."), Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận chưa khớp.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Page();
        var result = await accounts.RegisterAsync(FullName, Email, Phone, Password, cancellationToken);
        if (!result.Succeeded)
        {
            ErrorMessage = result.Error;
            return Page();
        }

        await AccountSession.SignInAsync(HttpContext, result.Account!);
        return RedirectToPage("/Account/Profile");
    }
}
