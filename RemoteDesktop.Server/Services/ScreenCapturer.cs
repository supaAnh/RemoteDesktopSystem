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
        private static ImageCodecInfo _jpegCodec;
        private static EncoderParameters _encoderParams;

        static ScreenCapturer()
        {
            _jpegCodec = GetEncoder(ImageFormat.Jpeg);
            _encoderParams = new EncoderParameters(1);
            // Chất lượng 30L là lựa chọn tốt nhất để cân bằng giữa độ rõ và tốc độ
            _encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 30L);
        }

        public static byte[] CaptureDesktop()
        {
            try
            {
                Rectangle bounds = Screen.PrimaryScreen.Bounds;
                // Sử dụng 'using' triệt để để giải phóng RAM ngay lập tức
                using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                    }

                    using (MemoryStream ms = new MemoryStream())
                    {
                        bitmap.Save(ms, _jpegCodec, _encoderParams);
                        return ms.ToArray();
                    }
                }
            }
            catch
            {
                return new byte[0];
            }
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            return ImageCodecInfo.GetImageDecoders().FirstOrDefault(codec => codec.FormatID == format.Guid);
        }
    }
}