namespace RemoteDesktop.Common.Models
{
    [Serializable]
    public enum CommandType
    {
        Login = 0,
        Chat = 1,
        FileTransfer = 2,
        ScreenUpdate = 3,
        Disconnect = 4,
        InputControl = 5
        // Đảm bảo thứ tự này không thay đổi giữa Server và Client
    }
}