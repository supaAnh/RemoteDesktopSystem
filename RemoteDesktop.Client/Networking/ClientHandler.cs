using System;
using System.Net.Sockets;
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
                _client.Connect(ip, port);
                _stream = _client.GetStream();
                _isConnected = true;
            }
            catch (Exception ex)
            {
                _isConnected = false;
                throw new Exception("Không thể kết nối tới Server: " + ex.Message);
            }
        }

        // Gửi gói tin đi sử dụng giao thức an toàn (AES + Length Header)
        public void SendPacket(Packet packet)
        {
            if (_isConnected && _stream != null)
            {
                try
                {
                    //Sử dụng NetworkHelper để đồng bộ giao thức với Server
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
            return _stream; // Trả về stream để frmRemote sử dụng trong ReceiveLoop
        }

        public void Disconnect()
        {
            _isConnected = false;
            _stream?.Close();
            _client?.Close();
        }
    }
}