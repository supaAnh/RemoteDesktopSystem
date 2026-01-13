using RemoteDesktop.Common.DTOs;
using RemoteDesktop.Common.Helpers;
using RemoteDesktop.Common.Models;
using RemoteDesktop.Common.Security;
using System;
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

        // Tham số 1: Client gửi, Tham số 2: Nội dung tin nhắn
        public delegate void ChatReceivedHandler(TcpClient sender, string message);
        // 2. Khai báo event dựa trên delegate trên
        public event ChatReceivedHandler OnChatReceived;

        public delegate void ClientConnectedHandler(TcpClient client);
        public event ClientConnectedHandler OnClientConnected;

        // Tham số 1: Client gửi, Tham số 2: Mảng byte dữ liệu file
        public delegate void FileReceivedHandler(TcpClient sender, byte[] data);

        // 2. Khai báo event dựa trên delegate
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
                TcpClient client = _server.AcceptTcpClient();
                // Khi có client kết nối, kích hoạt sự kiện
                OnClientConnected?.Invoke(client);

                Thread t = new Thread(() => HandleConnectedClient(client));
                t.IsBackground = true;
                t.Start();
            }
        }

        private void HandleConnectedClient(TcpClient client)
        {
            // Lấy thông tin IP của Client
            string clientIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            LogToUI($"Client ({clientIP}) đã kết nối.");

            try
            {
                using (NetworkStream stream = client.GetStream())
                {
                    // Vòng lặp liên tục nhận dữ liệu chừng nào server còn chạy và client còn kết nối
                    while (_isRunning && client.Connected)
                    {
                        try
                        {
                            // SỬ DỤNG NetworkHelper để nhận gói tin an toàn (có xử lý Header 4 byte và AES)
                            var packet = NetworkHelper.ReceiveSecurePacket(stream);

                            if (packet != null)
                            {
                                // Truyền thêm đối tượng 'client' để ProcessPacket có thể gửi phản hồi ngược lại
                                ProcessPacket(packet, client, clientIP);
                            }
                            else
                            {
                                // Nếu nhận packet null, có thể kết nối đã có vấn đề
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log lỗi nếu cần thiết và thoát vòng lặp
                            LogToUI($"Lỗi khi nhận dữ liệu từ {clientIP}: {ex.Message}");
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogToUI($"Lỗi luồng stream với {clientIP}: {ex.Message}");
            }
            finally
            {
                // Đảm bảo đóng kết nối và giải phóng tài nguyên
                client.Close();
                LogToUI($"Client ({clientIP}) đã ngắt kết nối.");
            }
        }

        private void ProcessPacket(Packet packet, TcpClient client, string ip)
        {
            // Log lệnh nhận được lên giao diện để theo dõi
            LogToUI($"Nhận lệnh: {packet.Type} từ {ip}");

            switch (packet.Type)
            {
                case CommandType.Login:
                    // 1. Giải mã DTO từ Client gửi lên
                    var loginInfo = DataHelper.Deserialize<LoginDTO>(packet.Data);
                    if (loginInfo == null) return;

                    // 2. Kiểm tra trong CSDL (Sử dụng DatabaseManager đã tạo trước đó)
                    bool isValid = _db.ValidateUser(loginInfo.Username, loginInfo.Password);

                    // 3. Gửi phản hồi lại cho Client (sử dụng NetworkHelper để đảm bảo an toàn/mã hóa)
                    Packet loginResponse = new Packet
                    {
                        Type = CommandType.Login,
                        Data = Encoding.UTF8.GetBytes(isValid ? "SUCCESS" : "FAIL")
                    };

                    // Gọi hàm gửi mà bạn đã viết (Nhớ dùng NetworkHelper.SendSecurePacket)
                    NetworkHelper.SendSecurePacket(client.GetStream(), loginResponse);

                    LogToUI($"Xác thực tài khoản '{loginInfo.Username}': {(isValid ? "Thành công" : "Thất bại")}");
                    break;

                case CommandType.Chat:
                    // Giải mã chuỗi tin nhắn từ mảng byte
                    string message = System.Text.Encoding.UTF8.GetString(packet.Data);

                    // Kích hoạt event để báo cho giao diện (frmRemote) biết
                    // Sử dụng ?.Invoke để tránh lỗi nếu chưa có ai đăng ký sự kiện
                    OnChatReceived?.Invoke(client, message);

                    break;

                case CommandType.FileTransfer:
                    // 1. Giải mã dữ liệu nhận được
                    var fileDto = DataHelper.Deserialize<FilePacketDTO>(packet.Data);

                    if (fileDto != null)
                    {
                        // 2. Kích hoạt sự kiện để báo cho frmRemote (Server) biết có file về
                        // Bạn cần đảm bảo đã khai báo OnFileReceived trong ServerHandler như hướng dẫn trước
                        OnFileReceived?.Invoke(client, packet.Data);

                        // 3. Vẫn có thể lưu một bản backup tại Server nếu muốn
                        string storagePath = Path.Combine(Application.StartupPath, "ReceivedFiles", fileDto.FileName);
                        Directory.CreateDirectory(Path.GetDirectoryName(storagePath));
                        File.WriteAllBytes(storagePath, fileDto.Buffer);    

                        LogToUI($"Đã nhận file '{fileDto.FileName}' từ {ip}");
                    }
                    break;

                case CommandType.MouseEvent:
                    // Sẽ xử lý trong phần Remote Desktop (sử dụng InputSimulator)
                    // Decode tọa độ và thực hiện mô phỏng chuột
                    break;

                case CommandType.KeyboardEvent:
                    // Giải mã phím và mô phỏng gõ phím trên Server
                    break;

                case CommandType.Disconnect:
                    LogToUI($"Client {ip} yêu cầu ngắt kết nối.");
                    client.Close();
                    break;

                default:
                    LogToUI($"Cảnh báo: Nhận loại gói tin không xác định ({packet.Type})");
                    break;
            }
        }

        private void LogToUI(string message)
        {
            // Vì Thread chạy ngầm không được can thiệp trực tiếp vào UI, cần dùng Invoke
            if (_logView.InvokeRequired)
            {
                _logView.Invoke(new Action(() => LogToUI(message)));
            }
            else
            {
                _logView.Items.Add(new ListViewItem(new[] { DateTime.Now.ToString("HH:mm:ss"), message }));
            }
        }

        public void SendPacketToClient(TcpClient client, Packet packet)
        {
            try
            {
                if (client != null && client.Connected)
                {
                    NetworkStream stream = client.GetStream();

                    // 1. Serialize đối tượng thành byte[]
                    byte[] rawData = DataHelper.Serialize(packet);

                    // 2. Mã hóa AES trước khi gửi (Như yêu cầu bảo mật của bạn)
                    byte[] encryptedData = AESEncryption.Encrypt(rawData); // Mã hóa dữ liệu

                    // 3. Gửi đi
                    stream.Write(encryptedData, 0, encryptedData.Length);
                    stream.Flush();
                }
            }
            catch (Exception ex)
            {
                LogToUI("Lỗi gửi dữ liệu tới Client: " + ex.Message);
            }
        }

        public void SendSecurePacket(NetworkStream stream, Packet packet)
        {
            // Gọi lại hàm từ NetworkHelper
            NetworkHelper.SendSecurePacket(stream, packet);
        }
        public void Stop()
        {
            _isRunning = false;
            _server?.Stop();
        }
    }
}