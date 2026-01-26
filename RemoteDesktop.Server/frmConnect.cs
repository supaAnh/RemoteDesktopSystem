using RemoteDesktop.Server.Database;
using RemoteDesktop.Server.Networking;
using System;
using System.Drawing; // Thư viện để chỉnh màu, vị trí nút
using System.Windows.Forms;

namespace RemoteDesktop.Server
{
    public partial class frmConnect : Form
    {
        private ServerHandler _server;
        private frmRemote _currentRemoteForm = null;

        public frmConnect()
        {
            InitializeComponent();

            // --- [PHẦN MỚI] THÊM NÚT XEM LỊCH SỬ BẰNG CODE ---
            // Tui viết code tạo nút ở đây để bạn khỏi cần kéo thả
            Button btnHistory = new Button();
            btnHistory.Text = "Xem Lại Record";
            btnHistory.Size = new Size(120, 30);
            btnHistory.Location = new Point(20, 20); // Nằm ở góc trên bên trái
            btnHistory.BackColor = Color.LightBlue;  // Màu xanh cho dễ nhìn

            // Sự kiện: Bấm nút thì mở Form History
            btnHistory.Click += (s, e) => {
                // Tạo và hiện form lịch sử
                frmHistory historyForm = new frmHistory();
                historyForm.ShowDialog();
            };

            // Thêm nút vào Form
            this.Controls.Add(btnHistory);
            // -------------------------------------------------

            // Gọi hàm tạo cột cho bảng Log
            InitializeLogView();
        }

        // Hàm tạo 3 cột cho bảng Log (Thời gian - Nguồn - Hành động)
        private void InitializeLogView()
        {
            lsvLog.View = View.Details;
            lsvLog.GridLines = true;
            lsvLog.FullRowSelect = true;

            // Xóa cột cũ và thêm cột mới
            lsvLog.Columns.Clear();
            lsvLog.Columns.Add("Thời gian", 100);
            lsvLog.Columns.Add("Nguồn", 150);
            lsvLog.Columns.Add("Hành động", 500);
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                int port = (int)textPortNum.Value;

                if (_server == null)
                {
                    // Truyền lsvLog vào để ServerHandler tự ghi log lên đó
                    _server = new ServerHandler(lsvLog);
                }

                // Xử lý khi có Client kết nối thành công
                _server.OnClientConnected += (client) => {
                    this.Invoke(new Action(() => {
                        // Nếu chưa có cửa sổ Remote nào thì mở mới
                        if (_currentRemoteForm == null || _currentRemoteForm.IsDisposed)
                        {
                            this.Hide(); // Ẩn form kết nối đi
                            _currentRemoteForm = new frmRemote(_server, client);
                            _currentRemoteForm.Show();
                        }
                    }));
                };

                // Kết nối Database
                DatabaseManager dbManager = new DatabaseManager();
                _server.LogToUI("Đang kết nối Database...");

                try
                {
                    dbManager.InitializeDatabase();
                    _server.LogToUI("Database đã sẵn sàng.");
                }
                catch (Exception dbEx)
                {
                    MessageBox.Show("Lỗi kết nối SQL Server: " + dbEx.Message, "Lỗi DB");
                    return;
                }

                // Bắt đầu chạy Server (Chế độ Non-blocking đã cài trong ServerHandler)
                _server.StartListening(port);

                // Khóa nút Start để không bấm lại lần 2
                btnStart.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khởi động: " + ex.ToString());
            }
        }

        // Sự kiện này để trống cũng được (do Visual Studio tự sinh ra)
        private void lsvLog_SelectedIndexChanged(object sender, EventArgs e)
        {
        }
    }
}