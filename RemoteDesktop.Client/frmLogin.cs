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
            if (_client == null || !_client.IsConnected)
            {
                MessageBox.Show("Lỗi: Chưa kết nối tới Server!");
                return;
            }

            // 1. Đóng gói thông tin vào DTO
            LoginDTO loginInfo = new LoginDTO
            {
                Username = txtUsername.Text,
                Password = txtPassword.Text
            };

            // 2. Tạo Packet loại Login
            Packet p = new Packet
            {
                Type = RemoteDesktop.Common.Models.CommandType.Login,
                Data = DataHelper.Serialize(loginInfo)
            };

            // 3. Gửi cho Server thông qua ClientHandler
            _client.SendPacket(p);
            lblStatus.Text = "Đang xác thực...";

            // 4. Đợi phản hồi từ Server (Đọc trực tiếp từ Stream cho bước đăng nhập)
            try
            {
                var response = NetworkHelper.ReceiveSecurePacket(_client.GetStream());
                if (response != null && response.Type == RemoteDesktop.Common.Models.CommandType.Login)
                {
                    string result = Encoding.UTF8.GetString(response.Data);
                    if (result == "SUCCESS")
                    {
                        // Đăng nhập thành công -> Chuyển sang màn hình điều khiển
                        frmRemote remote = new frmRemote(_client);
                        remote.Show();
                        this.Hide();
                    }
                    else
                    {
                        lblStatus.Text = "Sai tài khoản hoặc mật khẩu!";
                        MessageBox.Show("Đăng nhập thất bại!");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi xác thực: " + ex.Message);
            }
        }

        private void chkShowPassword_CheckedChanged(object sender, EventArgs e)
        {
            txtPassword.UseSystemPasswordChar = !chkShowPassword.Checked;
        }
    }
}
