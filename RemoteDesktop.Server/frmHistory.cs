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
        // Chỉ cần khai báo DatabaseManager, các nút khác Designer đã lo rồi
        private DatabaseManager _db = new DatabaseManager();

        public frmHistory()
        {
            InitializeComponent();
            SetupEvents();  // Cấu hình sự kiện
            LoadSessions(); // Tải dữ liệu ngay khi mở form
        }

        // Cấu hình các thiết lập phụ mà kéo thả không làm được
        private void SetupEvents()
        {
            // Cấu hình hiển thị cho ComboBox và ListBox
            // Lưu ý: Tên biến cboSessions, listBox1, pictureBox1 phải khớp với tên bạn đặt trong Designer
            cboSessions.DropDownStyle = ComboBoxStyle.DropDownList;

            // Gán sự kiện khi chọn dòng
            cboSessions.SelectedIndexChanged += CboSessions_SelectedIndexChanged;
            listBox1.SelectedIndexChanged += ListBox1_SelectedIndexChanged;

            // Cấu hình PictureBox
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.BackColor = Color.Black;
        }

        // 1. Tải danh sách các phiên vào ComboBox
        private void LoadSessions()
        {
            try
            {
                DataTable dt = _db.GetSessionList();

                if (dt.Rows.Count > 0)
                {
                    // Tạo cột hiển thị đẹp: "Giờ - IP"
                    dt.Columns.Add("DisplaySession", typeof(string), "StartTime + '  (IP: ' + IP + ')'");

                    cboSessions.DataSource = dt;
                    cboSessions.DisplayMember = "DisplaySession"; // Hiển thị tên
                    cboSessions.ValueMember = "SessionID";       // Giá trị ngầm (ID)
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải danh sách: " + ex.Message);
            }
        }

        // 2. Sự kiện: Khi chọn 1 phiên ở ComboBox -> Tải danh sách ảnh vào ListBox
        private void CboSessions_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboSessions.SelectedValue == null) return;

            // Lấy SessionID từ ComboBox (đã kéo thả)
            string selectedSessionID = cboSessions.SelectedValue.ToString();

            // Lấy danh sách ảnh từ DB
            DataTable dt = _db.GetRecordsBySession(selectedSessionID);

            // Đổ vào ListBox (đã kéo thả)
            listBox1.DataSource = dt;
            listBox1.DisplayMember = "CreatedAt";
            listBox1.ValueMember = "Id";

            // Xóa ảnh cũ đang hiện
            if (pictureBox1.Image != null) pictureBox1.Image.Dispose();
            pictureBox1.Image = null;
        }

        // 3. Sự kiện: Khi chọn 1 dòng giờ ở ListBox -> Hiện ảnh lên PictureBox
        private void ListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedValue == null) return;

            if (int.TryParse(listBox1.SelectedValue.ToString(), out int imageId))
            {
                byte[] imgBytes = _db.GetRecordImage(imageId);

                if (imgBytes != null && imgBytes.Length > 0)
                {
                    try
                    {
                        using (MemoryStream ms = new MemoryStream(imgBytes))
                        {
                            // Hiển thị lên PictureBox (đã kéo thả)
                            pictureBox1.Image = new Bitmap(ms);
                        }
                    }
                    catch { }
                }
            }
        }

        // Các hàm thừa do lỡ click đúp trong Designer (để trống để tránh lỗi)
        private void frmHistory_Load(object sender, EventArgs e) { }
        private void label1_Click(object sender, EventArgs e) { }
    }
}