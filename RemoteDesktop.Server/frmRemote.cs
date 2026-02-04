using RemoteDesktop.Common.DTOs;
using RemoteDesktop.Common.Helpers;
using RemoteDesktop.Common.Models;
using RemoteDesktop.Server.Networking;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using RemoteDesktop.Server.Database;

namespace RemoteDesktop.Server
{
    public partial class frmRemote : Form
    {
        private ServerHandler _server;
        private TcpClient _targetClient;
        private bool _isStreaming = false;
        private DatabaseManager _dbManager = new DatabaseManager();
        private string _currentSessionID;

        public frmRemote(ServerHandler server, TcpClient client)
        {
            InitializeComponent();
            this._server = server;
            this._targetClient = client;

            _currentSessionID = "Session_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

            // Đăng ký sự kiện an toàn
            this._server.OnChatReceived += Server_OnChatReceived;
            this._server.OnFileReceived += Server_OnFileReceived;
            this._server.OnLogAdded += Server_OnLogAdded;
        }

        private void Server_OnChatReceived(TcpClient sender, string message)
        {
            try
            {
                string senderIP = ((IPEndPoint)sender.Client.RemoteEndPoint).Address.ToString();
                AppendChatHistory($"[{senderIP}]: {message}");
                UpdateRemoteLog($"[{senderIP}] Chat: {message}");
            }
            catch { }
        }

        private void Server_OnFileReceived(TcpClient sender, byte[] data)
        {
            try
            {
                string senderIP = ((IPEndPoint)sender.Client.RemoteEndPoint).Address.ToString();
                var fileDto = DataHelper.Deserialize<FilePacketDTO>(data);
                if (fileDto != null)
                {
                    AppendChatHistory($"[{senderIP}] đã gửi file: {fileDto.FileName}");
                    HandleIncomingFile(data);
                    UpdateRemoteLog($"[{senderIP}] Gửi file: {fileDto.FileName}");
                }
            }
            catch { }
        }

        private void Server_OnLogAdded(string msg)
        {
            UpdateRemoteLog(msg);
        }

        private void frmRemote_Load(object sender, EventArgs e)
        {
            lsvLog.View = View.Details;
            lsvLog.GridLines = true;
            lsvLog.FullRowSelect = true;
            lsvLog.Columns.Clear();
            lsvLog.Columns.Add("Thời gian", 100);
            lsvLog.Columns.Add("Nội dung", 600);

            StartStreaming();
        }

        private void StartStreaming()
        {
            _isStreaming = true;
            Thread screenThread = new Thread(() =>
            {
                long frameCount = 0;
                while (_isStreaming)
                {
                    try
                    {
                        // 1. Chụp ảnh màn hình (Sử dụng 'using' ẩn bên trong ScreenCapturer)
                        byte[] screenData = RemoteDesktop.Server.Services.ScreenCapturer.CaptureDesktop();

                        if (screenData != null && screenData.Length > 0)
                        {
                            // 2. Gửi cho tất cả Client (đã được tối ưu ThreadPool ở ServerHandler)
                            var packet = new Packet { Type = CommandType.ScreenUpdate, Data = screenData };
                            _server.BroadcastPacket(packet);

                            // 3. Lưu DB định kỳ trên luồng riêng để tránh lag hình ảnh
                            frameCount++;
                            if (frameCount % 60 == 0) // Lưu mỗi ~3 giây (tốc độ 20FPS)
                            {
                                string sessionId = _currentSessionID;
                                byte[] dataToSave = screenData;
                                ThreadPool.QueueUserWorkItem(_ => _dbManager.SaveScreenRecord(sessionId, "SERVER", dataToSave));
                            }
                        }

                        // [QUAN TRỌNG NHẤT] Nghỉ 50ms để giải phóng CPU và xử lý các gói tin Input từ Client
                        Thread.Sleep(50);
                    }
                    catch { Thread.Sleep(100); }
                }
            });
            screenThread.IsBackground = true;
            screenThread.Start();
        }

        private void UpdateRemoteLog(string message)
        {
            // Tách IP và nội dung để lưu Log
            string ip = "SYSTEM";
            string content = message;
            if (message.StartsWith("[") && message.Contains("]"))
            {
                try
                {
                    int idx = message.IndexOf("]");
                    if (idx > 1)
                    {
                        ip = message.Substring(1, idx - 1);
                        content = message.Substring(idx + 1).Trim();
                    }
                }
                catch { }
            }

            // Lưu log vào Database qua luồng phụ
            _dbManager.SaveLog(_currentSessionID, ip, content);

            // Cập nhật UI an toàn bằng BeginInvoke (Bất đồng bộ)
            if (lsvLog.InvokeRequired)
            {
                lsvLog.BeginInvoke(new Action(() => UpdateRemoteLog(message)));
            }
            else
            {
                try
                {
                    // Giới hạn 50 dòng log để tránh tràn bộ nhớ giao diện
                    if (lsvLog.Items.Count > 50) lsvLog.Items.RemoveAt(0);

                    ListViewItem item = new ListViewItem(new[] { DateTime.Now.ToString("HH:mm:ss"), message });
                    lsvLog.Items.Add(item);
                    item.EnsureVisible();
                }
                catch { }
            }
        }

        private void btnSendChat_Click(object sender, EventArgs e)
        {
            string msg = txtChatInput.Text.Trim();
            if (string.IsNullOrEmpty(msg)) return;

            try
            {
                var packet = new Packet { Type = CommandType.Chat, Data = Encoding.UTF8.GetBytes($"[SERVER]: {msg}") };
                _server.BroadcastPacket(packet);

                AppendChatHistory($"[SERVER]: {msg}");
                UpdateRemoteLog("[SERVER] Chat: " + msg);
                txtChatInput.Clear();
            }
            catch (Exception ex) { MessageBox.Show("Lỗi gửi chat: " + ex.Message); }
        }

        private void btnSendFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        byte[] fileData = File.ReadAllBytes(ofd.FileName);
                        var fileDto = new FilePacketDTO { FileName = Path.GetFileName(ofd.FileName), Buffer = fileData };
                        var packet = new Packet { Type = CommandType.FileTransfer, Data = DataHelper.Serialize(fileDto) };

                        _server.BroadcastPacket(packet);
                        AppendChatHistory($"[Hệ thống]: Đã gửi file {fileDto.FileName}");
                        UpdateRemoteLog("[SERVER] Gửi file: " + fileDto.FileName);
                    }
                    catch (Exception ex) { MessageBox.Show("Lỗi gửi file: " + ex.Message); }
                }
            }
        }

        private void btnStopRemote_Click(object sender, EventArgs e)
        {
            StopStreamingAndClose();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopStreamingAndClose();
            base.OnFormClosing(e);
        }

        private void StopStreamingAndClose()
        {
            _isStreaming = false;

            // Hủy đăng ký sự kiện để tránh rò rỉ bộ nhớ
            this._server.OnChatReceived -= Server_OnChatReceived;
            this._server.OnFileReceived -= Server_OnFileReceived;
            this._server.OnLogAdded -= Server_OnLogAdded;

            // Ngắt kết nối Server nếu cần
            if (_server != null) _server.Stop();
        }

        private void AppendChatHistory(string text)
        {
            if (txtChatHistory.InvokeRequired)
                txtChatHistory.BeginInvoke(new Action(() => AppendChatHistory(text)));
            else
            {
                txtChatHistory.AppendText(text + Environment.NewLine);
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
                    string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", fileDto.FileName);
                    File.WriteAllBytes(path, fileDto.Buffer);

                    // Mở thư mục Downloads và bôi đậm file vừa nhận
                    Process.Start("explorer.exe", $"/select,\"{path}\"");
                }
            }
            catch { }
        }
    }
}