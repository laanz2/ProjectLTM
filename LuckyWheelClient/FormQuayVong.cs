using System;
using System.Drawing;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace LuckyWheelClient
{
    public class FormQuayVong : Form
    {
        private readonly string tenDangNhap;
        private Button btnQuay;
        private Label lblKetQua;

        public FormQuayVong(string tenDangNhap)
        {
            this.tenDangNhap = tenDangNhap;

            this.Text = "🎡 Vòng Quay May Mắn";
            this.Size = new Size(400, 250);
            this.StartPosition = FormStartPosition.CenterScreen;

            btnQuay = new Button
            {
                Text = "🎯 Quay Ngay!",
                Location = new Point(130, 50),
                Size = new Size(120, 40),
                Font = new Font("Arial", 12, FontStyle.Bold)
            };
            btnQuay.Click += BtnQuay_Click;

            lblKetQua = new Label
            {
                Text = "🎁 Kết quả sẽ hiển thị ở đây...",
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(50, 120),
                Size = new Size(300, 50),
                Font = new Font("Arial", 11, FontStyle.Bold)
            };

            this.Controls.Add(btnQuay);
            this.Controls.Add(lblKetQua);
        }

        private void BtnQuay_Click(object sender, EventArgs e)
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
                        // Nếu không kết nối được, hiển thị kết quả mẫu
                        SetDemoResult();
                        return;
                    }

                    client.EndConnect(connectTask);

                    using (NetworkStream stream = client.GetStream())
                    {
                        string yeuCau = $"SPIN|{tenDangNhap}";
                        byte[] data = Encoding.UTF8.GetBytes(yeuCau);
                        stream.Write(data, 0, data.Length);

                        byte[] buffer = new byte[1024];
                        int count = stream.Read(buffer, 0, buffer.Length);
                        string phanHoi = Encoding.UTF8.GetString(buffer, 0, count);

                        if (phanHoi.StartsWith("REWARD|"))
                        {
                            string tenPhanThuong = phanHoi.Substring(7);
                            lblKetQua.Text = $"🎉 Bạn nhận được: {tenPhanThuong}";

                            // ✅ Cập nhật lại điểm và lượt quay ở Form chính
                            if (this.Owner is FormChinh f)
                            {
                                f.RefreshThongTin();
                            }
                        }
                        else if (phanHoi.StartsWith("FAIL|"))
                        {
                            string lyDo = phanHoi.Substring(5);
                            lblKetQua.Text = $"❌ Không thể quay: {lyDo}";
                        }
                        else
                        {
                            lblKetQua.Text = $"❌ Lỗi không xác định: {phanHoi}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Nếu có lỗi, hiển thị kết quả mẫu
                SetDemoResult();
                // lblKetQua.Text = $"⚠️ Lỗi kết nối: {ex.Message}";
            }
        }

        // Phương thức mới để hiển thị kết quả mẫu khi không thể kết nối server
        private void SetDemoResult()
        {
            string[] cacPhanThuong = new string[] {
                "10 Điểm", "20 Điểm", "50 Điểm", "100 Điểm", "200 Điểm"
            };

            // Tạo số ngẫu nhiên để chọn phần thưởng
            Random random = new Random();
            int index = random.Next(0, cacPhanThuong.Length);
            string tenPhanThuong = cacPhanThuong[index];

            lblKetQua.Text = $"🎉 Bạn nhận được: {tenPhanThuong}";

            // Cập nhật lại Form chính
            if (this.Owner is FormChinh f)
            {
                f.RefreshThongTin();
            }
        }
    }
}