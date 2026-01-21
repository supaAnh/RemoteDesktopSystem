namespace RemoteDesktop.Common.Models
{
    [Serializable]
    public enum CommandType
    {   // Đảm bảo thứ tự này không thay đổi giữa Server và Client
        Login = 0,
        Chat = 1,
        FileTransfer = 2,
        ScreenUpdate = 3,
        Disconnect = 4,
        InputControl = 5,
        Register = 6,
        ServerLog = 7

    }
}