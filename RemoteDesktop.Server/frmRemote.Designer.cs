namespace RemoteDesktop.Server
{
    partial class frmRemote
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
            lblStatus = new Label();
            pnlSideBar = new Panel();
            btnSendFile = new Button();
            btnSendChat = new Button();
            txtChatInput = new TextBox();
            txtChatHistory = new TextBox();
            btnStopRemote = new Button();
            lsvLog = new ListView();
            openFileDialog1 = new OpenFileDialog();
            pnlSideBar.SuspendLayout();
            SuspendLayout();
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(12, 5);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(24, 20);
            lblStatus.TabIndex = 0;
            lblStatus.Text = "IP:";
            // 
            // pnlSideBar
            // 
            pnlSideBar.Controls.Add(btnSendFile);
            pnlSideBar.Controls.Add(btnSendChat);
            pnlSideBar.Controls.Add(txtChatInput);
            pnlSideBar.Controls.Add(txtChatHistory);
            pnlSideBar.Location = new Point(12, 48);
            pnlSideBar.Name = "pnlSideBar";
            pnlSideBar.Size = new Size(415, 401);
            pnlSideBar.TabIndex = 2;
            // 
            // btnSendFile
            // 
            btnSendFile.Location = new Point(333, 327);
            btnSendFile.Name = "btnSendFile";
            btnSendFile.Size = new Size(69, 59);
            btnSendFile.TabIndex = 3;
            btnSendFile.Text = "FILE";
            btnSendFile.UseVisualStyleBackColor = true;
            btnSendFile.Click += btnSendFile_Click;
            // 
            // btnSendChat
            // 
            btnSendChat.Location = new Point(258, 327);
            btnSendChat.Name = "btnSendChat";
            btnSendChat.Size = new Size(69, 59);
            btnSendChat.TabIndex = 2;
            btnSendChat.Text = "GỬI";
            btnSendChat.UseVisualStyleBackColor = true;
            btnSendChat.Click += btnSendChat_Click;
            // 
            // txtChatInput
            // 
            txtChatInput.Location = new Point(18, 327);
            txtChatInput.Multiline = true;
            txtChatInput.Name = "txtChatInput";
            txtChatInput.Size = new Size(234, 59);
            txtChatInput.TabIndex = 1;
            // 
            // txtChatHistory
            // 
            txtChatHistory.Location = new Point(17, 18);
            txtChatHistory.Multiline = true;
            txtChatHistory.Name = "txtChatHistory";
            txtChatHistory.Size = new Size(385, 303);
            txtChatHistory.TabIndex = 0;
            // 
            // btnStopRemote
            // 
            btnStopRemote.Font = new Font("Segoe UI", 10F);
            btnStopRemote.Location = new Point(306, 8);
            btnStopRemote.Name = "btnStopRemote";
            btnStopRemote.Size = new Size(121, 34);
            btnStopRemote.TabIndex = 3;
            btnStopRemote.Text = "Ngắt kết nối";
            btnStopRemote.UseVisualStyleBackColor = true;
            // 
            // lsvLog
            // 
            lsvLog.Location = new Point(12, 440);
            lsvLog.Name = "lsvLog";
            lsvLog.Size = new Size(415, 184);
            lsvLog.TabIndex = 4;
            lsvLog.UseCompatibleStateImageBehavior = false;
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            // 
            // frmRemote
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(439, 639);
            Controls.Add(lsvLog);
            Controls.Add(btnStopRemote);
            Controls.Add(pnlSideBar);
            Controls.Add(lblStatus);
            Name = "frmRemote";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "SERVER - Remote";
            pnlSideBar.ResumeLayout(false);
            pnlSideBar.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblStatus;
        private Panel pnlSideBar;
        private Button btnSendFile;
        private Button btnSendChat;
        private TextBox txtChatInput;
        private TextBox txtChatHistory;
        private Button btnStopRemote;
        private ListView lsvLog;
        private OpenFileDialog openFileDialog1;
    }
}