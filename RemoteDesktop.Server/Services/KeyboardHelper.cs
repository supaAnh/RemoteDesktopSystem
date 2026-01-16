using System;
using System.Runtime.InteropServices;

namespace RemoteDesktop.Server.Helpers
{
    public static class KeyboardHelper
    {
        // Import hàm từ thư viện hệ thống Windows để giả lập sự kiện bàn phím
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        // Các hằng số định nghĩa trạng thái phím
        private const int KEYEVENTF_KEYDOWN = 0x0000; // Nhấn xuống
        private const int KEYEVENTF_KEYUP = 0x0002;   // Thả ra

        public static void SimulateKeyPress(int keyCode)
        {
            try
            {
                // 1. Nhấn phím xuống
                keybd_event((byte)keyCode, 0, KEYEVENTF_KEYDOWN, 0);

                // 2. Thả phím ra
                keybd_event((byte)keyCode, 0, KEYEVENTF_KEYUP, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi giả lập bàn phím: " + ex.Message);
            }
        }
    }
}