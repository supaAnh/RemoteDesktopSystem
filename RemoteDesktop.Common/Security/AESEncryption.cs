using System;
using System.IO;
using System.Security.Cryptography;

namespace RemoteDesktop.Common.Security
{
    public static class AESEncryption
    {
        private static readonly byte[] Key = System.Text.Encoding.UTF8.GetBytes("1234567890123456"); // 16 bytes
        private static readonly byte[] IV = System.Text.Encoding.UTF8.GetBytes("6543210987654321");  // 16 bytes

        public static byte[] Encrypt(byte[] data)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;
                using (var encryptor = aes.CreateEncryptor())
                {
                    return encryptor.TransformFinalBlock(data, 0, data.Length);
                }
            }
        }

        public static byte[] Decrypt(byte[] data)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;
                using (var decryptor = aes.CreateDecryptor())
                {
                    return decryptor.TransformFinalBlock(data, 0, data.Length);
                }
            }
        }
    }
}