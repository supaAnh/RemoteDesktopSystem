using System;
using System.Net.Sockets;
using RemoteDesktop.Common.Models;
using RemoteDesktop.Common.Security;
using RemoteDesktop.Common.Helpers;

namespace RemoteDesktop.Common.Helpers
{
    public static class NetworkHelper
    {
        // Hàm Gửi gói tin an toàn (Dùng cho cả Client và Server)
        public static void SendSecurePacket(NetworkStream stream, Packet packet)
        {
            try
            {
                byte[] rawData = DataHelper.Serialize(packet);
                byte[] encryptedData = AESEncryption.Encrypt(rawData);

                // Gửi 4 byte độ dài trước
                byte[] lengthHeader = BitConverter.GetBytes(encryptedData.Length);
                stream.Write(lengthHeader, 0, 4);

                // Gửi dữ liệu thực tế
                stream.Write(encryptedData, 0, encryptedData.Length);
                stream.Flush();
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi gửi dữ liệu: " + ex.Message);
            }
        }

        // Hàm Nhận gói tin an toàn
        public static Packet ReceiveSecurePacket(NetworkStream stream)
        {
            // 1. Đọc 4 byte header
            byte[] header = new byte[4];
            int bytesRead = stream.Read(header, 0, 4);
            if (bytesRead < 4) return null;

            int dataLength = BitConverter.ToInt32(header, 0);

            // 2. Đọc dữ liệu theo độ dài đã biết
            byte[] encryptedData = new byte[dataLength];
            int totalRead = 0;
            while (totalRead < dataLength)
            {
                int read = stream.Read(encryptedData, totalRead, dataLength - totalRead);
                if (read == 0) break;
                totalRead += read;
            }

            // 3. Giải mã và Deserialize
            byte[] decryptedData = AESEncryption.Decrypt(encryptedData);
            return DataHelper.Deserialize<Packet>(decryptedData);
        }
    }
}