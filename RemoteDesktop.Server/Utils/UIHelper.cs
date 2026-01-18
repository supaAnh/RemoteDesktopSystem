using System;
using System.Windows.Forms;

namespace RemoteDesktop.Server.Utils
{
    public static class UIHelper
    {

        public static void AppendLog(ListView lsv, string message)
        {
            // Kiểm tra nếu ListView hoặc Form chứa nó đã bị hủy
            if (lsv == null || lsv.IsDisposed) return;

            if (lsv.InvokeRequired)
            {
                lsv.Invoke(new Action(() => AppendLog(lsv, message)));
            }
            else
            {
                // Tạo dòng log với thời gian và nội dung
                var item = new ListViewItem(new[] {
                    DateTime.Now.ToString("HH:mm:ss"),
                    message
                });

                lsv.Items.Add(item);

                // Tự động cuộn xuống dòng mới nhất
                if (lsv.Items.Count > 0)
                {
                    lsv.Items[lsv.Items.Count - 1].EnsureVisible();
                }
            }
        }
    }
}