using RemoteDesktop.Server.Networking;

namespace RemoteDesktop.Server
{
    public partial class frmConnect : Form
    {
        private ServerHandler _server;
        //private DatabaseManager _db = new DatabaseManager();

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

                // Đăng ký sự kiện: Khi có client vào thì mới mở Form Remote
                _server.OnClientConnected += (client) => {
                    // Sử dụng Invoke để đảm bảo mở Form từ luồng giao diện (UI Thread)
                    this.Invoke(new Action(() => {
                        this.Hide();

                        // SỬA TẠI ĐÂY: Bạn phải truyền đủ 2 tham số là (_server, client)
                        frmRemote remoteForm = new frmRemote(_server, client);

                        remoteForm.Show();
                    }));
                };

                _server.StartListening(port);

                btnStart.Enabled = false;
                lsvLog.Text = "Đang đợi máy khách kết nối...";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
