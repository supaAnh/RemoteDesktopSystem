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

        // Thêm dấu ? để tránh lỗi null
        public event Action<string>? OnLogAdded;
        public delegate void ChatReceivedHandler(TcpClient sender, string message);
        public event ChatReceivedHandler? OnChatReceived;
        public delegate void ClientConnectedHandler(TcpClient client);
        public event ClientConnectedHandler? OnClientConnected;
        public delegate void FileReceivedHandler(TcpClient sender, byte[] data);
        public event FileReceivedHandler? OnFileReceived;

        private Database.DatabaseManager _dbManager = new Database.DatabaseManager();

        // --- SỬ DỤNG CONNECTION GUARD ---
        private ConnectionGuard _connectionGuard = new ConnectionGuard();
        // --------------------------------

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

                    // --- [SỬA ĐỔI QUAN TRỌNG 1] ---
                    // KHÔNG thêm vào ConnectionGuard ngay lập tức.
                    // Lý do: Nếu thêm ngay, Server sẽ gửi stream ảnh cho Client này
                    // trong khi Client đang chờ phản hồi Login -> Gây lỗi "Invalid Response".
                    // _connectionGuard.AddClient(client);  <-- ĐÃ XÓA/COMMENT
                    // ------------------------------

                    // Lấy IP để log
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
                // Xóa khỏi danh sách khi thoát
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
                    // --- QUAN TRỌNG: Truyền client vào để kiểm tra quyền ---
                    HandleInputControl(packet, client);
                    break;

                default:
                    LogToUI($"Nhận loại gói tin không xác định: {packet.Type}");
                    break;
            }
        }

        // --- HÀM XỬ LÝ ĐIỀU KHIỂN (CHUỘT/PHÍM) ---
        private void HandleInputControl(Packet packet, TcpClient sender)
        {
            // 1. CHỐT CHẶN: Kiểm tra xem người gửi có phải là "Trùm" không?
            if (!_connectionGuard.IsController(sender))
            {
                // Nếu không phải Trùm -> Bỏ qua lệnh này (không làm gì cả)
                return;
            }

            // 2. Nếu là Trùm -> Thực hiện lệnh như bình thường
            var input = DataHelper.Deserialize<InputDTO>(packet.Data);
            if (input == null) return;

            if (input.Type == 0) // Chuột
            {
                int sw = Screen.PrimaryScreen.Bounds.Width;
                int sh = Screen.PrimaryScreen.Bounds.Height;
                int realX = (input.X * sw) / 1000;
                int realY = (input.Y * sh) / 1000;
                MouseHelper.SetCursorPos(realX, realY);
                if (input.Action > 0) MouseHelper.SimulateMouseEvent(input.Action);
            }
            else if (input.Type == 1) // Phím
            {
                KeyboardHelper.SimulateKeyPress(input.KeyCode);
            }
        }

        // --- CÁC HÀM KHÁC ---
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

            // Gửi phản hồi ngay lập tức
            NetworkHelper.SendSecurePacket(client.GetStream(), response);

            if (isValid)
            {
                LogToUI($"Người dùng '{loginInfo.Username}' đăng nhập thành công.");

                // --- [SỬA ĐỔI QUAN TRỌNG 2] ---
                // Chỉ khi đăng nhập thành công mới thêm vào danh sách nhận Stream
                _connectionGuard.AddClient(client);
                // ------------------------------

                OnClientConnected?.Invoke(client);
            }
            else LogToUI($"Đăng nhập thất bại: Tài khoản '{loginInfo.Username}' sai.");
        }

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
                BroadcastPacket(broadcastPacket);
                BroadcastPacket(packet);
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
            // --- [SỬA ĐỔI 3] ---
            // Bắn sự kiện này ra ngoài để frmRemote (form chính) có thể bắt được và hiển thị
            OnLogAdded?.Invoke(message);
            // -------------------

            if (_logView.InvokeRequired)
            {
                _logView.Invoke(new Action(() => LogToUI(message)));
            }
            else
            {
                try
                {
                    ListViewItem item = new ListViewItem(new[] { DateTime.Now.ToString("HH:mm:ss"), message });
                    _logView.Items.Add(item);
                    item.EnsureVisible();
                }
                catch { } // Bỏ qua lỗi nếu form cũ đã đóng hoặc bị dispose
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