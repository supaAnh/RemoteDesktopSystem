using System;
using System.Collections.Generic;
using System.Text;

namespace RemoteDesktop.Common.Models
{
    [Serializable] // Cho phép chuyển đối tượng thành mảng Byte để gửi đi
    public class Packet
    {
        public CommandType Type { get; set; }
        public byte[] Data { get; set; }      // Dữ liệu thực tế (đã mã hóa AES)
        public string SenderId { get; set; }  // ID người gửi
    }
}
