using System;
using System.Drawing;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LuckyWheelClient
{
    public class FormChinh : Form
    {
        private readonly string tenDangNhap;
        // Loại bỏ readonly từ các controls
        private Label lblChaoMung;
        private Label lblDiem;
        private Label lblLuot;
        private Label lblThoiGian;
        private Button btnVaoVongQuay;
        private Button btnLichSu;
        private Button btnVIP;       // Nút vòng quay VIP
        private Button btnDangKyVIP; // Nút đăng ký VIP
        private Button btnDangKy;    // Nút đăng ký tài khoản mới
        private Timer updateTimer;   // Timer định kỳ cập nhật thông tin
        private Label lblTrangThaiServer;

        public FormChinh(string tenDangNhap)
        {
            this.tenDangNhap = tenDangNhap;
            this.Text = "🎮 Vòng Quay May Mắn - Trang Chính";
            this.Size = new Size(600, 450);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 240, 250); // Background màu nhẹ

            // Khởi tạo giao diện
            InitializeComponents();

            // Khởi tạo timer để cập nhật thời gian và thông tin người chơi định kỳ
            updateTimer = new Timer
            {
                Interval = 1000 // Cập nhật mỗi giây
            };
            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Start();

            // Gọi lần đầu khi form mở
            RefreshThongTin();
            KiemTraVaHienThiNutVIP();
            CheckServerConnection();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            // Dừng các timer và dọn dẹp tài nguyên
            updateTimer?.Stop();
            updateTimer?.Dispose();
        }

        private void InitializeComponents()
        {
            // Panel thông tin người dùng
            Panel pnlInfo = new Panel
            {
                Location = new Point(20, 20),
                Size = new Size(560, 100),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Khởi tạo các điều khiển thông tin
            lblChaoMung = new Label
            {
                Text = $"🎉 Chào mừng, {tenDangNhap}!",
                AutoSize = true,
                Location = new Point(20, 15),
                Font = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 150)
            };

            lblDiem = new Label
            {
                Text = "Điểm: ...",
                Location = new Point(20, 50),
                AutoSize = true,
                Font = new Font("Arial", 11),
                ForeColor = Color.FromArgb(50, 120, 50)
            };

            lblLuot = new Label
            {
                Text = "Lượt quay miễn phí: ...",
                Location = new Point(20, 75),
                AutoSize = true,
                Font = new Font("Arial", 11),
                ForeColor = Color.FromArgb(50, 120, 50)
            };

            // Thêm label hiển thị thời gian
            lblThoiGian = new Label
            {
                Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                Location = new Point(350, 15),
                AutoSize = true,
                Font = new Font("Arial", 10),
                ForeColor = Color.FromArgb(70, 70, 70)
            };

            // Thêm label hiển thị trạng thái server
            lblTrangThaiServer = new Label
            {
                Text = "Đang kiểm tra kết nối...",
                Location = new Point(350, 40),
                AutoSize = true,
                Font = new Font("Arial", 8),
                ForeColor = Color.Gray
            };

            pnlInfo.Controls.Add(lblChaoMung);
            pnlInfo.Controls.Add(lblDiem);
            pnlInfo.Controls.Add(lblLuot);
            pnlInfo.Controls.Add(lblThoiGian);
            pnlInfo.Controls.Add(lblTrangThaiServer);

            // Panel chức năng
            FlowLayoutPanel pnlButtons = new FlowLayoutPanel
            {
                Location = new Point(150, 140),
                Size = new Size(300, 250),
                AutoScroll = true,
                WrapContents = false,
                FlowDirection = FlowDirection.TopDown,
                BorderStyle = BorderStyle.None
            };

            // Tạo style cho các nút
            btnVaoVongQuay = CreateStyledButton("👉 Vào Vòng Quay", Color.FromArgb(52, 152, 219));
            btnLichSu = CreateStyledButton("📜 Xem Lịch Sử Quay", Color.FromArgb(155, 89, 182));
            btnVIP = CreateStyledButton("💎 Vòng Quay VIP", Color.FromArgb(241, 196, 15));
            btnDangKyVIP = CreateStyledButton("🔑 Đăng ký VIP (500 điểm)", Color.FromArgb(230, 126, 34));
            btnDangKy = CreateStyledButton("📝 Đăng Ký Tài Khoản", Color.FromArgb(231, 76, 60));

            // Đăng ký sự kiện cho các nút
            btnVaoVongQuay.Click += BtnVaoVongQuay_Click;
            btnLichSu.Click += BtnLichSu_Click;
            btnVIP.Click += BtnVIP_Click;
            btnDangKyVIP.Click += BtnDangKyVIP_Click;
            btnDangKy.Click += BtnDangKy_Click;

            // Thêm các nút vào panel
            pnlButtons.Controls.Add(btnVaoVongQuay);
            pnlButtons.Controls.Add(btnLichSu);
            pnlButtons.Controls.Add(btnVIP);
            pnlButtons.Controls.Add(btnDangKyVIP);
            pnlButtons.Controls.Add(btnDangKy);

            // Thêm các panel vào form
            this.Controls.Add(pnlInfo);
            this.Controls.Add(pnlButtons);
        }

        private Button CreateStyledButton(string text, Color color)
        {
            Button btn = new Button
            {
                Text = text,
                Size = new Size(220, 40),
                Margin = new Padding(5),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 10, FontStyle.Bold),
                BackColor = color,
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;

            return btn;
        }

        private async void CheckServerConnection()
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    var connectTask = client.BeginConnect("localhost", 9876, null, null);
                    bool connected = connectTask.AsyncWaitHandle.WaitOne(2000); // Chỉ đợi 2 giây

                    if (connected)
                    {
                        lblTrangThaiServer.Text = "✅ Kết nối server thành công";
                        lblTrangThaiServer.ForeColor = Color.Green;
                    }
                    else
                    {
                        lblTrangThaiServer.Text = "⚠️ Đang chạy ở chế độ ngoại tuyến";
                        lblTrangThaiServer.ForeColor = Color.Orange;
                    }
                }
            }
            catch
            {
                lblTrangThaiServer.Text = "⚠️ Đang chạy ở chế độ ngoại tuyến";
                lblTrangThaiServer.ForeColor = Color.Orange;
            }
        }

        private void BtnVaoVongQuay_Click(object sender, EventArgs e)
        {
            // Sử dụng vòng quay mới thay vì vòng quay cũ
            FormQuayVongGUI quayForm = new FormQuayVongGUI(tenDangNhap)
            {
                Owner = this
            };
            quayForm.ShowDialog();
            RefreshThongTin();
        }

        private void BtnLichSu_Click(object sender, EventArgs e)
        {
            FormLichSuQuay lichSu = new FormLichSuQuay(tenDangNhap);
            lichSu.ShowDialog();
        }

        private void BtnVIP_Click(object sender, EventArgs e)
        {
            FormQuayVongVIP vipForm = new FormQuayVongVIP(tenDangNhap)
            {
                Owner = this
            };
            vipForm.ShowDialog();
            RefreshThongTin();
        }

        private async void BtnDangKyVIP_Click(object sender, EventArgs e)
        {
            DialogResult xacNhan = MessageBox.Show("Bạn muốn nâng cấp tài khoản VIP với 500 điểm?", "Xác nhận",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (xacNhan == DialogResult.Yes)
            {
                try
                {
                    btnDangKyVIP.Enabled = false;
                    btnDangKyVIP.Text = "Đang xử lý...";

                    bool upgradeSuccess = false;

                    // Thử nâng cấp qua server trước
                    using (TcpClient client = new TcpClient())
                    {
                        var connectTask = client.BeginConnect("localhost", 9876, null, null);
                        bool connected = connectTask.AsyncWaitHandle.WaitOne(3000); // 3 giây timeout

                        if (connected)
                        {
                            client.EndConnect(connectTask);

                            using (NetworkStream stream = client.GetStream())
                            {
                                string request = $"UPGRADEVIP|{tenDangNhap}";
                                byte[] data = Encoding.UTF8.GetBytes(request);
                                stream.Write(data, 0, data.Length);

                                byte[] buffer = new byte[1024];
                                int byteCount = stream.Read(buffer, 0, buffer.Length);
                                string response = Encoding.UTF8.GetString(buffer, 0, byteCount);

                                upgradeSuccess = response == "VIP_OK";
                            }
                        }
                        else
                        {
                            // Nếu không kết nối được server, nâng cấp VIP cục bộ
                            upgradeSuccess = LocalAuthManager.UpgradeToVIP(tenDangNhap);
                        }
                    }

                    if (upgradeSuccess)
                    {
                        MessageBox.Show("🎉 Chúc mừng! Tài khoản đã được nâng cấp VIP.");
                        RefreshThongTin();
                        KiemTraVaHienThiNutVIP();
                    }
                    else
                    {
                        MessageBox.Show("❌ Không thể nâng cấp VIP: Không đủ điểm hoặc đã là VIP.");
                    }
                }
                catch (Exception)
                {
                    // Thử nâng cấp cục bộ
                    bool upgradeSuccess = LocalAuthManager.UpgradeToVIP(tenDangNhap);

                    if (upgradeSuccess)
                    {
                        MessageBox.Show("🎉 Chúc mừng! Tài khoản đã được nâng cấp VIP.");
                    }
                    else
                    {
                        MessageBox.Show("❌ Không thể nâng cấp VIP: Không đủ điểm hoặc đã là VIP.");
                    }

                    RefreshThongTin();
                    KiemTraVaHienThiNutVIP();
                }
                finally
                {
                    btnDangKyVIP.Enabled = true;
                    btnDangKyVIP.Text = "🔑 Đăng ký VIP (500 điểm)";
                }
            }
        }

        private void BtnDangKy_Click(object sender, EventArgs e)
        {
            FormDangKy formDangKy = new FormDangKy();
            formDangKy.ShowDialog();
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            // Cập nhật thời gian hiện tại
            lblThoiGian.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            // Định kỳ cập nhật thông tin người chơi (mỗi 60 giây)
            if (DateTime.Now.Second == 0)
            {
                RefreshThongTin();
                KiemTraVaHienThiNutVIP();
            }
        }

        public async void RefreshThongTin()
        {
            try
            {
                int points = 1000;  // Giá trị mặc định
                int freeTurns = 5;  // Giá trị mặc định

                // Thử lấy thông tin từ server
                using (TcpClient client = new TcpClient())
                {
                    var connectTask = client.BeginConnect("localhost", 9876, null, null);
                    bool connected = connectTask.AsyncWaitHandle.WaitOne(3000); // 3 giây timeout

                    if (connected)
                    {
                        client.EndConnect(connectTask);

                        using (NetworkStream stream = client.GetStream())
                        {
                            string request = $"INFO|{tenDangNhap}";
                            byte[] data = Encoding.UTF8.GetBytes(request);
                            stream.Write(data, 0, data.Length);

                            byte[] buffer = new byte[1024];
                            int byteCount = stream.Read(buffer, 0, buffer.Length);
                            string response = Encoding.UTF8.GetString(buffer, 0, byteCount);

                            if (response.StartsWith("INFO|"))
                            {
                                string[] parts = response.Split('|');
                                if (parts.Length >= 3)
                                {
                                    points = int.Parse(parts[1]);
                                    freeTurns = int.Parse(parts[2]);
                                }
                            }
                        }
                    }
                    else
                    {
                        // Nếu không kết nối được server, lấy thông tin cục bộ
                        points = LocalAuthManager.GetUserPoints(tenDangNhap);
                    }
                }

                // Cập nhật giao diện
                lblDiem.Text = $"Điểm: {points}";
                lblLuot.Text = $"Lượt quay miễn phí: {freeTurns}";
                lblChaoMung.Text = $"🎉 Chào mừng, {tenDangNhap}!";
            }
            catch (Exception)
            {
                // Nếu có lỗi, lấy thông tin cục bộ
                int points = LocalAuthManager.GetUserPoints(tenDangNhap);

                // Cập nhật giao diện
                lblDiem.Text = $"Điểm: {points}";
                lblLuot.Text = $"Lượt quay miễn phí: 5"; // Mặc định
                lblChaoMung.Text = $"🎉 Chào mừng, {tenDangNhap}!";
            }
        }

        private async void KiemTraVaHienThiNutVIP()
        {
            try
            {
                bool isVIP = false;

                // Thử kiểm tra VIP qua server
                using (TcpClient client = new TcpClient())
                {
                    var connectTask = client.BeginConnect("localhost", 9876, null, null);
                    bool connected = connectTask.AsyncWaitHandle.WaitOne(3000); // 3 giây timeout

                    if (connected)
                    {
                        client.EndConnect(connectTask);

                        using (NetworkStream stream = client.GetStream())
                        {
                            string request = $"CHECKVIP|{tenDangNhap}";
                            byte[] data = Encoding.UTF8.GetBytes(request);
                            stream.Write(data, 0, data.Length);

                            byte[] buffer = new byte[1024];
                            int byteCount = stream.Read(buffer, 0, buffer.Length);
                            string response = Encoding.UTF8.GetString(buffer, 0, byteCount);

                            isVIP = response == "VIP|TRUE";
                        }
                    }
                    else
                    {
                        // Nếu không kết nối được server, kiểm tra VIP cục bộ
                        isVIP = LocalAuthManager.IsUserVIP(tenDangNhap);
                    }
                }

                // Hiển thị nút VIP dựa trên kết quả
                btnVIP.Visible = isVIP;
                btnDangKyVIP.Visible = !isVIP;

                if (btnVIP.Visible)
                {
                    btnVIP.BackColor = Color.Gold;
                    btnVIP.ForeColor = Color.DarkBlue;
                }
            }
            catch (Exception)
            {
                // Nếu có lỗi, kiểm tra VIP cục bộ
                bool isVIP = LocalAuthManager.IsUserVIP(tenDangNhap);

                btnVIP.Visible = isVIP;
                btnDangKyVIP.Visible = !isVIP;

                if (btnVIP.Visible)
                {
                    btnVIP.BackColor = Color.Gold;
                    btnVIP.ForeColor = Color.DarkBlue;
                }
            }
        }
    }
}