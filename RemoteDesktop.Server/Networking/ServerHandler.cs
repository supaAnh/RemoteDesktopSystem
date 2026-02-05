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

        // Giữ lại DatabaseManager để dùng cho Register (nếu cần), nhưng Login sẽ không dùng tới nó nữa
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
                _serverSocket.Blocking = false;

                _isRunning = true;

                Thread t = new Thread(AcceptClient);
                t.IsBackground = true;
                t.Start();

                LogToUI($"Server đang chạy trên cổng {port}!");
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
                    // Lấy IP để log
                    string ip = "UNKNOWN";
                    try { ip = ((IPEndPoint)clientSocket.RemoteEndPoint).Address.ToString(); } catch { }

                    LogToUI($"Client [{ip}] đã kết nối Socket.");

                    clientSocket.Blocking = true;
                    TcpClient tcpClient = new TcpClient { Client = clientSocket };

                    Thread t = new Thread(() => HandleConnectedClient(tcpClient));
                    t.IsBackground = true;
                    t.Start();
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.WouldBlock) Thread.Sleep(100);
                }
                catch { }
            }
        }

        private void HandleConnectedClient(TcpClient client)
        {
            string clientIP = "UNKNOWN";
            try
            {
                if (client.Client != null && client.Client.RemoteEndPoint != null)
                    clientIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

                using (NetworkStream stream = client.GetStream())
                {
                    while (_isRunning && client.Connected)
                    {
                        var packet = NetworkHelper.ReceiveSecurePacket(stream);
                        if (packet != null) ProcessPacket(packet, client, clientIP);
                        else break; // Client ngắt kết nối
                    }
                }
            }
            catch (Exception ex)
            {
                LogToUI($"[{clientIP}] Lỗi kết nối: {ex.Message}");
            }
            finally
            {
                _connectionGuard.RemoveClient(client);
                try { client.Close(); } catch { }
                LogToUI($"[{clientIP}] Đã ngắt kết nối.");
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

        private void HandleLogin(Packet packet, TcpClient client)
        {
            var loginInfo = DataHelper.Deserialize<LoginDTO>(packet.Data);
            if (loginInfo == null) return;

            string username = loginInfo.Username;
            string password = loginInfo.Password;

            // 1. Logic kiểm tra cứng: Mật khẩu 123456 và Tên bắt đầu bằng "admin"
            // StringComparison.OrdinalIgnoreCase giúp admin1 và Admin1 đều được chấp nhận
            bool isValidFormat = (password == "123456" &&
                                  !string.IsNullOrEmpty(username) &&
                                  username.StartsWith("admin", StringComparison.OrdinalIgnoreCase));

            // 2. Kiểm tra xem tên này có đang Online không
            bool isAlreadyOnline = _connectionGuard.IsUsernameOnline(username);

            // LOGIC: Đúng định dạng + Chưa online = CHO VÀO (Không cần hỏi Database)
            if (isValidFormat && !isAlreadyOnline)
            {
                _connectionGuard.AddClient(client, username);
                OnClientConnected?.Invoke(client);

                Packet response = new Packet { Type = CommandType.Login, Data = Encoding.UTF8.GetBytes("SUCCESS") };
                NetworkHelper.SendSecurePacket(client.GetStream(), response);
                LogToUI($"Client [{username}] đăng nhập thành công.");
            }
            else
            {
                // Tìm nguyên nhân để log ra màn hình Server cho dễ debug
                string reason = "Lỗi không xác định";
                if (!isValidFormat) reason = "Sai tên tài khoản hoặc mật khẩu";
                else if (isAlreadyOnline) reason = "Tài khoản đang được sử dụng!";

                Packet response = new Packet { Type = CommandType.Login, Data = Encoding.UTF8.GetBytes("FAIL") };
                NetworkHelper.SendSecurePacket(client.GetStream(), response);
                LogToUI($"Đăng nhập thất bại ({username}): {reason}");
            }
        }

        private void HandleRegister(Packet packet, TcpClient client)
        {

            LoginDTO regInfo = DataHelper.Deserialize<LoginDTO>(packet.Data);
            if (regInfo != null && regInfo.Username.StartsWith("admin", StringComparison.OrdinalIgnoreCase))
            {
                bool isRegistered = _dbManager.RegisterUser(regInfo.Username, regInfo.Password);
                string responseMsg = isRegistered ? "REGISTER_PENDING" : "REGISTER_FAILED";
                NetworkHelper.SendSecurePacket(client.GetStream(), new Packet { Type = CommandType.Register, Data = Encoding.UTF8.GetBytes(responseMsg) });
            }
            else
            {
                NetworkHelper.SendSecurePacket(client.GetStream(), new Packet { Type = CommandType.Register, Data = Encoding.UTF8.GetBytes("REGISTER_FAILED") });
            }
        }

        private void HandleInputControl(Packet packet, TcpClient sender)
        {
            // Chỉ cho phép điều khiển nếu đã đăng nhập (nằm trong Guard)
            if (!_connectionGuard.IsController(sender)) return;

            var input = DataHelper.Deserialize<InputDTO>(packet.Data);
            if (input == null) return;

            // Xử lý chuột/phím trên luồng riêng để chạy mượt
            ThreadPool.QueueUserWorkItem(_ => {
                try
                {
                    if (input.Type == 0) // Mouse
                    {
                        int sw = Screen.PrimaryScreen.Bounds.Width;
                        int sh = Screen.PrimaryScreen.Bounds.Height;
                        MouseHelper.SetCursorPos((input.X * sw) / 1000, (input.Y * sh) / 1000);
                        if (input.Action > 0) MouseHelper.SimulateMouseEvent(input.Action);
                    }
                    else if (input.Type == 1) // Keyboard
                    {
                        KeyboardHelper.SimulateKeyPress(input.KeyCode);
                    }
                }
                catch { }
            });
        }

        private void HandleChatRequest(Packet packet, TcpClient client, string ip)
        {
            string rawMsg = Encoding.UTF8.GetString(packet.Data);
            OnChatReceived?.Invoke(client, rawMsg);
            // Gửi lại tin nhắn cho tất cả client
            BroadcastPacket(new Packet { Type = CommandType.Chat, Data = Encoding.UTF8.GetBytes($"[{ip}]: {rawMsg}") });
        }

        private void HandleFileTransfer(Packet packet, TcpClient client, string ip)
        {
            OnFileReceived?.Invoke(client, packet.Data);
            BroadcastPacket(packet);
        }

        public void BroadcastPacket(Packet packet)
        {
            var clients = _connectionGuard.GetConnectedClients();
            // Duyệt ngược để an toàn khi xóa phần tử
            for (int i = clients.Count - 1; i >= 0; i--)
            {
                var client = clients[i];
                ThreadPool.QueueUserWorkItem(_ => {
                    try
                    {
                        if (client != null && client.Connected)
                        {
                            NetworkHelper.SendSecurePacket(client.GetStream(), packet);
                        }
                    }
                    catch
                    {
                        _connectionGuard.RemoveClient(client);
                    }
                });
            }
        }

        public void LogToUI(string message)
        {
            OnLogAdded?.Invoke(message);
            if (_logView.InvokeRequired)
            {
                _logView.BeginInvoke(new Action(() => LogToUI(message)));
            }
            else
            {
                try
                {
                    if (_logView.Items.Count > 100) _logView.Items.RemoveAt(0);
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
            _isRunning = false;
            BroadcastPacket(new Packet { Type = CommandType.Disconnect, Data = Encoding.UTF8.GetBytes("Server Stop") });
            Thread.Sleep(500); // Đợi client nhận tin
            try { _serverSocket.Close(); } catch { }
            _connectionGuard.ClearAll();
        }
    }
}