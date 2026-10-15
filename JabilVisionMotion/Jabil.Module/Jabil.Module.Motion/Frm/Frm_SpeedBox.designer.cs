namespace Jabil.Module.Motion.Frm
{
    partial class Frm_SpeedBox
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
            this.uiPanel_window = new Sunny.UI.UIPanel();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.lbl_info = new System.Windows.Forms.Label();
            this.uiPanel1 = new Sunny.UI.UIPanel();
            this.uiTrackBar_速度比例 = new Sunny.UI.UITrackBar();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.label56 = new System.Windows.Forms.Label();
            this.lab_速度比例显示 = new System.Windows.Forms.Label();
            this.btn_cancel = new Sunny.UI.UIButton();
            this.uiPanel_window.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.uiPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // uiPanel_window
            // 
            this.uiPanel_window.Controls.Add(this.tableLayoutPanel1);
            this.uiPanel_window.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiPanel_window.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.uiPanel_window.FillColor2 = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.uiPanel_window.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiPanel_window.Location = new System.Drawing.Point(0, 0);
            this.uiPanel_window.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.uiPanel_window.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiPanel_window.Name = "uiPanel_window";
            this.uiPanel_window.RectColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(45)))), ((int)(((byte)(80)))));
            this.uiPanel_window.Size = new System.Drawing.Size(500, 250);
            this.uiPanel_window.Style = Sunny.UI.UIStyle.Custom;
            this.uiPanel_window.TabIndex = 112;
            this.uiPanel_window.Text = null;
            this.uiPanel_window.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.BackColor = System.Drawing.Color.Transparent;
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.lbl_info, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.uiPanel1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 2);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(500, 250);
            this.tableLayoutPanel1.TabIndex = 110;
            // 
            // lbl_info
            // 
            this.lbl_info.AutoEllipsis = true;
            this.lbl_info.AutoSize = true;
            this.lbl_info.BackColor = System.Drawing.Color.Transparent;
            this.lbl_info.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbl_info.Font = new System.Drawing.Font("微软雅黑", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lbl_info.ForeColor = System.Drawing.Color.White;
            this.lbl_info.Location = new System.Drawing.Point(3, 0);
            this.lbl_info.Name = "lbl_info";
            this.lbl_info.Size = new System.Drawing.Size(494, 75);
            this.lbl_info.TabIndex = 109;
            this.lbl_info.Text = "请选择轴运行的速度";
            this.lbl_info.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // uiPanel1
            // 
            this.uiPanel1.Controls.Add(this.uiTrackBar_速度比例);
            this.uiPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiPanel1.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(75)))), ((int)(((byte)(130)))));
            this.uiPanel1.FillColor2 = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(75)))), ((int)(((byte)(130)))));
            this.uiPanel1.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiPanel1.Location = new System.Drawing.Point(20, 80);
            this.uiPanel1.Margin = new System.Windows.Forms.Padding(20, 5, 20, 5);
            this.uiPanel1.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiPanel1.Name = "uiPanel1";
            this.uiPanel1.RectColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(65)))), ((int)(((byte)(120)))));
            this.uiPanel1.Size = new System.Drawing.Size(460, 90);
            this.uiPanel1.Style = Sunny.UI.UIStyle.Custom;
            this.uiPanel1.TabIndex = 110;
            this.uiPanel1.Text = null;
            this.uiPanel1.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // uiTrackBar_速度比例
            // 
            this.uiTrackBar_速度比例.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiTrackBar_速度比例.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(95)))), ((int)(((byte)(150)))));
            this.uiTrackBar_速度比例.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiTrackBar_速度比例.ForeColor = System.Drawing.Color.White;
            this.uiTrackBar_速度比例.Location = new System.Drawing.Point(0, 0);
            this.uiTrackBar_速度比例.Maximum = 150;
            this.uiTrackBar_速度比例.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiTrackBar_速度比例.Name = "uiTrackBar_速度比例";
            this.uiTrackBar_速度比例.Size = new System.Drawing.Size(460, 90);
            this.uiTrackBar_速度比例.Style = Sunny.UI.UIStyle.Custom;
            this.uiTrackBar_速度比例.TabIndex = 3;
            this.uiTrackBar_速度比例.Text = "uiTrackBar_速度比例";
            this.uiTrackBar_速度比例.Value = 100;
            this.uiTrackBar_速度比例.ValueChanged += new System.EventHandler(this.uiTrackBar_速度比例_ValueChanged);
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 3;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tableLayoutPanel2.Controls.Add(this.label56, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.lab_速度比例显示, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.btn_cancel, 2, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 178);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(494, 69);
            this.tableLayoutPanel2.TabIndex = 0;
            // 
            // label56
            // 
            this.label56.AutoSize = true;
            this.label56.BackColor = System.Drawing.Color.Transparent;
            this.label56.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label56.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label56.ForeColor = System.Drawing.Color.White;
            this.label56.Location = new System.Drawing.Point(15, 0);
            this.label56.Margin = new System.Windows.Forms.Padding(15, 0, 2, 0);
            this.label56.Name = "label56";
            this.label56.Size = new System.Drawing.Size(180, 69);
            this.label56.TabIndex = 106;
            this.label56.Text = "速度比例：";
            this.label56.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lab_速度比例显示
            // 
            this.lab_速度比例显示.AutoSize = true;
            this.lab_速度比例显示.BackColor = System.Drawing.Color.Transparent;
            this.lab_速度比例显示.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lab_速度比例显示.Font = new System.Drawing.Font("微软雅黑", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lab_速度比例显示.ForeColor = System.Drawing.Color.Gold;
            this.lab_速度比例显示.Location = new System.Drawing.Point(200, 0);
            this.lab_速度比例显示.Name = "lab_速度比例显示";
            this.lab_速度比例显示.Size = new System.Drawing.Size(142, 69);
            this.lab_速度比例显示.TabIndex = 107;
            this.lab_速度比例显示.Text = "100%";
            this.lab_速度比例显示.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btn_cancel
            // 
            this.btn_cancel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_cancel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_cancel.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(95)))), ((int)(((byte)(150)))));
            this.btn_cancel.FillColor2 = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(75)))), ((int)(((byte)(130)))));
            this.btn_cancel.FillColorGradient = true;
            this.btn_cancel.Font = new System.Drawing.Font("微软雅黑", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_cancel.Location = new System.Drawing.Point(348, 10);
            this.btn_cancel.Margin = new System.Windows.Forms.Padding(3, 10, 20, 10);
            this.btn_cancel.MinimumSize = new System.Drawing.Size(1, 1);
            this.btn_cancel.Name = "btn_cancel";
            this.btn_cancel.RectColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(85)))), ((int)(((byte)(140)))));
            this.btn_cancel.Size = new System.Drawing.Size(126, 49);
            this.btn_cancel.Style = Sunny.UI.UIStyle.Custom;
            this.btn_cancel.TabIndex = 2;
            this.btn_cancel.Text = "确定";
            this.btn_cancel.TipsFont = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_cancel.Click += new System.EventHandler(this.btn_cancel_Click);
            // 
            // Frm_SpeedBox
            // 
            this.AllowShowTitle = false;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.ClientSize = new System.Drawing.Size(500, 250);
            this.ControlBox = false;
            this.Controls.Add(this.uiPanel_window);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Frm_SpeedBox";
            this.Padding = new System.Windows.Forms.Padding(0);
            this.ShowTitle = false;
            this.Style = Sunny.UI.UIStyle.Custom;
            this.ZoomScaleRect = new System.Drawing.Rectangle(15, 15, 500, 250);
            this.Load += new System.EventHandler(this.Frm_SpeedBox_Load);
            this.uiPanel_window.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.uiPanel1.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private Sunny.UI.UIPanel uiPanel_window;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private Sunny.UI.UIPanel uiPanel1;
        public Sunny.UI.UITrackBar uiTrackBar_速度比例;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Label label56;
        public System.Windows.Forms.Label lab_速度比例显示;
        private Sunny.UI.UIButton btn_cancel;
        public System.Windows.Forms.Label lbl_info;
    }
}