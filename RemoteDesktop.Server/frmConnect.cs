using RemoteDesktop.Server.Database;
using RemoteDesktop.Server.Networking;
using RemoteDesktop.Server.Utils;
using System;
using System.Windows.Forms;

namespace RemoteDesktop.Server
{
    public partial class frmConnect : Form
    {
        private ServerHandler _server;

        // --- THÊM BIẾN NÀY ĐỂ QUẢN LÝ CỬA SỔ DUY NHẤT ---
        private frmRemote _currentRemoteForm = null;

        public frmConnect()
        {
            InitializeComponent();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                int port = (int)textPortNum.Value;

                // Chỉ tạo ServerHandler mới nếu chưa có
                if (_server == null)
                {
                    _server = new ServerHandler(lsvLog);
                }

                // --- SỬA LOGIC SỰ KIỆN KẾT NỐI ---
                // Thay vì mỗi lần client vào lại new frmRemote(), ta kiểm tra xem form đã mở chưa
                _server.OnClientConnected += (client) => {
                    this.Invoke(new Action(() => {
                        // Nếu chưa có form hoặc form cũ đã bị tắt
                        if (_currentRemoteForm == null || _currentRemoteForm.IsDisposed)
                        {
                            this.Hide();
                            // Tạo form mới và lưu vào biến _currentRemoteForm
                            _currentRemoteForm = new frmRemote(_server, client);
                            _currentRemoteForm.Show();
                        }
                        else
                        {
                            // Nếu form đã mở rồi thì không làm gì cả (Server tự động chấp nhận kết nối ngầm)
                            // Bạn có thể log thêm dòng này nếu muốn:
                            // UIHelper.AppendLog(lsvLog, "Có thêm Client mới vừa tham gia.");
                        }
                    }));
                };
                // ----------------------------------

                // Kiểm tra Database
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