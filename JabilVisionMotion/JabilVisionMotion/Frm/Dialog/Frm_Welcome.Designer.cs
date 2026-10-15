namespace JabilVisionMotion.Frm.Dialog
{
    partial class Frm_Welcome
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Frm_Welcome));
            this.label1 = new System.Windows.Forms.Label();
            this.lbl_version = new System.Windows.Forms.Label();
            this.bar_step = new System.Windows.Forms.ProgressBar();
            this.lbl_step = new System.Windows.Forms.Label();
            this.btn_exit = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("微软雅黑", 12F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label1.Location = new System.Drawing.Point(12, 5);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(218, 22);
            this.label1.TabIndex = 13;
            this.label1.Text = "捷普（中国）自动化有限公司";
            // 
            // lbl_version
            // 
            this.lbl_version.AutoSize = true;
            this.lbl_version.BackColor = System.Drawing.Color.Transparent;
            this.lbl_version.Font = new System.Drawing.Font("微软雅黑", 14.25F);
            this.lbl_version.ForeColor = System.Drawing.Color.White;
            this.lbl_version.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lbl_version.Location = new System.Drawing.Point(708, 406);
            this.lbl_version.Name = "lbl_version";
            this.lbl_version.Size = new System.Drawing.Size(81, 25);
            this.lbl_version.TabIndex = 14;
            this.lbl_version.Text = "Version";
            this.lbl_version.Visible = false;
            // 
            // bar_step
            // 
            this.bar_step.BackColor = System.Drawing.Color.White;
            this.bar_step.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.bar_step.ForeColor = System.Drawing.Color.Red;
            this.bar_step.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.bar_step.Location = new System.Drawing.Point(0, 433);
            this.bar_step.Name = "bar_step";
            this.bar_step.Size = new System.Drawing.Size(789, 11);
            this.bar_step.TabIndex = 15;
            // 
            // lbl_step
            // 
            this.lbl_step.AutoSize = true;
            this.lbl_step.BackColor = System.Drawing.Color.Transparent;
            this.lbl_step.Font = new System.Drawing.Font("新宋体", 15F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lbl_step.ForeColor = System.Drawing.Color.Gray;
            this.lbl_step.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lbl_step.Location = new System.Drawing.Point(12, 409);
            this.lbl_step.Name = "lbl_step";
            this.lbl_step.Size = new System.Drawing.Size(180, 20);
            this.lbl_step.TabIndex = 16;
            this.lbl_step.Text = "正在初始化......";
            // 
            // btn_exit
            // 
            this.btn_exit.BackColor = System.Drawing.Color.Transparent;
            this.btn_exit.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_exit.FlatAppearance.BorderSize = 0;
            this.btn_exit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_exit.Font = new System.Drawing.Font("黑体", 15F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_exit.ForeColor = System.Drawing.Color.White;
            this.btn_exit.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.btn_exit.Location = new System.Drawing.Point(764, 0);
            this.btn_exit.Name = "btn_exit";
            this.btn_exit.Size = new System.Drawing.Size(25, 27);
            this.btn_exit.TabIndex = 17;
            this.btn_exit.TabStop = false;
            this.btn_exit.Text = "×";
            this.btn_exit.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.btn_exit.UseVisualStyleBackColor = false;
            this.btn_exit.Click += new System.EventHandler(this.Btn_Exit_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackColor = System.Drawing.Color.Transparent;
            this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBox1.Image = global::JabilVisionMotion.Properties.Resources.屏保;
            this.pictureBox1.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.pictureBox1.InitialImage = ((System.Drawing.Image)(resources.GetObject("pictureBox1.InitialImage")));
            this.pictureBox1.Location = new System.Drawing.Point(0, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(789, 444);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 4;
            this.pictureBox1.TabStop = false;
            // 
            // Frm_Welcome
            // 
            this.AllowShowTitle = false;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackgroundImage = global::JabilVisionMotion.Properties.Resources.屏保;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(789, 444);
            this.Controls.Add(this.lbl_step);
            this.Controls.Add(this.btn_exit);
            this.Controls.Add(this.bar_step);
            this.Controls.Add(this.lbl_version);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pictureBox1);
            this.Name = "Frm_Welcome";
            this.Padding = new System.Windows.Forms.Padding(0);
            this.ShowTitle = false;
            this.Text = "欢迎使用JabilVisionMotion";
            this.ZoomScaleRect = new System.Drawing.Rectangle(15, 15, 800, 450);
            this.Load += new System.EventHandler(this.Frm_Welcome_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        public System.Windows.Forms.Label label1;
        public System.Windows.Forms.Label lbl_version;
        public System.Windows.Forms.ProgressBar bar_step;
        public System.Windows.Forms.Label lbl_step;
        private System.Windows.Forms.Button btn_exit;
    }
}