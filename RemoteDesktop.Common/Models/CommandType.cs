namespace RemoteDesktop.Common.Models
{
    public enum CommandType
    {
        Login,
        ScreenUpdate,
        MouseEvent,
        KeyboardEvent,
        FileTransfer,
        Chat,
        Disconnect
    }
}