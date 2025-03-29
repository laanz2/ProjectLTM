using System;
using System.Drawing;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LuckyWheelClient
{
    public class FormQuenMatKhau : Form
    {
        private readonly Label lblHuongDan;
        private readonly Label lblEmail;
        private readonly TextBox txtEmail;
        private readonly Button btnGuiYeuCau;
        private readonly Label lblKetQua;
        private readonly PictureBox picLoading;
        private readonly Label lblStatus;

        public FormQuenMatKhau()
        {
            // Thiết lập form
            this.Text = "Quên Mật Khẩu";
            this.Size = new Size(400, 320); // Tăng kích thước để hiển thị status
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Hướng dẫn
            lblHuongDan = new Label
            {
                Text = "Vui lòng nhập email đã đăng ký. Chúng tôi sẽ gửi hướng dẫn đặt lại mật khẩu.",
                Location = new Point(20, 20),
                Size = new Size(350, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Email
            lblEmail = new Label
            {
                Text = "Email:",
                Location = new Point(20, 80),
                AutoSize = true
            };

            txtEmail = new TextBox
            {
                Location = new Point(100, 80),
                Size = new Size(250, 20)
            };

            // Nút gửi yêu cầu
            btnGuiYeuCau = new Button
            {
                Text = "Gửi Yêu Cầu",
                Location = new Point(150, 120),
                Size = new Size(100, 30)
            };
            btnGuiYeuCau.Click += BtnGuiYeuCau_ClickAsync;

            // Label kết quả
            lblKetQua = new Label
            {
                Location = new Point(20, 170),
                Size = new Size(350, 60),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Loading indicator
            picLoading = new PictureBox
            {
                Size = new Size(32, 32),
                Location = new Point(184, 120),
                Visible = false,
                BackColor = Color.Transparent
            };
            picLoading.Image = CreateLoadingImage();

            // Status label
            lblStatus = new Label
            {
                Location = new Point(20, 250),
                Size = new Size(350, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Arial", 8),
                ForeColor = Color.Gray
            };

            // Thêm controls vào form
            this.Controls.Add(lblHuongDan);
            this.Controls.Add(lblEmail);
            this.Controls.Add(txtEmail);
            this.Controls.Add(btnGuiYeuCau);
            this.Controls.Add(lblKetQua);
            this.Controls.Add(picLoading);
            this.Controls.Add(lblStatus);

            // Kiểm tra kết nối khi khởi động
            CheckServerConnection();
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
                        lblStatus.Text = "✅ Kết nối server thành công";
                        lblStatus.ForeColor = Color.Green;
                    }
                    else
                    {
                        lblStatus.Text = "⚠️ Đang chạy ở chế độ ngoại tuyến";
                        lblStatus.ForeColor = Color.Orange;
                    }
                }
            }
            catch
            {
                lblStatus.Text = "⚠️ Đang chạy ở chế độ ngoại tuyến";
                lblStatus.ForeColor = Color.Orange;
            }
        }

        private Image CreateLoadingImage()
        {
            // Tạo một hình tròn đơn giản làm biểu tượng loading
            Bitmap bmp = new Bitmap(32, 32);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (Pen pen = new Pen(Color.FromArgb(52, 152, 219), 3))
                {
                    g.DrawEllipse(pen, 2, 2, 28, 28);
                }
            }
            return bmp;
        }

        private async void BtnGuiYeuCau_ClickAsync(object sender, EventArgs e)
        {
            string email = txtEmail.Text.Trim();

            if (string.IsNullOrEmpty(email))
            {
                lblKetQua.ForeColor = Color.Red;
                lblKetQua.Text = "❌ Vui lòng nhập email!";
                return;
            }

            // Hiển thị đang xử lý
            btnGuiYeuCau.Visible = false;
            picLoading.Visible = true;
            lblKetQua.Text = "Đang xử lý yêu cầu...";
            lblKetQua.ForeColor = Color.Black;

            string resetToken = null;
            bool serverRequestSuccess = false;

            try
            {
                // Thử gửi yêu cầu đến server trước
                using (TcpClient client = new TcpClient())
                {
                    var connectTask = client.BeginConnect("localhost", 9876, null, null);
                    bool connected = connectTask.AsyncWaitHandle.WaitOne(3000); // 3 giây timeout

                    if (connected)
                    {
                        client.EndConnect(connectTask);

                        using (NetworkStream stream = client.GetStream())
                        {
                            // Gửi yêu cầu khôi phục mật khẩu
                            resetToken = EmailHelper.GenerateResetToken();
                            string request = $"RECOVER|{email}|{resetToken}";
                            byte[] data = Encoding.UTF8.GetBytes(request);
                            await stream.WriteAsync(data, 0, data.Length);

                            // Đợi phản hồi
                            byte[] buffer = new byte[1024];
                            int byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
                            string response = Encoding.UTF8.GetString(buffer, 0, byteCount);

                            serverRequestSuccess = (response == "OK");
                        }
                    }
                }

                // Nếu server request thành công, gửi email
                if (serverRequestSuccess)
                {
                    await EmailHelper.SendPasswordResetEmailAsync(email, resetToken);
                }
            }
            catch (Exception)
            {
                // Nếu có lỗi giao tiếp với server, chuyển sang phương án cục bộ
                serverRequestSuccess = false;
            }

            // Nếu không thể gửi yêu cầu đến server, thử phương án cục bộ
            if (!serverRequestSuccess)
            {
                resetToken = LocalAuthManager.CreatePasswordResetRequest(email);
                if (resetToken == null)
                {
                    // Email không tồn tại trong hệ thống cục bộ
                    lblKetQua.ForeColor = Color.Red;
                    lblKetQua.Text = "❌ Email không tồn tại trong hệ thống.";
                    btnGuiYeuCau.Visible = true;
                    picLoading.Visible = false;
                    return;
                }
            }

            // Hiển thị kết quả thành công
            lblKetQua.ForeColor = Color.Green;
            lblKetQua.Text = "✅ Yêu cầu đã được gửi thành công!\n" +
                           "Vui lòng kiểm tra email của bạn hoặc nhập mã xác thực.";

            // Mở form nhập mã xác thực và đặt lại mật khẩu
            FormDatLaiMatKhau formDatLaiMatKhau = new FormDatLaiMatKhau(email, resetToken);

            if (formDatLaiMatKhau.ShowDialog() == DialogResult.OK)
            {
                this.Close();
            }
            else
            {
                // Hiển thị lại nút gửi yêu cầu
                btnGuiYeuCau.Visible = true;
                picLoading.Visible = false;
            }
        }
    }
}