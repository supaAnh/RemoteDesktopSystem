namespace RemoteDesktop.Client
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
            label1 = new Label();
            label2 = new Label();
            txtIP = new TextBox();
            textPortNum = new NumericUpDown();
            btnConnect = new Button();
            ((System.ComponentModel.ISupportInitialize)textPortNum).BeginInit();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 10F);
            label1.Location = new Point(103, 64);
            label1.Name = "label1";
            label1.Size = new Size(34, 23);
            label1.TabIndex = 0;
            label1.Text = "IP: ";
            label1.Click += label1_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 10F);
            label2.Location = new Point(103, 109);
            label2.Name = "label2";
            label2.Size = new Size(50, 23);
            label2.TabIndex = 1;
            label2.Text = "Port: ";
            // 
            // txtIP
            // 
            txtIP.Location = new Point(169, 64);
            txtIP.Name = "txtIP";
            txtIP.Size = new Size(167, 27);
            txtIP.TabIndex = 2;
            txtIP.Text = "127.0.0.1";
            // 
            // textPortNum
            // 
            textPortNum.Location = new Point(169, 109);
            textPortNum.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            textPortNum.Minimum = new decimal(new int[] { 1024, 0, 0, 0 });
            textPortNum.Name = "textPortNum";
            textPortNum.Size = new Size(167, 27);
            textPortNum.TabIndex = 3;
            textPortNum.Value = new decimal(new int[] { 8000, 0, 0, 0 });
            // 
            // btnConnect
            // 
            btnConnect.Location = new Point(169, 191);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(167, 50);
            btnConnect.TabIndex = 4;
            btnConnect.Text = "Kết nối";
            btnConnect.UseVisualStyleBackColor = true;
            btnConnect.Click += btnConnect_Click;
            // 
            // frmConnect
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(466, 300);
            Controls.Add(btnConnect);
            Controls.Add(textPortNum);
            Controls.Add(txtIP);
            Controls.Add(label2);
            Controls.Add(label1);
            Name = "frmConnect";
            SizeGripStyle = SizeGripStyle.Hide;
            Text = "CLIENT - Connect";
            ((System.ComponentModel.ISupportInitialize)textPortNum).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private Label label2;
        private TextBox txtIP;
        private NumericUpDown textPortNum;
        private Button btnConnect;
    }
}
