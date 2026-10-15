namespace JabilVisionMotion.Frm.Dialog
{
    partial class Frm_AlarmWindow
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
            this.uiPanel1 = new Sunny.UI.UIPanel();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.label_报警状态 = new Sunny.UI.UILabel();
            this.uiTextBox1 = new Sunny.UI.UITextBox();
            this.btn_手动复位 = new Sunny.UI.UIButton();
            this.chk_拍照NG强制跳过 = new Sunny.UI.UICheckBox();
            this.uiPanel1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // uiPanel1
            // 
            this.uiPanel1.Controls.Add(this.tableLayoutPanel1);
            this.uiPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiPanel1.FillColor = System.Drawing.Color.Red;
            this.uiPanel1.FillColor2 = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.uiPanel1.FillColorGradient = true;
            this.uiPanel1.FillColorGradientDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.uiPanel1.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiPanel1.Location = new System.Drawing.Point(0, 0);
            this.uiPanel1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.uiPanel1.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiPanel1.Name = "uiPanel1";
            this.uiPanel1.RectColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.uiPanel1.Size = new System.Drawing.Size(800, 141);
            this.uiPanel1.Style = Sunny.UI.UIStyle.Custom;
            this.uiPanel1.TabIndex = 0;
            this.uiPanel1.Text = null;
            this.uiPanel1.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 74.375F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25.625F));
            this.tableLayoutPanel1.Controls.Add(this.label_报警状态, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.uiTextBox1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.btn_手动复位, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.chk_拍照NG强制跳过, 1, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 30.49645F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 69.50355F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(800, 141);
            this.tableLayoutPanel1.TabIndex = 15;
            // 
            // label_报警状态
            // 
            this.label_报警状态.BackColor = System.Drawing.Color.Transparent;
            this.label_报警状态.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label_报警状态.Font = new System.Drawing.Font("微软雅黑", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label_报警状态.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.label_报警状态.Location = new System.Drawing.Point(3, 0);
            this.label_报警状态.Name = "label_报警状态";
            this.label_报警状态.Size = new System.Drawing.Size(589, 42);
            this.label_报警状态.TabIndex = 11;
            this.label_报警状态.Text = "报警中......";
            this.label_报警状态.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.label_报警状态.MouseDown += new System.Windows.Forms.MouseEventHandler(this.panelTitle_MouseDown);
            // 
            // uiTextBox1
            // 
            this.uiTextBox1.Cursor = System.Windows.Forms.Cursors.Default;
            this.uiTextBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiTextBox1.FillReadOnlyColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.uiTextBox1.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiTextBox1.ForeReadOnlyColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.uiTextBox1.Location = new System.Drawing.Point(4, 47);
            this.uiTextBox1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.uiTextBox1.MinimumSize = new System.Drawing.Size(1, 16);
            this.uiTextBox1.Multiline = true;
            this.uiTextBox1.Name = "uiTextBox1";
            this.uiTextBox1.Padding = new System.Windows.Forms.Padding(5);
            this.uiTextBox1.RectReadOnlyColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.uiTextBox1.ShowText = false;
            this.uiTextBox1.Size = new System.Drawing.Size(587, 89);
            this.uiTextBox1.Style = Sunny.UI.UIStyle.Custom;
            this.uiTextBox1.TabIndex = 12;
            this.uiTextBox1.TextAlignment = System.Drawing.ContentAlignment.TopLeft;
            this.uiTextBox1.Watermark = "报警信息显示";
            // 
            // btn_手动复位
            // 
            this.btn_手动复位.BackColor = System.Drawing.Color.Transparent;
            this.btn_手动复位.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_手动复位.Dock = System.Windows.Forms.DockStyle.Top;
            this.btn_手动复位.FillColorGradient = true;
            this.btn_手动复位.Font = new System.Drawing.Font("微软雅黑", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_手动复位.Location = new System.Drawing.Point(598, 3);
            this.btn_手动复位.MinimumSize = new System.Drawing.Size(1, 1);
            this.btn_手动复位.Name = "btn_手动复位";
            this.btn_手动复位.Size = new System.Drawing.Size(199, 36);
            this.btn_手动复位.TabIndex = 13;
            this.btn_手动复位.Text = "复位按钮";
            this.btn_手动复位.TipsFont = new System.Drawing.Font("黑体", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_手动复位.TipsForeColor = System.Drawing.Color.Transparent;
            // 
            // chk_拍照NG强制跳过
            // 
            this.chk_拍照NG强制跳过.BackColor = System.Drawing.Color.Transparent;
            this.chk_拍照NG强制跳过.Cursor = System.Windows.Forms.Cursors.Hand;
            this.chk_拍照NG强制跳过.Dock = System.Windows.Forms.DockStyle.Top;
            this.chk_拍照NG强制跳过.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.chk_拍照NG强制跳过.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.chk_拍照NG强制跳过.Location = new System.Drawing.Point(598, 45);
            this.chk_拍照NG强制跳过.MinimumSize = new System.Drawing.Size(1, 1);
            this.chk_拍照NG强制跳过.Name = "chk_拍照NG强制跳过";
            this.chk_拍照NG强制跳过.Padding = new System.Windows.Forms.Padding(22, 0, 0, 0);
            this.chk_拍照NG强制跳过.Size = new System.Drawing.Size(199, 29);
            this.chk_拍照NG强制跳过.Style = Sunny.UI.UIStyle.Custom;
            this.chk_拍照NG强制跳过.TabIndex = 14;
            this.chk_拍照NG强制跳过.Text = "流程强制（一次性信号）";
            // 
            // Frm_AlarmWindow
            // 
            this.AllowShowTitle = false;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(800, 141);
            this.Controls.Add(this.uiPanel1);
            this.Name = "Frm_AlarmWindow";
            this.Padding = new System.Windows.Forms.Padding(0);
            this.ShowTitle = false;
            this.Text = "Frm_CheckAlarmWindow";
            this.ZoomScaleRect = new System.Drawing.Rectangle(15, 15, 800, 450);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Frm_AlarmWindow_FormClosing);
            this.Load += new System.EventHandler(this.Frm_AlarmWindow_Load);
            this.uiPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private Sunny.UI.UIPanel uiPanel1;
        public Sunny.UI.UIButton btn_手动复位;
        private Sunny.UI.UICheckBox chk_拍照NG强制跳过;
        private Sunny.UI.UITextBox uiTextBox1;
        private Sunny.UI.UILabel label_报警状态;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
    }
}