using System;
using System.Drawing;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LuckyWheelClient
{
    public class FormDangKy : Form
    {
        private readonly TextBox txtUsername;
        private readonly TextBox txtPassword;
        private readonly TextBox txtEmail;
        private readonly Button btnDangKy;
        private readonly Label lblKetQua;
        private readonly Label lblUsername;
        private readonly Label lblPassword;
        private readonly Label lblEmail;
        private readonly Label lblStatus;

        // Thuộc tính để truyền tên đăng nhập về form đăng nhập
        public string RegisteredUsername { get; private set; }

        public FormDangKy()
        {
            // Form properties
            this.Text = "Đăng Ký Tài Khoản";
            this.Size = new Size(350, 330); // Tăng kích thước để hiển thị trạng thái
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Labels
            lblUsername = new Label
            {
                Text = "Tên đăng nhập:",
                Location = new Point(20, 20),
                AutoSize = true
            };

            lblPassword = new Label
            {
                Text = "Mật khẩu:",
                Location = new Point(20, 70),
                AutoSize = true
            };

            lblEmail = new Label
            {
                Text = "Email:",
                Location = new Point(20, 120),
                AutoSize = true
            };

            // TextBoxes
            txtUsername = new TextBox
            {
                Location = new Point(150, 20),
                Width = 150
            };
            txtUsername.TextChanged += TextBox_TextChanged;

            txtPassword = new TextBox
            {
                Location = new Point(150, 70),
                Width = 150,
                UseSystemPasswordChar = true
            };
            txtPassword.TextChanged += TextBox_TextChanged;

            txtEmail = new TextBox
            {
                Location = new Point(150, 120),
                Width = 150
            };
            txtEmail.TextChanged += TextBox_TextChanged;

            // Button
            btnDangKy = new Button
            {
                Text = "Đăng Ký",
                Location = new Point(100, 170),
                Width = 120
            };
            btnDangKy.Click += BtnDangKy_Click;

            // Result Label
            lblKetQua = new Label
            {
                Location = new Point(20, 210),
                Size = new Size(300, 60),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Status Label
            lblStatus = new Label
            {
                Location = new Point(20, 270),
                Size = new Size(300, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Arial", 8),
                ForeColor = Color.Gray
            };

            // Add controls
            this.Controls.AddRange(new Control[]
            {
                lblUsername, lblPassword, lblEmail,
                txtUsername, txtPassword, txtEmail,
                btnDangKy, lblKetQua, lblStatus
            });

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

        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            lblKetQua.Text = string.Empty;
        }

        private async void BtnDangKy_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();
            string email = txtEmail.Text.Trim();

            // Validate input
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(email))
            {
                lblKetQua.ForeColor = Color.Red;
                lblKetQua.Text = "❌ Vui lòng nhập đủ thông tin!";
                return;
            }

            // Disable button to prevent multiple clicks
            btnDangKy.Enabled = false;
            btnDangKy.Text = "Đang xử lý...";
            lblKetQua.Text = "Đang đăng ký...";

            bool serverRegistrationSuccess = false;
            bool localRegistrationSuccess = false;

            try
            {
                // Thử đăng ký trên server trước
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
                            // Mã hóa mật khẩu trước khi gửi
                            string hashedPassword = LocalAuthManager.HashPassword(password);
                            string request = $"SIGNUP|{username}|{hashedPassword}|{email}";
                            byte[] data = Encoding.UTF8.GetBytes(request);
                            stream.Write(data, 0, data.Length);

                            byte[] buffer = new byte[1024];
                            int byteCount = stream.Read(buffer, 0, buffer.Length);
                            string response = Encoding.UTF8.GetString(buffer, 0, byteCount);

                            serverRegistrationSuccess = (response == "OK");
                        }
                    }
                }

                // Nếu đăng ký server thành công, gửi email xác nhận
                if (serverRegistrationSuccess)
                {
                    await EmailHelper.SendRegistrationConfirmationAsync(email, username);
                }
            }
            catch (Exception)
            {
                // Bỏ qua lỗi đăng ký server
                serverRegistrationSuccess = false;
            }

            // Nếu không thể đăng ký trên server, đăng ký cục bộ
            if (!serverRegistrationSuccess)
            {
                // Đăng ký cục bộ
                localRegistrationSuccess = LocalAuthManager.RegisterLocalUser(username, password, email);
            }

            // Khôi phục trạng thái button
            btnDangKy.Enabled = true;
            btnDangKy.Text = "Đăng Ký";

            // Xử lý kết quả đăng ký
            if (serverRegistrationSuccess || localRegistrationSuccess)
            {
                // Lưu username để truyền lại cho form đăng nhập
                RegisteredUsername = username;

                lblKetQua.ForeColor = Color.Green;

                if (serverRegistrationSuccess)
                {
                    lblKetQua.Text = "✅ Đăng ký thành công! Đang chuyển sang đăng nhập...";
                }
                else
                {
                    lblKetQua.Text = "✅ Đăng ký cục bộ thành công! Đang chuyển sang đăng nhập...";
                    lblStatus.Text = "⚠️ Đăng ký cục bộ (không kết nối server)";
                    lblStatus.ForeColor = Color.Orange;
                }

                // Clear fields
                txtUsername.Clear();
                txtPassword.Clear();
                txtEmail.Clear();

                // Sử dụng Timer để delay
                Timer timer = new Timer
                {
                    Interval = 2000 // 2 giây
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
                lblKetQua.Text = "❌ Đăng ký thất bại! Tên người dùng có thể đã tồn tại.";
            }
        }
    }
}