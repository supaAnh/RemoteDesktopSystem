using RemoteDesktop.Common.DTOs;
using RemoteDesktop.Common.Helpers;
using RemoteDesktop.Common.Models;
using RemoteDesktop.Server.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace RemoteDesktop.Server.Networking
{
    public class ServerHandler
    {
        private Socket _serverSocket;
        private bool _isRunning;
        private ListView _logView;

        public event Action<string>? OnLogAdded;
        public delegate void ChatReceivedHandler(TcpClient sender, string message);
        public event ChatReceivedHandler? OnChatReceived;
        public delegate void ClientConnectedHandler(TcpClient client);
        public event ClientConnectedHandler? OnClientConnected;
        public delegate void FileReceivedHandler(TcpClient sender, byte[] data);
        public event FileReceivedHandler? OnFileReceived;

        private Database.DatabaseManager _dbManager = new Database.DatabaseManager();
        private ConnectionGuard _connectionGuard = new ConnectionGuard();

        public ServerHandler(ListView logView)
        {
            _logView = logView;
        }

        public void StartListening(int port)
        {
            try
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);
                _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _serverSocket.Bind(endPoint);
                _serverSocket.Listen(10);
                _serverSocket.Blocking = false; // Non-blocking mode

                _isRunning = true;

                Thread t = new Thread(AcceptClient);
                t.IsBackground = true;
                t.Start();

                LogToUI($"Server đang chạy trên cổng {port} (Non-blocking)...");
            }
            catch (Exception ex)
            {
                LogToUI("Lỗi khởi động Server: " + ex.Message);
            }
        }

        private void AcceptClient()
        {
            while (_isRunning)
            {
                try
                {
                    Socket clientSocket = _serverSocket.Accept();

                    // Client kết nối thành công
                    string ip = ((IPEndPoint)clientSocket.RemoteEndPoint).Address.ToString();
                    LogToUI($"Client {ip} đã kết nối.");

                    // Chuyển lại Blocking = true cho socket con để truyền nhận dữ liệu ổn định
                    clientSocket.Blocking = true;
                    TcpClient tcpClient = new TcpClient { Client = clientSocket };

                    Thread t = new Thread(() => HandleConnectedClient(tcpClient));
                    t.IsBackground = true;
                    t.Start();
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.WouldBlock)
                    {
                        Thread.Sleep(100); // Chờ kết nối mới
                    }
                    else
                    {
                        LogToUI("Lỗi Accept: " + ex.Message);
                    }
                }
                catch { }
            }
        }

        private void HandleConnectedClient(TcpClient client)
        {
            string clientIP = "UNKNOWN";
            try
            {
                clientIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                using (NetworkStream stream = client.GetStream())
                {
                    while (_isRunning && client.Connected)
                    {
                        var packet = NetworkHelper.ReceiveSecurePacket(stream);
                        if (packet != null) ProcessPacket(packet, client, clientIP);
                        else break;
                    }
                }
            }
            catch (Exception ex)
            {
                LogToUI($"[{clientIP}] Ngắt kết nối: {ex.Message}");
            }
            finally
            {
                _connectionGuard.RemoveClient(client);
                client.Close();
                LogToUI($"[{clientIP}] Client đã thoát.");
            }
        }

        private void ProcessPacket(Packet packet, TcpClient client, string ip)
        {
            switch (packet.Type)
            {
                case CommandType.Login: HandleLogin(packet, client); break;
                case CommandType.Register: HandleRegister(packet, client); break;
                case CommandType.Chat: HandleChatRequest(packet, client, ip); break;
                case CommandType.FileTransfer: HandleFileTransfer(packet, client, ip); break;
                case CommandType.Disconnect: client.Close(); break;
                case CommandType.InputControl: HandleInputControl(packet, client); break;
            }
        }

        private void HandleInputControl(Packet packet, TcpClient sender)
        {
            if (!_connectionGuard.IsController(sender)) return;
            var input = DataHelper.Deserialize<InputDTO>(packet.Data);
            if (input == null) return;

            if (input.Type == 0) // Chuột
            {
                int sw = Screen.PrimaryScreen.Bounds.Width;
                int sh = Screen.PrimaryScreen.Bounds.Height;
                MouseHelper.SetCursorPos((input.X * sw) / 1000, (input.Y * sh) / 1000);
                if (input.Action > 0) MouseHelper.SimulateMouseEvent(input.Action);
            }
            else if (input.Type == 1) // Phím
            {
                KeyboardHelper.SimulateKeyPress(input.KeyCode);
            }
        }

        private void HandleLogin(Packet packet, TcpClient client)
        {
            var loginInfo = DataHelper.Deserialize<LoginDTO>(packet.Data);
            bool isValid = _dbManager.ValidateUser(loginInfo.Username, loginInfo.Password);

            Packet response = new Packet { Type = CommandType.Login, Data = Encoding.UTF8.GetBytes(isValid ? "SUCCESS" : "FAIL") };
            NetworkHelper.SendSecurePacket(client.GetStream(), response);

            if (isValid)
            {
                _connectionGuard.AddClient(client);
                OnClientConnected?.Invoke(client);
                LogToUI($"Client {loginInfo.Username} đã đăng nhập.");
            }
        }

        private void HandleRegister(Packet packet, TcpClient client)
        {
            LoginDTO regInfo = DataHelper.Deserialize<LoginDTO>(packet.Data);
            bool isRegistered = _dbManager.RegisterUser(regInfo.Username, regInfo.Password);
            string responseMsg = isRegistered ? "REGISTER_PENDING" : "REGISTER_FAILED";
            NetworkHelper.SendSecurePacket(client.GetStream(), new Packet { Type = CommandType.Register, Data = Encoding.UTF8.GetBytes(responseMsg) });
        }

        private void HandleChatRequest(Packet packet, TcpClient client, string ip)
        {
            string rawMsg = Encoding.UTF8.GetString(packet.Data);

            // 1. Kích hoạt sự kiện để hiện lên UI Server
            OnChatReceived?.Invoke(client, rawMsg);

            // 2. Gửi cho tất cả các Client khác
            BroadcastPacket(new Packet { Type = CommandType.Chat, Data = Encoding.UTF8.GetBytes($"[{ip}]: {rawMsg}") });
        }

        private void HandleFileTransfer(Packet packet, TcpClient client, string ip)
        {
            OnFileReceived?.Invoke(client, packet.Data);
            BroadcastPacket(packet); // Chuyển tiếp file cho mọi người
        }

        public void BroadcastPacket(Packet packet)
        {
            // Lấy danh sách client đang online
            var clients = _connectionGuard.GetConnectedClients();

            // Duyệt ngược để an toàn khi xóa
            for (int i = clients.Count - 1; i >= 0; i--)
            {
                var client = clients[i];
                try
                {
                    if (client != null && client.Connected)
                    {
                        NetworkHelper.SendSecurePacket(client.GetStream(), packet);
                    }
                }
                catch
                {
                    // Nếu gửi lỗi thì xóa client đó ra khỏi danh sách
                    _connectionGuard.RemoveClient(client);
                }
            }
        }

        public void LogToUI(string message)
        {
            OnLogAdded?.Invoke(message);
            if (_logView.InvokeRequired) _logView.Invoke(new Action(() => LogToUI(message)));
            else
            {
                try
                {
                    ListViewItem item = new ListViewItem(new[] { DateTime.Now.ToString("HH:mm:ss"), "SYSTEM", message });
                    item.ForeColor = Color.Blue;
                    _logView.Items.Add(item);
                    if (_logView.Items.Count > 0) _logView.Items[_logView.Items.Count - 1].EnsureVisible();
                }
                catch { }
            }
        }

        public void Stop()
        {
            BroadcastPacket(new Packet { Type = CommandType.Disconnect, Data = Encoding.UTF8.GetBytes("Server Stop") });
            _isRunning = false;
            try { _serverSocket.Close(); } catch { }
            _connectionGuard.ClearAll();
        }
    }
}