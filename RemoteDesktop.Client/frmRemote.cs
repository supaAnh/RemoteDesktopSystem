using RemoteDesktop.Client.Networking;
using RemoteDesktop.Common.DTOs;
using RemoteDesktop.Common.Helpers;
using RemoteDesktop.Common.Models;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

// Dùng alias để tránh lỗi Ambiguous reference (CS0104)
using MyCommand = RemoteDesktop.Common.Models.CommandType;


namespace RemoteDesktop.Client
{
    public partial class frmRemote : Form
    {
        // Biến toàn cục trong Form
        private ClientHandler _client;



        public frmRemote(ClientHandler client)
        {
            InitializeComponent();
            this._client = client;

            if (this._client == null || !this._client.IsConnected)
            {
                MessageBox.Show("CẢNH BÁO: Đối tượng kết nối bị null hoặc chưa kết nối!");
            }
        }
        //
        //
        //REMOTE SERVER
        //
        //
        // Gửi tọa độ chuột khi di chuyển trên PictureBox
        private void picScreen_MouseMove(object sender, MouseEventArgs e)
        {
            SendInput(0, 0, e.X, e.Y, 0); // Type 0: Mouse, Action 0: Move
        }

        // Gửi lệnh nhấn chuột trái
        private void picScreen_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                SendInput(0, 1, e.X, e.Y, 0); // Action 1: LeftDown
            }
        }

        // Gửi lệnh thả chuột trái
        private void picScreen_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                SendInput(0, 2, e.X, e.Y, 0);
            }
        }

        // Gửi lệnh nhấn phím
        private void frmRemote_KeyDown(object sender, KeyEventArgs e)
        {
            // Gửi mã KeyValue của phím sang Server
            SendInput(1, 3, 0, 0, e.KeyValue);
        }

        // Gửi lệnh thả phím
        private void SendInput(int type, int action, int x, int y, int keyCode)
        {
            // Tính toán tỷ lệ dựa trên kích thước thực tế của picScreen
            float percentX = (float)x / picScreen.Width;
            float percentY = (float)y / picScreen.Height;

            var input = new InputDTO
            {
                Type = type,
                Action = action,
                // Nhân với 1000 để gửi tỷ lệ phần nghìn (tránh mất dữ liệu khi ép kiểu int)
                X = (int)(percentX * 1000),
                Y = (int)(percentY * 1000),
                KeyCode = keyCode
            };

            var packet = new Packet
            {
                Type = RemoteDesktop.Common.Models.CommandType.InputControl,
                Data = DataHelper.Serialize(input)
            };

            // Gửi gói tin qua ClientHandler
            if (_client != null && _client.IsConnected)
            {
                _client.SendPacket(packet);
            }
        }



        private void frmRemote_Load(object sender, EventArgs e)
        {
            // Đảm bảo luồng chỉ bắt đầu khi giao diện đã hiện lên
            Thread t = new Thread(ReceiveLoop);
            t.IsBackground = true;
            t.Start();
        }

        // nhận dữ liệu từ Server
        private void ReceiveLoop()
        {
            while (_client != null && _client.IsConnected)
            {
                try
                {
                    // Khai báo rõ ràng biến stream cho mỗi lần lặp nhận gói tin
                    var currentStream = _client.GetStream();
                    var packet = NetworkHelper.ReceiveSecurePacket(currentStream);

                    if (packet != null)
                    {
                        this.Invoke(new Action(() =>
                        {
                            if (packet.Type == MyCommand.Chat)
                            {
                                string msg = Encoding.UTF8.GetString(packet.Data);
                                AppendChatHistory(msg); // Hiển thị tin nhắn từ Server
                            }
                            else if (packet.Type == MyCommand.FileTransfer)
                            {
                                HandleIncomingFile(packet.Data);
                            }
                            else if (packet.Type == MyCommand.ScreenUpdate)
                            {
                                UpdateScreen(packet.Data);
                            }
                            else if (packet.Type == MyCommand.Disconnect)
                            {
                                HandleServerDisconnect();
                            }
                        }));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Lỗi luồng nhận: " + ex.Message);
                    break;
                }
            }
        }

        private void AppendChatHistory(string message)
        {
            if (txtChatHistory.InvokeRequired)
            {
                txtChatHistory.Invoke(new Action(() => AppendChatHistory(message)));
            }
            else
            {
                // Sử dụng AppendText giúp tự động cuộn xuống cuối
                txtChatHistory.AppendText(message + Environment.NewLine);

                // Buộc UI vẽ lại để tránh hiện tượng trắng màn hình
                txtChatHistory.Refresh();
            }
        }
        // Xử lý khi server ngắt kết nối
        private void HandleServerDisconnect()
        {
            // Hiển thị thông báo xác nhận
            DialogResult result = MessageBox.Show(
                "Server đã ngắt kết nối hoặc ngừng hoạt động. Bạn có muốn thoát không?",
                "Thông báo",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (result == DialogResult.Yes)
            {
                // Thực hiện ngắt kết nối và quay về màn hình chính
                if (_client != null) _client.Disconnect();

                frmConnect connectForm = new frmConnect();
                connectForm.Show();
                this.Close();
            }
        }

        private void HandleIncomingFile(byte[] rawData)
        {
            try
            {
                var fileDto = DataHelper.Deserialize<FilePacketDTO>(rawData);
                if (fileDto != null)
                {
                    // Đường dẫn tới thư mục Downloads
                    string downloadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", fileDto.FileName);

                    File.WriteAllBytes(downloadPath, fileDto.Buffer);

                    AppendChatHistory($"[Hệ thống]: Đã nhận file '{fileDto.FileName}' thành công.");
                    MessageBox.Show($"File đã được tải về: {fileDto.FileName}", "Thông báo");
                    Process.Start("explorer.exe", Path.GetDirectoryName(downloadPath));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi lưu file: " + ex.Message);
            }
        }

        private void UpdateScreen(byte[] data)
        {
            if (picScreen.InvokeRequired)
            {
                picScreen.Invoke(new Action(() => UpdateScreen(data)));
            }
            else
            {
                try
                {
                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        // Xóa ảnh cũ để giải phóng bộ nhớ trước khi gán ảnh mới
                        if (picScreen.Image != null) picScreen.Image.Dispose();
                        picScreen.Image = Image.FromStream(ms);
                    }
                }
                catch { /* Xử lý lỗi giải mã ảnh nếu cần */ }
            }
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            try
            {
                // 1. Gọi hàm ngắt kết nối trong ClientHandler để đóng Stream và TcpClient
                if (_client != null)
                {
                    _client.Disconnect();
                }

                // 2. Hiển thị lại Form kết nối ban đầu
                frmConnect connectForm = new frmConnect();
                connectForm.Show();

                // 3. Đóng Form điều khiển hiện tại
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi ngắt kết nối: " + ex.Message);
            }
        }

        private void btnSendChat_Click(object sender, EventArgs e)
        {
            string msg = txtChatInput.Text.Trim();
            if (string.IsNullOrEmpty(msg)) return;

            try
            {
                var packet = new Packet
                {
                    Type = MyCommand.Chat,
                    Data = Encoding.UTF8.GetBytes(msg)
                };

                // Lấy stream nhưng TUYỆT ĐỐI KHÔNG dùng using ở đây
                var stream = _client.GetStream();

                if (stream != null)
                {
                    NetworkHelper.SendSecurePacket(stream, packet);
                    txtChatInput.Clear();
                    // Không gọi AppendChatHistory ở đây, đợi Server phản hồi
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi gửi tin nhắn: " + ex.Message);
            }
        }

        private void btnSendFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var fileDto = new FilePacketDTO
                        {
                            FileName = Path.GetFileName(ofd.FileName),
                            Buffer = File.ReadAllBytes(ofd.FileName)
                        };

                        var packet = new Packet
                        {
                            Type = MyCommand.FileTransfer,
                            Data = DataHelper.Serialize(fileDto)
                        };

                        _client.SendPacket(packet); // Gửi tới Server
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi khi gửi file: " + ex.Message);
                    }
                }
            }
        }

    }
}