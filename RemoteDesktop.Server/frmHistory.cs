using RemoteDesktop.Server.Database;
using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace RemoteDesktop.Server
{
    public partial class frmHistory : Form
    {
        private DatabaseManager _db = new DatabaseManager();
        private ComboBox cboSessions; // [MỚI] Hộp chọn phiên
        private ListBox lstRecords;
        private PictureBox picRecord;
        private Label lblInfo;

        public frmHistory()
        {
            InitializeComponent();
            SetupUI();
            LoadSessions(); // Tải danh sách các lần kết nối
        }

        private void SetupUI()
        {
            this.Text = "Lịch sử Record (Phân loại theo lần kết nối)";
            this.Size = new Size(1100, 700);
            this.StartPosition = FormStartPosition.CenterScreen;

            // 1. [MỚI] Label và ComboBox chọn Phiên
            Label lblSession = new Label() { Text = "Chọn lần kết nối:", Location = new Point(10, 10), AutoSize = true, Font = new Font("Arial", 10, FontStyle.Bold) };
            this.Controls.Add(lblSession);

            cboSessions = new ComboBox();
            cboSessions.Location = new Point(150, 8);
            cboSessions.Size = new Size(300, 30);
            cboSessions.DropDownStyle = ComboBoxStyle.DropDownList; // Chỉ được chọn, ko được nhập
            cboSessions.SelectedIndexChanged += CboSessions_SelectedIndexChanged;
            this.Controls.Add(cboSessions);

            // 2. Danh sách ảnh (Listbox)
            lstRecords = new ListBox();
            lstRecords.Location = new Point(10, 50);
            lstRecords.Size = new Size(280, 600);
            lstRecords.Font = new Font("Consolas", 10);
            lstRecords.SelectedIndexChanged += LstRecords_SelectedIndexChanged;
            this.Controls.Add(lstRecords);

            // 3. Khung ảnh
            picRecord = new PictureBox();
            picRecord.Location = new Point(300, 50);
            picRecord.Size = new Size(770, 600);
            picRecord.SizeMode = PictureBoxSizeMode.Zoom;
            picRecord.BorderStyle = BorderStyle.FixedSingle;
            picRecord.BackColor = Color.Black;
            this.Controls.Add(picRecord);

            // 4. Info
            lblInfo = new Label() { Text = "...", Location = new Point(470, 10), AutoSize = true, ForeColor = Color.Blue };
            this.Controls.Add(lblInfo);
        }

        // Tải danh sách các phiên (Sessions) vào ComboBox
        private void LoadSessions()
        {
            try
            {
                DataTable dt = _db.GetSessionList();

                // Tạo cột hiển thị: "Giờ bắt đầu - IP"
                dt.Columns.Add("DisplaySession", typeof(string), "StartTime + '  (IP: ' + IP + ')'");

                cboSessions.DataSource = dt;
                cboSessions.DisplayMember = "DisplaySession"; // Hiển thị giờ
                cboSessions.ValueMember = "SessionID";       // Giá trị ngầm là SessionID
            }
            catch (Exception ex) { MessageBox.Show("Lỗi tải session: " + ex.Message); }
        }

        // Khi chọn 1 phiên -> Tải danh sách ảnh của phiên đó
        private void CboSessions_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboSessions.SelectedValue == null) return;
            string selectedSessionID = cboSessions.SelectedValue.ToString();

            DataTable dt = _db.GetRecordsBySession(selectedSessionID);

            lstRecords.DataSource = dt;
            lstRecords.DisplayMember = "CreatedAt"; // Chỉ hiện giờ chụp
            lstRecords.ValueMember = "Id";

            lblInfo.Text = $"Đã tìm thấy {dt.Rows.Count} ảnh trong phiên này.";
            picRecord.Image = null; // Xóa ảnh cũ
        }

        // Khi chọn 1 dòng giờ -> Hiện ảnh
        private void LstRecords_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstRecords.SelectedValue == null) return;
            if (int.TryParse(lstRecords.SelectedValue.ToString(), out int id))
            {
                byte[] imgBytes = _db.GetRecordImage(id);
                if (imgBytes != null)
                {
                    using (MemoryStream ms = new MemoryStream(imgBytes))
                    {
                        if (picRecord.Image != null) picRecord.Image.Dispose();
                        picRecord.Image = Image.FromStream(ms);
                    }
                }
            }
        }

        private void frmHistory_Load(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}