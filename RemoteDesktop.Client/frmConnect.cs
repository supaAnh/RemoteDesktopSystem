using RemoteDesktop.Client.Networking;

namespace RemoteDesktop.Client
{
    public partial class frmConnect : Form
    {
        private ClientHandler _client = new ClientHandler();


        public frmConnect()
        {
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                string ip = txtIP.Text;
                int port = (int)textPortNum.Value;

                _client.Connect(ip, port);

                if (_client.IsConnected)
                {
                    MessageBox.Show("Kết nối thành công");
                    //Chuyển sang đăng nhập
                    frmLogin loginForm = new frmLogin(_client);
                    loginForm.Show();
                    this.Hide();
                    
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kết nối thất bại: {ex.Message}");
            }
        }
    }
}
