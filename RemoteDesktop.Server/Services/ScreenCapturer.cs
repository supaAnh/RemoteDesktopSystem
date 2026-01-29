using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using System;
using System.Linq;

namespace RemoteDesktop.Server.Services
{
    public static class ScreenCapturer
    {
        // Cache các thông số nén để không phải tạo lại liên tục -> Tăng tốc độ
        private static ImageCodecInfo _jpegCodec;
        private static EncoderParameters _encoderParams;

        static ScreenCapturer()
        {
            // Lấy bộ mã hóa JPEG
            _jpegCodec = GetEncoder(ImageFormat.Jpeg);

            // Cấu hình tham số nén chất lượng ảnh
            _encoderParams = new EncoderParameters(1);

            // [QUAN TRỌNG] Đặt chất lượng là 30L (30%)
            // Mức này giúp ảnh cực nhẹ (~20KB-40KB), giúp chat gửi đi được ngay lập tức
            // Nếu để mặc định (75-100%), ảnh sẽ nặng ~500KB gây nghẽn mạng
            _encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 30L);
        }

        public static byte[] CaptureDesktop()
        {
            try
            {
                Rectangle bounds = Screen.PrimaryScreen.Bounds;
                using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        // Chụp màn hình
                        g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                    }

                    using (MemoryStream ms = new MemoryStream())
                    {
                        // Lưu ảnh với tham số nén đã cài đặt
                        bitmap.Save(ms, _jpegCodec, _encoderParams);
                        return ms.ToArray();
                    }
                }
            }
            catch
            {
                return new byte[0]; // Trả về rỗng nếu lỗi để không crash app
            }
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            return ImageCodecInfo.GetImageDecoders().FirstOrDefault(codec => codec.FormatID == format.Guid);
        }
    }
}