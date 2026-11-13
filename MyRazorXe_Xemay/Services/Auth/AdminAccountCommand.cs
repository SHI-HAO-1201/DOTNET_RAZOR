namespace MyRazorXe_Xemay.Services.Auth;

public static class AdminAccountCommand
{
    public static async Task<int> RunAsync(SharedAdminStore admins)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("TẠO / ĐẶT LẠI TÀI KHOẢN QUẢN TRỊ CHUNG");
        Console.Write("Họ và tên: ");
        var fullName = Console.ReadLine();
        Console.Write("Email đăng nhập: ");
        var email = Console.ReadLine();
        Console.Write("Số điện thoại (có thể để trống): ");
        var phone = Console.ReadLine();
        var password = ReadPassword("Mật khẩu (ít nhất 8 ký tự): ");
        var confirmation = ReadPassword("Nhập lại mật khẩu: ");

        if (!string.Equals(password, confirmation, StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Hai mật khẩu không khớp. Chưa có thay đổi nào được lưu.");
            return 1;
        }

        try
        {
            var id = await admins.CreateOrResetAsync(fullName, email, phone, password);
            Console.WriteLine($"Đã tạo/cập nhật tài khoản quản trị dùng chung (ID: {id}, email: {email?.Trim()}).");
            Console.WriteLine("Tài khoản đã được gán quyền Admin và QuanTriVien trong MotoSpeedDb.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Không thể tạo tài khoản quản trị: {exception.Message}");
            return 1;
        }
    }

    private static string ReadPassword(string prompt)
    {
        Console.Write(prompt);
        var password = new System.Text.StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return password.ToString();
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (password.Length > 0) password.Length--;
                continue;
            }

            if (!char.IsControl(key.KeyChar)) password.Append(key.KeyChar);
        }
    }
}
