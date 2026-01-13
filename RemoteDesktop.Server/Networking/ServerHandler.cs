using RemoteDesktop.Common.DTOs;
using RemoteDesktop.Common.Helpers;
using RemoteDesktop.Common.Models;
using RemoteDesktop.Common.Security;
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
        private Database.DatabaseManager _db = new Database.DatabaseManager();

        // Danh sách quản lý tất cả Client đang kết nối
        private readonly List<TcpClient> _connectedClients = new List<TcpClient>();
        private readonly object _clientLock = new object();

        // Events để thông báo cho Giao diện Server
        public delegate void ChatReceivedHandler(TcpClient sender, string message);
        public event ChatReceivedHandler OnChatReceived;

        public delegate void ClientConnectedHandler(TcpClient client);
        public event ClientConnectedHandler OnClientConnected;

        public delegate void FileReceivedHandler(TcpClient sender, byte[] data);
        public event FileReceivedHandler OnFileReceived;

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

                    // Thêm client vào danh sách quản lý chung
                    lock (_clientLock)
                    {
                        _connectedClients.Add(client);
                    }

                    OnClientConnected?.Invoke(client);

                    Thread t = new Thread(() => HandleConnectedClient(client));
                    t.IsBackground = true;
                    t.Start();
                }
                catch
                {
                    if (!_isRunning) break;
                }
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
                // QUAN TRỌNG: Phải xóa khỏi danh sách khi ngắt kết nối
                lock (_clientLock)
                {
                    _connectedClients.Remove(client);
                }
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
                    HandleInputControl(packet);
                    break;
                


                default:
                    LogToUI($"Nhận loại gói tin không xác định: {packet.Type}");
                    break;
            }
        }

        // --- CÁC HÀM XỬ LÝ LOGIC CHI TIẾT ---

        private void HandleInputControl(Packet packet)
        {
            var input = DataHelper.Deserialize<InputDTO>(packet.Data);
            if (input == null) return;

            // Lấy độ phân giải thực của màn hình Server hiện tại
            int sw = Screen.PrimaryScreen.Bounds.Width;
            int sh = Screen.PrimaryScreen.Bounds.Height;

            // Tính toán tọa độ thực tế
            int realX = (input.X * sw) / 1000;
            int realY = (input.Y * sh) / 1000;

            // Di chuyển chuột
            MouseHelper.SetCursorPos(realX, realY);

            // Click chuột nếu có action
            if (input.Action > 0)
            {
                MouseHelper.SimulateMouseEvent(input.Action);
            }
        }


        private void HandleLogin(Packet packet, TcpClient client)
        {
            var loginInfo = DataHelper.Deserialize<LoginDTO>(packet.Data);
            if (loginInfo == null) return;

            bool isValid = _db.ValidateUser(loginInfo.Username, loginInfo.Password);
            Packet response = new Packet
            {
                Type = CommandType.Login,
                Data = Encoding.UTF8.GetBytes(isValid ? "SUCCESS" : "FAIL")
            };

            NetworkHelper.SendSecurePacket(client.GetStream(), response);
            LogToUI($"Xác thực '{loginInfo.Username}': {(isValid ? "Thành công" : "Thất bại")}");
        }

        private void HandleChatRequest(Packet packet, TcpClient client, string ip)
        {
            // 1. Giải mã nội dung tin nhắn từ Client gửi lên
            string rawMsg = Encoding.UTF8.GetString(packet.Data);

            // 2. Gửi sự kiện để hiện lên giao diện Server
            OnChatReceived?.Invoke(client, rawMsg);

            // 3. BROADCAST: Gửi tin nhắn này cho TẤT CẢ các Client đang online
            string broadcastContent = $"[{ip}]: {rawMsg}";
            var broadcastPacket = new Packet
            {
                Type = CommandType.Chat,
                Data = Encoding.UTF8.GetBytes(broadcastContent)
            };

            BroadcastPacket(broadcastPacket);
            LogToUI($"Broadcast tin nhắn từ {ip}");
        }

        private void HandleFileTransfer(Packet packet, TcpClient client, string ip)
        {
            var fileDto = DataHelper.Deserialize<FilePacketDTO>(packet.Data);
            if (fileDto != null)
            {
                // 1. Thông báo cho giao diện Server (Sự kiện OnFileReceived)
                OnFileReceived?.Invoke(client, packet.Data);

                // 2. Lưu bản backup tại Server (Tùy chọn)
                string storagePath = Path.Combine(Application.StartupPath, "ReceivedFiles", fileDto.FileName);
                Directory.CreateDirectory(Path.GetDirectoryName(storagePath));
                File.WriteAllBytes(storagePath, fileDto.Buffer);

                LogToUI($"Đã nhận file '{fileDto.FileName}' từ {ip}");

                // 3. BROADCAST thông báo chat: [IP] đã gửi file: [Tên file]
                string broadcastContent = $"[{ip}] đã gửi file: {fileDto.FileName}";
                var broadcastPacket = new Packet
                {
                    Type = CommandType.Chat,
                    Data = Encoding.UTF8.GetBytes(broadcastContent)
                };

                BroadcastPacket(broadcastPacket); // Gửi cho tất cả Client để đồng bộ khung chat

                // 4. Gửi chính gói tin File này cho các Client khác nếu cần
                BroadcastPacket(packet);
            }
        }

        // --- CƠ CHẾ GỬI DỮ LIỆU ---

        public void BroadcastPacket(Packet packet)
        {
            int count = 0;
            // Chỉ cần 1 lần lock danh sách để đảm bảo an toàn dữ liệu
            lock (_clientLock)
            {
                // Duyệt ngược từ cuối danh sách lên đầu
                // Cách này cho phép xóa Client khỏi list ngay khi gặp lỗi mà không làm hỏng vòng lặp
                for (int i = _connectedClients.Count - 1; i >= 0; i--)
                {
                    var client = _connectedClients[i];
                    try
                    {
                        if (client != null && client.Connected)
                        {
                            var stream = client.GetStream();
                            NetworkHelper.SendSecurePacket(client.GetStream(), packet);
                            count++;
                        }
                        else
                        {
                            // Client đã ngắt kết nối âm thầm, dọn dẹp khỏi danh sách
                            _connectedClients.RemoveAt(i);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[DEBUG SERVER] Lỗi gửi cho 1 client: {ex.Message}");
                        // Nếu gửi lỗi (mạng đứt đột ngột), xóa client này đi
                        _connectedClients.RemoveAt(i);
                    }
                }
            }
            Console.WriteLine($"[DEBUG SERVER] Broadcast hoàn tất. Đã gửi thành công tới {count} máy khách.");
        }

        public void SendSecurePacket(NetworkStream stream, Packet packet)
        {
            // Gọi đến Helper dùng chung trong project Common
            NetworkHelper.SendSecurePacket(stream, packet);
        }

        private void LogToUI(string message)
        {
            if (_logView.InvokeRequired)
            {
                _logView.Invoke(new Action(() => LogToUI(message)));
            }
            else
            {
                _logView.Items.Add(new ListViewItem(new[] { DateTime.Now.ToString("HH:mm:ss"), message }));
                _logView.Items[_logView.Items.Count - 1].EnsureVisible();
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _server?.Stop();
            lock (_clientLock)
            {
                foreach (var client in _connectedClients) client.Close();
                _connectedClients.Clear();
            }
        }
    }
}