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
        // KHAI BÁO TẠI ĐÂY: Biến toàn cục trong Form
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
                        this.Invoke(new Action(() => {
                            if (packet.Type == MyCommand.Chat && packet.Data != null)
                            {
                                string msg = Encoding.UTF8.GetString(packet.Data);
                                AppendChatHistory(msg);
                            }
                            else if (packet.Type == MyCommand.FileTransfer)
                            {
                                HandleIncomingFile(packet.Data);
                            }
                            else if (packet.Type == MyCommand.ScreenUpdate)
                            {
                                UpdateScreen(packet.Data);
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
        private void HandleIncomingFile(byte[] rawData)
        {
            try
            {
                var fileDto = DataHelper.Deserialize<RemoteDesktop.Common.DTOs.FilePacketDTO>(rawData);
                if (fileDto != null)
                {
                    // Lưu vào thư mục Downloads của máy Client
                    string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", fileDto.FileName);

                    File.WriteAllBytes(path, fileDto.Buffer);

                    AppendChatHistory($"[Hệ thống]: Đã nhận file '{fileDto.FileName}' và lưu tại thư mục Downloads.");
                    MessageBox.Show($"Bạn đã nhận được file: {fileDto.FileName}", "Thông báo");
                    path = Path.Combine(Application.StartupPath, "ReceivedFiles");
                    Process.Start("explorer.exe", path); // Thư mục sẽ tự bật lên khi file tải xong
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
                using (MemoryStream ms = new MemoryStream(data))
                {
                    picScreen.Image = Image.FromStream(ms);
                }
            }
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            frmConnect connectForm = new frmConnect();
            connectForm.Show();
            this.Close();
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
                        
                        // 1. Đóng gói dữ liệu file
                        var fileDto = new FilePacketDTO
                        {
                            FileName = Path.GetFileName(ofd.FileName),
                            Buffer = File.ReadAllBytes(ofd.FileName)
                        };

                        // 2. Tạo Packet
                        var packet = new Packet
                        {
                            Type = MyCommand.FileTransfer,
                            Data = DataHelper.Serialize(fileDto)
                        };

                        // 3. Gửi
                        _client.SendPacket(packet);

                        AppendChatHistory($"[Hệ thống]: Đang gửi file '{fileDto.FileName}'...");
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