using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using System.Reflection;

namespace LuckyWheelClient
{
    public class FormQuayVongVIP : Form
    {
        private readonly string tenDangNhap;
        private Button btnQuay;
        private Label lblKetQua;
        private Panel pnlWheel;
        private Timer spinTimer;
        private int currentAngle = 0;
        private int targetAngle = 0;
        private bool isSpinning = false;
        private int spinSpeed = 0; // Biến mới để kiểm soát tốc độ quay
        private int initialSpeed = 15; // Tốc độ ban đầu
        private int slowdownFactor = 10; // Hệ số giảm tốc
        private string[] cacPhanThuong = new string[] {
            "100 Điểm", "200 Điểm", "300 Điểm", "500 Điểm",
            "1000 Điểm", "1500 Điểm", "2000 Điểm", "Jackpot"
        };
        private Color[] wheelColors = new Color[] {
            Color.Crimson, Color.RoyalBlue, Color.MediumSeaGreen, Color.DarkOrange,
            Color.DarkViolet, Color.DeepSkyBlue, Color.HotPink, Color.Gold
        };
        private Random random = new Random();

        public FormQuayVongVIP(string tenDangNhap)
        {
            this.tenDangNhap = tenDangNhap;
            this.Text = "💎 Vòng Quay VIP";
            this.Size = new Size(550, 550);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(30, 30, 60); // Màu nền tối hơn để tạo cảm giác VIP

            // Bật double buffering để ngăn nhấp nháy
            this.DoubleBuffered = true;

            InitializeComponents();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            base.OnPaint(e);
        }

        private void InitializeComponents()
        {
            // Panel chứa vòng quay
            pnlWheel = new Panel
            {
                Size = new Size(400, 400),
                Location = new Point(75, 20),
                BackColor = Color.FromArgb(50, 50, 80)
            };

            // Bật double buffering cho panel để ngăn nhấp nháy
            typeof(Panel).GetProperty("DoubleBuffered",
                BindingFlags.NonPublic |
                BindingFlags.Instance)
                .SetValue(pnlWheel, true, null);

            pnlWheel.Paint += PnlWheel_Paint;

            // Nút quay
            btnQuay = new Button
            {
                Text = "QUAY VIP",
                Location = new Point(225, 430),
                Size = new Size(100, 40),
                Font = new Font("Arial", 12, FontStyle.Bold),
                BackColor = Color.Gold,
                ForeColor = Color.DarkBlue
            };
            btnQuay.FlatStyle = FlatStyle.Flat;
            btnQuay.FlatAppearance.BorderSize = 0;
            btnQuay.Click += BtnQuay_Click;

            // Label kết quả
            lblKetQua = new Label
            {
                Text = "Nhấn QUAY VIP để bắt đầu!",
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(75, 480),
                Size = new Size(400, 30),
                Font = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = Color.White
            };

            // Timer cho hiệu ứng quay - giảm interval để nhanh hơn
            spinTimer = new Timer
            {
                Interval = 20 // Giảm từ 30ms xuống 20ms
            };
            spinTimer.Tick += SpinTimer_Tick;

            // Thêm controls vào form
            this.Controls.Add(pnlWheel);
            this.Controls.Add(btnQuay);
            this.Controls.Add(lblKetQua);
        }

        private void PnlWheel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int segments = cacPhanThuong.Length;
            float anglePerSegment = 360f / segments;
            int centerX = pnlWheel.Width / 2;
            int centerY = pnlWheel.Height / 2;
            int radius = Math.Min(centerX, centerY) - 10;

            // Vẽ viền vòng quay VIP
            g.FillEllipse(new SolidBrush(Color.Gold),
                centerX - radius - 5, centerY - radius - 5,
                (radius + 5) * 2, (radius + 5) * 2);

            // Vẽ các phân đoạn vòng quay
            for (int i = 0; i < segments; i++)
            {
                float startAngle = i * anglePerSegment + currentAngle;
                using (SolidBrush brush = new SolidBrush(wheelColors[i]))
                {
                    g.FillPie(brush, centerX - radius, centerY - radius,
                             radius * 2, radius * 2, startAngle, anglePerSegment);
                }

                // Vẽ đường kẻ phân chia
                double line_angle = (startAngle + anglePerSegment / 2) * Math.PI / 180;
                g.DrawLine(Pens.White,
                    centerX, centerY,
                    centerX + (int)(radius * Math.Cos(line_angle)),
                    centerY + (int)(radius * Math.Sin(line_angle)));

                // Vẽ text phần thưởng
                double text_angle = (startAngle + anglePerSegment / 2) * Math.PI / 180;
                int textX = centerX + (int)((radius * 0.7) * Math.Cos(text_angle));
                int textY = centerY + (int)((radius * 0.7) * Math.Sin(text_angle));

                // Tạo StringFormat để xoay text
                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                // Lưu trạng thái graphics, xoay, vẽ text, khôi phục trạng thái
                GraphicsState state = g.Save();
                g.TranslateTransform(textX, textY);
                g.RotateTransform((float)(text_angle * 180 / Math.PI + 90));
                g.DrawString(cacPhanThuong[i], new Font("Arial", 9, FontStyle.Bold),
                            Brushes.White, 0, 0, sf);
                g.Restore(state);
            }

            // Vẽ vòng tròn trung tâm
            g.FillEllipse(Brushes.Gold, centerX - 25, centerY - 25, 50, 50);
            g.DrawEllipse(Pens.DarkBlue, centerX - 25, centerY - 25, 50, 50);
            g.DrawString("VIP", new Font("Arial", 14, FontStyle.Bold),
                         Brushes.DarkBlue, centerX - 15, centerY - 10);

            // Vẽ mũi tên chỉ vị trí
            Point[] arrow = new Point[] {
                new Point(centerX + radius + 5, centerY),
                new Point(centerX + radius + 25, centerY - 15),
                new Point(centerX + radius + 25, centerY + 15)
            };
            g.FillPolygon(Brushes.Gold, arrow);
            g.DrawPolygon(new Pen(Color.DarkBlue, 2), arrow);
        }

        private void BtnQuay_Click(object sender, EventArgs e)
        {
            if (isSpinning) return;

            try
            {
                btnQuay.Enabled = false;
                isSpinning = true;
                lblKetQua.Text = "Đang quay...";

                using (TcpClient client = new TcpClient())
                {
                    // Tăng thời gian timeout và thay đổi cổng
                    var connectTask = client.BeginConnect("localhost", 9876, null, null);
                    bool connected = connectTask.AsyncWaitHandle.WaitOne(3000); // 3 giây timeout

                    if (connected)
                    {
                        client.EndConnect(connectTask);

                        using (NetworkStream stream = client.GetStream())
                        {
                            string yeuCau = $"SPINVIP|{tenDangNhap}";
                            byte[] data = Encoding.UTF8.GetBytes(yeuCau);
                            stream.Write(data, 0, data.Length);

                            byte[] buffer = new byte[1024];
                            int count = stream.Read(buffer, 0, buffer.Length);
                            string phanHoi = Encoding.UTF8.GetString(buffer, 0, count);

                            if (phanHoi.StartsWith("REWARD|"))
                            {
                                string tenPhanThuong = phanHoi.Substring(7);
                                StartSpin(tenPhanThuong);

                                // Cập nhật lại điểm và lượt quay ở Form chính
                                if (this.Owner is FormChinh f)
                                {
                                    f.RefreshThongTin();
                                }
                            }
                            else
                            {
                                isSpinning = false;
                                btnQuay.Enabled = true;
                                lblKetQua.Text = $"❌ {phanHoi}";
                            }
                        }
                    }
                    else
                    {
                        // Nếu không kết nối được, hiển thị kết quả mẫu
                        string demoResult = GetDemoResult();
                        StartSpin(demoResult);
                    }
                }
            }
            catch (Exception ex)
            {
                // Nếu có lỗi, hiển thị kết quả mẫu
                string demoResult = GetDemoResult();
                StartSpin(demoResult);
            }
        }

        private string GetDemoResult()
        {
            int index = random.Next(0, cacPhanThuong.Length);
            return $"{cacPhanThuong[index]} (Demo)";
        }

        private void StartSpin(string result)
        {
            // Xác định phần thưởng và vị trí tương ứng
            string resultText = result.Contains("(") ?
                result.Substring(0, result.IndexOf("(")).Trim() : result.Trim();

            int segmentIndex = -1;
            for (int i = 0; i < cacPhanThuong.Length; i++)
            {
                if (resultText.Contains(cacPhanThuong[i]))
                {
                    segmentIndex = i;
                    break;
                }
            }

            if (segmentIndex == -1)
            {
                segmentIndex = random.Next(0, cacPhanThuong.Length);
            }

            // Tính góc để vòng quay dừng ở phần thưởng
            int segmentAngle = 360 / cacPhanThuong.Length;
            int destinationAngle = 270 - (segmentIndex * segmentAngle); // 270 độ là vị trí con trỏ
            destinationAngle = (destinationAngle + 360) % 360; // Đảm bảo góc trong khoảng 0-359

            // Thêm 3 vòng quay đầy đủ (1080 độ) vào góc mục tiêu
            targetAngle = 1080 + destinationAngle;

            // Reset góc hiện tại về 0
            currentAngle = 0;

            // Lưu kết quả để hiển thị sau khi quay
            string finalResult = result;

            // Bắt đầu quay
            spinTimer.Start();

            // Thiết lập sự kiện khi quay xong
            EventHandler onSpinComplete = null;
            onSpinComplete = (s, ev) => {
                if (currentAngle >= targetAngle)
                {
                    spinTimer.Stop(); // Dừng timer
                    spinTimer.Tick -= onSpinComplete; // Loại bỏ sự kiện

                    // Hiệu ứng khi trúng thưởng lớn
                    if (finalResult.Contains("1000") || finalResult.Contains("2000") ||
                        finalResult.Contains("Jackpot"))
                    {
                        ShowBigWinEffect(finalResult);
                    }
                    else
                    {
                        lblKetQua.Text = $"🎉 Chúc mừng! Bạn nhận được: {finalResult}";
                        btnQuay.Enabled = true;
                        isSpinning = false;
                    }

                    // Cập nhật lại thông tin
                    if (this.Owner is FormChinh owner)
                    {
                        owner.RefreshThongTin();
                    }
                }
            };
            spinTimer.Tick += onSpinComplete;
        }

        private void ShowBigWinEffect(string prize)
        {
            // Timer để thay đổi màu text thông báo
            Timer blinkTimer = new Timer { Interval = 200 };
            int blinkCount = 0;
            Color[] blinkColors = new Color[] { Color.Gold, Color.Red, Color.White, Color.Yellow };

            blinkTimer.Tick += (s, e) => {
                lblKetQua.ForeColor = blinkColors[blinkCount % blinkColors.Length];
                lblKetQua.Text = $"🎊 BIG WIN! Bạn trúng: {prize} 🎊";
                blinkCount++;

                if (blinkCount >= 15) // Nhấp nháy khoảng 3 giây
                {
                    blinkTimer.Stop();
                    lblKetQua.ForeColor = Color.White;
                    btnQuay.Enabled = true;
                    isSpinning = false;

                    // Hiển thị thông báo đặc biệt
                    MessageBox.Show($"🎊 Chúc mừng bạn đã trúng giải lớn: {prize}! 🎊",
                        "VIP JACKPOT!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            blinkTimer.Start();

            // Phát âm thanh (nếu có thể)
            try { System.Media.SystemSounds.Exclamation.Play(); } catch { }
        }

        private void SpinTimer_Tick(object sender, EventArgs e)
        {
            if (currentAngle < targetAngle)
            {
                // Tính toán lại phương pháp giảm tốc để mượt hơn và nhanh hơn
                int remainingAngle = targetAngle - currentAngle;
                int step;

                // Cách tính giảm tốc mới, mượt hơn và nhanh hơn
                if (remainingAngle > 720)
                {
                    // Khi còn quay nhiều, duy trì tốc độ cao hơn
                    step = 15; // Tăng từ 5 lên 15
                }
                else if (remainingAngle > 360)
                {
                    // Bắt đầu giảm tốc sau 2 vòng nhưng vẫn nhanh
                    step = 12; // Tăng từ 4 lên 12
                }
                else if (remainingAngle > 180)
                {
                    // Giảm thêm khi gần đến 1 vòng cuối
                    step = 8; // Tăng từ 3 lên 8
                }
                else if (remainingAngle > 90)
                {
                    // Giảm nhiều hơn ở nửa vòng cuối
                    step = 5; // Tăng từ 2 lên 5
                }
                else if (remainingAngle > 45)
                {
                    // Bắt đầu chậm lại khi gần đích
                    step = 3; // Tăng từ 1 lên 3
                }
                else
                {
                    // Chậm lại khi rất gần đích
                    step = 2; // Tăng từ 1 lên 2
                }

                currentAngle += step;

                // Điều chỉnh để đảm bảo vòng quay không bao giờ quay quá mục tiêu
                if (currentAngle >= targetAngle)
                {
                    currentAngle = targetAngle;
                }

                pnlWheel.Invalidate();
            }
        }
    }
}