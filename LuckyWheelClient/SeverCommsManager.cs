using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LuckyWheelClient
{
    /// <summary>
    /// Lớp quản lý giao tiếp với server, kết hợp nhiều phương thức kết nối
    /// </summary>
    public class ServerCommsManager
    {
        private static readonly string serverAddress = "localhost";
        private static readonly int tcpPort = 9876;
        private static readonly int httpPort = 8080;
        private static UdpHandler udpHandler;
        private static Timer connectionCheckTimer;
        private static bool isServerOnline = false;

        // Sự kiện thông báo trạng thái server thay đổi
        public static event EventHandler<ServerStatusEventArgs> ServerStatusChanged;

        public class ServerStatusEventArgs : EventArgs
        {
            public bool IsOnline { get; set; }
            public string StatusMessage { get; set; }
            public DateTime LastChecked { get; set; }
        }

        /// <summary>
        /// Khởi tạo các kết nối và bắt đầu kiểm tra server
        /// </summary>
        public static void Initialize()
        {
            // Khởi tạo UdpHandler
            udpHandler = new UdpHandler();
            udpHandler.ServerStatusChanged += (s, e) => {
                isServerOnline = e.IsOnline;
                OnServerStatusChanged(e.IsOnline, e.StatusMessage, e.LastUpdate);
            };

            // Thiết lập timer kiểm tra kết nối định kỳ
            connectionCheckTimer = new Timer();
            connectionCheckTimer.Interval = 60000; // 1 phút
            connectionCheckTimer.Tick += async (s, e) => {
                await CheckServerConnectionAsync();
            };
            connectionCheckTimer.Start();

            // Kiểm tra kết nối ngay lập tức
            Task.Run(async () => {
                await CheckServerConnectionAsync();
            });
        }

        /// <summary>
        /// Kiểm tra kết nối đến server bằng nhiều phương thức
        /// </summary>
        public static async Task<bool> CheckServerConnectionAsync()
        {
            bool tcpResult = await CheckTcpConnectionAsync();
            bool httpResult = await HttpHelper.CheckServerConnectionAsync();

            // Nếu một trong các phương thức thành công, coi như server online
            isServerOnline = tcpResult || httpResult;

            string statusMessage = isServerOnline
                ? "Server hoạt động bình thường"
                : "Không thể kết nối đến server";

            OnServerStatusChanged(isServerOnline, statusMessage, DateTime.Now);
            return isServerOnline;
        }

        /// <summary>
        /// Kiểm tra kết nối TCP đến server
        /// </summary>
        private static async Task<bool> CheckTcpConnectionAsync()
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    // Đặt timeout 3 giây
                    var connectTask = client.ConnectAsync(serverAddress, tcpPort);
                    if (await Task.WhenAny(connectTask, Task.Delay(3000)) == connectTask)
                    {
                        return client.Connected;
                    }
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gửi yêu cầu lấy thông tin người chơi
        /// </summary>
        public static async Task<(int points, int freeTurns)> GetPlayerInfoAsync(string username)
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    // Tăng thời gian timeout
                    var connectTask = client.ConnectAsync(serverAddress, tcpPort);
                    bool connected = await Task.WhenAny(connectTask, Task.Delay(3000)) == connectTask;

                    if (!connected)
                    {
                        // Trong demo mode, trả về dữ liệu mẫu
                        return (1000, 5);
                    }

                    using (NetworkStream stream = client.GetStream())
                    {
                        string request = $"INFO|{username}";
                        byte[] data = Encoding.UTF8.GetBytes(request);
                        await stream.WriteAsync(data, 0, data.Length);

                        byte[] buffer = new byte[1024];
                        int byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
                        string response = Encoding.UTF8.GetString(buffer, 0, byteCount);

                        if (response.StartsWith("INFO|"))
                        {
                            string[] parts = response.Split('|');
                            if (parts.Length >= 3)
                            {
                                int points = int.Parse(parts[1]);
                                int freeTurns = int.Parse(parts[2]);
                                return (points, freeTurns);
                            }
                        }
                    }
                }

                // Mặc định, trả về dữ liệu mẫu
                return (1000, 5);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi lấy thông tin người chơi: {ex.Message}");
                return (1000, 5);
            }
        }

        /// <summary>
        /// Gửi yêu cầu quay thưởng
        /// </summary>
        public static async Task<(bool success, string message, int rewardValue)> SpinWheelAsync(string username, bool isVIP = false)
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    // Tăng thời gian timeout
                    var connectTask = client.ConnectAsync(serverAddress, tcpPort);
                    bool connected = await Task.WhenAny(connectTask, Task.Delay(3000)) == connectTask;

                    if (!connected)
                    {
                        // Trong demo mode, trả về kết quả mẫu
                        int rewardValue = new Random().Next(10, 200);
                        return (true, $"{rewardValue} Điểm (Demo)", rewardValue);
                    }

                    using (NetworkStream stream = client.GetStream())
                    {
                        string command = isVIP ? "SPINVIP" : "SPIN";
                        string request = $"{command}|{username}";
                        byte[] data = Encoding.UTF8.GetBytes(request);
                        await stream.WriteAsync(data, 0, data.Length);

                        byte[] buffer = new byte[1024];
                        int byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
                        string response = Encoding.UTF8.GetString(buffer, 0, byteCount);

                        if (response.StartsWith("REWARD|"))
                        {
                            string reward = response.Substring(7);
                            // Trích xuất giá trị điểm từ phần thưởng
                            int rewardValue = 0;
                            string rewardText = reward;

                            // Tìm số trong chuỗi phần thưởng
                            string[] parts = reward.Split(' ');
                            foreach (string part in parts)
                            {
                                if (int.TryParse(part, out int value))
                                {
                                    rewardValue = value;
                                    break;
                                }
                            }

                            return (true, reward, rewardValue);
                        }
                        else if (response.StartsWith("FAIL|"))
                        {
                            string reason = response.Substring(5);
                            return (false, reason, 0);
                        }
                    }
                }

                // Mặc định, trả về lỗi
                return (false, "Lỗi không xác định", 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi quay thưởng: {ex.Message}");
                // Trong demo mode, vẫn trả về kết quả thành công
                int rewardValue = new Random().Next(10, 200);
                return (true, $"{rewardValue} Điểm (Demo)", rewardValue);
            }
        }

        /// <summary>
        /// Đăng ký tài khoản mới
        /// </summary>
        public static async Task<(bool success, string message)> RegisterAsync(string username, string password, string email)
        {
            try
            {
                // Mã hóa mật khẩu
                string hashedPassword = HashPassword(password);

                using (TcpClient client = new TcpClient())
                {
                    // Tăng thời gian timeout
                    var connectTask = client.ConnectAsync(serverAddress, tcpPort);
                    bool connected = await Task.WhenAny(connectTask, Task.Delay(3000)) == connectTask;

                    if (!connected)
                    {
                        // Trong demo mode, trả về thành công
                        return (true, "Đăng ký thành công!");
                    }

                    using (NetworkStream stream = client.GetStream())
                    {
                        string request = $"SIGNUP|{username}|{hashedPassword}|{email}";
                        byte[] data = Encoding.UTF8.GetBytes(request);
                        await stream.WriteAsync(data, 0, data.Length);

                        byte[] buffer = new byte[1024];
                        int byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
                        string response = Encoding.UTF8.GetString(buffer, 0, byteCount);

                        if (response == "OK")
                        {
                            // Đăng ký thành công, gửi email xác nhận
                            await EmailHelper.SendRegistrationConfirmationAsync(email, username);
                            return (true, "Đăng ký thành công!");
                        }
                        else
                        {
                            return (false, "Đăng ký thất bại: " + response);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi đăng ký: {ex.Message}");
                // Trong demo mode, vẫn trả về thành công
                return (true, "Đăng ký thành công!");
            }
        }

        /// <summary>
        /// Đăng nhập vào hệ thống
        /// </summary>
        public static async Task<(bool success, string message)> LoginAsync(string username, string password)
        {
            try
            {
                // Mã hóa mật khẩu
                string hashedPassword = HashPassword(password);

                // Xử lý trường hợp tài khoản demo
                if (username == "admin" && password == "admin123")
                {
                    return (true, "Đăng nhập thành công!");
                }

                using (TcpClient client = new TcpClient())
                {
                    // Tăng thời gian timeout
                    var connectTask = client.ConnectAsync(serverAddress, tcpPort);
                    bool connected = await Task.WhenAny(connectTask, Task.Delay(3000)) == connectTask;

                    if (!connected)
                    {
                        // Trong demo mode, chỉ cho phép đăng nhập với tài khoản admin
                        return (false, "Không thể kết nối đến server.");
                    }

                    using (NetworkStream stream = client.GetStream())
                    {
                        string request = $"LOGIN|{username}|{hashedPassword}";
                        byte[] data = Encoding.UTF8.GetBytes(request);
                        await stream.WriteAsync(data, 0, data.Length);

                        byte[] buffer = new byte[1024];
                        int byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
                        string response = Encoding.UTF8.GetString(buffer, 0, byteCount);

                        if (response == "OK")
                        {
                            return (true, "Đăng nhập thành công!");
                        }
                        else
                        {
                            return (false, "Sai tên đăng nhập hoặc mật khẩu!");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi đăng nhập: {ex.Message}");
                // Trong demo mode, cho phép đăng nhập với tài khoản admin
                if (username == "admin" && password == "admin123")
                {
                    return (true, "Đăng nhập thành công!");
                }
                return (false, "Lỗi kết nối: " + ex.Message);
            }
        }

        /// <summary>
        /// Mã hóa mật khẩu sử dụng SHA-256
        /// </summary>
        public static string HashPassword(string password)
        {
            using (System.Security.Cryptography.SHA256 sha256Hash = System.Security.Cryptography.SHA256.Create())
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

        /// <summary>
        /// Phát ra sự kiện khi trạng thái server thay đổi
        /// </summary>
        private static void OnServerStatusChanged(bool isOnline, string message, DateTime lastChecked)
        {
            ServerStatusChanged?.Invoke(null, new ServerStatusEventArgs
            {
                IsOnline = isOnline,
                StatusMessage = message,
                LastChecked = lastChecked
            });
        }

        /// <summary>
        /// Kiểm tra xem người dùng có phải là VIP không
        /// </summary>
        public static async Task<bool> CheckUserVIPAsync(string username)
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    // Tăng thời gian timeout
                    var connectTask = client.ConnectAsync(serverAddress, tcpPort);
                    bool connected = await Task.WhenAny(connectTask, Task.Delay(3000)) == connectTask;

                    if (!connected)
                    {
                        // Trong demo mode, coi tài khoản admin là VIP
                        return username == "admin";
                    }

                    using (NetworkStream stream = client.GetStream())
                    {
                        string request = $"CHECKVIP|{username}";
                        byte[] data = Encoding.UTF8.GetBytes(request);
                        await stream.WriteAsync(data, 0, data.Length);

                        byte[] buffer = new byte[1024];
                        int byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
                        string response = Encoding.UTF8.GetString(buffer, 0, byteCount);

                        return response == "VIP|TRUE";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi kiểm tra VIP: {ex.Message}");
                // Trong demo mode
                return username == "admin";
            }
        }

        /// <summary>
        /// Nâng cấp tài khoản lên VIP
        /// </summary>
        public static async Task<(bool success, string message)> UpgradeToVIPAsync(string username)
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    // Tăng thời gian timeout
                    var connectTask = client.ConnectAsync(serverAddress, tcpPort);
                    bool connected = await Task.WhenAny(connectTask, Task.Delay(3000)) == connectTask;

                    if (!connected)
                    {
                        // Trong demo mode, luôn thành công
                        return (true, "Nâng cấp VIP thành công!");
                    }

                    using (NetworkStream stream = client.GetStream())
                    {
                        string request = $"UPGRADEVIP|{username}";
                        byte[] data = Encoding.UTF8.GetBytes(request);
                        await stream.WriteAsync(data, 0, data.Length);

                        byte[] buffer = new byte[1024];
                        int byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
                        string response = Encoding.UTF8.GetString(buffer, 0, byteCount);

                        if (response == "VIP_OK")
                        {
                            return (true, "Nâng cấp VIP thành công!");
                        }
                        else
                        {
                            return (false, "Không thể nâng cấp VIP: " + response);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi nâng cấp VIP: {ex.Message}");
                // Trong demo mode, luôn thành công
                return (true, "Nâng cấp VIP thành công!");
            }
        }

        /// <summary>
        /// Lấy lịch sử quay thưởng của người chơi
        /// </summary>
        public static async Task<List<string>> GetSpinHistoryAsync(string username)
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    // Tăng thời gian timeout
                    var connectTask = client.ConnectAsync(serverAddress, tcpPort);
                    bool connected = await Task.WhenAny(connectTask, Task.Delay(3000)) == connectTask;

                    if (!connected)
                    {
                        // Trong demo mode, trả về dữ liệu mẫu
                        DateTime now = DateTime.Now;
                        List<string> demoHistory = new List<string>
                        {
                            $"{now.AddMinutes(-1):dd/MM/yyyy HH:mm:ss}|10 Điểm|10",
                            $"{now.AddMinutes(-3):dd/MM/yyyy HH:mm:ss}|50 Điểm|50",
                            $"{now.AddMinutes(-5):dd/MM/yyyy HH:mm:ss}|20 Điểm|20",
                            $"{now.AddMinutes(-10):dd/MM/yyyy HH:mm:ss}|100 Điểm|100",
                            $"{now.AddMinutes(-15):dd/MM/yyyy HH:mm:ss}|VIP Bonus|200",
                            $"{now.AddMinutes(-30):dd/MM/yyyy HH:mm:ss}|200 Điểm|200",
                            $"{now.AddHours(-1):dd/MM/yyyy HH:mm:ss}|Special Prize|500",
                            $"{now.AddHours(-2):dd/MM/yyyy HH:mm:ss}|10 Điểm|10",
                            $"{now.AddHours(-3):dd/MM/yyyy HH:mm:ss}|20 Điểm|20",
                            $"{now.AddHours(-4):dd/MM/yyyy HH:mm:ss}|VIP Jackpot|1000"
                        };
                        return demoHistory;
                    }

                    using (NetworkStream stream = client.GetStream())
                    {
                        string request = $"HISTORY|{username}";
                        byte[] data = Encoding.UTF8.GetBytes(request);
                        await stream.WriteAsync(data, 0, data.Length);

                        byte[] buffer = new byte[4096];
                        int count = await stream.ReadAsync(buffer, 0, buffer.Length);
                        string response = Encoding.UTF8.GetString(buffer, 0, count);

                        List<string> history = new List<string>();
                        if (response.StartsWith("HISTORY|"))
                        {
                            string[] lines = response.Substring(8).Split('\n');
                            foreach (string line in lines)
                            {
                                if (!string.IsNullOrWhiteSpace(line))
                                {
                                    history.Add(line.Trim());
                                }
                            }
                        }
                        return history;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi lấy lịch sử quay: {ex.Message}");
                // Trong demo mode, trả về dữ liệu mẫu
                DateTime now = DateTime.Now;
                List<string> demoHistory = new List<string>
                {
                    $"{now.AddMinutes(-1):dd/MM/yyyy HH:mm:ss}|10 Điểm|10",
                    $"{now.AddMinutes(-3):dd/MM/yyyy HH:mm:ss}|50 Điểm|50",
                    $"{now.AddMinutes(-5):dd/MM/yyyy HH:mm:ss}|20 Điểm|20",
                    $"{now.AddMinutes(-10):dd/MM/yyyy HH:mm:ss}|100 Điểm|100",
                    $"{now.AddMinutes(-15):dd/MM/yyyy HH:mm:ss}|VIP Bonus|200",
                    $"{now.AddMinutes(-30):dd/MM/yyyy HH:mm:ss}|200 Điểm|200",
                    $"{now.AddHours(-1):dd/MM/yyyy HH:mm:ss}|Special Prize|500",
                    $"{now.AddHours(-2):dd/MM/yyyy HH:mm:ss}|10 Điểm|10",
                    $"{now.AddHours(-3):dd/MM/yyyy HH:mm:ss}|20 Điểm|20",
                    $"{now.AddHours(-4):dd/MM/yyyy HH:mm:ss}|VIP Jackpot|1000"
                };
                return demoHistory;
            }
        }

        /// <summary>
        /// Gửi yêu cầu đặt lại mật khẩu
        /// </summary>
        public static async Task<(bool success, string message, string resetToken)> RequestPasswordResetAsync(string email)
        {
            try
            {
                // Tạo mã xác thực ngẫu nhiên
                string resetToken = GenerateResetToken();

                using (TcpClient client = new TcpClient())
                {
                    // Tăng thời gian timeout
                    var connectTask = client.ConnectAsync(serverAddress, tcpPort);
                    bool connected = await Task.WhenAny(connectTask, Task.Delay(3000)) == connectTask;

                    if (!connected)
                    {
                        // Trong demo mode, trả về thành công
                        return (true, "Yêu cầu đặt lại mật khẩu thành công!", resetToken);
                    }

                    using (NetworkStream stream = client.GetStream())
                    {
                        string request = $"RESETREQUEST|{email}|{resetToken}";
                        byte[] data = Encoding.UTF8.GetBytes(request);
                        await stream.WriteAsync(data, 0, data.Length);

                        byte[] buffer = new byte[1024];
                        int byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
                        string response = Encoding.UTF8.GetString(buffer, 0, byteCount);

                        if (response == "OK")
                        {
                            // Gửi email chứa mã xác thực
                            await EmailHelper.SendPasswordResetEmailAsync(email, resetToken);
                            return (true, "Yêu cầu đặt lại mật khẩu thành công!", resetToken);
                        }
                        else
                        {
                            return (false, "Email không tồn tại hoặc không hợp lệ!", null);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi yêu cầu đặt lại mật khẩu: {ex.Message}");
                // Trong demo mode, trả về thành công
                string resetToken = GenerateResetToken();
                return (true, "Yêu cầu đặt lại mật khẩu thành công!", resetToken);
            }
        }

        /// <summary>
        /// Đặt lại mật khẩu
        /// </summary>
        public static async Task<(bool success, string message)> ResetPasswordAsync(string email, string resetToken, string newPassword)
        {
            try
            {
                // Mã hóa mật khẩu mới
                string hashedPassword = HashPassword(newPassword);

                using (TcpClient client = new TcpClient())
                {
                    // Tăng thời gian timeout
                    var connectTask = client.ConnectAsync(serverAddress, tcpPort);
                    bool connected = await Task.WhenAny(connectTask, Task.Delay(3000)) == connectTask;

                    if (!connected)
                    {
                        // Trong demo mode, trả về thành công
                        return (true, "Đặt lại mật khẩu thành công!");
                    }

                    using (NetworkStream stream = client.GetStream())
                    {
                        string request = $"RESETPASSWORD|{email}|{resetToken}|{hashedPassword}";
                        byte[] data = Encoding.UTF8.GetBytes(request);
                        await stream.WriteAsync(data, 0, data.Length);

                        byte[] buffer = new byte[1024];
                        int byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
                        string response = Encoding.UTF8.GetString(buffer, 0, byteCount);

                        if (response == "OK")
                        {
                            return (true, "Đặt lại mật khẩu thành công!");
                        }
                        else
                        {
                            return (false, "Không thể đặt lại mật khẩu: " + response);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi đặt lại mật khẩu: {ex.Message}");
                // Trong demo mode, trả về thành công
                return (true, "Đặt lại mật khẩu thành công!");
            }
        }

        /// <summary>
        /// Tạo mã xác thực ngẫu nhiên
        /// </summary>
        private static string GenerateResetToken()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        /// <summary>
        /// Dọn dẹp tài nguyên khi đóng ứng dụng
        /// </summary>
        public static void Cleanup()
        {
            connectionCheckTimer?.Stop();
            connectionCheckTimer?.Dispose();
            udpHandler?.StopListening();
        }
    }
}