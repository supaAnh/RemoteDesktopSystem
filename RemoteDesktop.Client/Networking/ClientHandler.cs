using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using RemoteDesktop.Common.Helpers;
using RemoteDesktop.Common.Models;

namespace RemoteDesktop.Client.Networking
{
    public class ClientHandler
    {
        // [THAY ĐỔI 1] Dùng Socket thay vì TcpClient
        private Socket _clientSocket;
        private TcpClient _tcpClientWrapper; // Wrapper để dùng NetworkStream
        private NetworkStream _stream;
        private bool _isConnected;
        private string _sessionKey;
        public string SessionKey { get => _sessionKey; set => _sessionKey = value; }
        public bool IsConnected => _isConnected;

        public void Connect(string ip, int port)
        {
            try
            {
                // [THAY ĐỔI 2] Tạo Socket và đặt chế độ Non-blocking
                _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _clientSocket.Blocking = false;

                try
                {
                    _clientSocket.Connect(ip, port);
                }
                catch (SocketException ex)
                {
                    // [QUAN TRỌNG] Xử lý lỗi WouldBlock khi Connect (Giống ảnh Client)
                    if (ex.SocketErrorCode != SocketError.WouldBlock && ex.SocketErrorCode != SocketError.IsConnected)
                    {
                        throw; // Nếu lỗi thật thì ném ra
                    }

                    // Vì Non-blocking nên Connect sẽ không xong ngay, ta cần chờ một chút
                    // Dùng Poll để kiểm tra khi nào Socket sẵn sàng ghi (Write) -> Tức là đã kết nối xong
                    int timeoutMicroseconds = 5000000; // 5 giây
                    if (!_clientSocket.Poll(timeoutMicroseconds, SelectMode.SelectWrite))
                    {
                        throw new Exception("Quá thời gian kết nối Server (Timeout).");

                    }
                }

                // Sau khi Connect thành công theo kiểu Non-blocking
                // Ta bọc nó vào TcpClient để lấy Stream xử lý gửi nhận ảnh/file
                _clientSocket.Blocking = true; // Chuyển lại Blocking để truyền dữ liệu ổn định
                _tcpClientWrapper = new TcpClient { Client = _clientSocket };
                _stream = _tcpClientWrapper.GetStream();
                _isConnected = true;
            }
            catch (Exception ex)
            {
                _isConnected = false;
                throw new Exception("Không thể kết nối tới Server: " + ex.Message);
            }
        }

        public Packet ReceivePacket()
        {
            if (_isConnected && _stream != null)
            {
                try
                {
                    return NetworkHelper.ReceiveSecurePacket(_stream);
                }
                catch
                {
                    Disconnect();
                }
            }
            return null;
        }

        public void SendPacket(Packet packet)
        {
            if (_isConnected && _stream != null)
            {
                try
                {
                    packet.SenderId = _sessionKey; // Gắn Key xác thực vào gói tin
                    NetworkHelper.SendSecurePacket(_stream, packet);
                }
                catch
                {
                    Disconnect();
                }
            }
        }

        public NetworkStream GetStream()
        {
            return _stream;
        }

        public void Disconnect()
        {
            _isConnected = false;
            _stream?.Close();
            _tcpClientWrapper?.Close();
            if (_clientSocket != null && _clientSocket.Connected) _clientSocket.Close();
        }
    }
}