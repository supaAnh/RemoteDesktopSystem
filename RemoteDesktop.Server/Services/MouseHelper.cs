using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RemoteDesktop.Server.Helpers
{
    public static class MouseHelper
    {
        // Import hàm từ thư viện hệ thống Windows để giả lập sự kiện chuột
        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        // Các hằng số định nghĩa loại hành động của chuột
        private const int MOUSEEVENTF_MOVE = 0x0001;
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;

        /// <summary>
        /// Di chuyển con trỏ chuột đến tọa độ chỉ định
        /// </summary>
        public static void SetCursorPos(int x, int y)
        {
            Cursor.Position = new Point(x, y);
        }

        /// <summary>
        /// Giả lập các sự kiện Click chuột
        /// action: 1 (Left Down), 2 (Left Up), 3 (Right Down), 4 (Right Up)
        /// </summary>
        public static void SimulateMouseEvent(int action)
        {
            switch (action)
            {
                case 1: // Left Down
                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                    break;
                case 2: // Left Up
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                    break;
                case 3: // Right Down
                    mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
                    break;
                case 4: // Right Up
                    mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
                    break;
            }
        }
    }
}