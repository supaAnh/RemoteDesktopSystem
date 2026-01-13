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

            // Khởi động việc lắng nghe dữ liệu từ Server
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
                    // Nhận gói tin từ Server
                    var packet = NetworkHelper.ReceiveSecurePacket(_client.GetStream());

                    if (packet != null)
                    {
                        // Thay vì dùng Event, ta dùng switch-case để phân loại dữ liệu
                        switch (packet.Type)
                        {
                            case MyCommand.Chat:
                                string msg = Encoding.UTF8.GetString(packet.Data);
                                // Cập nhật lên giao diện
                                AppendChatHistory($"[SERVER]: {msg}");
                                break;

                            case MyCommand.ScreenUpdate:
                                UpdateScreen(packet.Data);
                                break;

                            // Các trường hợp khác (FileTransfer)
                            case MyCommand.FileTransfer:
                               
                                HandleIncomingFile(packet.Data);
                                break;
                        }
                    }
                }
                catch { break; }
            }
        }

        private void AppendChatHistory(string text)
        {
            if (txtChatHistory.InvokeRequired)
            {
                txtChatHistory.Invoke(new Action(() => AppendChatHistory(text)));
            }
            else
            {
                txtChatHistory.AppendText(text + Environment.NewLine);
                txtChatHistory.ScrollToCaret(); // Tự động cuộn xuống tin mới nhất
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

            if (string.IsNullOrEmpty(txtChatInput.Text)) return;

            try
            {
                var packet = new Packet
                {
                    Type = MyCommand.Chat,
                    Data = Encoding.UTF8.GetBytes(txtChatInput.Text)
                };

                // Kiểm tra xem _client.GetStream() có bị null không
                var stream = _client.GetStream();
                NetworkHelper.SendSecurePacket(stream, packet);

                AppendChatHistory($"[CLIENT]: {txtChatInput.Text}");
                txtChatInput.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi gửi: " + ex.Message);
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