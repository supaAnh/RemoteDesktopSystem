namespace RemoteDesktop.Server
{
    partial class frmConnect
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lsvLog = new ListView();
            columnHeader1 = new ColumnHeader();
            columnHeader2 = new ColumnHeader();
            btnStart = new Button();
            textPortNum = new NumericUpDown();
            label2 = new Label();
            ((System.ComponentModel.ISupportInitialize)textPortNum).BeginInit();
            SuspendLayout();
            // 
            // lsvLog
            // 
            lsvLog.Columns.AddRange(new ColumnHeader[] { columnHeader1, columnHeader2 });
            lsvLog.Location = new Point(-2, 221);
            lsvLog.Name = "lsvLog";
            lsvLog.Size = new Size(804, 233);
            lsvLog.TabIndex = 0;
            lsvLog.UseCompatibleStateImageBehavior = false;
            lsvLog.View = View.Details;
            lsvLog.SelectedIndexChanged += lsvLog_SelectedIndexChanged;
            // 
            // columnHeader1
            // 
            columnHeader1.Text = "Thời gian";
            columnHeader1.Width = 150;
            // 
            // columnHeader2
            // 
            columnHeader2.Text = "Nội dung";
            columnHeader2.Width = 700;
            // 
            // btnStart
            // 
            btnStart.Location = new Point(298, 130);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(151, 48);
            btnStart.TabIndex = 1;
            btnStart.Text = "Khởi động Server";
            btnStart.UseVisualStyleBackColor = true;
            btnStart.Click += btnStart_Click;
            // 
            // textPortNum
            // 
            textPortNum.Location = new Point(298, 87);
            textPortNum.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            textPortNum.Minimum = new decimal(new int[] { 1024, 0, 0, 0 });
            textPortNum.Name = "textPortNum";
            textPortNum.Size = new Size(151, 27);
            textPortNum.TabIndex = 2;
            textPortNum.Value = new decimal(new int[] { 8000, 0, 0, 0 });
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 10F);
            label2.Location = new Point(331, 45);
            label2.Name = "label2";
            label2.Size = new Size(87, 23);
            label2.TabIndex = 4;
            label2.Text = "Chọn Port";
            label2.TextAlign = ContentAlignment.TopCenter;
            // 
            // frmConnect
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(label2);
            Controls.Add(textPortNum);
            Controls.Add(btnStart);
            Controls.Add(lsvLog);
            Name = "frmConnect";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "SERVER - Connect";
            ((System.ComponentModel.ISupportInitialize)textPortNum).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ListView lsvLog;
        private ColumnHeader columnHeader1;
        private ColumnHeader columnHeader2;
        private Button btnStart;
        private NumericUpDown textPortNum;
        private Label label2;
    }
}
