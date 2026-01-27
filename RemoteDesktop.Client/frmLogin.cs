using RemoteDesktop.Client.Networking;
using RemoteDesktop.Common.DTOs;
using RemoteDesktop.Common.Helpers;
using RemoteDesktop.Common.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace RemoteDesktop.Client
{
    public partial class frmLogin : Form
    {
        private ClientHandler _client;
        public frmLogin(ClientHandler client)
        {
            InitializeComponent();
            this._client = client;
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void frmLogin_Load(object sender, EventArgs e)
        {

        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            // 1. Kiểm tra tính hợp lệ cơ bản của đầu vào
            if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu!", "Thông báo");
                return;
            }

            if (_client == null || !_client.IsConnected)
            {
                MessageBox.Show("Lỗi: Mất kết nối tới Server. Vui lòng thử lại!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // 2. Đóng gói thông tin tài khoản vào LoginDTO
                LoginDTO loginInfo = new LoginDTO
                {
                    Username = txtUsername.Text,
                    Password = txtPassword.Text
                };

                // 3. Tạo Packet loại Login và Serialize dữ liệu
                Packet p = new Packet
                {
                    Type = RemoteDesktop.Common.Models.CommandType.Login,
                    Data = DataHelper.Serialize(loginInfo)
                };

                // 4. Gửi yêu cầu đăng nhập cho Server qua luồng bảo mật
                _client.SendPacket(p);
                lblStatus.Text = "Đang xác thực thông tin...";
                btnLogin.Enabled = false; // Tạm khóa nút để tránh gửi nhiều lần

                // 5. Đợi phản hồi trực tiếp từ Server cho bước xác thực
                var response = NetworkHelper.ReceiveSecurePacket(_client.GetStream());

                if (response != null && response.Type == RemoteDesktop.Common.Models.CommandType.Login)
                {
                    string result = Encoding.UTF8.GetString(response.Data);

                    if (result != "FAIL") // Nếu không phải FAIL thì result chính là Session Key
                    {
                        _client.SessionKey = result; // Lưu Key vào ClientHandler
                        lblStatus.Text = "Đăng nhập thành công!";

                        frmRemote remoteForm = new frmRemote(_client);
                        remoteForm.Show();
                        this.Hide();
                    }
                    else
                    {
                        lblStatus.Text = "Sai tài khoản hoặc mật khẩu!";
                        MessageBox.Show("Đăng nhập thất bại: Thông tin không chính xác hoặc tài khoản chưa được phê duyệt.",
                                        "Lỗi xác thực", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        btnLogin.Enabled = true;
                    }
                }
                else
                {
                    MessageBox.Show("Không nhận được phản hồi hợp lệ từ máy chủ.", "Lỗi");
                    btnLogin.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi trong quá trình xác thực: " + ex.Message, "Lỗi hệ thống");
                btnLogin.Enabled = true;
                lblStatus.Text = "Lỗi kết nối.";
            }
        }

        private void chkShowPassword_CheckedChanged(object sender, EventArgs e)
        {
            txtPassword.UseSystemPasswordChar = !chkShowPassword.Checked;
        }
    }
}
