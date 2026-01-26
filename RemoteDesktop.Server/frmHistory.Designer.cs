namespace RemoteDesktop.Server
{
    partial class frmHistory
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            pictureBox1 = new PictureBox();
            listBox1 = new ListBox();
            label1 = new Label();
            cboSessions = new ComboBox();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // pictureBox1
            // 
            pictureBox1.Location = new Point(253, 49);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(586, 345);
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // listBox1
            // 
            listBox1.FormattingEnabled = true;
            listBox1.Location = new Point(12, 49);
            listBox1.Name = "listBox1";
            listBox1.Size = new Size(173, 344);
            listBox1.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 9);
            label1.Name = "label1";
            label1.Size = new Size(136, 20);
            label1.TabIndex = 2;
            label1.Text = "Lịch Sử Đăng Nhập";
            label1.Click += label1_Click;
            // 
            // cboSessions
            // 
            cboSessions.FormattingEnabled = true;
            cboSessions.Location = new Point(154, 6);
            cboSessions.Name = "cboSessions";
            cboSessions.Size = new Size(212, 28);
            cboSessions.TabIndex = 3;
            // 
            // frmHistory
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(851, 450);
            Controls.Add(cboSessions);
            Controls.Add(label1);
            Controls.Add(listBox1);
            Controls.Add(pictureBox1);
            Name = "frmHistory";
            Text = "frmHistory";
            Load += frmHistory_Load;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private PictureBox pictureBox1;
        private ListBox listBox1;
        private Label label1;
        private ComboBox cboSessions;
    }
}