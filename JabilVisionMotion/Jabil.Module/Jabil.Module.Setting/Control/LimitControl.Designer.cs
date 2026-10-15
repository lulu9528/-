namespace Jabil.Module.Setting.Control
{
    partial class LimitControl
    {
        /// <summary> 
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.uiTitlePanel_FAI = new Sunny.UI.UITitlePanel();
            this.txt_下限 = new Sunny.UI.UITextBox();
            this.txt_上限 = new Sunny.UI.UITextBox();
            this.txt_标准值 = new Sunny.UI.UITextBox();
            this.uiLabel1 = new Sunny.UI.UILabel();
            this.uiLabel2 = new Sunny.UI.UILabel();
            this.uiLabel3 = new Sunny.UI.UILabel();
            this.uiTitlePanel_FAI.SuspendLayout();
            this.SuspendLayout();
            // 
            // uiTitlePanel_FAI
            // 
            this.uiTitlePanel_FAI.Controls.Add(this.txt_下限);
            this.uiTitlePanel_FAI.Controls.Add(this.txt_上限);
            this.uiTitlePanel_FAI.Controls.Add(this.txt_标准值);
            this.uiTitlePanel_FAI.Controls.Add(this.uiLabel1);
            this.uiTitlePanel_FAI.Controls.Add(this.uiLabel2);
            this.uiTitlePanel_FAI.Controls.Add(this.uiLabel3);
            this.uiTitlePanel_FAI.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiTitlePanel_FAI.Font = new System.Drawing.Font("宋体", 12F);
            this.uiTitlePanel_FAI.Location = new System.Drawing.Point(0, 0);
            this.uiTitlePanel_FAI.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.uiTitlePanel_FAI.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiTitlePanel_FAI.Name = "uiTitlePanel_FAI";
            this.uiTitlePanel_FAI.Padding = new System.Windows.Forms.Padding(0, 35, 0, 0);
            this.uiTitlePanel_FAI.ShowText = false;
            this.uiTitlePanel_FAI.Size = new System.Drawing.Size(196, 145);
            this.uiTitlePanel_FAI.TabIndex = 48;
            this.uiTitlePanel_FAI.Text = "标准值";
            this.uiTitlePanel_FAI.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            this.uiTitlePanel_FAI.TitleHeight = 30;
            // 
            // txt_下限
            // 
            this.txt_下限.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txt_下限.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.txt_下限.Location = new System.Drawing.Point(94, 107);
            this.txt_下限.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txt_下限.MinimumSize = new System.Drawing.Size(1, 16);
            this.txt_下限.Name = "txt_下限";
            this.txt_下限.Padding = new System.Windows.Forms.Padding(5);
            this.txt_下限.ShowText = false;
            this.txt_下限.Size = new System.Drawing.Size(95, 29);
            this.txt_下限.TabIndex = 40;
            this.txt_下限.Text = "0";
            this.txt_下限.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            this.txt_下限.Watermark = "";
            // 
            // txt_上限
            // 
            this.txt_上限.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txt_上限.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.txt_上限.Location = new System.Drawing.Point(94, 74);
            this.txt_上限.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txt_上限.MinimumSize = new System.Drawing.Size(1, 16);
            this.txt_上限.Name = "txt_上限";
            this.txt_上限.Padding = new System.Windows.Forms.Padding(5);
            this.txt_上限.ShowText = false;
            this.txt_上限.Size = new System.Drawing.Size(95, 29);
            this.txt_上限.TabIndex = 39;
            this.txt_上限.Text = "0";
            this.txt_上限.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            this.txt_上限.Watermark = "";
            // 
            // txt_标准值
            // 
            this.txt_标准值.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txt_标准值.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.txt_标准值.Location = new System.Drawing.Point(94, 41);
            this.txt_标准值.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txt_标准值.MinimumSize = new System.Drawing.Size(1, 16);
            this.txt_标准值.Name = "txt_标准值";
            this.txt_标准值.Padding = new System.Windows.Forms.Padding(5);
            this.txt_标准值.ShowText = false;
            this.txt_标准值.Size = new System.Drawing.Size(95, 29);
            this.txt_标准值.TabIndex = 38;
            this.txt_标准值.Text = "0";
            this.txt_标准值.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            this.txt_标准值.Watermark = "";
            // 
            // uiLabel1
            // 
            this.uiLabel1.AutoSize = true;
            this.uiLabel1.Font = new System.Drawing.Font("宋体", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiLabel1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.uiLabel1.Location = new System.Drawing.Point(3, 107);
            this.uiLabel1.Name = "uiLabel1";
            this.uiLabel1.Size = new System.Drawing.Size(32, 21);
            this.uiLabel1.TabIndex = 37;
            this.uiLabel1.Text = "R:";
            this.uiLabel1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // uiLabel2
            // 
            this.uiLabel2.AutoSize = true;
            this.uiLabel2.Font = new System.Drawing.Font("宋体", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiLabel2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.uiLabel2.Location = new System.Drawing.Point(3, 74);
            this.uiLabel2.Name = "uiLabel2";
            this.uiLabel2.Size = new System.Drawing.Size(32, 21);
            this.uiLabel2.TabIndex = 36;
            this.uiLabel2.Text = "Y:";
            this.uiLabel2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // uiLabel3
            // 
            this.uiLabel3.AutoSize = true;
            this.uiLabel3.Font = new System.Drawing.Font("宋体", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiLabel3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.uiLabel3.Location = new System.Drawing.Point(3, 41);
            this.uiLabel3.Name = "uiLabel3";
            this.uiLabel3.Size = new System.Drawing.Size(32, 21);
            this.uiLabel3.TabIndex = 35;
            this.uiLabel3.Text = "X:";
            this.uiLabel3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // LimitControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.uiTitlePanel_FAI);
            this.Name = "LimitControl";
            this.Size = new System.Drawing.Size(196, 145);
            this.uiTitlePanel_FAI.ResumeLayout(false);
            this.uiTitlePanel_FAI.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        public Sunny.UI.UITextBox txt_下限;
        public Sunny.UI.UITextBox txt_上限;
        public Sunny.UI.UITextBox txt_标准值;
        private Sunny.UI.UILabel uiLabel1;
        private Sunny.UI.UILabel uiLabel2;
        private Sunny.UI.UILabel uiLabel3;
        public Sunny.UI.UITitlePanel uiTitlePanel_FAI;
    }
}
