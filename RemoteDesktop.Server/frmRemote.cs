using RemoteDesktop.Common.DTOs;
using RemoteDesktop.Common.Helpers;
using RemoteDesktop.Common.Models;
using RemoteDesktop.Server.Networking;
using System;
using System.Diagnostics; // Dùng cho Process.Start
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
        private string _currentSessionID; // Mã phiên kết nối

        public frmRemote(ServerHandler server, TcpClient client)
        {
            InitializeComponent();
            this._server = server;
            this._targetClient = client;

            // Tạo mã phiên mới: Session_NămThángNgày_GiờPhútGiây
            _currentSessionID = "Session_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

            // Đăng ký nhận tin nhắn Chat
            this._server.OnChatReceived += (sender, message) =>
            {
                try
                {
                    string senderIP = ((IPEndPoint)sender.Client.RemoteEndPoint).Address.ToString();
                    AppendChatHistory($"[{senderIP}]: {message}");
                    UpdateRemoteLog($"[{senderIP}] Chat: {message}");
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
                        AppendChatHistory($"[{senderIP}] đã gửi file: {fileDto.FileName}");
                        HandleIncomingFile(data);
                        UpdateRemoteLog($"[{senderIP}] Gửi file: {fileDto.FileName}");
                    }
                }
                catch { }
            };

            // Đăng ký nhận Log hệ thống
            this._server.OnLogAdded += (msg) => {
                UpdateRemoteLog(msg);
            };
        }

        private void frmRemote_Load(object sender, EventArgs e)
        {
            // Cấu hình bảng Log
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
                        // 1. Chụp ảnh
                        byte[] screenData = RemoteDesktop.Server.Services.ScreenCapturer.CaptureDesktop();

                        // 2. Gửi cho Client
                        var packet = new Packet { Type = CommandType.ScreenUpdate, Data = screenData };
                        _server.BroadcastPacket(packet);

                        // 3. Lưu vào Database (Mỗi 50 khung hình ~ 5s lưu 1 lần)
                        frameCount++;
                        if (frameCount % 50 == 0)
                        {
                            _dbManager.SaveScreenRecord(_currentSessionID, "SERVER", screenData);
                        }

                        Thread.Sleep(100);
                    }
                    catch { }
                }
            });
            screenThread.IsBackground = true;
            screenThread.Start();
        }

        private void UpdateRemoteLog(string message)
        {
            // Lưu log vào Database (Chạy ngầm)
            new Thread(() => {
                string ip = "SYSTEM";
                string content = message;
                if (message.StartsWith("[") && message.Contains("]"))
                {
                    int idx = message.IndexOf("]");
                    ip = message.Substring(1, idx - 1);
                    content = message.Substring(idx + 1).Trim();
                }
                _dbManager.SaveLog(_currentSessionID, ip, content);
            }).Start();

            // Hiện lên giao diện
            if (lsvLog.InvokeRequired)
            {
                lsvLog.Invoke(new Action(() => UpdateRemoteLog(message)));
            }
            else
            {
                try
                {
                    ListViewItem item = new ListViewItem(new[] { DateTime.Now.ToString("HH:mm:ss"), message });
                    lsvLog.Items.Add(item);
                    item.EnsureVisible();
                }
                catch { }
            }
        }

        // --- CÁC HÀM NÚT BẤM (ĐỂ SỬA LỖI CS0103) ---

        // Nút Gửi Chat
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
            catch (Exception ex) { MessageBox.Show("Lỗi gửi: " + ex.Message); }
        }

        // Nút Gửi File
        private void btnSendFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var fileDto = new FilePacketDTO { FileName = Path.GetFileName(ofd.FileName), Buffer = File.ReadAllBytes(ofd.FileName) };
                        var packet = new Packet { Type = CommandType.FileTransfer, Data = DataHelper.Serialize(fileDto) };

                        _server.BroadcastPacket(packet);
                        AppendChatHistory($"[Hệ thống]: Đã gửi file {fileDto.FileName}");
                        UpdateRemoteLog("[SERVER] Gửi file: " + fileDto.FileName);
                    }
                    catch (Exception ex) { MessageBox.Show("Lỗi gửi file: " + ex.Message); }
                }
            }
        }

        // Nút Ngắt kết nối
        private void btnStopRemote_Click(object sender, EventArgs e)
        {
            _isStreaming = false;
            if (_server != null) _server.Stop();

            frmConnect connectForm = new frmConnect();
            connectForm.Show();
            this.Close();
        }

        private void AppendChatHistory(string text)
        {
            if (txtChatHistory.InvokeRequired)
                txtChatHistory.Invoke(new Action(() => AppendChatHistory(text)));
            else
            {
                txtChatHistory.AppendText(text + Environment.NewLine);
                txtChatHistory.ScrollToCaret();
            }
        }

        private void HandleIncomingFile(byte[] rawData)
        {
            // Logic nhận file giữ nguyên
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