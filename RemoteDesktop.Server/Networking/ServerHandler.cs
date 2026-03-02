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

        // Sự kiện khi có log mới, giúp Form Remote biết để hiển thị vào lịch sử log
        public event Action<string>? OnLogAdded;

        // Sự kiện khi nhận được tin nhắn chat từ client, giúp Form Remote biết để hiển thị vào lịch sử chat
        public delegate void ChatReceivedHandler(TcpClient sender, string message);
        public event ChatReceivedHandler? OnChatReceived;


        // Sự kiện khi có client mới kết nối thành công, giúp Form Remote biết để hiển thị thông tin và chuẩn bị cho việc điều khiển nếu cần
        public delegate void ClientConnectedHandler(TcpClient client);
        public event ClientConnectedHandler? OnClientConnected;

        // Sự kiện khi nhận được file từ client, giúp Form Remote biết để hiển thị thông báo và lưu vào thư mục tạm nếu cần
        public delegate void FileReceivedHandler(TcpClient sender, byte[] data);
        public event FileReceivedHandler? OnFileReceived;

        // Sự kiện khi client ngắt kết nối, giúp Form Remote biết để tự đóng lại nếu đang mở với client đó
        public delegate void ClientDisconnectedHandler(TcpClient client);
        public event ClientDisconnectedHandler? OnClientDisconnected;


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

                // Kích hoạt báo hiệu Client đã ngắt kết nối về cho Form
                OnClientDisconnected?.Invoke(client);
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

                // Truyền thêm tham số ip vào hàm HandleInputControl để ghi Log cho chính xác
                case CommandType.InputControl: HandleInputControl(packet, client, ip); break;

                // BẮT GÓI TIN VIDEO RECORD
                case CommandType.VideoRecord:
                    var videoDto = DataHelper.Deserialize<FilePacketDTO>(packet.Data);
                    if (videoDto != null)
                    {
                        // Gọi DB lưu mảng byte vào SQL
                        _dbManager.SaveVideoRecord("Session_Video", videoDto.FileName, videoDto.Buffer);
                        LogToUI($"[{ip}] Đã nhận và lưu Video Record: {videoDto.FileName}");
                    }
                    break;
            }
        }

        private void HandleLogin(Packet packet, TcpClient client)
        {
            var loginInfo = DataHelper.Deserialize<LoginDTO>(packet.Data);
            if (loginInfo == null) return;

            string username = loginInfo.Username;
            string password = loginInfo.Password;

            // 1. XÁC THỰC 100% BẰNG DATABASE
            // Gọi thẳng vào hàm ValidateUser, nó sẽ kiểm tra Username, Password và Status = 1
            bool isValidFormat = _dbManager.ValidateUser(username, password);

            // 2. Kiểm tra xem tài khoản này có đang Online hay không (chống đăng nhập trùng)
            bool isAlreadyOnline = _connectionGuard.IsUsernameOnline(username);

            // LOGIC: Đăng nhập đúng thông tin trong DB + Chưa online = CHO VÀO
            if (isValidFormat && !isAlreadyOnline)
            {
                _connectionGuard.AddClient(client, username);
                OnClientConnected?.Invoke(client);

                Packet response = new Packet { Type = CommandType.Login, Data = Encoding.UTF8.GetBytes("SUCCESS") };
                NetworkHelper.SendSecurePacket(client.GetStream(), response);
                LogToUI($"Client [{username}] đăng nhập thành công qua Database.");
            }
            else
            {
                // Phân loại lỗi để ghi Log hiển thị lên màn hình Server cho dễ quản lý
                string reason = "Lỗi không xác định";
                if (!isValidFormat)
                    reason = "Sai tài khoản, mật khẩu hoặc tài khoản chưa được duyệt (Status = 0)";
                else if (isAlreadyOnline)
                    reason = "Tài khoản đang được đăng nhập ở một nơi khác!";

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

        private void HandleInputControl(Packet packet, TcpClient sender, string ip)
        {
            // Chỉ cho phép điều khiển nếu đã đăng nhập (nằm trong Guard)
            if (!_connectionGuard.IsController(sender)) return;

            var input = DataHelper.Deserialize<InputDTO>(packet.Data);
            if (input == null) return;

            // --- THÊM PHẦN GHI LOG CLICK CHUỘT ---
            // Kiểm tra Type == 0 (Là chuột) và Action > 0 (Khác 0 là đang có hành động click)
            if (input.Type == 0 && input.Action > 0)
            {
                string actionDesc = "";
                switch (input.Action)
                {
                    case 1: actionDesc = "Left Click (Down)"; break;
                    case 2: actionDesc = "Left Click (Up)"; break;
                    case 3: actionDesc = "Right Click (Down)"; break;
                    case 4: actionDesc = "Right Click (Up)"; break;
                    default: actionDesc = $"Click Action {input.Action}"; break;
                }

                // Ghi Log lên UI. Form Remote của Server sẽ tự động tách IP ra và lưu vào DB kèm lsvLog
                // input.X và input.Y đang ở tỷ lệ phần nghìn, chia 10.0 để ra phần trăm (%)
                LogToUI($"[{ip}] Sự kiện chuột: {actionDesc} tại tọa độ ({input.X / 10.0}%, {input.Y / 10.0}%)");
            }

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


        // Đếm số Client đang online
        public int GetClientCount()
        {
            return _connectionGuard.GetConnectedClients().Count;
        }

        // Kích tất cả Client nhưng Server vẫn giữ Port lắng nghe
        public void DisconnectAllClients()
        {
            BroadcastPacket(new Packet { Type = CommandType.Disconnect, Data = Encoding.UTF8.GetBytes("Server Kick") });
            Thread.Sleep(500); // Đợi Client nhận được thông báo trước khi đóng
            _connectionGuard.ClearAll();
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