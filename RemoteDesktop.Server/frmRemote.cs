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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using CommandType = RemoteDesktop.Common.Models.CommandType;

namespace RemoteDesktop.Server
{
    public partial class frmRemote : Form
    {
        private ServerHandler _server;
        private TcpClient _targetClient; // Vẫn giữ biến này để tham khảo, nhưng không phụ thuộc hoàn toàn vào nó nữa

        private bool _isStreaming = false;

        public frmRemote(ServerHandler server, TcpClient client)
        {
            InitializeComponent();
            this._server = server;
            this._targetClient = client;

            // Đăng ký nhận Chat: Khi có bất kỳ ai nhắn, hiện lên khung chat Server
            this._server.OnChatReceived += (sender, message) =>
            {
                try
                {
                    // Lấy IP của người gửi để hiển thị cho rõ
                    string senderIP = ((IPEndPoint)sender.Client.RemoteEndPoint).Address.ToString();
                    AppendChatHistory($"[{senderIP}]: {message}");
                }
                catch { }
            };

            // Đăng ký nhận File
            this._server.OnFileReceived += (sender, data) =>
            {
                try
                {
                    string senderIP = ((IPEndPoint)sender.Client.RemoteEndPoint).Address.ToString();
                    var fileDto = DataHelper.Deserialize<FilePacketDTO>(data);

                    if (fileDto != null)
                    {
                        // Hiển thị lên khung chat của Server
                        AppendChatHistory($"[{senderIP}] đã gửi file: {fileDto.FileName}");
                        // Gọi hàm lưu file
                        HandleIncomingFile(data);
                    }
                }
                catch { }
            };

            // Đăng ký nhận log cho ListView trên form Remote này
            this._server.OnLogAdded += (msg) => {
                UpdateRemoteLog(msg);
            };
        }

        private void frmRemote_Load(object sender, EventArgs e)
        {
            // Gọi hàm StartStreaming khi Form bắt đầu hiển thị
            StartStreaming();
        }

        // --- HÀM ĐÃ SỬA: STREAM MÀN HÌNH (BROADCAST) ---
        private void StartStreaming()
        {
            _isStreaming = true;
            Thread screenThread = new Thread(() =>
            {
                // Vòng lặp chỉ dựa vào biến cờ _isStreaming, không phụ thuộc vào client cụ thể nào
                while (_isStreaming)
                {
                    try
                    {
                        // 1. Chụp màn hình
                        byte[] screenData = RemoteDesktop.Server.Services.ScreenCapturer.CaptureDesktop();

                        // 2. Tạo gói tin ScreenUpdate
                        var packet = new Packet
                        {
                            Type = CommandType.ScreenUpdate,
                            Data = screenData
                        };

                        // 3. QUAN TRỌNG: Gửi Broadcast cho TẤT CẢ Client đang kết nối
                        _server.BroadcastPacket(packet);

                        // Đợi một khoảng ngắn để giảm tải CPU và mạng
                        Thread.Sleep(100);
                    }
                    catch
                    {
                        // Nếu có lỗi (ví dụ chưa có ai kết nối), cứ tiếp tục lặp, không break
                    }
                }
            });
            screenThread.IsBackground = true;
            screenThread.Start();
        }
        // ------------------------------------------------

        private void UpdateRemoteLog(string message)
        {
            if (lsvLog.InvokeRequired)
            {
                lsvLog.Invoke(new Action(() => UpdateRemoteLog(message)));
            }
            else
            {
                ListViewItem item = new ListViewItem(new[] {
                    DateTime.Now.ToString("HH:mm:ss"),
                    message
                });
                lsvLog.Items.Add(item);
                try { item.EnsureVisible(); } catch { }
            }
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
                    Type = CommandType.Chat,
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

        // Ngắt kết nối và trở về frmConnect
        private void btnStopRemote_Click(object sender, EventArgs e)
        {
            // Dừng luồng gửi màn hình trước
            _isStreaming = false;

            if (_server != null)
            {
                _server.Stop(); // Gửi gói tin Disconnect và đóng Socket
            }

            // Mở lại form kết nối
            frmConnect connectForm = new frmConnect();
            connectForm.Show();

            this.Close();
        }
    }
}