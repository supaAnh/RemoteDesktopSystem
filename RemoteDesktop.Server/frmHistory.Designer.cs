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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmHistory));
            listBox1 = new ListBox();
            label2 = new Label();
            comboBoxRecord = new ComboBox();
            axWindowsMediaPlayer1 = new AxWMPLib.AxWindowsMediaPlayer();
            ((System.ComponentModel.ISupportInitialize)axWindowsMediaPlayer1).BeginInit();
            SuspendLayout();
            // 
            // listBox1
            // 
            listBox1.FormattingEnabled = true;
            listBox1.Location = new Point(12, 54);
            listBox1.Name = "listBox1";
            listBox1.Size = new Size(173, 384);
            listBox1.TabIndex = 1;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 12);
            label2.Name = "label2";
            label2.Size = new Size(109, 20);
            label2.TabIndex = 4;
            label2.Text = "Các bản record";
            // 
            // comboBoxRecord
            // 
            comboBoxRecord.FormattingEnabled = true;
            comboBoxRecord.Location = new Point(191, 12);
            comboBoxRecord.Name = "comboBoxRecord";
            comboBoxRecord.Size = new Size(212, 28);
            comboBoxRecord.TabIndex = 5;
            // 
            // axWindowsMediaPlayer1
            // 
            axWindowsMediaPlayer1.Enabled = true;
            axWindowsMediaPlayer1.Location = new Point(191, 54);
            axWindowsMediaPlayer1.Name = "axWindowsMediaPlayer1";
            axWindowsMediaPlayer1.OcxState = (AxHost.State)resources.GetObject("axWindowsMediaPlayer1.OcxState");
            axWindowsMediaPlayer1.Size = new Size(648, 384);
            axWindowsMediaPlayer1.TabIndex = 6;
            // 
            // frmHistory
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(851, 450);
            Controls.Add(axWindowsMediaPlayer1);
            Controls.Add(comboBoxRecord);
            Controls.Add(label2);
            Controls.Add(listBox1);
            Name = "frmHistory";
            Text = "frmHistory";
            Load += frmHistory_Load;
            ((System.ComponentModel.ISupportInitialize)axWindowsMediaPlayer1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private ListBox listBox1;
        private Label label2;
        private ComboBox comboBoxRecord;
        private AxWMPLib.AxWindowsMediaPlayer axWindowsMediaPlayer1;
    }
}