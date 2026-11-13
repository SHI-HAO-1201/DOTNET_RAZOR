using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;

namespace MyRazorXe_Xemay.Services.Auth;

public sealed class SharedAdminStore(IConfiguration configuration)
{
    private readonly PasswordHasher<SharedAdminStore> _hasher = new();

    public async Task<AccountRecord?> AuthenticateAsync(string? email, string? password, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (!new EmailAddressAttribute().IsValid(normalizedEmail) || string.IsNullOrEmpty(password)) return null;

        await using var connection = new SqlConnection(GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand("""
            SELECT TOP (1) u.Id, u.HoTen, u.Email, u.PhoneNumber, u.PasswordHash, u.NgayTao
            FROM dbo.AspNetUsers AS u
            INNER JOIN dbo.AspNetUserRoles AS ur ON ur.UserId = u.Id
            INNER JOIN dbo.AspNetRoles AS r ON r.Id = ur.RoleId
            WHERE u.NormalizedEmail = @email AND u.DangHoatDong = 1 AND r.Name IN (N'Admin', N'QuanTriVien')
            """, connection);
        command.Parameters.AddWithValue("@email", normalizedEmail.ToUpperInvariant());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;

        var hash = reader.IsDBNull(4) ? null : reader.GetString(4);
        if (string.IsNullOrEmpty(hash) || _hasher.VerifyHashedPassword(this, hash, password) == PasswordVerificationResult.Failed) return null;
        return new AccountRecord
        {
            Id = $"shared-admin-{reader.GetInt32(0)}",
            FullName = reader.GetString(1),
            Email = reader.GetString(2) ?? normalizedEmail,
            Phone = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
            PasswordHash = hash,
            CreatedUtc = new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Utc)),
            IsAdmin = true
        };
    }

    public async Task<int> CreateOrResetAsync(string? fullName, string? email, string? phone, string? password, CancellationToken cancellationToken = default)
    {
        fullName = (fullName ?? string.Empty).Trim();
        email = NormalizeEmail(email);
        phone = (phone ?? string.Empty).Trim();
        if (fullName.Length is < 2 or > 100 || !new EmailAddressAttribute().IsValid(email) || phone.Length > 30 || password is null || password.Length is < 8 or > 256)
            throw new ArgumentException("Thông tin chưa hợp lệ. Tên 2–100 ký tự, email hợp lệ, số điện thoại tối đa 30 ký tự, mật khẩu 8–256 ký tự.");

        var passwordHash = _hasher.HashPassword(this, password);
        await using var connection = new SqlConnection(GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        var userId = await ScalarAsync(connection, transaction, """
            SELECT TOP (1) Id FROM dbo.AspNetUsers WITH (UPDLOCK, HOLDLOCK) WHERE NormalizedEmail = @email
            """, cancellationToken, ("@email", email.ToUpperInvariant()));
        if (userId is null)
        {
            userId = await ScalarAsync(connection, transaction, """
                INSERT INTO dbo.AspNetUsers
                    (HoTen, DangHoatDong, ChiNhanhId, NgayTao, UserName, NormalizedUserName, Email, NormalizedEmail,
                     EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed,
                     TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
                OUTPUT INSERTED.Id
                VALUES (@name, 1, NULL, SYSUTCDATETIME(), @email, @normalizedEmail, @email, @normalizedEmail,
                    1, @hash, @stamp, @stamp, @phone, 0, 0, 1, 0)
                """, cancellationToken,
                ("@name", fullName), ("@email", email), ("@normalizedEmail", email.ToUpperInvariant()),
                ("@hash", passwordHash), ("@stamp", Guid.NewGuid().ToString("N")), ("@phone", phone));
        }
        else
        {
            await ExecuteAsync(connection, transaction, """
                UPDATE dbo.AspNetUsers SET HoTen = @name, DangHoatDong = 1, PasswordHash = @hash,
                    SecurityStamp = @stamp, ConcurrencyStamp = @stamp, PhoneNumber = @phone,
                    EmailConfirmed = 1, AccessFailedCount = 0, LockoutEnd = NULL
                WHERE Id = @id
                """, cancellationToken,
                ("@name", fullName), ("@hash", passwordHash), ("@stamp", Guid.NewGuid().ToString("N")),
                ("@phone", phone), ("@id", userId));
        }
        var resolvedUserId = Convert.ToInt32(userId, System.Globalization.CultureInfo.InvariantCulture);

        foreach (var roleName in new[] { "Admin", "QuanTriVien" })
        {
            var roleId = await ScalarAsync(connection, transaction,
                "SELECT TOP (1) Id FROM dbo.AspNetRoles WHERE NormalizedName = @name",
                cancellationToken, ("@name", roleName.ToUpperInvariant()));
            if (roleId is null)
            {
                roleId = await ScalarAsync(connection, transaction, """
                    INSERT INTO dbo.AspNetRoles (Name, NormalizedName, ConcurrencyStamp)
                    OUTPUT INSERTED.Id VALUES (@name, @normalizedName, @stamp)
                    """, cancellationToken, ("@name", roleName),
                    ("@normalizedName", roleName.ToUpperInvariant()), ("@stamp", Guid.NewGuid().ToString("N")));
            }
            var resolvedRoleId = Convert.ToInt32(roleId, System.Globalization.CultureInfo.InvariantCulture);

            await ExecuteAsync(connection, transaction, """
                IF NOT EXISTS (SELECT 1 FROM dbo.AspNetUserRoles WHERE UserId = @userId AND RoleId = @roleId)
                    INSERT INTO dbo.AspNetUserRoles (UserId, RoleId) VALUES (@userId, @roleId)
                """, cancellationToken, ("@userId", resolvedUserId), ("@roleId", resolvedRoleId));
        }

        await transaction.CommitAsync(cancellationToken);
        return resolvedUserId;
    }

    private string GetConnectionString() => configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Chưa cấu hình ConnectionStrings:DefaultConnection trong User Secrets hoặc biến môi trường.");

    private static async Task<object?> ScalarAsync(SqlConnection connection, SqlTransaction transaction, string sql, CancellationToken token, params (string Name, object Value)[] parameters)
    {
        await using var command = new SqlCommand(sql, connection, transaction);
        foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        return await command.ExecuteScalarAsync(token);
    }

    private static async Task ExecuteAsync(SqlConnection connection, SqlTransaction transaction, string sql, CancellationToken token, params (string Name, object Value)[] parameters)
    {
        await using var command = new SqlCommand(sql, connection, transaction);
        foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        await command.ExecuteNonQueryAsync(token);
    }

    private static string NormalizeEmail(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();
}
