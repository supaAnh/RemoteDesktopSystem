using RemoteDesktop.Common.DTOs;
using RemoteDesktop.Common.Helpers;
using RemoteDesktop.Common.Models;
using RemoteDesktop.Server.Helpers;
using System;
using System.Collections.Generic;
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
        private TcpListener _server;
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
                _server = new TcpListener(IPAddress.Any, port);
                _server.Start();
                _isRunning = true;

                Thread t = new Thread(AcceptClient);
                t.IsBackground = true;
                t.Start();

                LogToUI("Server đã bắt đầu lắng nghe trên cổng " + port);
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
                    TcpClient client = _server.AcceptTcpClient();

                    // Lấy IP để log tạm
                    string ip = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                    LogToUI($"Client {ip} đã kết nối TCP (Đang chờ đăng nhập...)");

                    Thread t = new Thread(() => HandleConnectedClient(client));
                    t.IsBackground = true;
                    t.Start();
                }
                catch { if (!_isRunning) break; }
            }
        }

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
                        if (packet != null)
                        {
                            ProcessPacket(packet, client, clientIP);
                        }
                        else break;
                    }
                }
            }
            catch (Exception ex)
            {
                LogToUI($"Lỗi với {clientIP}: {ex.Message}");
            }
            finally
            {
                _connectionGuard.RemoveClient(client);
                client.Close();
                LogToUI($"Client ({clientIP}) đã thoát.");
            }
        }

        private void ProcessPacket(Packet packet, TcpClient client, string ip)
        {
            switch (packet.Type)
            {
                case CommandType.Login:
                    HandleLogin(packet, client);
                    break;
                case CommandType.Register:
                    HandleRegister(packet, client);
                    break;
                case CommandType.Chat:
                    HandleChatRequest(packet, client, ip);
                    break;
                case CommandType.FileTransfer:
                    HandleFileTransfer(packet, client, ip);
                    break;
                case CommandType.Disconnect:
                    client.Close();
                    break;
                case CommandType.InputControl:
                    HandleInputControl(packet, client);
                    break;
                default:
                    LogToUI($"Nhận loại gói tin không xác định: {packet.Type}");
                    break;
            }
        }

        // --- ĐOẠN 1 ĐÃ SỬA: XỬ LÝ ĐIỀU KHIỂN (CÓ LOG CLICK CHUỘT) ---
        private void HandleInputControl(Packet packet, TcpClient sender)
        {
            // Kiểm tra xem có phải là người đang nắm quyền điều khiển không
            if (!_connectionGuard.IsController(sender)) return;

            var input = DataHelper.Deserialize<InputDTO>(packet.Data);
            if (input == null) return;

            string clientIP = ((IPEndPoint)sender.Client.RemoteEndPoint).Address.ToString();

            if (input.Type == 0) // Chuột
            {
                // Action = 0 (Move) -> Không ghi log để tránh lag
                // Action > 0 (Click Left/Right) -> Ghi log
                if (input.Action > 0)
                {
                    LogToUI($"[{clientIP}] đã Click chuột (Action: {input.Action}).");
                }

                int sw = Screen.PrimaryScreen.Bounds.Width;
                int sh = Screen.PrimaryScreen.Bounds.Height;
                int realX = (input.X * sw) / 1000;
                int realY = (input.Y * sh) / 1000;

                MouseHelper.SetCursorPos(realX, realY);
                if (input.Action > 0) MouseHelper.SimulateMouseEvent(input.Action);
            }
            else if (input.Type == 1) // Phím
            {
                LogToUI($"[{clientIP}] đã nhấn phím (Mã: {input.KeyCode}).");
                KeyboardHelper.SimulateKeyPress(input.KeyCode);
            }
        }
        // -----------------------------------------------------------

        // --- ĐOẠN 2 ĐÃ SỬA: XỬ LÝ ĐĂNG NHẬP (CÓ ĐẾM SỐ LƯỢNG) ---
        private void HandleLogin(Packet packet, TcpClient client)
        {
            var loginInfo = DataHelper.Deserialize<LoginDTO>(packet.Data);
            if (loginInfo == null) return;
            bool isValid = _dbManager.ValidateUser(loginInfo.Username, loginInfo.Password);

            Packet response = new Packet
            {
                Type = CommandType.Login,
                Data = Encoding.UTF8.GetBytes(isValid ? "SUCCESS" : "FAIL")
            };

            NetworkHelper.SendSecurePacket(client.GetStream(), response);

            if (isValid)
            {
                // Chỉ khi đăng nhập thành công mới thêm vào danh sách quản lý
                _connectionGuard.AddClient(client);

                // Đếm số lượng người đang kết nối
                int count = _connectionGuard.GetConnectedClients().Count;

                LogToUI($"Người dùng '{loginInfo.Username}' đã đăng nhập thành công. (Tổng online: {count})");

                OnClientConnected?.Invoke(client);
            }
            else
            {
                LogToUI($"Đăng nhập thất bại: Tài khoản '{loginInfo.Username}' sai hoặc chưa được duyệt.");
            }
        }
        // --------------------------------------------------------

        private void HandleRegister(Packet packet, TcpClient client)
        {
            LoginDTO regInfo = DataHelper.Deserialize<LoginDTO>(packet.Data);
            bool isRegistered = _dbManager.RegisterUser(regInfo.Username, regInfo.Password);
            string responseMsg = isRegistered ? "REGISTER_PENDING" : "REGISTER_FAILED";
            Packet responsePacket = new Packet
            {
                Type = CommandType.Register,
                Data = Encoding.UTF8.GetBytes(responseMsg)
            };
            NetworkHelper.SendSecurePacket(client.GetStream(), responsePacket);
        }

        private void HandleChatRequest(Packet packet, TcpClient client, string ip)
        {
            string rawMsg = Encoding.UTF8.GetString(packet.Data);
            OnChatReceived?.Invoke(client, rawMsg);

            // Broadcast tin nhắn cho mọi người
            string broadcastContent = $"[{ip}]: {rawMsg}";
            var broadcastPacket = new Packet
            {
                Type = CommandType.Chat,
                Data = Encoding.UTF8.GetBytes(broadcastContent)
            };
            BroadcastPacket(broadcastPacket);
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

                string broadcastContent = $"[{ip}] đã gửi file: {fileDto.FileName}";
                var broadcastPacket = new Packet { Type = CommandType.Chat, Data = Encoding.UTF8.GetBytes(broadcastContent) };

                BroadcastPacket(broadcastPacket); // Báo tin nhắn
                BroadcastPacket(packet);          // Gửi file thực tế
            }
        }

        public void CheckDatabaseConnection()
        {
            try { _dbManager.InitializeDatabase(); LogToUI("Kết nối Database thành công."); }
            catch (Exception ex) { LogToUI("Lỗi DB: " + ex.Message); }
        }

        public void BroadcastPacket(Packet packet)
        {
            var clients = _connectionGuard.GetConnectedClients();
            foreach (var client in clients)
            {
                try
                {
                    if (client != null && client.Connected)
                        NetworkHelper.SendSecurePacket(client.GetStream(), packet);
                }
                catch { }
            }
        }

        public void SendSecurePacket(NetworkStream stream, Packet packet)
        {
            NetworkHelper.SendSecurePacket(stream, packet);
        }

        public void LogToUI(string message)
        {
            // Bắn sự kiện ra ngoài (giữ nguyên logic cũ)
            OnLogAdded?.Invoke(message);

            if (_logView.InvokeRequired)
            {
                _logView.Invoke(new Action(() => LogToUI(message)));
            }
            else
            {
                try
                {
                    // --- LOGIC MỚI: PHÂN TÍCH TIN NHẮN ĐỂ TÔ MÀU VÀ CHIA CỘT ---
                    string source = "SYSTEM";
                    string content = message;
                    Color textColor = Color.Red; // Mặc định là đỏ (Hệ thống)

                    // Kiểm tra xem tin nhắn có định dạng "[IP] Nội dung" hay không
                    // Ví dụ: "[127.0.0.1] đã Click chuột"
                    if (message.StartsWith("[") && message.Contains("]"))
                    {
                        int closeBracketIndex = message.IndexOf("]");
                        // Lấy IP từ trong ngoặc [ ... ]
                        source = message.Substring(1, closeBracketIndex - 1);
                        // Lấy nội dung phía sau
                        content = message.Substring(closeBracketIndex + 1).Trim();

                        // Nếu là IP Client thì chuyển màu Xanh
                        textColor = Color.Blue;
                    }

                    // Tạo dòng log với 3 cột: Thời gian - Nguồn - Hành động
                    ListViewItem item = new ListViewItem(new[] {
                        DateTime.Now.ToString("HH:mm:ss"),
                        source,
                        content
                    });

                    item.ForeColor = textColor; // Áp dụng màu sắc

                    _logView.Items.Add(item);
                    item.EnsureVisible();
                    // -----------------------------------------------------------
                }
                catch { }
            }
        }

        public void Stop()
        {
            var disconnectPacket = new Packet { Type = CommandType.Disconnect, Data = Encoding.UTF8.GetBytes("Server Stop") };
            BroadcastPacket(disconnectPacket);
            Thread.Sleep(500);
            _isRunning = false;
            _server?.Stop();
            _connectionGuard.ClearAll();
        }
    }
}