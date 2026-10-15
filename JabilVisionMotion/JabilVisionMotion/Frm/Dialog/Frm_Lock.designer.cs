namespace JabilVisionMotion.Frm.Dialog
{
    partial class Frm_Lock
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Frm_Lock));
            this.btn_unlock = new System.Windows.Forms.Button();
            this.tbx_password = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // btn_unlock
            // 
            this.btn_unlock.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_unlock.Font = new System.Drawing.Font("宋体", 9F);
            this.btn_unlock.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.btn_unlock.Location = new System.Drawing.Point(160, 52);
            this.btn_unlock.Name = "btn_unlock";
            this.btn_unlock.Size = new System.Drawing.Size(50, 26);
            this.btn_unlock.TabIndex = 1;
            this.btn_unlock.Text = "解锁";
            this.btn_unlock.UseVisualStyleBackColor = true;
            this.btn_unlock.Click += new System.EventHandler(this.Btn_Unlock_Click);
            // 
            // tbx_password
            // 
            this.tbx_password.Font = new System.Drawing.Font("宋体", 12F);
            this.tbx_password.Location = new System.Drawing.Point(20, 52);
            this.tbx_password.Name = "tbx_password";
            this.tbx_password.PasswordChar = '*';
            this.tbx_password.Size = new System.Drawing.Size(137, 26);
            this.tbx_password.TabIndex = 2;
            this.tbx_password.Click += new System.EventHandler(this.Tbx_Password_Click);
            this.tbx_password.TextChanged += new System.EventHandler(this.Tbx_Password_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("宋体", 12F);
            this.label1.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label1.Location = new System.Drawing.Point(47, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(104, 16);
            this.label1.TabIndex = 0;
            this.label1.Text = "锁定中......";
            this.label1.Click += new System.EventHandler(this.Frm_Lock_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.pictureBox1.Location = new System.Drawing.Point(1, 1);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(36, 33);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 3;
            this.pictureBox1.TabStop = false;
            // 
            // Frm_Lock
            // 
            this.AcceptButton = this.btn_unlock;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.ClientSize = new System.Drawing.Size(227, 99);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.tbx_password);
            this.Controls.Add(this.btn_unlock);
            this.Controls.Add(this.label1);
            this.DoubleBuffered = true;
            this.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "Frm_Lock";
            this.Opacity = 0.6D;
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Frm_Lock";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Frm_Lock_FormClosing);
            this.Load += new System.EventHandler(this.Frm_Lock_Load);
            this.Shown += new System.EventHandler(this.Frm_Lock_Shown);
            this.Click += new System.EventHandler(this.Frm_Lock_Click);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btn_unlock;
        private System.Windows.Forms.TextBox tbx_password;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.PictureBox pictureBox1;
    }
}