using System;
using System.Drawing;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace LuckyWheelClient
{
    public class FormDatLaiMatKhau : Form
    {
        private readonly string email;
        private readonly string resetToken;

        private readonly Label lblHuongDan;
        private readonly Label lblMaXacThuc;
        private readonly TextBox txtMaXacThuc;
        private readonly Label lblMatKhauMoi;
        private readonly TextBox txtMatKhauMoi;
        private readonly Label lblXacNhanMatKhau;
        private readonly TextBox txtXacNhanMatKhau;
        private readonly Button btnDatLaiMatKhau;
        private readonly Label lblKetQua;
        private readonly Label lblStatus;

        public FormDatLaiMatKhau(string email, string resetToken)
        {
            this.email = email;
            this.resetToken = resetToken;

            this.Text = "Đặt Lại Mật Khẩu";
            this.Size = new Size(400, 380); // Tăng kích thước để hiển thị status
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Hướng dẫn
            lblHuongDan = new Label
            {
                Text = "Vui lòng nhập mã xác thực đã được gửi đến email của bạn và mật khẩu mới.",
                Location = new Point(20, 20),
                Size = new Size(350, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Mã xác thực
            lblMaXacThuc = new Label
            {
                Text = "Mã xác thực:",
                Location = new Point(20, 80),
                AutoSize = true
            };

            txtMaXacThuc = new TextBox
            {
                Location = new Point(150, 80),
                Size = new Size(200, 20),
                Text = resetToken // Tự động điền resetToken để dễ demo
            };

            // Mật khẩu mới
            lblMatKhauMoi = new Label
            {
                Text = "Mật khẩu mới:",
                Location = new Point(20, 120),
                AutoSize = true
            };

            txtMatKhauMoi = new TextBox
            {
                Location = new Point(150, 120),
                Size = new Size(200, 20),
                UseSystemPasswordChar = true
            };

            // Xác nhận mật khẩu
            lblXacNhanMatKhau = new Label
            {
                Text = "Xác nhận mật khẩu:",
                Location = new Point(20, 160),
                AutoSize = true
            };

            txtXacNhanMatKhau = new TextBox
            {
                Location = new Point(150, 160),
                Size = new Size(200, 20),
                UseSystemPasswordChar = true
            };

            // Nút đặt lại mật khẩu
            btnDatLaiMatKhau = new Button
            {
                Text = "Đặt Lại Mật Khẩu",
                Location = new Point(150, 200),
                Size = new Size(120, 30)
            };
            btnDatLaiMatKhau.Click += BtnDatLaiMatKhau_Click;

            // Label kết quả
            lblKetQua = new Label
            {
                Location = new Point(20, 250),
                Size = new Size(350, 60),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Status label
            lblStatus = new Label
            {
                Location = new Point(20, 320),
                Size = new Size(350, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Arial", 8),
                ForeColor = Color.Gray
            };

            // Thêm controls vào form
            this.Controls.Add(lblHuongDan);
            this.Controls.Add(lblMaXacThuc);
            this.Controls.Add(txtMaXacThuc);
            this.Controls.Add(lblMatKhauMoi);
            this.Controls.Add(txtMatKhauMoi);
            this.Controls.Add(lblXacNhanMatKhau);
            this.Controls.Add(txtXacNhanMatKhau);
            this.Controls.Add(btnDatLaiMatKhau);
            this.Controls.Add(lblKetQua);
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

        private async void BtnDatLaiMatKhau_Click(object sender, EventArgs e)
        {
            string maXacThuc = txtMaXacThuc.Text.Trim();
            string matKhauMoi = txtMatKhauMoi.Text.Trim();
            string xacNhanMatKhau = txtXacNhanMatKhau.Text.Trim();

            // Kiểm tra thông tin
            if (string.IsNullOrEmpty(maXacThuc) || string.IsNullOrEmpty(matKhauMoi) || string.IsNullOrEmpty(xacNhanMatKhau))
            {
                lblKetQua.ForeColor = Color.Red;
                lblKetQua.Text = "❌ Vui lòng nhập đầy đủ thông tin!";
                return;
            }

            if (matKhauMoi != xacNhanMatKhau)
            {
                lblKetQua.ForeColor = Color.Red;
                lblKetQua.Text = "❌ Mật khẩu xác nhận không khớp!";
                return;
            }

            if (maXacThuc != resetToken)
            {
                lblKetQua.ForeColor = Color.Red;
                lblKetQua.Text = "❌ Mã xác thực không đúng!";
                return;
            }

            // Disable button để ngăn nhiều lần click
            btnDatLaiMatKhau.Enabled = false;
            btnDatLaiMatKhau.Text = "Đang xử lý...";

            bool serverResetSuccess = false;
            bool localResetSuccess = false;

            try
            {
                // Thử đặt lại mật khẩu trên server trước
                using (TcpClient client = new TcpClient())
                {
                    // Kết nối đến server
                    var connectTask = client.BeginConnect("localhost", 9876, null, null);
                    bool connected = connectTask.AsyncWaitHandle.WaitOne(3000);

                    if (connected)
                    {
                        client.EndConnect(connectTask);

                        using (NetworkStream stream = client.GetStream())
                        {
                            // Mã hóa mật khẩu mới
                            string hashedPassword = LocalAuthManager.HashPassword(matKhauMoi);
                            string request = $"RESETPASSWORD|{email}|{maXacThuc}|{hashedPassword}";
                            byte[] data = Encoding.UTF8.GetBytes(request);
                            await stream.WriteAsync(data, 0, data.Length);

                            byte[] buffer = new byte[1024];
                            int byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
                            string response = Encoding.UTF8.GetString(buffer, 0, byteCount);

                            serverResetSuccess = (response == "OK");
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Bỏ qua lỗi server
                serverResetSuccess = false;
            }

            // Nếu không thể đặt lại mật khẩu trên server, thử đặt lại cục bộ
            if (!serverResetSuccess)
            {
                localResetSuccess = LocalAuthManager.ResetPassword(email, maXacThuc, matKhauMoi);
            }

            // Hiển thị kết quả
            btnDatLaiMatKhau.Enabled = true;
            btnDatLaiMatKhau.Text = "Đặt Lại Mật Khẩu";

            if (serverResetSuccess || localResetSuccess)
            {
                lblKetQua.ForeColor = Color.Green;

                if (serverResetSuccess)
                {
                    lblKetQua.Text = "✅ Đặt lại mật khẩu thành công!\nBạn có thể đăng nhập với mật khẩu mới.";
                }
                else
                {
                    lblKetQua.Text = "✅ Đặt lại mật khẩu cục bộ thành công!\nBạn có thể đăng nhập với mật khẩu mới.";
                    lblStatus.Text = "⚠️ Mật khẩu chỉ được đặt lại cục bộ (không kết nối server)";
                    lblStatus.ForeColor = Color.Orange;
                }

                // Sử dụng Timer để tự động đóng form sau 3 giây
                Timer timer = new Timer
                {
                    Interval = 3000
                };
                timer.Tick += (s, args) => {
                    timer.Stop();
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                };
                timer.Start();
            }
            else
            {
                lblKetQua.ForeColor = Color.Red;
                lblKetQua.Text = "❌ Không thể đặt lại mật khẩu. Vui lòng thử lại sau.";
            }
        }
    }
}