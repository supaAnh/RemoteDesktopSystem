using RemoteDesktop.Server.Database;
using RemoteDesktop.Server.Networking;
using RemoteDesktop.Server.Utils;
using System;
using System.Windows.Forms;
using System.Drawing; // Thêm thư viện này để dùng Color

namespace RemoteDesktop.Server
{
    public partial class frmConnect : Form
    {
        private ServerHandler _server;
        private frmRemote _currentRemoteForm = null;

        public frmConnect()
        {
            InitializeComponent();
            InitializeLogView(); // <--- Gọi hàm tạo cột
        }

        // --- HÀM MỚI: TẠO CỘT CHO BẢNG LOG ---
        private void InitializeLogView()
        {
            lsvLog.View = View.Details;
            lsvLog.GridLines = true;
            lsvLog.FullRowSelect = true;

            // Xóa cột cũ (nếu có) và thêm 3 cột mới giống hệt frmRemote
            lsvLog.Columns.Clear();
            lsvLog.Columns.Add("Thời gian", 100);
            lsvLog.Columns.Add("Nguồn", 150);     // Cột mới
            lsvLog.Columns.Add("Hành động", 500); // Cột nội dung
        }
        // -------------------------------------

        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                int port = (int)textPortNum.Value;

                if (_server == null)
                {
                    // Truyền lsvLog vào ServerHandler để nó tự ghi log
                    _server = new ServerHandler(lsvLog);
                }

                _server.OnClientConnected += (client) => {
                    this.Invoke(new Action(() => {
                        if (_currentRemoteForm == null || _currentRemoteForm.IsDisposed)
                        {
                            this.Hide();
                            _currentRemoteForm = new frmRemote(_server, client);
                            _currentRemoteForm.Show();
                        }
                    }));
                };

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

                _server.StartListening(port);
                btnStart.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khởi động: " + ex.ToString());
            }
        }

        private void lsvLog_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}