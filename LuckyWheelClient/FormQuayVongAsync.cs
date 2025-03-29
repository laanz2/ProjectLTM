using System;
using System.Drawing;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LuckyWheelClient
{
    public class FormQuayVongAsync : Form
    {
        private readonly string tenDangNhap;
        private Button btnQuay;
        private Label lblKetQua;
        private ProgressBar progressBar;
        private System.Windows.Forms.Timer animationTimer;
        private int animationStep = 0;
        private readonly Random random = new Random();

        // Mảng các màu sắc cho hiệu ứng quay
        private readonly Color[] colors = new Color[]
        {
            Color.Red, Color.Blue, Color.Green, Color.Yellow,
            Color.Purple, Color.Orange, Color.Pink, Color.Cyan
        };

        public FormQuayVongAsync(string tenDangNhap)
        {
            this.tenDangNhap = tenDangNhap;

            this.Text = "🎡 Vòng Quay May Mắn (Async)";
            this.Size = new Size(450, 300);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Khởi tạo các controls
            InitializeControls();

            // Khởi tạo timer cho hiệu ứng
            animationTimer = new System.Windows.Forms.Timer();
            animationTimer.Interval = 100; // 100ms
            animationTimer.Tick += AnimationTimer_Tick;
        }

        private void InitializeControls()
        {
            // Nút quay
            btnQuay = new Button
            {
                Text = "🎯 Quay Ngay!",
                Location = new Point(150, 50),
                Size = new Size(120, 40),
                Font = new Font("Arial", 12, FontStyle.Bold),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnQuay.FlatAppearance.BorderSize = 0;
            btnQuay.Click += BtnQuay_ClickAsync;

            // Progress bar để hiển thị khi đang quay
            progressBar = new ProgressBar
            {
                Location = new Point(100, 120),
                Size = new Size(250, 20),
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Visible = false
            };

            // Label kết quả
            lblKetQua = new Label
            {
                Text = "🎁 Kết quả sẽ hiển thị ở đây...",
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(50, 160),
                Size = new Size(350, 80),
                Font = new Font("Arial", 12, FontStyle.Bold)
            };

            // Thêm controls vào form
            this.Controls.Add(btnQuay);
            this.Controls.Add(progressBar);
            this.Controls.Add(lblKetQua);
        }

        private async void BtnQuay_ClickAsync(object sender, EventArgs e)
        {
            try
            {
                // Vô hiệu hóa nút để tránh nhấn nhiều lần
                btnQuay.Enabled = false;

                // Hiển thị progress bar và bắt đầu hiệu ứng
                progressBar.Visible = true;
                lblKetQua.Text = "Đang quay...";
                StartAnimation();

                // Thực hiện quay bất đồng bộ
                string ketQua = await QuayVongAsync();

                // Dừng hiệu ứng và ẩn progress bar
                StopAnimation();
                progressBar.Visible = false;

                // Hiển thị kết quả
                DisplayResult(ketQua);

                // Cập nhật lại thông tin người dùng
                if (this.Owner is FormChinh formChinh)
                {
                    formChinh.RefreshThongTin();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Có lỗi xảy ra: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Kích hoạt lại nút quay
                btnQuay.Enabled = true;
            }
        }

        private async Task<string> QuayVongAsync()
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    // Kết nối đến server với timeout 5 giây
                    var connectTask = client.ConnectAsync("localhost", 9876);
                    if (await Task.WhenAny(connectTask, Task.Delay(5000)) != connectTask)
                    {
                        // Nếu timeout, trả về kết quả mẫu
                        // Thêm delay giả lập thời gian quay
                        await Task.Delay(random.Next(1000, 2000));
                        return GetDemoResult();
                    }

                    using (NetworkStream stream = client.GetStream())
                    {
                        // Chuẩn bị yêu cầu
                        string yeuCau = $"SPIN|{tenDangNhap}";
                        byte[] data = Encoding.UTF8.GetBytes(yeuCau);

                        // Gửi yêu cầu không đồng bộ
                        await stream.WriteAsync(data, 0, data.Length);

                        // Thêm delay giả lập thời gian quay (1-2 giây)
                        await Task.Delay(random.Next(1000, 2000));

                        // Đọc phản hồi không đồng bộ
                        byte[] buffer = new byte[1024];
                        int count = await stream.ReadAsync(buffer, 0, buffer.Length);

                        return Encoding.UTF8.GetString(buffer, 0, count);
                    }
                }
            }
            catch (Exception)
            {
                // Nếu có lỗi, trả về kết quả mẫu
                await Task.Delay(random.Next(1000, 2000)); // Delay giả lập
                return GetDemoResult();
            }
        }

        private string GetDemoResult()
        {
            string[] cacPhanThuong = new string[] {
                "10 Điểm", "20 Điểm", "50 Điểm", "100 Điểm", "200 Điểm", "500 Điểm"
            };

            int index = random.Next(0, cacPhanThuong.Length);
            return $"REWARD|{cacPhanThuong[index]} (Demo)";
        }

        private void StartAnimation()
        {
            animationStep = 0;
            animationTimer.Start();
        }

        private void StopAnimation()
        {
            animationTimer.Stop();
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            // Đổi màu cho label kết quả để tạo hiệu ứng
            animationStep = (animationStep + 1) % colors.Length;
            lblKetQua.ForeColor = colors[animationStep];

            if (animationStep % 2 == 0)
            {
                lblKetQua.Text = "Đang quay...";
            }
            else
            {
                lblKetQua.Text = "Vui lòng đợi...";
            }
        }

        private void DisplayResult(string result)
        {
            if (result.StartsWith("REWARD|"))
            {
                string tenPhanThuong = result.Substring(7);
                lblKetQua.Text = $"🎉 Chúc mừng!\nBạn nhận được: {tenPhanThuong}";
                lblKetQua.ForeColor = Color.FromArgb(46, 204, 113); // Màu xanh lá

                // Nếu là giải lớn, hiển thị thông báo đặc biệt
                if (tenPhanThuong.Contains("1000") || tenPhanThuong.Contains("Jackpot"))
                {
                    ShowBigWinEffect();
                }
            }
            else if (result.StartsWith("FAIL|"))
            {
                string lyDo = result.Substring(5);
                lblKetQua.Text = $"❌ Không thể quay:\n{lyDo}";
                lblKetQua.ForeColor = Color.FromArgb(231, 76, 60); // Màu đỏ
            }
            else if (result.StartsWith("ERROR|"))
            {
                string error = result.Substring(6);
                lblKetQua.Text = $"⚠️ Lỗi kết nối:\n{error}";
                lblKetQua.ForeColor = Color.FromArgb(243, 156, 18); // Màu cam
            }
            else
            {
                lblKetQua.Text = $"❓ Phản hồi không xác định:\n{result}";
                lblKetQua.ForeColor = Color.Black;
            }
        }

        private void ShowBigWinEffect()
        {
            // Hiển thị hiệu ứng đặc biệt khi trúng giải lớn
            try
            {
                // Phát âm thanh (nếu có)
                System.Media.SystemSounds.Exclamation.Play();

                // Hiệu ứng nhấp nháy
                for (int i = 0; i < 5; i++)
                {
                    lblKetQua.Visible = false;
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(100);
                    lblKetQua.Visible = true;
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(100);
                }

                // Hiển thị thông báo đặc biệt
                MessageBox.Show("🎊 Chúc mừng bạn đã trúng giải lớn! 🎊",
                    "Giải Thưởng Lớn!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch
            {
                // Bỏ qua lỗi hiệu ứng (nếu có)
            }
        }
    }
}