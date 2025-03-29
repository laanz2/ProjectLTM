using System;
using System.Windows.Forms;

namespace LuckyWheelClient
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Hiển thị form đăng nhập trước
            FormDangNhap formDangNhap = new FormDangNhap();

            // Chỉ khi đăng nhập thành công mới mở FormChinh
            if (formDangNhap.ShowDialog() == DialogResult.OK)
            {
                string tenDangNhap = formDangNhap.TenDangNhap;

                // Truyền tên đăng nhập vào FormChinh
                Application.Run(new FormChinh(tenDangNhap));
            }
            else
            {
                // Thoát chương trình nếu người dùng đóng form hoặc nhập sai
                Application.Exit();
            }
        }
    }
}