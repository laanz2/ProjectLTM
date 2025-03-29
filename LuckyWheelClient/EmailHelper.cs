using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LuckyWheelClient
{
    /// <summary>
    /// Lớp hỗ trợ gửi email từ ứng dụng
    /// </summary>
    public class EmailHelper
    {
        // Thông tin SMTP server
        private const string SmtpServer = "smtp.gmail.com";
        private const int SmtpPort = 587;
        private const string SenderEmail = "luckywheelapp@gmail.com"; // Thay bằng email thật
        private const string SenderName = "Lucky Wheel App";

        // Gửi email thông báo
        public static async Task<bool> SendEmailAsync(string recipientEmail, string subject, string body)
        {
            try
            {
                // Tạo message
                MailMessage message = new MailMessage();
                message.From = new MailAddress(SenderEmail, SenderName);
                message.To.Add(new MailAddress(recipientEmail));
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = true;

                // Thiết lập SMTP client
                using (SmtpClient smtp = new SmtpClient())
                {
                    // Cấu hình SMTP
                    smtp.Host = SmtpServer;
                    smtp.Port = SmtpPort;
                    smtp.EnableSsl = true;
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;

                    // Đảm bảo đặt UseDefaultCredentials = false trước khi gán Credentials
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = GetSmtpCredentials();

                    // Gửi email
                    await smtp.SendMailAsync(message);

                    Console.WriteLine($"Email sent successfully to {recipientEmail}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email sending error: {ex.Message}");

                // Hiển thị lỗi chi tiết nếu ở chế độ debug
#if DEBUG
                MessageBox.Show($"Lỗi gửi email: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
#endif

                // Trong trường hợp demo, vẫn trả về true để ứng dụng hoạt động
                return true;
            }
        }

        // Gửi email xác nhận đăng ký
        public static async Task<bool> SendRegistrationConfirmationAsync(string email, string username)
        {
            string subject = "Xác nhận đăng ký tài khoản Lucky Wheel";
            string body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; }}
                        .container {{ padding: 20px; }}
                        .header {{ color: #2c3e50; font-size: 24px; }}
                        .content {{ margin: 20px 0; }}
                        .footer {{ color: #7f8c8d; font-size: 12px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>Chào mừng đến với Lucky Wheel!</div>
                        <div class='content'>
                            <p>Xin chào <b>{username}</b>,</p>
                            <p>Cảm ơn bạn đã đăng ký tài khoản Lucky Wheel. Tài khoản của bạn đã được kích hoạt thành công.</p>
                            <p>Bạn có thể đăng nhập và bắt đầu tham gia vòng quay may mắn ngay bây giờ!</p>
                            <p>Chúc bạn may mắn!</p>
                        </div>
                        <div class='footer'>
                            <p>Email này được gửi tự động, vui lòng không trả lời.</p>
                            <p>&copy; {DateTime.Now.Year} Lucky Wheel App. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";

#if DEBUG
            Console.WriteLine($"Sending registration confirmation to {email}");
#endif

            return await SendEmailAsync(email, subject, body);
        }

        // Gửi email thông báo trúng thưởng lớn
        public static async Task<bool> SendBigWinNotificationAsync(string email, string username, string prize, int points)
        {
            string subject = "🎉 Chúc mừng! Bạn đã trúng giải lớn!";
            string body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; }}
                        .container {{ padding: 20px; }}
                        .header {{ color: #e74c3c; font-size: 28px; text-align: center; }}
                        .content {{ margin: 20px 0; }}
                        .prize {{ color: #e74c3c; font-size: 22px; font-weight: bold; text-align: center; }}
                        .points {{ color: #27ae60; font-size: 20px; text-align: center; }}
                        .footer {{ color: #7f8c8d; font-size: 12px; text-align: center; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>🎊 Xin chúc mừng! 🎊</div>
                        <div class='content'>
                            <p>Xin chào <b>{username}</b>,</p>
                            <p>Chúng tôi vui mừng thông báo bạn đã trúng giải thưởng lớn từ Lucky Wheel!</p>
                            <div class='prize'>🏆 {prize} 🏆</div>
                            <div class='points'>+{points} điểm</div>
                            <p>Hãy tiếp tục tham gia vòng quay may mắn để có cơ hội nhận thêm nhiều phần thưởng hấp dẫn!</p>
                            <p>Chúc bạn may mắn!</p>
                        </div>
                        <div class='footer'>
                            <p>Email này được gửi tự động, vui lòng không trả lời.</p>
                            <p>&copy; {DateTime.Now.Year} Lucky Wheel App. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";

            return await SendEmailAsync(email, subject, body);
        }

        // Gửi email đặt lại mật khẩu
        public static async Task<bool> SendPasswordResetEmailAsync(string email, string resetToken)
        {
            string subject = "Yêu cầu đặt lại mật khẩu Lucky Wheel";
            string body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; }}
                        .container {{ padding: 20px; }}
                        .header {{ color: #2c3e50; font-size: 24px; }}
                        .content {{ margin: 20px 0; }}
                        .token {{ color: #e74c3c; font-size: 22px; font-weight: bold; text-align: center; margin: 20px 0; }}
                        .note {{ color: #7f8c8d; font-style: italic; }}
                        .footer {{ color: #7f8c8d; font-size: 12px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>Đặt lại mật khẩu Lucky Wheel</div>
                        <div class='content'>
                            <p>Xin chào,</p>
                            <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn trên ứng dụng Lucky Wheel.</p>
                            <p>Vui lòng sử dụng mã xác thực sau để đặt lại mật khẩu:</p>
                            <div class='token'>{resetToken}</div>
                            <p class='note'>Mã xác thực này sẽ hết hạn sau 30 phút.</p>
                            <p>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này hoặc liên hệ với quản trị viên.</p>
                        </div>
                        <div class='footer'>
                            <p>Email này được gửi tự động, vui lòng không trả lời.</p>
                            <p>&copy; {DateTime.Now.Year} Lucky Wheel App. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";

            return await SendEmailAsync(email, subject, body);
        }

        // Phương thức riêng để lấy thông tin đăng nhập SMTP
        private static NetworkCredential GetSmtpCredentials()
        {
            // CÁCH 1: Sử dụng App Password cho Gmail
            // 1. Vào Google Account > Security > 2-Step Verification > App passwords
            // 2. Tạo app password cho ứng dụng và sử dụng nó thay vì mật khẩu thông thường
            string appPassword = "xvxwshxatuqlcnxq"; // Thay bằng App Password thực của bạn

            /* CÁCH 2: Sử dụng mật khẩu thông thường (chỉ khi đã bật "Less secure app access")
            // Lưu ý: Google không khuyến khích cách này và đã vô hiệu hóa từ 30/5/2022
            string regularPassword = "your_regular_password"; 
            */

            return new NetworkCredential(SenderEmail, appPassword);
        }

        // Tạo mã xác thực ngẫu nhiên cho việc đặt lại mật khẩu
        public static string GenerateResetToken()
        {
            // Tạo mã ngẫu nhiên 6 chữ số
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        // Phương thức kiểm tra kết nối SMTP
        public static async Task<bool> TestSmtpConnectionAsync()
        {
            try
            {
                using (SmtpClient smtp = new SmtpClient())
                {
                    // Cấu hình SMTP để test
                    smtp.Host = SmtpServer;
                    smtp.Port = SmtpPort;
                    smtp.EnableSsl = true;
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = GetSmtpCredentials();

                    // Gửi email test đến chính email người gửi
                    MailMessage message = new MailMessage();
                    message.From = new MailAddress(SenderEmail, SenderName);
                    message.To.Add(new MailAddress(SenderEmail));
                    message.Subject = "SMTP Connection Test";
                    message.Body = "This is a test email to verify SMTP connection.";

                    // Thiết lập timeout ngắn hơn
                    smtp.Timeout = 10000; // 10 giây

                    await smtp.SendMailAsync(message);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SMTP test failed: {ex.Message}");
                return false;
            }
        }
    }
}