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

        private System.Diagnostics.Process _ffmpegProcess;
        private System.IO.Stream _ffmpegStdin;
        private string _recordFileName;
        private string _localVideoPath;

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
            if (txtChatInput.Focused)
            {
                return;
            }
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

            StartRecording();
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
                        this.BeginInvoke(new Action(() =>
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

        // Hàm bổ trợ cập nhật lsvLog trên giao diện Client
      
            

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
        // Cập nhật hàm xử lý khi Server chủ động ngắt kết nối
        private void HandleServerDisconnect()
        {
            // Do hàm này chạy ở luồng mạng (background thread), ta cần dùng Invoke để hiện UI an toàn
            this.Invoke(new Action(() => {
                MessageBox.Show("Máy chủ đã ngắt kết nối. Chương trình sẽ tự động đóng.",
                                "Thông báo ngắt kết nối", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                if (_client != null) _client.Disconnect();
                Environment.Exit(0); // Đóng hoàn toàn chương trình Client
            }));
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
            // 1. [QUAN TRỌNG] Bơm dữ liệu vào FFmpeg trên một luồng nền (Background Thread)
            // Việc này ngăn việc FFmpeg làm "kẹt" giao diện khiến khung chat bị đơ.
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                if (_ffmpegStdin != null && _ffmpegProcess != null && !_ffmpegProcess.HasExited)
                {
                    try
                    {
                        _ffmpegStdin.Write(data, 0, data.Length);
                        _ffmpegStdin.Flush();
                    }
                    catch { }
                }
            });

            // 2. Vẽ hình lên PictureBox
            if (picScreen.InvokeRequired)
            {
                // Dùng BeginInvoke để giao diện không bị bắt chờ luồng mạng
                picScreen.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        using (MemoryStream ms = new MemoryStream(data))
                        {
                            if (picScreen.Image != null) picScreen.Image.Dispose();
                            picScreen.Image = Image.FromStream(ms);
                        }
                    }
                    catch { }
                }));
            }
            else
            {
                try
                {
                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        if (picScreen.Image != null) picScreen.Image.Dispose();
                        picScreen.Image = Image.FromStream(ms);
                    }
                }
                catch { }
            }
        }


        private void StartRecording()
        {
            try
            {
                // 1. Tạo tên file và đường dẫn (Lưu ở thư mục Downloads)
                _recordFileName = $"Record_{DateTime.Now:yyyyMMdd_HHmmss}.avi";
                _localVideoPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", _recordFileName);

                // 2. Cấu hình Process gọi ffmpeg.exe
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "ffmpeg.exe",
                    // Giải thích lệnh: Đọc ảnh JPEG liên tục từ Stdin (-f image2pipe -c:v mjpeg -i -), 
                    // frame rate 15, xuất ra AVI bằng codec mpeg4 (hỗ trợ tốt nhất cho Windows Media Player)
                    Arguments = $"-y -f image2pipe -c:v mjpeg -r 15 -i - -vf \"scale=trunc(iw/2)*2:trunc(ih/2)*2\" -c:v mpeg4 -pix_fmt yuv420p \"{_localVideoPath}\"",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    CreateNoWindow = true // Ẩn cửa sổ cmd của ffmpeg
                };

                _ffmpegProcess = System.Diagnostics.Process.Start(psi);
                _ffmpegStdin = _ffmpegProcess.StandardInput.BaseStream; // Lấy luồng nhập
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể khởi động FFmpeg. Vui lòng kiểm tra lại file ffmpeg.exe. Lỗi: " + ex.Message);
            }
        }

        private void StopRecordingAndSend()
        {
            try
            {
                // 1. Đóng luồng nhập để báo cho FFmpeg biết là đã kết thúc việc gửi ảnh
                if (_ffmpegStdin != null)
                {
                    _ffmpegStdin.Close();
                    _ffmpegStdin = null;
                }

                // 2. Đợi FFmpeg chốt file video (chờ tối đa 5 giây)
                if (_ffmpegProcess != null && !_ffmpegProcess.HasExited)
                {
                    _ffmpegProcess.WaitForExit(5000);
                    if (!_ffmpegProcess.HasExited) _ffmpegProcess.Kill();
                    _ffmpegProcess.Dispose();
                    _ffmpegProcess = null;
                }

                // 3. Đọc file video vừa quay và gửi lên Server
                if (File.Exists(_localVideoPath))
                {
                    byte[] videoData = File.ReadAllBytes(_localVideoPath);
                    var fileDto = new FilePacketDTO { FileName = _recordFileName, Buffer = videoData };

                    // Nhớ thêm VideoRecord vào enum CommandType trong thư mục Common
                    var packet = new Packet
                    {
                        Type = RemoteDesktop.Common.Models.CommandType.VideoRecord,
                        Data = DataHelper.Serialize(fileDto)
                    };

                    _client.SendPacket(packet);
                    MessageBox.Show($"Đã tự động quay màn hình và lưu tại:\n{_localVideoPath}\nFile cũng đã được gửi lên Server.", "Thông báo Record");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi đóng gói video: " + ex.Message);
            }
        }


        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            try
            {
                StopRecordingAndSend();

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