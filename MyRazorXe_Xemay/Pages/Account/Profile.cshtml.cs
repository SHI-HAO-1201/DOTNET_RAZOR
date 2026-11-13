using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyRazorXe_Xemay.Services.Auth;

namespace MyRazorXe_Xemay.Pages.Account;

[Authorize]
public sealed class ProfileModel(AccountStore accounts) : PageModel
{
    [BindProperty, Required(ErrorMessage = "Vui lòng nhập họ tên."), StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên cần từ 2 đến 100 ký tự.")]
    public string FullName { get; set; } = string.Empty;

    [BindProperty, StringLength(30, ErrorMessage = "Số điện thoại không hợp lệ."), Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string Phone { get; set; } = string.Empty;

    [BindProperty, StringLength(240, ErrorMessage = "Địa chỉ không được vượt quá 240 ký tự.")]
    public string Address { get; set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var account = accountId is null ? null : await accounts.FindByIdAsync(accountId, cancellationToken);
        if (account is null) return Challenge();

        FullName = account.FullName;
        Phone = account.Phone;
        Address = account.Address;
        Email = account.Email;
        CreatedUtc = account.CreatedUtc;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!ModelState.IsValid)
        {
            await LoadReadOnlyProfileAsync(accountId, cancellationToken);
            return Page();
        }

        var result = await accounts.UpdateProfileAsync(accountId, FullName, Phone, Address, cancellationToken);
        if (!result.Succeeded)
        {
            ErrorMessage = result.Error;
            await LoadReadOnlyProfileAsync(accountId, cancellationToken);
            return Page();
        }

        await AccountSession.SignInAsync(HttpContext, result.Account!);
        TempData["ProfileSuccess"] = "Đã cập nhật thông tin tài khoản.";
        return RedirectToPage();
    }

    private async Task LoadReadOnlyProfileAsync(string? accountId, CancellationToken cancellationToken)
    {
        var account = accountId is null ? null : await accounts.FindByIdAsync(accountId, cancellationToken);
        if (account is null) throw new InvalidOperationException("Không tìm thấy tài khoản đang đăng nhập.");
        Email = account.Email;
        CreatedUtc = account.CreatedUtc;
    }
}
