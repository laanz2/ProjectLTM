using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq; // Thêm namespace này để sử dụng phương thức Sum()
using System.Security.Cryptography;
using System.Text;

namespace LuckyWheelServer
{
    public class CoSoDuLieu
    {
        private static readonly string chuoiKetNoi = @"Server=DESKTOP-0OJI63E;Database=LuckyWheelDB;User Id=luckyuser;Password=Lucky@123;";

        public static SqlConnection MoKetNoi()
        {
            try
            {
                SqlConnection ketNoi = new SqlConnection(chuoiKetNoi);
                ketNoi.Open();
                Console.WriteLine("✅ Ket noi CSDL thanh cong!");
                return ketNoi;
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Loi khi ket noi CSDL: " + ex.Message);
                throw;
            }
        }

        // Hàm mã hóa mật khẩu
        public static string MaHoaMatKhau(string matKhau)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(matKhau));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        // Hàm đăng ký tài khoản - Đã sửa để trả về bool
        public static bool DangKyTaiKhoan(string ten, string matKhau, string email)
        {
            try
            {
                using (SqlConnection ketNoi = MoKetNoi())
                {
                    // Kiểm tra xem tên người dùng đã tồn tại chưa
                    string kiemTraLenh = "SELECT COUNT(*) FROM Users WHERE Username = @ten";
                    using (SqlCommand kiemTraCmd = new SqlCommand(kiemTraLenh, ketNoi))
                    {
                        kiemTraCmd.Parameters.AddWithValue("@ten", ten);
                        int count = (int)kiemTraCmd.ExecuteScalar();
                        if (count > 0)
                        {
                            return false; // Tên người dùng đã tồn tại
                        }
                    }

                    string hashedPassword = MaHoaMatKhau(matKhau);
                    string lenh = "INSERT INTO Users (Username, Password, Email, Points) VALUES (@ten, @password, @email, 0)";
                    using (SqlCommand cmd = new SqlCommand(lenh, ketNoi))
                    {
                        cmd.Parameters.AddWithValue("@ten", ten);
                        cmd.Parameters.AddWithValue("@password", hashedPassword);
                        cmd.Parameters.AddWithValue("@email", email);
                        cmd.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Lỗi khi đăng ký tài khoản: " + ex.Message);
                return false;
            }
        }

        // Kiểm tra đăng nhập
        public static bool KiemTraDangNhap(string ten, string matKhau)
        {
            try
            {
                using (SqlConnection ketNoi = MoKetNoi())
                {
                    string lenh = "SELECT Password FROM Users WHERE Username = @ten";
                    using (SqlCommand cmd = new SqlCommand(lenh, ketNoi))
                    {
                        cmd.Parameters.AddWithValue("@ten", ten);
                        object result = cmd.ExecuteScalar();

                        if (result == null)
                            return false; // Không tìm thấy người dùng

                        string storedPasswordHash = (string)result;
                        // So sánh mật khẩu đã mã hóa
                        return storedPasswordHash == MaHoaMatKhau(matKhau);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Lỗi khi kiểm tra đăng nhập: " + ex.Message);
                return false;
            }
        }

        // Tìm ID người dùng từ tên đăng nhập
        public static int TimUserID(string username)
        {
            try
            {
                using (SqlConnection ketNoi = MoKetNoi())
                {
                    string lenh = "SELECT UserID FROM Users WHERE Username = @username";
                    using (SqlCommand cmd = new SqlCommand(lenh, ketNoi))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        object result = cmd.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                            return (int)result;
                        else
                            return -1; // Không tìm thấy người dùng
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Lỗi khi tìm ID người dùng: " + ex.Message);
                return -1;
            }
        }

        // Lấy điểm của người chơi
        public static int LayDiemNguoiChoi(string username)
        {
            try
            {
                using (SqlConnection ketNoi = MoKetNoi())
                {
                    string lenh = "SELECT Points FROM Users WHERE Username = @username";
                    using (SqlCommand cmd = new SqlCommand(lenh, ketNoi))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        object result = cmd.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                            return (int)result;
                        else
                            return 0; // Mặc định 0 điểm hoặc không tìm thấy
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Lỗi khi lấy điểm người chơi: " + ex.Message);
                return 0;
            }
        }

        // Lấy lịch sử quay của người chơi
        public static string LayLichSuQuay(string username)
        {
            try
            {
                using (SqlConnection ketNoi = MoKetNoi())
                {
                    int userId = TimUserID(username);
                    if (userId == -1) return "Không tìm thấy người dùng.";

                    string lenh = @"
                        SELECT TOP 10 r.RewardName, h.RewardValue, h.SpinTime 
                        FROM SpinHistory h
                        JOIN Rewards r ON h.RewardID = r.RewardID
                        WHERE h.UserID = @userId
                        ORDER BY h.SpinTime DESC";

                    using (SqlCommand cmd = new SqlCommand(lenh, ketNoi))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            string history = "";
                            while (reader.Read())
                            {
                                string rewardName = reader.GetString(0);
                                int rewardValue = reader.GetInt32(1);
                                DateTime spinTime = reader.GetDateTime(2);

                                history += $"{spinTime.ToString("dd/MM/yyyy HH:mm:ss")}|{rewardName}|{rewardValue}\\n";
                            }
                            return history.TrimEnd('\\', 'n');
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Lỗi khi lấy lịch sử quay: " + ex.Message);
                return "Lỗi khi lấy lịch sử.";
            }
        }

        // Lấy random reward ID từ bảng Rewards
        public static int LayRewardNgauNhien()
        {
            try
            {
                using (SqlConnection ketNoi = MoKetNoi())
                {
                    // Lấy tất cả reward có trọng số > 0
                    string lenh = @"
                        SELECT RewardID, Probability 
                        FROM Rewards 
                        WHERE Probability > 0";

                    using (SqlCommand cmd = new SqlCommand(lenh, ketNoi))
                    {
                        // Dùng algorithm tỷ lệ xác suất theo trọng số
                        List<int> rewardIds = new List<int>();
                        List<int> probabilities = new List<int>();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                rewardIds.Add(reader.GetInt32(0));
                                probabilities.Add(reader.GetInt32(1));
                            }
                        }

                        // Nếu không có rewards nào
                        if (rewardIds.Count == 0)
                            return 1; // Trả về ID mặc định

                        // Tính tổng trọng số
                        int sumProbability = probabilities.Sum(); // Sử dụng LINQ Sum() extension method

                        // Tạo số ngẫu nhiên từ 1 đến tổng trọng số
                        Random random = new Random();
                        int randomValue = random.Next(1, sumProbability + 1);

                        // Tìm reward tương ứng với số ngẫu nhiên
                        int cumulativeProbability = 0;
                        for (int i = 0; i < rewardIds.Count; i++)
                        {
                            cumulativeProbability += probabilities[i];
                            if (randomValue <= cumulativeProbability)
                            {
                                return rewardIds[i];
                            }
                        }

                        // Mặc định trả về phần thưởng đầu tiên
                        return rewardIds[0];
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Lỗi khi lấy reward ngẫu nhiên: " + ex.Message);
                return 1; // Trả về ID mặc định
            }
        }

        // Lấy tên phần thưởng từ ID
        public static string LayTenReward(int rewardId)
        {
            try
            {
                using (SqlConnection ketNoi = MoKetNoi())
                {
                    string lenh = "SELECT RewardName FROM Rewards WHERE RewardID = @rewardId";
                    using (SqlCommand cmd = new SqlCommand(lenh, ketNoi))
                    {
                        cmd.Parameters.AddWithValue("@rewardId", rewardId);
                        object result = cmd.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                            return (string)result;
                        else
                            return "Không xác định";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Lỗi khi lấy tên reward: " + ex.Message);
                return "Không xác định";
            }
        }

        // Lấy giá trị phần thưởng từ ID
        public static int LayGiaTriReward(int rewardId)
        {
            try
            {
                using (SqlConnection ketNoi = MoKetNoi())
                {
                    string lenh = "SELECT RewardValue FROM Rewards WHERE RewardID = @rewardId";
                    using (SqlCommand cmd = new SqlCommand(lenh, ketNoi))
                    {
                        cmd.Parameters.AddWithValue("@rewardId", rewardId);
                        object result = cmd.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                            return (int)result;
                        else
                            return 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Lỗi khi lấy giá trị reward: " + ex.Message);
                return 0;
            }
        }

        // Ghi lịch sử quay và cộng điểm cho người chơi
        public static void GhiLichSuVaCongDiem(int userId, int rewardId, int rewardValue)
        {
            try
            {
                using (SqlConnection ketNoi = MoKetNoi())
                {
                    // Bắt đầu transaction
                    SqlTransaction transaction = ketNoi.BeginTransaction();

                    try
                    {
                        // 1. Ghi lịch sử quay
                        string lenhGhiLichSu = @"
                            INSERT INTO SpinHistory (UserID, RewardID, RewardValue, SpinTime) 
                            VALUES (@userId, @rewardId, @rewardValue, GETDATE())";

                        using (SqlCommand cmd = new SqlCommand(lenhGhiLichSu, ketNoi, transaction))
                        {
                            cmd.Parameters.AddWithValue("@userId", userId);
                            cmd.Parameters.AddWithValue("@rewardId", rewardId);
                            cmd.Parameters.AddWithValue("@rewardValue", rewardValue);
                            cmd.ExecuteNonQuery();
                        }

                        // 2. Cộng điểm cho người chơi
                        string lenhCongDiem = @"
                            UPDATE Users SET Points = Points + @rewardValue 
                            WHERE UserID = @userId";

                        using (SqlCommand cmd = new SqlCommand(lenhCongDiem, ketNoi, transaction))
                        {
                            cmd.Parameters.AddWithValue("@userId", userId);
                            cmd.Parameters.AddWithValue("@rewardValue", rewardValue);
                            cmd.ExecuteNonQuery();
                        }

                        // Commit transaction
                        transaction.Commit();
                    }
                    catch
                    {
                        // Rollback nếu có lỗi
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Lỗi khi ghi lịch sử và cộng điểm: " + ex.Message);
            }
        }
    }
}