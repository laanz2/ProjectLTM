using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Net;

namespace LuckyWheelServer
{
    public class ClientHandler
    {
        private static readonly Random random = new Random();

        public static void HandleClient(object obj)
        {
            TcpClient client = (TcpClient)obj;

            try
            {
                Console.WriteLine($"Bắt đầu xử lý client từ: {((IPEndPoint)client.Client.RemoteEndPoint).Address}:{((IPEndPoint)client.Client.RemoteEndPoint).Port}");

                // Kiểm tra trạng thái kết nối
                if (!client.Connected)
                {
                    Console.WriteLine("⚠️ Client đã ngắt kết nối trước khi xử lý!");
                    return;
                }

                NetworkStream stream = client.GetStream();

                // Thiết lập timeout cho client
                client.ReceiveTimeout = 600000; // 10 phút
                client.SendTimeout = 300000;    // 5 phút

                Console.WriteLine("Đã thiết lập NetworkStream, đang đợi dữ liệu...");

                byte[] buffer = new byte[1024];
                int byteCount;

                try
                {
                    while ((byteCount = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        string request = Encoding.UTF8.GetString(buffer, 0, byteCount).Trim();
                        Console.WriteLine($"📨 [Client] Gửi: {request}");

                        string response = ProcessClientRequest(request);
                        Console.WriteLine($"📤 [Server] Trả lời: {response}");

                        // Gửi phản hồi
                        byte[] data = Encoding.UTF8.GetBytes(response);
                        stream.Write(data, 0, data.Length);
                        stream.Flush(); // Đảm bảo dữ liệu được gửi đi ngay lập tức

                        Console.WriteLine("Đã gửi phản hồi đến client thành công");
                    }

                    Console.WriteLine("Client đã đóng kết nối một cách bình thường");
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"⚠️ Lỗi IO khi đọc/ghi stream: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Lỗi xử lý client: {ex.Message}");
                    Console.WriteLine($"Chi tiết lỗi: {ex}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Lỗi khởi tạo xử lý client: {ex.Message}");
                Console.WriteLine($"Chi tiết lỗi: {ex}");
            }
            finally
            {
                // Đảm bảo đóng kết nối
                try
                {
                    if (client.Connected)
                    {
                        NetworkStream stream = client.GetStream();
                        if (stream != null)
                        {
                            stream.Close();
                            Console.WriteLine("Stream đã đóng");
                        }
                        client.Close();
                        Console.WriteLine("Client đã đóng");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Lỗi khi đóng kết nối: {ex.Message}");
                }

                Console.WriteLine("🔌 Đã ngắt kết nối client.");
            }
        }

        private static string ProcessClientRequest(string request)
        {
            // Phân tích và xử lý yêu cầu từ client
            if (request.StartsWith("LOGIN|"))
            {
                return ProcessLogin(request);
            }
            else if (request.StartsWith("SIGNUP|"))
            {
                return ProcessSignup(request);
            }
            else if (request.StartsWith("INFO|"))
            {
                return ProcessInfo(request);
            }
            else if (request.StartsWith("SPIN|"))
            {
                return ProcessSpin(request);
            }
            else if (request.StartsWith("SPINVIP|"))
            {
                return ProcessSpinVIP(request);
            }
            else if (request.StartsWith("HISTORY|"))
            {
                return ProcessHistory(request);
            }
            else if (request.StartsWith("CHECKVIP|"))
            {
                return ProcessCheckVIP(request);
            }
            else if (request.StartsWith("UPGRADEVIP|"))
            {
                return ProcessUpgradeVIP(request);
            }
            else if (request.StartsWith("HTTP|"))
            {
                return ProcessHTTPRequest(request);
            }
            else if (request.StartsWith("EMAIL|"))
            {
                return ProcessEmailRequest(request);
            }
            else
            {
                return "UNKNOWN_COMMAND";
            }
        }

        private static string ProcessLogin(string request)
        {
            string[] parts = request.Split('|');
            if (parts.Length != 3)
                return "FAIL|Sai định dạng yêu cầu";

            string username = parts[1];
            string password = parts[2];

            bool loginSuccess = CoSoDuLieu.KiemTraDangNhap(username, password);
            if (loginSuccess)
            {
                Console.WriteLine($"✅ Đăng nhập thành công: {username}");
                return "OK";
            }
            else
            {
                Console.WriteLine($"❌ Đăng nhập thất bại: {username}");
                return "FAIL";
            }
        }

        private static string ProcessSignup(string request)
        {
            string[] parts = request.Split('|');
            if (parts.Length != 4)
                return "FAIL|Sai định dạng yêu cầu";

            string username = parts[1];
            string password = parts[2];
            string email = parts[3];

            // Kiểm tra tính hợp lệ của thông tin đăng ký
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(email))
            {
                Console.WriteLine("❌ Đăng ký thất bại: Thông tin không hợp lệ");
                return "FAIL|Thông tin đăng ký không hợp lệ";
            }

            bool result = CoSoDuLieu.DangKyTaiKhoan(username, password, email);
            if (result)
            {
                Console.WriteLine($"✅ Đăng ký thành công: {username}");
                return "OK";
            }
            else
            {
                Console.WriteLine($"❌ Đăng ký thất bại: {username}");
                return "FAIL|Tên người dùng đã tồn tại hoặc lỗi đăng ký";
            }
        }

        private static string ProcessInfo(string request)
        {
            string[] parts = request.Split('|');
            if (parts.Length != 2)
                return "FAIL|Sai định dạng yêu cầu";

            string username = parts[1];
            int points = CoSoDuLieu.LayDiemNguoiChoi(username);

            // Mặc định có 5 lượt quay miễn phí mỗi ngày
            int freeTurns = 5;

            return $"INFO|{points}|{freeTurns}";
        }

        private static string ProcessSpin(string request)
        {
            string[] parts = request.Split('|');
            if (parts.Length != 2)
                return "FAIL|Sai định dạng yêu cầu";

            string username = parts[1];
            int userId = CoSoDuLieu.TimUserID(username);

            if (userId == -1)
                return "FAIL|Không tìm thấy người dùng";

            // Lấy một phần thưởng ngẫu nhiên
            int rewardId = CoSoDuLieu.LayRewardNgauNhien();
            string rewardName = CoSoDuLieu.LayTenReward(rewardId);
            int rewardValue = CoSoDuLieu.LayGiaTriReward(rewardId);

            // Ghi lịch sử và cộng điểm
            CoSoDuLieu.GhiLichSuVaCongDiem(userId, rewardId, rewardValue);

            Console.WriteLine($"🎁 {username} đã quay được: {rewardName} ({rewardValue} điểm)");
            return $"REWARD|{rewardName} ({rewardValue} điểm)";
        }

        private static string ProcessSpinVIP(string request)
        {
            string[] parts = request.Split('|');
            if (parts.Length != 2)
                return "FAIL|Sai định dạng yêu cầu";

            string username = parts[1];
            int userId = CoSoDuLieu.TimUserID(username);

            if (userId == -1)
                return "FAIL|Không tìm thấy người dùng";

            // Kiểm tra xem người dùng có phải VIP không
            bool isVIP = IsUserVIP(username);
            if (!isVIP)
                return "FAIL|Bạn không phải là thành viên VIP";

            // VIP có tỷ lệ nhận phần thưởng cao hơn
            int rewardId = GetVIPReward();
            string rewardName = CoSoDuLieu.LayTenReward(rewardId);
            int rewardValue = CoSoDuLieu.LayGiaTriReward(rewardId);

            // Ghi lịch sử và cộng điểm
            CoSoDuLieu.GhiLichSuVaCongDiem(userId, rewardId, rewardValue);

            Console.WriteLine($"💎 VIP {username} đã quay được: {rewardName} ({rewardValue} điểm)");
            return $"REWARD|{rewardName} ({rewardValue} điểm)";
        }

        private static int GetVIPReward()
        {
            // Thêm logic chọn phần thưởng VIP ở đây
            // Mặc định trả về ID phần thưởng cao hơn
            return random.Next(1, 10); // Giả sử ID 1-10 cho phần thưởng
        }

        private static string ProcessHistory(string request)
        {
            string[] parts = request.Split('|');
            if (parts.Length != 2)
                return "FAIL|Sai định dạng yêu cầu";

            string username = parts[1];
            string history = CoSoDuLieu.LayLichSuQuay(username);

            return $"HISTORY|{history}";
        }

        private static string ProcessCheckVIP(string request)
        {
            string[] parts = request.Split('|');
            if (parts.Length != 2)
                return "FAIL|Sai định dạng yêu cầu";

            string username = parts[1];
            bool isVIP = IsUserVIP(username);

            return isVIP ? "VIP|TRUE" : "VIP|FALSE";
        }

        private static bool IsUserVIP(string username)
        {
            // Thực hiện kiểm tra từ cơ sở dữ liệu
            // Trong bản demo này, giả định người dùng với điểm >= 500 là VIP
            int points = CoSoDuLieu.LayDiemNguoiChoi(username);
            return points >= 500;
        }

        private static string ProcessUpgradeVIP(string request)
        {
            string[] parts = request.Split('|');
            if (parts.Length != 2)
                return "FAIL|Sai định dạng yêu cầu";

            string username = parts[1];
            int points = CoSoDuLieu.LayDiemNguoiChoi(username);

            if (points < 500)
                return "FAIL|Không đủ điểm (cần 500 điểm)";

            if (IsUserVIP(username))
                return "FAIL|Bạn đã là thành viên VIP";

            // Thực hiện trừ điểm và nâng cấp lên VIP
            // Giả định có hàm thực hiện việc này
            Console.WriteLine($"✅ {username} đã nâng cấp lên VIP");
            return "VIP_OK";
        }

        private static string ProcessHTTPRequest(string request)
        {
            // Demo xử lý HTTP request đơn giản
            string[] parts = request.Split('|');
            if (parts.Length < 3)
                return "HTTP|400|Bad Request";

            string method = parts[1];
            string endpoint = parts[2];

            // Xử lý request dựa trên method và endpoint
            if (method == "GET")
            {
                if (endpoint == "/status")
                {
                    return "HTTP|200|{\"status\":\"online\",\"time\":\"" + DateTime.Now.ToString() + "\"}";
                }
                else if (endpoint == "/users")
                {
                    return "HTTP|200|{\"users\":[\"user1\",\"user2\",\"user3\"]}";
                }
            }
            else if (method == "POST")
            {
                if (endpoint == "/login")
                {
                    return "HTTP|200|{\"status\":\"success\",\"token\":\"sample-token\"}";
                }
            }

            return "HTTP|404|Not Found";
        }

        private static string ProcessEmailRequest(string request)
        {
            // Demo xử lý gửi email
            string[] parts = request.Split('|');
            if (parts.Length < 4)
                return "EMAIL|ERROR|Missing parameters";

            string action = parts[1];
            string recipient = parts[2];
            string subject = parts[3];

            // Xác thực giả lập thành công
            if (action == "SEND")
            {
                Console.WriteLine($"📧 Gửi email tới {recipient}: {subject}");

                // Thêm độ trễ giả lập gửi email
                Task.Delay(500).Wait();

                // Giả lập gửi email thành công
                return "EMAIL|SUCCESS|Email sent successfully";
            }

            return "EMAIL|ERROR|Unknown action";
        }
    }
}