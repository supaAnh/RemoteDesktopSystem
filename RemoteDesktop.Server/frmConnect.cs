using RemoteDesktop.Server.Database;
using RemoteDesktop.Server.Networking;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace RemoteDesktop.Server
{
    public partial class frmConnect : Form
    {
        private ServerHandler _server;
        private frmRemote _currentRemoteForm = null;

        // Biến lưu trữ tạm Log "Hiện tại" (Real-time logs)
        private List<ListViewItem> _realTimeLogs = new List<ListViewItem>();

        public frmConnect()
        {
            InitializeComponent();

            // Khởi tạo các cột cho bảng lsvLog
            InitializeLogView();

            // Cấu hình ComboBox (đã có sẵn trên Form)
            comboBoxSession.DropDownStyle = ComboBoxStyle.DropDownList;

            // Tải danh sách các phiên Log
            LoadSessionsIntoComboBox();

            // Gán sự kiện chọn phiên
            comboBoxSession.SelectedIndexChanged += ComboBoxSession_SelectedIndexChanged;

            // Tự động tải lại danh sách phiên mới nhất mỗi khi bấm mũi tên xổ xuống
            comboBoxSession.DropDown += (s, e) => LoadSessionsIntoComboBox();
        }

        // Tải danh sách Session vào ComboBox
        private void LoadSessionsIntoComboBox()
        {
            // Lưu lại vị trí đang chọn để không bị nhảy khi làm mới
            string selectedVal = comboBoxSession.SelectedValue?.ToString();

            DatabaseManager db = new DatabaseManager();
            DataTable dt = db.GetSessionList();

            DataTable dtMerged = new DataTable();
            dtMerged.Columns.Add("SessionID", typeof(string));
            dtMerged.Columns.Add("DisplaySession", typeof(string));

            // Thêm mục mặc định ở đầu tiên
            dtMerged.Rows.Add("CURRENT", "--- HIỆN TẠI ---");

            if (dt != null)
            {
                foreach (DataRow row in dt.Rows)
                {
                    string sId = row["SessionID"].ToString();
                    string disp = row["StartTime"].ToString() + " (IP: " + row["IP"].ToString() + ")";
                    dtMerged.Rows.Add(sId, disp);
                }
            }

            // Tạm tắt sự kiện để gán Data không bị trigger logic
            comboBoxSession.SelectedIndexChanged -= ComboBoxSession_SelectedIndexChanged;

            comboBoxSession.DataSource = dtMerged;
            comboBoxSession.DisplayMember = "DisplaySession";
            comboBoxSession.ValueMember = "SessionID";

            // Phục hồi lại lựa chọn cũ hoặc mặc định chọn "Hiện tại"
            if (selectedVal != null && dtMerged.Select($"SessionID = '{selectedVal}'").Length > 0)
                comboBoxSession.SelectedValue = selectedVal;
            else
                comboBoxSession.SelectedIndex = 0;

            // Bật lại sự kiện
            comboBoxSession.SelectedIndexChanged += ComboBoxSession_SelectedIndexChanged;
        }

        // Sự kiện khi người dùng đổi lựa chọn trên ComboBox
        private void ComboBoxSession_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxSession.SelectedValue == null) return;
            string selectedSession = comboBoxSession.SelectedValue.ToString();

            // Xóa sạch log trên màn hình để chuẩn bị nạp log mới
            lsvLog.Items.Clear();

            if (selectedSession == "CURRENT")
            {
                // Nếu chọn "Hiện tại", đổ lại các log đang cache trong RAM ra màn hình
                foreach (var item in _realTimeLogs)
                {
                    lsvLog.Items.Add((ListViewItem)item.Clone());
                }
                if (lsvLog.Items.Count > 0) lsvLog.Items[lsvLog.Items.Count - 1].EnsureVisible();
            }
            else
            {
                // Nếu chọn phiên cũ, tải log từ SQL Server
                DatabaseManager db = new DatabaseManager();
                DataTable dt = db.GetLogsBySession(selectedSession);

                foreach (DataRow row in dt.Rows)
                {
                    string time = Convert.ToDateTime(row["CreatedAt"]).ToString("HH:mm:ss");
                    string ip = row["IPAddress"].ToString();
                    string action = row["Action"].ToString();

                    ListViewItem item = new ListViewItem(new[] { time, ip, action });
                    lsvLog.Items.Add(item);
                }
            }
        }

        // Tạo cột cho bảng Log
        private void InitializeLogView()
        {
            lsvLog.View = View.Details;
            lsvLog.GridLines = true;
            lsvLog.FullRowSelect = true;
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
                    // Truyền 1 ListView ảo để chặn ServerHandler tự ý ghi đè lên lsvLog của Form
                    ListView hiddenLogView = new ListView();
                    _server = new ServerHandler(hiddenLogView);

                    // Lắng nghe sự kiện Log để tự lưu và quản lý hiển thị
                    _server.OnLogAdded += (msg) =>
                    {
                        if (this.IsDisposed) return;
                        this.Invoke(new Action(() =>
                        {
                            var item = new ListViewItem(new[] { DateTime.Now.ToString("HH:mm:ss"), "SYSTEM", msg });
                            item.ForeColor = Color.Blue;

                            // 1. Lưu luôn vào bộ nhớ đệm
                            _realTimeLogs.Add(item);

                            // 2. CHỈ hiển thị nếu ComboBox đang ở chế độ "HIỆN TẠI"
                            if (comboBoxSession.SelectedValue != null && comboBoxSession.SelectedValue.ToString() == "CURRENT")
                            {
                                lsvLog.Items.Add((ListViewItem)item.Clone());
                                if (lsvLog.Items.Count > 0) lsvLog.Items[lsvLog.Items.Count - 1].EnsureVisible();
                            }
                        }));
                    };
                }

                _server.OnClientConnected += (client) =>
                {
                    this.Invoke(new Action(() =>
                    {
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

        // Sự kiện click nút Xem lại Record (Hãy chắc chắn nút này có tên là btnHistory trong Designer)
        private void btnHistory_Click(object sender, EventArgs e)
        {
            frmHistory historyForm = new frmHistory();
            historyForm.ShowDialog();
        }

        // Sự kiện trống mặc định của Designer
        private void lsvLog_SelectedIndexChanged(object sender, EventArgs e) { }

        
    }
}