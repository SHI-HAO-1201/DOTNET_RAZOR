using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;

namespace MyRazorXe_Xemay.Services.Auth;

public sealed class AccountStore
{
    private const int MinimumPasswordLength = 8;
    private readonly string _filePath;
    private readonly PasswordHasher<AccountRecord> _passwordHasher = new();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public AccountStore(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _filePath = configuration["Auth:UsersFile"] ?? Path.Combine(environment.ContentRootPath, "App_Data", "accounts.json");
    }

    public async Task<AccountResult> RegisterAsync(string? fullName, string? email, string? phone, string? password, CancellationToken cancellationToken = default)
    {
        fullName = (fullName ?? string.Empty).Trim();
        email = NormalizeEmail(email);
        phone = (phone ?? string.Empty).Trim();
        password ??= string.Empty;

        if (fullName.Length is < 2 or > 100)
            return new(null, "Họ tên cần từ 2 đến 100 ký tự.");
        if (!new EmailAddressAttribute().IsValid(email))
            return new(null, "Địa chỉ email không hợp lệ.");
        if (phone.Length > 30)
            return new(null, "Số điện thoại không hợp lệ.");
        if (password.Length < MinimumPasswordLength || password.Length > 256)
            return new(null, $"Mật khẩu cần từ {MinimumPasswordLength} đến 256 ký tự.");

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var accounts = await ReadAccountsAsync(cancellationToken);
            if (accounts.Any(account => account.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
                return new(null, "Email này đã được đăng ký.");

            var account = new AccountRecord
            {
                Id = Guid.NewGuid().ToString("N"),
                FullName = fullName,
                Email = email,
                Phone = phone,
                PasswordHash = string.Empty,
                CreatedUtc = DateTimeOffset.UtcNow
            };
            account.PasswordHash = _passwordHasher.HashPassword(account, password);
            accounts.Add(account);
            await WriteAccountsAsync(accounts, cancellationToken);
            return new(account, null);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<AccountResult> CreateOrResetAdminAsync(string? fullName, string? email, string? phone, string? password, CancellationToken cancellationToken = default)
    {
        fullName = (fullName ?? string.Empty).Trim();
        email = NormalizeEmail(email);
        phone = (phone ?? string.Empty).Trim();
        password ??= string.Empty;

        if (fullName.Length is < 2 or > 100)
            return new(null, "Họ tên cần từ 2 đến 100 ký tự.");
        if (!new EmailAddressAttribute().IsValid(email))
            return new(null, "Địa chỉ email không hợp lệ.");
        if (phone.Length > 30)
            return new(null, "Số điện thoại không hợp lệ.");
        if (password.Length < MinimumPasswordLength || password.Length > 256)
            return new(null, $"Mật khẩu cần từ {MinimumPasswordLength} đến 256 ký tự.");

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var accounts = await ReadAccountsAsync(cancellationToken);
            var account = accounts.FirstOrDefault(item => item.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            if (account is null)
            {
                account = new AccountRecord
                {
                    Id = Guid.NewGuid().ToString("N"),
                    FullName = fullName,
                    Email = email,
                    Phone = phone,
                    PasswordHash = string.Empty,
                    CreatedUtc = DateTimeOffset.UtcNow,
                    IsAdmin = true
                };
                accounts.Add(account);
            }
            else
            {
                account.FullName = fullName;
                account.Phone = phone;
                account.IsAdmin = true;
            }

            account.PasswordHash = _passwordHasher.HashPassword(account, password);
            await WriteAccountsAsync(accounts, cancellationToken);
            return new(account, null);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<AccountResult> AuthenticateAsync(string? email, string? password, CancellationToken cancellationToken = default)
    {
        email = NormalizeEmail(email);
        password ??= string.Empty;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var account = (await ReadAccountsAsync(cancellationToken))
                .FirstOrDefault(item => item.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            if (account is null || _passwordHasher.VerifyHashedPassword(account, account.PasswordHash, password) == PasswordVerificationResult.Failed)
                return new(null, "Email hoặc mật khẩu không chính xác.");
            return new(account, null);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<AccountRecord?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return (await ReadAccountsAsync(cancellationToken)).FirstOrDefault(account => account.Id == id);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<AccountResult> UpdateProfileAsync(string? id, string? fullName, string? phone, string? address, CancellationToken cancellationToken = default)
    {
        fullName = (fullName ?? string.Empty).Trim();
        phone = (phone ?? string.Empty).Trim();
        address = (address ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(id))
            return new(null, "Không tìm thấy tài khoản.");
        if (fullName.Length is < 2 or > 100)
            return new(null, "Họ tên cần từ 2 đến 100 ký tự.");
        if (phone.Length > 30 || phone.Length > 0 && !new PhoneAttribute().IsValid(phone))
            return new(null, "Số điện thoại không hợp lệ.");
        if (address.Length > 240)
            return new(null, "Địa chỉ không được vượt quá 240 ký tự.");

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var accounts = await ReadAccountsAsync(cancellationToken);
            var account = accounts.FirstOrDefault(item => item.Id == id);
            if (account is null)
                return new(null, "Không tìm thấy tài khoản.");

            account.FullName = fullName;
            account.Phone = phone;
            account.Address = address;
            await WriteAccountsAsync(accounts, cancellationToken);
            return new(account, null);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<AccountRecord>> ReadAccountsAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath)) return [];
        await using var stream = File.OpenRead(_filePath);
        return await JsonSerializer.DeserializeAsync<List<AccountRecord>>(stream, _jsonOptions, cancellationToken) ?? [];
    }

    private async Task WriteAccountsAsync(List<AccountRecord> accounts, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_filePath) ?? throw new InvalidOperationException("Invalid account data path.");
        Directory.CreateDirectory(directory);
        var temporaryPath = $"{_filePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            {
                await JsonSerializer.SerializeAsync(stream, accounts, _jsonOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }
            File.Move(temporaryPath, _filePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    private static string NormalizeEmail(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();
}
