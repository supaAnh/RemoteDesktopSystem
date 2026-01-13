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

namespace RemoteDesktop.Server
{
    public partial class frmRemote : Form
    {
        private ServerHandler _server;
        private TcpClient _targetClient; // Lưu trữ client đang kết nối


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
                if (sender == _targetClient)
                {
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
                ofd.Title = "Chọn file muốn gửi tới Client";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var stream = _targetClient.GetStream();
                        // 1. Đóng gói file vào DTO
                        var fileDto = new RemoteDesktop.Common.DTOs.FilePacketDTO
                        {
                            FileName = System.IO.Path.GetFileName(ofd.FileName),
                            Buffer = System.IO.File.ReadAllBytes(ofd.FileName)
                        };

                        // 2. Tạo Packet FileTransfer
                        var packet = new RemoteDesktop.Common.Models.Packet
                        {
                            Type = RemoteDesktop.Common.Models.CommandType.FileTransfer,
                            Data = RemoteDesktop.Common.Helpers.DataHelper.Serialize(fileDto)
                        };

                        // 3. Gửi đi
                        _server.BroadcastPacket(packet);

                        // 4. Thông báo và log
                        txtChatHistory.AppendText($"[Hệ thống]: Đang gửi file {fileDto.FileName}...{Environment.NewLine}");
                        NetworkHelper.SendSecurePacket(stream, packet);

                        AppendChatHistory($"[Hệ thống]: Đã gửi xong file {fileDto.FileName}");
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
                var fileDto = DataHelper.Deserialize<RemoteDesktop.Common.DTOs.FilePacketDTO>(rawData);
                if (fileDto != null)
                {
                    // Lưu vào thư mục Downloads của máy Client
                    string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", fileDto.FileName);

                    File.WriteAllBytes(path, fileDto.Buffer);

                    AppendChatHistory($"[Hệ thống]: Đã nhận file '{fileDto.FileName}' và lưu tại thư mục Downloads.");
                    MessageBox.Show($"Bạn đã nhận được file: {fileDto.FileName}", "Thông báo");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi lưu file: " + ex.Message);
            }
        }
    }
}
