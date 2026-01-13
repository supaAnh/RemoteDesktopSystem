namespace RemoteDesktop.Client
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
            pnlSideBar = new Panel();
            btnSendFile = new Button();
            btnSendChat = new Button();
            txtChatInput = new TextBox();
            txtChatHistory = new TextBox();
            picScreen = new PictureBox();
            lsvLog = new ListView();
            openFileDialog1 = new OpenFileDialog();
            btnDisconnect = new Button();
            pnlSideBar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picScreen).BeginInit();
            SuspendLayout();
            // 
            // pnlSideBar
            // 
            pnlSideBar.Controls.Add(btnSendFile);
            pnlSideBar.Controls.Add(btnSendChat);
            pnlSideBar.Controls.Add(txtChatInput);
            pnlSideBar.Controls.Add(txtChatHistory);
            pnlSideBar.Location = new Point(629, 50);
            pnlSideBar.Name = "pnlSideBar";
            pnlSideBar.Size = new Size(415, 401);
            pnlSideBar.TabIndex = 5;
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
            txtChatHistory.Location = new Point(18, 13);
            txtChatHistory.Multiline = true;
            txtChatHistory.Name = "txtChatHistory";
            txtChatHistory.ReadOnly = true;
            txtChatHistory.Size = new Size(384, 308);
            txtChatHistory.TabIndex = 0;
            // 
            // picScreen
            // 
            picScreen.Location = new Point(12, 50);
            picScreen.Name = "picScreen";
            picScreen.Size = new Size(611, 401);
            picScreen.TabIndex = 6;
            picScreen.TabStop = false;
            // 
            // lsvLog
            // 
            lsvLog.Location = new Point(12, 457);
            lsvLog.Name = "lsvLog";
            lsvLog.Size = new Size(1032, 189);
            lsvLog.TabIndex = 7;
            lsvLog.UseCompatibleStateImageBehavior = false;
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            // 
            // btnDisconnect
            // 
            btnDisconnect.Font = new Font("Segoe UI", 10F);
            btnDisconnect.Location = new Point(923, 10);
            btnDisconnect.Name = "btnDisconnect";
            btnDisconnect.Size = new Size(121, 34);
            btnDisconnect.TabIndex = 8;
            btnDisconnect.Text = "Ngắt kết nối";
            btnDisconnect.UseVisualStyleBackColor = true;
            btnDisconnect.Click += btnDisconnect_Click;
            // 
            // frmRemote
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1056, 658);
            Controls.Add(btnDisconnect);
            Controls.Add(lsvLog);
            Controls.Add(picScreen);
            Controls.Add(pnlSideBar);
            Name = "frmRemote";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "CLIENT - Remote";
            pnlSideBar.ResumeLayout(false);
            pnlSideBar.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)picScreen).EndInit();
            ResumeLayout(false);
        }

        #endregion
        private Panel pnlSideBar;
        private Button btnSendFile;
        private Button btnSendChat;
        private TextBox txtChatInput;
        private TextBox txtChatHistory;
        private PictureBox picScreen;
        private ListView lsvLog;
        private OpenFileDialog openFileDialog1;
        private Button btnDisconnect;
    }
}