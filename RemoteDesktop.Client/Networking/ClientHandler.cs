using System;
using System.Net.Sockets;
using System.Threading;
using RemoteDesktop.Common.Helpers;
using RemoteDesktop.Common.Models;

namespace RemoteDesktop.Client.Networking
{
    public class ClientHandler
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private bool _isConnected;

        public bool IsConnected => _isConnected;

        public void Connect(string ip, int port)
        {
            try
            {
                _client = new TcpClient();
                _client.Connect(ip, port); // Kết nối tới IP và Port của Server
                _stream = _client.GetStream();
                _isConnected = true;

                // Chạy một luồng để lắng nghe phản hồi từ Server (nếu cần)
                Thread listenThread = new Thread(ReceiveData);
                listenThread.IsBackground = true;
                listenThread.Start();
            }
            catch (Exception ex)
            {
                _isConnected = false;
                throw new Exception("Không thể kết nối tới Server: " + ex.Message);
            }
        }

        // Gửi gói tin đi
        public void SendPacket(Packet packet)
        {
            if (_isConnected && _stream != null)
            {
                try
                {
                    byte[] data = DataHelper.Serialize(packet);
                    _stream.Write(data, 0, data.Length);
                    _stream.Flush(); // Đẩy dữ liệu đi ngay lập tức
                }
                catch { Disconnect(); }
            }
        }

        private void ReceiveData()
        {
            byte[] buffer = new byte[1024 * 5000];
            while (_isConnected)
            {
                try
                {
                    if (_stream.DataAvailable)
                    {
                        int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                        if (bytesRead > 0)
                        {
                            byte[] actualData = new byte[bytesRead];
                            Array.Copy(buffer, actualData, bytesRead);

                            var packet = DataHelper.Deserialize<Packet>(actualData);
                            // Xử lý gói tin Server gửi về (ví dụ: Chat, Lệnh ngắt...)
                        }
                    }
                }
                catch { break; }
            }
            Disconnect();
        }

        public NetworkStream GetStream()
        {
            return _stream; // Trả về stream để frmRemote sử dụng
        }
        public void Disconnect()
        {
            _isConnected = false;
            _stream?.Close();
            _client?.Close();
        }
    }
}