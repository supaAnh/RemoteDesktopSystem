using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.IO;

namespace RemoteDesktop.Server.Services
{
    public static class ScreenCapturer
    {
        public static byte[] CaptureDesktop()
        {
            // 1. Lấy kích thước toàn màn hình
            Rectangle bounds = Screen.PrimaryScreen.Bounds;
            using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    // 2. Chụp ảnh màn hình
                    g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                }

                // 3. Nén ảnh thành JPEG để giảm dung lượng (cực kỳ quan trọng)
                using (MemoryStream ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Jpeg);
                    return ms.ToArray();
                }
            }
        }
    }
}