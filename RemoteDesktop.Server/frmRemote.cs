using RemoteDesktop.Common.DTOs;
using RemoteDesktop.Common.Helpers;
using RemoteDesktop.Common.Models;
using RemoteDesktop.Server.Networking;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

using CommandType = RemoteDesktop.Common.Models.CommandType;


namespace RemoteDesktop.Server
{
    public partial class frmRemote : Form
    {
        private ServerHandler _server;
        private TcpClient _targetClient; // Lưu trữ client đang kết nối


        private bool _isStreaming = false;
        private void frmRemote_Load(object sender, EventArgs e)
        {
            // Gọi hàm StartStreaming khi Form bắt đầu hiển thị
            StartStreaming();
        }
        //Stream MÀN HÌNH
        private void StartStreaming()
        {
            _isStreaming = true;
            Thread screenThread = new Thread(() =>
            {
                while (_isStreaming && _targetClient.Connected)
                {
                    try
                    {
                        // 1. Chụp màn hình
                        byte[] screenData = RemoteDesktop.Server.Services.ScreenCapturer.CaptureDesktop();

                        // 2. Tạo gói tin ScreenUpdate
                        var packet = new Packet
                        {
                            Type = RemoteDesktop.Common.Models.CommandType.ScreenUpdate,
                            Data = screenData
                        };

                        // 3. Gửi cho Client đang kết nối
                        _server.SendSecurePacket(_targetClient.GetStream(), packet);

                        // Đợi một khoảng ngắn (VD: 100ms ~ 10 FPS) để tránh quá tải mạng
                        Thread.Sleep(100);
                    }
                    catch { break; }
                }
            });
            screenThread.IsBackground = true;
            screenThread.Start();
        }


        public frmRemote(ServerHandler server, TcpClient client)
        {
            InitializeComponent();
            this._server = server;
            this._targetClient = client; // Client mà bạn đang tập trung điều khiển

            // Đăng ký nhận Chat: Khi có bất kỳ ai nhắn, hiện lên khung chat Server
            this._server.OnChatReceived += (sender, message) => {
                // Lấy IP của người gửi để hiển thị cho rõ
                string senderIP = ((IPEndPoint)sender.Client.RemoteEndPoint).Address.ToString();
                AppendChatHistory($"[{senderIP}]: {message}");
            };

            // Đăng ký nhận File
            this._server.OnFileReceived += (sender, data) => {
                string senderIP = ((IPEndPoint)sender.Client.RemoteEndPoint).Address.ToString();
                var fileDto = DataHelper.Deserialize<FilePacketDTO>(data);

                if (fileDto != null)
                {
                    // Hiển thị lên khung chat của Server: [IP] đã gửi file: [Tên file]
                    AppendChatHistory($"[{senderIP}] đã gửi file: {fileDto.FileName}");

                    // Gọi hàm lưu file và mở thư mục như đã thảo luận trước đó
                    HandleIncomingFile(data);
                }
            };
        }

        


        private void btnSendChat_Click(object sender, EventArgs e)
        {
            string msg = txtChatInput.Text.Trim();
            if (string.IsNullOrEmpty(msg)) return;

            try
            {
                // Tạo gói tin Chat của Server
                var packet = new Packet
                {
                    Type = RemoteDesktop.Common.Models.CommandType.Chat,
                    Data = Encoding.UTF8.GetBytes($"[SERVER]: {msg}")
                };

                // Gửi cho TẤT CẢ các Client đang online
                _server.BroadcastPacket(packet);

                // Hiển thị nội dung vừa gửi lên khung chat của chính Server
                AppendChatHistory($"[SERVER]: {msg}");
                txtChatInput.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Server gửi lỗi: " + ex.Message);
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
                            Type = CommandType.FileTransfer,
                            Data = DataHelper.Serialize(fileDto)
                        };

                        // Server gửi cho tất cả Client
                        _server.BroadcastPacket(packet);
                        AppendChatHistory($"[Hệ thống]: Server đã gửi file {fileDto.FileName}");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi gửi file: {ex.Message}");
                    }
                }
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
                // Tự động cuộn xuống dòng cuối cùng
                txtChatHistory.SelectionStart = txtChatHistory.Text.Length;
                txtChatHistory.ScrollToCaret();
            }
        }

        private void HandleIncomingFile(byte[] rawData)
        {
            try
            {
                var fileDto = DataHelper.Deserialize<FilePacketDTO>(rawData);
                if (fileDto != null)
                {
                    // Lưu vào thư mục Downloads
                    string downloadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                    string filePath = Path.Combine(downloadPath, fileDto.FileName);

                    File.WriteAllBytes(filePath, fileDto.Buffer);

                    // Mở thư mục và bôi đậm file vừa nhận
                    Process.Start("explorer.exe", $"/select,\"{filePath}\"");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi lưu file tại Server: " + ex.Message);
            }
        }


        //STREAM MÀN HÌNH
        

        
    }
}
