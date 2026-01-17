using RemoteDesktop.Server.Database;
using RemoteDesktop.Server.Networking;
using RemoteDesktop.Server.Networking;
namespace RemoteDesktop.Server
{
    public partial class frmConnect : Form
    {
        private ServerHandler _server;

        public frmConnect()
        {
            InitializeComponent();
        }



        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                // 1. Khởi tạo Database và lấy thông tin Port
                Database.DatabaseManager _dbManager = new Database.DatabaseManager();
                int port = (int)textPortNum.Value;

                // 2. Khởi tạo ServerHandler
                _server = new ServerHandler(lsvLog);

                // 3. Kiểm tra kết nối Database trước khi chạy Server
                _server.LogToUI("Đang kiểm tra kết nối SQL Server...");
                try
                {
                    _dbManager.InitializeDatabase();
                    _server.LogToUI("Kết nối Database thành công.");
                }
                catch (Exception dbEx)
                {
                    _server.LogToUI("LỖI SQL: " + dbEx.Message);
                    MessageBox.Show("Lỗi Database: " + dbEx.Message);
                    return; // Dừng khởi động nếu DB lỗi
                }

                // 4. Đăng ký sự kiện: CHỈ chuyển form khi Login thành công
                // (Lưu ý: Bạn nên tạo sự kiện OnLoginSuccess trong ServerHandler)
                _server.OnClientConnected += (client) => {
                    this.Invoke(new Action(() => {
                        // Chỉ ẩn form kết nối và hiện form Remote khi xác thực hoàn tất
                        this.Hide();
                        frmRemote remoteForm = new frmRemote(_server, client);
                        remoteForm.Show();
                        _server.LogToUI("Đã chuyển sang màn hình điều khiển.");
                    }));
                };

                // 5. Bắt đầu lắng nghe
                _server.StartListening(port);

                btnStart.Enabled = false;
                _server.LogToUI("Server đang chạy và đợi xác thực từ máy khách...");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khởi động: " + ex.Message);
            }
        }
    }
}
