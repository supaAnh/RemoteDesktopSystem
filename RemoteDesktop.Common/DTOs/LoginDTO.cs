using System;
namespace RemoteDesktop.Common.DTOs
{
    [Serializable]
    public class LoginDTO
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}