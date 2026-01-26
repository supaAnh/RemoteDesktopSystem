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
        // [THAY ĐỔI 1] Dùng Socket thay vì TcpListener
        private Socket _serverSocket;
        private bool _isRunning;
        private ListView _logView;

        // Các sự kiện
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
                // [THAY ĐỔI 2] Khởi tạo Socket theo mô hình trong ảnh
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);
                _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _serverSocket.Bind(endPoint);
                _serverSocket.Listen(10);

                // [QUAN TRỌNG] Chuyển sang chế độ Non-blocking như yêu cầu
                _serverSocket.Blocking = false;

                _isRunning = true;

                Thread t = new Thread(AcceptClient);
                t.IsBackground = true;
                t.Start();

                LogToUI($"Server đang chạy chế độ Non-blocking trên cổng {port}...");
            }
            catch (Exception ex)
            {
                LogToUI("Lỗi khởi động Server: " + ex.Message);
            }
        }

        // [THAY ĐỔI 3] Vòng lặp Accept Client theo chuẩn Non-blocking (Giống ảnh)
        private void AcceptClient()
        {
            while (_isRunning)
            {
                try
                {
                    // Thử chấp nhận kết nối
                    Socket clientSocket = _serverSocket.Accept();

                    // Nếu thành công (không bị lỗi), xử lý client mới
                    string ip = ((IPEndPoint)clientSocket.RemoteEndPoint).Address.ToString();
                    LogToUI($"Client {ip} đã kết nối (Non-blocking Socket).");

                    // Chuyển Socket thành TcpClient để tương thích với code xử lý cũ (NetworkStream)
                    // Lưu ý: Ta cần chuyển lại Blocking = true cho Socket con để đảm bảo truyền File/Ảnh ổn định
                    clientSocket.Blocking = true;
                    TcpClient tcpClient = new TcpClient { Client = clientSocket };

                    Thread t = new Thread(() => HandleConnectedClient(tcpClient));
                    t.IsBackground = true;
                    t.Start();
                }
                catch (SocketException ex)
                {
                    // [QUAN TRỌNG] Bắt lỗi WouldBlock - Đây là dấu hiệu của Non-blocking khi chưa có ai kết nối
                    if (ex.SocketErrorCode == SocketError.WouldBlock)
                    {
                        // Chưa có kết nối nào, tiếp tục vòng lặp (giống mô phỏng công việc khác)
                        Thread.Sleep(100);
                    }
                    else
                    {
                        LogToUI("Lỗi Accept: " + ex.Message);
                    }
                }
                catch (Exception ex)
                {
                    if (_isRunning) LogToUI("Lỗi khác: " + ex.Message);
                }
            }
        }

        // --- Các phần xử lý bên dưới giữ nguyên logic ---
        private void HandleConnectedClient(TcpClient client)
        {
            string clientIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            try
            {
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
                default: LogToUI($"Gói tin lạ: {packet.Type}"); break;
            }
        }

        private void HandleInputControl(Packet packet, TcpClient sender)
        {
            if (!_connectionGuard.IsController(sender)) return;
            var input = DataHelper.Deserialize<InputDTO>(packet.Data);
            if (input == null) return;
            string clientIP = ((IPEndPoint)sender.Client.RemoteEndPoint).Address.ToString();

            if (input.Type == 0) // Chuột
            {
                if (input.Action > 0) LogToUI($"[{clientIP}] Click chuột (Action: {input.Action}).");
                int sw = Screen.PrimaryScreen.Bounds.Width;
                int sh = Screen.PrimaryScreen.Bounds.Height;
                MouseHelper.SetCursorPos((input.X * sw) / 1000, (input.Y * sh) / 1000);
                if (input.Action > 0) MouseHelper.SimulateMouseEvent(input.Action);
            }
            else if (input.Type == 1) // Phím
            {
                LogToUI($"[{clientIP}] Nhấn phím (Mã: {input.KeyCode}).");
                KeyboardHelper.SimulateKeyPress(input.KeyCode);
            }
        }

        private void HandleLogin(Packet packet, TcpClient client)
        {
            var loginInfo = DataHelper.Deserialize<LoginDTO>(packet.Data);
            string clientIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            if (loginInfo == null) return;
            bool isValid = _dbManager.ValidateUser(loginInfo.Username, loginInfo.Password);

            Packet response = new Packet { Type = CommandType.Login, Data = Encoding.UTF8.GetBytes(isValid ? "SUCCESS" : "FAIL") };
            NetworkHelper.SendSecurePacket(client.GetStream(), response);

            if (isValid)
            {
                _connectionGuard.AddClient(client);
                int count = _connectionGuard.GetConnectedClients().Count;
                LogToUI($"[{clientIP}] '{loginInfo.Username}' đăng nhập thành công. (Online: {count})");
                OnClientConnected?.Invoke(client);
            }
            else LogToUI($"[{clientIP}] Đăng nhập thất bại: '{loginInfo.Username}'");
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
            OnChatReceived?.Invoke(client, rawMsg);
            BroadcastPacket(new Packet { Type = CommandType.Chat, Data = Encoding.UTF8.GetBytes($"[{ip}]: {rawMsg}") });
        }

        private void HandleFileTransfer(Packet packet, TcpClient client, string ip)
        {
            var fileDto = DataHelper.Deserialize<FilePacketDTO>(packet.Data);
            if (fileDto != null)
            {
                OnFileReceived?.Invoke(client, packet.Data);
                string storagePath = Path.Combine(Application.StartupPath, "ReceivedFiles", fileDto.FileName);
                Directory.CreateDirectory(Path.GetDirectoryName(storagePath));
                File.WriteAllBytes(storagePath, fileDto.Buffer);
                BroadcastPacket(new Packet { Type = CommandType.Chat, Data = Encoding.UTF8.GetBytes($"[{ip}] gửi file: {fileDto.FileName}") });
                BroadcastPacket(packet);
            }
        }

        public void CheckDatabaseConnection() { try { _dbManager.InitializeDatabase(); LogToUI("Kết nối Database thành công."); } catch (Exception ex) { LogToUI("Lỗi DB: " + ex.Message); } }

        public void BroadcastPacket(Packet packet)
        {
            foreach (var client in _connectionGuard.GetConnectedClients())
            {
                try { if (client != null && client.Connected) NetworkHelper.SendSecurePacket(client.GetStream(), packet); } catch { }
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
                    string source = "SYSTEM";
                    string content = message;
                    Color textColor = Color.Red;
                    if (message.Trim().StartsWith("[") && message.Contains("]"))
                    {
                        int closeBracketIndex = message.IndexOf("]");
                        if (closeBracketIndex > 1)
                        {
                            source = message.Substring(1, closeBracketIndex - 1);
                            content = message.Substring(closeBracketIndex + 1).Trim();
                            textColor = Color.Blue;
                        }
                    }
                    ListViewItem item = new ListViewItem(new[] { DateTime.Now.ToString("HH:mm:ss"), source, content });
                    item.ForeColor = textColor;
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
            if (_serverSocket != null) { try { _serverSocket.Close(); } catch { } }
            _connectionGuard.ClearAll();
        }
    }
}