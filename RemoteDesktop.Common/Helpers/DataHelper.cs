using System;
using System.Text;
using System.Text.Json;

namespace RemoteDesktop.Common.Helpers
{
    //Biến đối tượng thành mảng byte để truyền nhận qua mạng




    public static class DataHelper
    {
        // Chuyển đổi một đối tượng (như Packet, LoginDTO) thành mảng byte để gửi qua NetworkStream.
        public static byte[] Serialize(object obj)
        {
            if (obj == null) return null;

            try
            {
                // 1. Chuyển đối tượng thành chuỗi JSON
                string jsonString = JsonSerializer.Serialize(obj);

                // 2. Chuyển chuỗi JSON thành mảng byte (sử dụng mã hóa UTF-8)
                return Encoding.UTF8.GetBytes(jsonString);
            }
            catch (Exception ex)
            {
                // Bạn có thể log lỗi ở đây nếu cần
                return null;
            }
        }

        // Chuyển đổi mảng byte nhận được từ NetworkStream trở lại thành đối tượng ban đầu.
        public static T Deserialize<T>(byte[] data)
        {
            if (data == null || data.Length == 0) return default;

            try
            {
                // 1. Chuyển mảng byte về lại chuỗi văn bản (UTF-8)
                string jsonString = Encoding.UTF8.GetString(data);

                // 2. Giải mã chuỗi JSON thành đối tượng kiểu T
                return JsonSerializer.Deserialize<T>(jsonString);
            }
            catch (Exception)
            {
                return default;
            }
        }
    }
}