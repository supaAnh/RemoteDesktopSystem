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
        private TcpClient _targetClient; // Vẫn giữ biến này để tham khảo
        private bool _isStreaming = false;

        // [MỚI] Khai báo quản lý Database để lưu log
        private Database.DatabaseManager _dbManager = new Database.DatabaseManager();

        public frmRemote(ServerHandler server, TcpClient client)
        {
            InitializeComponent();
            this._server = server;
            this._targetClient = client;

            // 1. XỬ LÝ KHI NHẬN CHAT TỪ CLIENT
            this._server.OnChatReceived += (sender, message) =>
            {
                try
                {
                    // Lấy IP của người gửi
                    string senderIP = ((IPEndPoint)sender.Client.RemoteEndPoint).Address.ToString();

                    // Hiện lên khung chat
                    AppendChatHistory($"[{senderIP}]: {message}");

                    // [MỚI] Ghi vào Log và Database
                    UpdateRemoteLog(senderIP, "Chat: " + message);
                }
                catch { }
            };

            // 2. XỬ LÝ KHI NHẬN FILE TỪ CLIENT
            this._server.OnFileReceived += (sender, data) =>
            {
                try
                {
                    string senderIP = ((IPEndPoint)sender.Client.RemoteEndPoint).Address.ToString();
                    var fileDto = DataHelper.Deserialize<FilePacketDTO>(data);

                    if (fileDto != null)
                    {
                        // Hiển thị thông báo lên khung chat
                        AppendChatHistory($"[{senderIP}] đã gửi file: {fileDto.FileName}");
                        // Gọi hàm xử lý lưu file
                        HandleIncomingFile(data);

                        // [MỚI] Ghi vào Log và Database
                        UpdateRemoteLog(senderIP, "Gửi file: " + fileDto.FileName);
                    }
                }
                catch { }
            };

            // 3. XỬ LÝ LOG HỆ THỐNG (Kết nối, ngắt kết nối, v.v.)
            this._server.OnLogAdded += (msg) => {
                // Log hệ thống thì để tên là SYSTEM
                UpdateRemoteLog("SYSTEM", msg);
            };
        }

        private void frmRemote_Load(object sender, EventArgs e)
        {
            // [MỚI] Cấu hình bảng Log (ListView)
            lsvLog.View = View.Details;           // Chế độ xem chi tiết
            lsvLog.GridLines = true;              // Kẻ bảng
            lsvLog.FullRowSelect = true;          // Chọn cả dòng khi click

            // Xóa cột cũ (nếu có) và thêm 3 cột mới
            lsvLog.Columns.Clear();
            lsvLog.Columns.Add("Thời gian", 100);
            lsvLog.Columns.Add("IP / Nguồn", 150);
            lsvLog.Columns.Add("Hành động", 500); // Cột này rộng để hiển thị nội dung

            // Gọi hàm StartStreaming khi Form bắt đầu hiển thị
            StartStreaming();
        }

        // --- HÀM [MỚI]: GHI LOG VÀO UI VÀ DATABASE ---
        private void UpdateRemoteLog(string ip, string action)
        {
            // 1. Lưu vào Database (Chạy luồng riêng để không làm đơ giao diện)
            new Thread(() => {
                _dbManager.SaveLog(ip, action);
            }).Start();

            // 2. Hiển thị lên giao diện (ListView)
            if (lsvLog.InvokeRequired)
            {
                lsvLog.Invoke(new Action(() => UpdateRemoteLog(ip, action)));
            }
            else
            {
                ListViewItem item = new ListViewItem(new[] {
                    DateTime.Now.ToString("HH:mm:ss"), // Cột 1: Thời gian
                    ip,                                // Cột 2: IP
                    action                             // Cột 3: Hành động
                });

                // Tô màu đỏ cho thông báo hệ thống hoặc Server, màu xanh cho Client
                if (ip == "SERVER" || ip == "SYSTEM")
                    item.ForeColor = Color.Red;
                else
                    item.ForeColor = Color.Blue;

                lsvLog.Items.Add(item);

                // Tự động cuộn xuống dòng cuối cùng
                if (lsvLog.Items.Count > 0)
                {
                    try { lsvLog.Items[lsvLog.Items.Count - 1].EnsureVisible(); } catch { }
                }
            }
        }
        // ------------------------------------------------

        private void StartStreaming()
        {
            _isStreaming = true;
            Thread screenThread = new Thread(() =>
            {
                while (_isStreaming)
                {
                    try
                    {
                        byte[] screenData = RemoteDesktop.Server.Services.ScreenCapturer.CaptureDesktop();
                        var packet = new Packet
                        {
                            Type = CommandType.ScreenUpdate,
                            Data = screenData
                        };
                        _server.BroadcastPacket(packet);
                        Thread.Sleep(100);
                    }
                    catch
                    {
                        // Bỏ qua lỗi
                    }
                }
            });
            screenThread.IsBackground = true;
            screenThread.Start();
        }

        private void btnSendChat_Click(object sender, EventArgs e)
        {
            string msg = txtChatInput.Text.Trim();
            if (string.IsNullOrEmpty(msg)) return;

            try
            {
                var packet = new Packet
                {
                    Type = CommandType.Chat,
                    Data = Encoding.UTF8.GetBytes($"[SERVER]: {msg}")
                };

                _server.BroadcastPacket(packet);
                AppendChatHistory($"[SERVER]: {msg}");

                // [MỚI] Cũng ghi lại hành động của chính Server
                UpdateRemoteLog("SERVER", "Gửi tin nhắn: " + msg);

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

                        _server.BroadcastPacket(packet);
                        AppendChatHistory($"[Hệ thống]: Server đã gửi file {fileDto.FileName}");

                        // [MỚI] Ghi log Server gửi file
                        UpdateRemoteLog("SERVER", "Gửi file: " + fileDto.FileName);
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
                    string downloadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                    string filePath = Path.Combine(downloadPath, fileDto.FileName);

                    File.WriteAllBytes(filePath, fileDto.Buffer);
                    Process.Start("explorer.exe", $"/select,\"{filePath}\"");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi lưu file tại Server: " + ex.Message);
            }
        }

        private void btnStopRemote_Click(object sender, EventArgs e)
        {
            _isStreaming = false;
            if (_server != null)
            {
                _server.Stop();
            }

            frmConnect connectForm = new frmConnect();
            connectForm.Show();
            this.Close();
        }
    }
}