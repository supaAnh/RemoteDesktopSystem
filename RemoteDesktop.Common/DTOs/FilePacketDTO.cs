using System;

namespace RemoteDesktop.Common.DTOs
{
    [Serializable]
    public class FilePacketDTO
    {
        public string FileName { get; set; }
        public byte[] Buffer { get; set; }

        
    }
}