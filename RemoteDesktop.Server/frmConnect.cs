using RemoteDesktop.Server.Database;
using RemoteDesktop.Server.Networking;
using RemoteDesktop.Server.Networking;
using RemoteDesktop.Server.Utils;
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
                int port = (int)textPortNum.Value;
                _server = new ServerHandler(lsvLog);

                // Đăng ký sự kiện chuyển form khi có kết nối (đã có trong code của bạn)
                _server.OnClientConnected += (client) => {
                    this.Invoke(new Action(() => {
                        this.Hide();
                        frmRemote remoteForm = new frmRemote(_server, client);
                        remoteForm.Show();
                    }));
                };

                // Kiểm tra Database
                Database.DatabaseManager dbManager = new Database.DatabaseManager();
                _server.LogToUI("Đang kết nối Database...");

                // Thêm try-catch riêng cho DB để không làm crash toàn bộ nút Start
                try
                {
                    dbManager.InitializeDatabase();
                    _server.LogToUI("Database đã sẵn sàng.");
                }
                catch (Exception dbEx)
                {
                    MessageBox.Show("Lỗi kết nối SQL Server: " + dbEx.Message, "Lỗi DB");
                    return; // Dừng lại nếu không có DB
                }

                _server.StartListening(port);
                btnStart.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khởi động: " + ex.ToString()); // Dùng ex.ToString() để xem chi tiết lỗi
            }
        }

        private void lsvLog_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
