using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace LuckyWheelClient
{
    /// <summary>
    /// Lớp quản lý xác thực cục bộ khi không thể kết nối với server
    /// </summary>
    public static class LocalAuthManager
    {
        // Danh sách tài khoản cục bộ
        private static readonly Dictionary<string, UserInfo> localUsers = new Dictionary<string, UserInfo>();
        private static readonly string localDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LuckyWheel",
            "localauth.dat");

        // Thông tin người dùng
        public class UserInfo
        {
            public string Username { get; set; }
            public string PasswordHash { get; set; }
            public string Email { get; set; }
            public int Points { get; set; }
            public bool IsVIP { get; set; }
            public DateTime CreatedDate { get; set; }
            public string ResetToken { get; set; }
            public DateTime? ResetTokenExpiry { get; set; }
        }

        // Khởi tạo LocalAuthManager
        static LocalAuthManager()
        {
            // Thêm tài khoản admin mặc định
            localUsers["admin"] = new UserInfo
            {
                Username = "admin",
                PasswordHash = HashPassword("admin123"),
                Email = "admin@luckywheel.com",
                Points = 9999,
                IsVIP = true,
                CreatedDate = DateTime.Now
            };

            // Tạo thư mục nếu chưa tồn tại
            Directory.CreateDirectory(Path.GetDirectoryName(localDataPath));

            // Tải dữ liệu người dùng cục bộ nếu có
            LoadLocalUsers();
        }

        // Đăng ký người dùng cục bộ
        public static bool RegisterLocalUser(string username, string password, string email)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(email))
                return false;

            // Kiểm tra người dùng đã tồn tại chưa
            if (localUsers.ContainsKey(username.ToLower()))
                return false;

            // Tạo người dùng mới
            UserInfo newUser = new UserInfo
            {
                Username = username,
                PasswordHash = HashPassword(password),
                Email = email,
                Points = 1000, // Số điểm khởi đầu
                IsVIP = false,
                CreatedDate = DateTime.Now
            };

            // Thêm vào danh sách và lưu
            localUsers[username.ToLower()] = newUser;
            SaveLocalUsers();

            return true;
        }

        // Kiểm tra đăng nhập cục bộ
        public static bool ValidateLogin(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return false;

            // Kiểm tra tài khoản admin đặc biệt
            if (username.ToLower() == "admin" && password == "admin123")
                return true;

            // Kiểm tra trong danh sách tài khoản cục bộ
            if (localUsers.TryGetValue(username.ToLower(), out UserInfo user))
            {
                return user.PasswordHash == HashPassword(password);
            }

            return false;
        }

        // Tạo yêu cầu đặt lại mật khẩu
        public static string CreatePasswordResetRequest(string email)
        {
            foreach (var user in localUsers.Values)
            {
                if (user.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
                {
                    // Tạo token và thiết lập thời gian hết hạn
                    string resetToken = GenerateResetToken();
                    user.ResetToken = resetToken;
                    user.ResetTokenExpiry = DateTime.Now.AddMinutes(30); // Hết hạn sau 30 phút

                    SaveLocalUsers();
                    return resetToken;
                }
            }

            // Không tìm thấy email
            return null;
        }

        // Đặt lại mật khẩu
        public static bool ResetPassword(string email, string resetToken, string newPassword)
        {
            foreach (var user in localUsers.Values)
            {
                if (user.Email.Equals(email, StringComparison.OrdinalIgnoreCase) &&
                    user.ResetToken == resetToken &&
                    user.ResetTokenExpiry.HasValue &&
                    user.ResetTokenExpiry.Value > DateTime.Now)
                {
                    // Đặt lại mật khẩu
                    user.PasswordHash = HashPassword(newPassword);
                    user.ResetToken = null;
                    user.ResetTokenExpiry = null;

                    SaveLocalUsers();
                    return true;
                }
            }

            return false;
        }

        // Tạo token đặt lại mật khẩu
        private static string GenerateResetToken()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        // Cập nhật điểm cho người dùng
        public static bool UpdatePoints(string username, int points)
        {
            if (localUsers.TryGetValue(username.ToLower(), out UserInfo user))
            {
                user.Points += points;
                SaveLocalUsers();
                return true;
            }
            return false;
        }

        // Lấy thông tin điểm của người dùng
        public static int GetUserPoints(string username)
        {
            if (username.ToLower() == "admin")
                return 9999;

            if (localUsers.TryGetValue(username.ToLower(), out UserInfo user))
            {
                return user.Points;
            }
            return 1000; // Mặc định
        }

        // Kiểm tra người dùng có VIP không
        public static bool IsUserVIP(string username)
        {
            if (username.ToLower() == "admin")
                return true;

            if (localUsers.TryGetValue(username.ToLower(), out UserInfo user))
            {
                return user.IsVIP;
            }
            return false;
        }

        // Nâng cấp người dùng thành VIP
        public static bool UpgradeToVIP(string username)
        {
            if (localUsers.TryGetValue(username.ToLower(), out UserInfo user))
            {
                if (user.Points >= 500)
                {
                    user.Points -= 500;
                    user.IsVIP = true;
                    SaveLocalUsers();
                    return true;
                }
            }
            return false;
        }

        // Lưu danh sách người dùng cục bộ
        private static void SaveLocalUsers()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(localDataPath))
                {
                    foreach (var user in localUsers.Values)
                    {
                        // Format: username|passwordHash|email|points|isVIP|createdDate
                        string line = $"{user.Username}|{user.PasswordHash}|{user.Email}|{user.Points}|{user.IsVIP}|{user.CreatedDate}";
                        writer.WriteLine(line);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving local users: {ex.Message}");
            }
        }

        // Tải danh sách người dùng cục bộ
        private static void LoadLocalUsers()
        {
            try
            {
                if (File.Exists(localDataPath))
                {
                    string[] lines = File.ReadAllLines(localDataPath);
                    foreach (string line in lines)
                    {
                        string[] parts = line.Split('|');
                        if (parts.Length >= 6)
                        {
                            string username = parts[0];
                            UserInfo user = new UserInfo
                            {
                                Username = username,
                                PasswordHash = parts[1],
                                Email = parts[2],
                                Points = int.Parse(parts[3]),
                                IsVIP = bool.Parse(parts[4]),
                                CreatedDate = DateTime.Parse(parts[5])
                            };

                            localUsers[username.ToLower()] = user;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading local users: {ex.Message}");
            }
        }

        // Hàm băm mật khẩu
        public static string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}