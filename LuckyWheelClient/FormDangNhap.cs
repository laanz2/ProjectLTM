using System;
using System.Drawing;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace LuckyWheelClient
{
    public class FormDangNhap : Form
    {
        // Thuộc tính dùng để truyền sang FormChinh
        public string TenDangNhap { get; private set; }

        private readonly Label lblTen;
        private readonly Label lblMatKhau;
        private readonly TextBox txtTen;
        private readonly TextBox txtMatKhau;
        private readonly Button btnDangNhap;
        private readonly Label lblKetQua;
        private readonly Label lblStatus;

        public FormDangNhap()
        {
            this.Text = "Đăng nhập";
            this.Size = new Size(300, 300); // Tăng chiều cao để chứa thêm nút
            this.StartPosition = FormStartPosition.CenterScreen;

            lblTen = new Label { Text = "Tên đăng nhập:", Location = new Point(20, 20), AutoSize = true };
            lblMatKhau = new Label { Text = "Mật khẩu:", Location = new Point(20, 60), AutoSize = true };

            txtTen = new TextBox { Location = new Point(120, 20), Width = 130 };
            txtMatKhau = new TextBox { Location = new Point(120, 60), Width = 130, UseSystemPasswordChar = true };

            btnDangNhap = new Button { Text = "Đăng nhập", Location = new Point(90, 100), Width = 100 };
            btnDangNhap.Click += BtnDangNhap_Click;

            // Thêm nút Đăng ký và Quên mật khẩu
            LinkLabel lnkDangKy = new LinkLabel
            {
                Text = "Đăng ký tài khoản mới",
                Location = new Point(75, 140),
                AutoSize = true
            };
            lnkDangKy.Click += (s, e) => {
                FormDangKy formDangKy = new FormDangKy();
                if (formDangKy.ShowDialog() == DialogResult.OK)
                {
                    // Tự động điền thông tin từ form đăng ký nếu có
                    if (!string.IsNullOrEmpty(formDangKy.RegisteredUsername))
                    {
                        txtTen.Text = formDangKy.RegisteredUsername;
                        txtMatKhau.Focus();
                    }
                }
            };

            LinkLabel lnkQuenMatKhau = new LinkLabel
            {
                Text = "Quên mật khẩu?",
                Location = new Point(90, 170),
                AutoSize = true
            };
            lnkQuenMatKhau.Click += (s, e) => {
                FormQuenMatKhau formQuenMatKhau = new FormQuenMatKhau();
                formQuenMatKhau.ShowDialog();
            };

            lblKetQua = new Label
            {
                Location = new Point(20, 200),
                Size = new Size(260, 40),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Red
            };

            // Thêm label hiển thị trạng thái kết nối
            lblStatus = new Label
            {
                Location = new Point(20, 240),
                Size = new Size(260, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Arial", 8),
                ForeColor = Color.Gray
            };

            this.Controls.Add(lblTen);
            this.Controls.Add(lblMatKhau);
            this.Controls.Add(txtTen);
            this.Controls.Add(txtMatKhau);
            this.Controls.Add(btnDangNhap);
            this.Controls.Add(lnkDangKy);
            this.Controls.Add(lnkQuenMatKhau);
            this.Controls.Add(lblKetQua);
            this.Controls.Add(lblStatus);

            // Kiểm tra kết nối server khi form khởi động
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

        private async void BtnDangNhap_Click(object sender, EventArgs e)
        {
            string username = txtTen.Text.Trim();
            string password = txtMatKhau.Text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                lblKetQua.Text = "⚠ Vui lòng nhập đầy đủ thông tin!";
                return;
            }

            btnDangNhap.Enabled = false;
            lblKetQua.Text = "Đang xác thực...";

            try
            {
                // Trường hợp tài khoản admin đặc biệt
                if (username == "admin" && password == "admin123")
                {
                    ProcessSuccessfulLogin(username);
                    return;
                }

                // Thử đăng nhập qua server
                bool serverLoginSuccess = false;

                using (TcpClient client = new TcpClient())
                {
                    // Thử kết nối đến server với timeout ngắn
                    var connectTask = client.BeginConnect("localhost", 9876, null, null);
                    bool connected = connectTask.AsyncWaitHandle.WaitOne(3000); // 3 giây timeout

                    if (connected)
                    {
                        client.EndConnect(connectTask);

                        using (NetworkStream stream = client.GetStream())
                        {
                            // Mã hóa mật khẩu trước khi gửi
                            string hashedPassword = LocalAuthManager.HashPassword(password);
                            string request = $"LOGIN|{username}|{hashedPassword}";
                            byte[] data = Encoding.UTF8.GetBytes(request);
                            stream.Write(data, 0, data.Length);

                            byte[] buffer = new byte[1024];
                            int byteCount = stream.Read(buffer, 0, buffer.Length);
                            string response = Encoding.UTF8.GetString(buffer, 0, byteCount);

                            serverLoginSuccess = (response == "OK");
                        }
                    }
                }

                // Nếu đăng nhập server thành công
                if (serverLoginSuccess)
                {
                    ProcessSuccessfulLogin(username);
                    return;
                }

                // Thử xác thực cục bộ nếu server không có hoặc không đăng nhập được
                if (LocalAuthManager.ValidateLogin(username, password))
                {
                    lblStatus.Text = "⚠️ Đăng nhập cục bộ thành công";
                    lblStatus.ForeColor = Color.Orange;
                    ProcessSuccessfulLogin(username);
                    return;
                }

                // Nếu cả hai cách đều thất bại
                lblKetQua.Text = "❌ Sai tài khoản hoặc mật khẩu!";
                btnDangNhap.Enabled = true;
            }
            catch (Exception ex)
            {
                // Thử xác thực cục bộ trong trường hợp có lỗi
                if (LocalAuthManager.ValidateLogin(username, password))
                {
                    lblStatus.Text = "⚠️ Đăng nhập cục bộ thành công";
                    lblStatus.ForeColor = Color.Orange;
                    ProcessSuccessfulLogin(username);
                }
                else
                {
                    lblKetQua.Text = "❌ Sai tài khoản hoặc mật khẩu!";
                    btnDangNhap.Enabled = true;
                }
            }
        }

        private void ProcessSuccessfulLogin(string username)
        {
            TenDangNhap = username;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}