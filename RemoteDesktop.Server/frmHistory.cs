using RemoteDesktop.Server.Database;
using System;
using System.Data;
using System.IO;
using System.Windows.Forms;

namespace RemoteDesktop.Server
{
    public partial class frmHistory : Form
    {
        private DatabaseManager _db = new DatabaseManager();
        private string _tempVideoPath; // Biến lưu đường dẫn file video tạm

        public frmHistory()
        {
            InitializeComponent();
            SetupEvents();
            LoadVideoList();
        }

        private void SetupEvents()
        {
            // Thiết lập comboBoxRecord (trong Designer bạn đang đặt tên là comboBoxRecord)
            comboBoxRecord.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxRecord.SelectedIndexChanged += ComboBoxRecord_SelectedIndexChanged;
        }

        // 1. Tải danh sách video từ Database lên ComboBox
        private void LoadVideoList()
        {
            try
            {
                DataTable dt = _db.GetVideoList();
                if (dt.Rows.Count > 0)
                {
                    // Tạo cột hiển thị: "Tên file (Thời gian)"
                    dt.Columns.Add("DisplayRecord", typeof(string), "FileName + ' (' + CreatedAt + ')'");

                    comboBoxRecord.DataSource = dt;
                    comboBoxRecord.DisplayMember = "DisplayRecord"; // Text hiện ra
                    comboBoxRecord.ValueMember = "Id";              // ID ẩn bên dưới
                }
                else
                {
                    comboBoxRecord.DataSource = null;
                    comboBoxRecord.Items.Add("Chưa có bản record nào");
                    comboBoxRecord.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải danh sách video: " + ex.Message);
            }
        }

        // 2. Khi chọn 1 bản Record trên ComboBox
        private void ComboBoxRecord_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxRecord.SelectedValue == null) return;

            // Lấy ID của video
            if (int.TryParse(comboBoxRecord.SelectedValue.ToString(), out int videoId))
            {
                // Dừng video cũ nếu đang phát
                axWindowsMediaPlayer1.Ctlcontrols.stop();

                // Lấy byte[] video từ Database
                byte[] videoBytes = _db.GetVideoData(videoId);

                if (videoBytes != null && videoBytes.Length > 0)
                {
                    try
                    {
                        // Tạo đường dẫn file tạm trong thư mục Temp của Windows
                        _tempVideoPath = Path.Combine(Path.GetTempPath(), $"temp_record_{videoId}.avi");

                        // Chỉ ghi file ra nếu nó chưa tồn tại để tiết kiệm thời gian
                        if (!File.Exists(_tempVideoPath))
                        {
                            File.WriteAllBytes(_tempVideoPath, videoBytes);
                        }

                        // Gắn URL cho Media Player và phát
                        axWindowsMediaPlayer1.URL = _tempVideoPath;
                        axWindowsMediaPlayer1.Ctlcontrols.play();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi giải mã file video: " + ex.Message);
                    }
                }
                else
                {
                    MessageBox.Show("Bản ghi này bị lỗi hoặc không có dữ liệu hình ảnh.");
                }
            }
        }

        // 3. Dọn dẹp RAM và file rác khi tắt Form
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                // Dừng Media Player
                axWindowsMediaPlayer1.Ctlcontrols.stop();
                axWindowsMediaPlayer1.close();

                // Cố gắng xóa file tạm (Nên bao trong try-catch vì WMP có thể chưa nhả file ra ngay lập tức)
                if (!string.IsNullOrEmpty(_tempVideoPath) && File.Exists(_tempVideoPath))
                {
                    File.Delete(_tempVideoPath);
                }
            }
            catch { }

            base.OnFormClosing(e);
        }

        // Giữ lại các hàm rỗng này nếu Designer đang trỏ event vào chúng
        private void frmHistory_Load(object sender, EventArgs e) { }
        private void label1_Click(object sender, EventArgs e) { }
    }
}