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

            // Đăng ký sự kiện
            this._server.OnChatReceived += Server_OnChatReceived;
            this._server.OnFileReceived += Server_OnFileReceived;
            this._server.OnLogAdded += Server_OnLogAdded;
        }

        // Tách hàm sự kiện ra để code gọn hơn
        private void Server_OnChatReceived(TcpClient sender, string message)
        {
            try
            {
                string senderIP = ((IPEndPoint)sender.Client.RemoteEndPoint).Address.ToString();
                // 1. Hiện lên khung chat
                AppendChatHistory($"[{senderIP}]: {message}");
                // 2. Lưu vào Log và DB
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
                        // 1. Chụp ảnh (đã được nén nhẹ ở ScreenCapturer.cs)
                        byte[] screenData = RemoteDesktop.Server.Services.ScreenCapturer.CaptureDesktop();

                        if (screenData.Length > 0)
                        {
                            // 2. Gửi cho Client
                            var packet = new Packet { Type = CommandType.ScreenUpdate, Data = screenData };
                            _server.BroadcastPacket(packet);

                            // 3. Lưu DB định kỳ (mỗi 50 khung hình)
                            frameCount++;
                            if (frameCount % 50 == 0)
                            {
                                _dbManager.SaveScreenRecord(_currentSessionID, "SERVER", screenData);
                            }
                        }

                        // [QUAN TRỌNG] Nghỉ 50ms (khoảng 20FPS)
                        // Khoảng nghỉ này CỰC KỲ QUAN TRỌNG để Server có thời gian xử lý gói tin Chat
                        Thread.Sleep(50);
                    }
                    catch { }
                }
            });
            screenThread.IsBackground = true;
            screenThread.Start();
        }

        private void UpdateRemoteLog(string message)
        {
            // Lưu log vào Database (DB Manager đã có Task.Run bên trong nên gọi trực tiếp ok)
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
            _dbManager.SaveLog(_currentSessionID, ip, content);

            // Hiện lên UI
            if (lsvLog.InvokeRequired)
            {
                lsvLog.BeginInvoke(new Action(() => UpdateRemoteLog(message)));
            }
            else
            {
                try
                {
                    ListViewItem item = new ListViewItem(new[] { DateTime.Now.ToString("HH:mm:ss"), message });
                    lsvLog.Items.Add(item);
                    if (lsvLog.Items.Count > 0) item.EnsureVisible();
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
                // Gửi Broadcast cho mọi người
                var packet = new Packet { Type = CommandType.Chat, Data = Encoding.UTF8.GetBytes($"[SERVER]: {msg}") };
                _server.BroadcastPacket(packet);

                // Hiển thị và lưu log tại Server
                AppendChatHistory($"[SERVER]: {msg}");
                UpdateRemoteLog("[SERVER] Chat: " + msg);
                txtChatInput.Clear();
            }
            catch (Exception ex) { MessageBox.Show("Lỗi gửi: " + ex.Message); }
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
            _isStreaming = false;

            // Hủy đăng ký sự kiện để tránh lỗi khi mở lại
            this._server.OnChatReceived -= Server_OnChatReceived;
            this._server.OnFileReceived -= Server_OnFileReceived;
            this._server.OnLogAdded -= Server_OnLogAdded;

            if (_server != null) _server.Stop();

            frmConnect connectForm = new frmConnect();
            connectForm.Show();
            this.Close();
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
                    Process.Start("explorer.exe", $"/select,\"{path}\"");
                }
            }
            catch { }
        }
    }
}