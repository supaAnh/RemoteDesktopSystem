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
            //    // 1. Đóng gói thông tin vào DTO
            //    LoginDTO loginInfo = new LoginDTO
            //    {
            //        Username = txtUsername.Text,
            //        Password = txtPassword.Text
            //    };

            //    // 2. Tạo Packet loại Login
            //    Packet p = new Packet
            //    {
            //        Type = RemoteDesktop.Common.Models.CommandType.Login,
            //        Data = DataHelper.Serialize(loginInfo)
            //    };

            //    // 3. Gửi cho Server
            //    _client.SendPacket(p);

            //    // 4. Lắng nghe phản hồi từ Server (Đoạn này tạm thời đợi phản hồi)
            //    lblStatus.Text = "Đang xác thực...";
            //
            if (_client == null)
            {
                MessageBox.Show("Lỗi: Đối tượng kết nối (ClientHandler) chưa được khởi tạo!");
                return;
            }
            frmRemote remote = new frmRemote(_client);
            remote.Show();
            this.Hide();
            Console.WriteLine("Đã chuyển sang frmRemote thành công.");
        }

        private void chkShowPassword_CheckedChanged(object sender, EventArgs e)
        {
            txtPassword.UseSystemPasswordChar = !chkShowPassword.Checked;
        }
    }
}
