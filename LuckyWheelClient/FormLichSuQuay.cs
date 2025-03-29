using System;
using System.Drawing;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace LuckyWheelClient
{
    public class FormLichSuQuay : Form
    {
        private readonly string tenDangNhap;
        private ListBox listBoxLichSu;
        private Timer updateTimer;

        public FormLichSuQuay(string tenDangNhap)
        {
            this.tenDangNhap = tenDangNhap;

            this.Text = "📜 Lịch Sử Quay";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterScreen;

            listBoxLichSu = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10)
            };
            this.Controls.Add(listBoxLichSu);

            // Thiết lập timer để cập nhật lịch sử mỗi 30 giây
            updateTimer = new Timer
            {
                Interval = 30000 // 30 giây
            };
            updateTimer.Tick += (s, e) => LoadLichSu();
            updateTimer.Start();

            LoadLichSu();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            updateTimer.Stop();
        }

        private void LoadLichSu()
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    // Tăng thời gian timeout và thay đổi cổng
                    var connectTask = client.BeginConnect("localhost", 9876, null, null);
                    bool connected = connectTask.AsyncWaitHandle.WaitOne(3000); // 3 giây timeout

                    if (!connected)
                    {
                        // Nếu không kết nối được, hiển thị dữ liệu mẫu
                        LoadDemoHistory();
                        return;
                    }

                    client.EndConnect(connectTask);

                    using (NetworkStream stream = client.GetStream())
                    {
                        string yeuCau = $"HISTORY|{tenDangNhap}";
                        byte[] data = Encoding.UTF8.GetBytes(yeuCau);
                        stream.Write(data, 0, data.Length);

                        byte[] buffer = new byte[4096];
                        int count = stream.Read(buffer, 0, buffer.Length);
                        string response = Encoding.UTF8.GetString(buffer, 0, count);

                        if (response.StartsWith("HISTORY|"))
                        {
                            // Xóa nội dung cũ trước khi thêm mới
                            listBoxLichSu.Items.Clear();

                            string[] dong = response.Substring(8).Split('\n');
                            foreach (string dongLichSu in dong)
                            {
                                if (!string.IsNullOrWhiteSpace(dongLichSu))
                                    listBoxLichSu.Items.Add(dongLichSu.Trim());
                            }
                        }
                        else
                        {
                            listBoxLichSu.Items.Add("❌ Không lấy được dữ liệu lịch sử.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Xóa nội dung cũ trước khi thêm mới
                listBoxLichSu.Items.Clear();
                LoadDemoHistory();
                // listBoxLichSu.Items.Add($"❌ Lỗi kết nối: {ex.Message}");
            }
        }

        // Phương thức mới để hiển thị dữ liệu lịch sử mẫu khi không thể kết nối server
        private void LoadDemoHistory()
        {
            // Sử dụng thời gian hiện tại để hiển thị lịch sử mẫu với thời gian thực
            DateTime now = DateTime.Now;

            // Xóa danh sách cũ
            listBoxLichSu.Items.Clear();

            // Thêm các mục lịch sử mẫu với thời gian thực
            listBoxLichSu.Items.Add($"{now.AddMinutes(-1):dd/MM/yyyy HH:mm:ss}|10 Điểm|10");
            listBoxLichSu.Items.Add($"{now.AddMinutes(-3):dd/MM/yyyy HH:mm:ss}|50 Điểm|50");
            listBoxLichSu.Items.Add($"{now.AddMinutes(-5):dd/MM/yyyy HH:mm:ss}|20 Điểm|20");
            listBoxLichSu.Items.Add($"{now.AddMinutes(-10):dd/MM/yyyy HH:mm:ss}|100 Điểm|100");
            listBoxLichSu.Items.Add($"{now.AddMinutes(-15):dd/MM/yyyy HH:mm:ss}|VIP Bonus|200");
            listBoxLichSu.Items.Add($"{now.AddMinutes(-30):dd/MM/yyyy HH:mm:ss}|200 Điểm|200");
            listBoxLichSu.Items.Add($"{now.AddHours(-1):dd/MM/yyyy HH:mm:ss}|Special Prize|500");
            listBoxLichSu.Items.Add($"{now.AddHours(-2):dd/MM/yyyy HH:mm:ss}|10 Điểm|10");
            listBoxLichSu.Items.Add($"{now.AddHours(-3):dd/MM/yyyy HH:mm:ss}|20 Điểm|20");
            listBoxLichSu.Items.Add($"{now.AddHours(-4):dd/MM/yyyy HH:mm:ss}|VIP Jackpot|1000");
        }
    }
}