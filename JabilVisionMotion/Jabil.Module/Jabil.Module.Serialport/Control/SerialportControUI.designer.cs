namespace Jabil.Module.Serialport.Control
{
    partial class SerialportControUI
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
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.uiTabControl1 = new Sunny.UI.UITabControl();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.uiSplitContainer1 = new Sunny.UI.UISplitContainer();
            this.uiGroupBox1 = new Sunny.UI.UIGroupBox();
            this.tbl_DI = new Sunny.UI.UITableLayoutPanel();
            this.uiGroupBox2 = new Sunny.UI.UIGroupBox();
            this.tbl_DO = new Sunny.UI.UITableLayoutPanel();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.uiPanel_信号模拟 = new Sunny.UI.UIPanel();
            this.uiTabControl1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.uiSplitContainer1)).BeginInit();
            this.uiSplitContainer1.Panel1.SuspendLayout();
            this.uiSplitContainer1.Panel2.SuspendLayout();
            this.uiSplitContainer1.SuspendLayout();
            this.uiGroupBox1.SuspendLayout();
            this.uiGroupBox2.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.SuspendLayout();
            // 
            // uiTabControl1
            // 
            this.uiTabControl1.Controls.Add(this.tabPage2);
            this.uiTabControl1.Controls.Add(this.tabPage1);
            this.uiTabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiTabControl1.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            this.uiTabControl1.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.uiTabControl1.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiTabControl1.ItemSize = new System.Drawing.Size(150, 40);
            this.uiTabControl1.Location = new System.Drawing.Point(0, 0);
            this.uiTabControl1.MainPage = "";
            this.uiTabControl1.MenuStyle = Sunny.UI.UIMenuStyle.Custom;
            this.uiTabControl1.Multiline = true;
            this.uiTabControl1.Name = "uiTabControl1";
            this.uiTabControl1.SelectedIndex = 0;
            this.uiTabControl1.Size = new System.Drawing.Size(862, 552);
            this.uiTabControl1.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            this.uiTabControl1.TabBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.uiTabControl1.TabIndex = 4;
            this.uiTabControl1.TabSelectedColor = System.Drawing.Color.DarkGray;
            this.uiTabControl1.TipsFont = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // tabPage2
            // 
            this.tabPage2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.tabPage2.Controls.Add(this.uiSplitContainer1);
            this.tabPage2.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tabPage2.Location = new System.Drawing.Point(0, 40);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Size = new System.Drawing.Size(862, 512);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "输入/出";
            // 
            // uiSplitContainer1
            // 
            this.uiSplitContainer1.Cursor = System.Windows.Forms.Cursors.Default;
            this.uiSplitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiSplitContainer1.Location = new System.Drawing.Point(0, 0);
            this.uiSplitContainer1.MinimumSize = new System.Drawing.Size(20, 20);
            this.uiSplitContainer1.Name = "uiSplitContainer1";
            // 
            // uiSplitContainer1.Panel1
            // 
            this.uiSplitContainer1.Panel1.Controls.Add(this.uiGroupBox1);
            this.uiSplitContainer1.Panel1MinSize = 250;
            // 
            // uiSplitContainer1.Panel2
            // 
            this.uiSplitContainer1.Panel2.Controls.Add(this.uiGroupBox2);
            this.uiSplitContainer1.Panel2MinSize = 250;
            this.uiSplitContainer1.Size = new System.Drawing.Size(862, 512);
            this.uiSplitContainer1.SplitterDistance = 408;
            this.uiSplitContainer1.SplitterWidth = 11;
            this.uiSplitContainer1.TabIndex = 0;
            // 
            // uiGroupBox1
            // 
            this.uiGroupBox1.BackColor = System.Drawing.Color.Transparent;
            this.uiGroupBox1.Controls.Add(this.tbl_DI);
            this.uiGroupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiGroupBox1.FillColor = System.Drawing.Color.Transparent;
            this.uiGroupBox1.FillColor2 = System.Drawing.Color.Transparent;
            this.uiGroupBox1.FillColorGradientDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.uiGroupBox1.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiGroupBox1.ForeColor = System.Drawing.Color.White;
            this.uiGroupBox1.Location = new System.Drawing.Point(0, 0);
            this.uiGroupBox1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.uiGroupBox1.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiGroupBox1.Name = "uiGroupBox1";
            this.uiGroupBox1.Padding = new System.Windows.Forms.Padding(0, 32, 0, 0);
            this.uiGroupBox1.Size = new System.Drawing.Size(408, 512);
            this.uiGroupBox1.TabIndex = 3;
            this.uiGroupBox1.Text = "输入";
            this.uiGroupBox1.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // tbl_DI
            // 
            this.tbl_DI.BackColor = System.Drawing.Color.Transparent;
            this.tbl_DI.ColumnCount = 1;
            this.tbl_DI.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tbl_DI.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbl_DI.Location = new System.Drawing.Point(0, 32);
            this.tbl_DI.Name = "tbl_DI";
            this.tbl_DI.RowCount = 1;
            this.tbl_DI.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tbl_DI.Size = new System.Drawing.Size(408, 480);
            this.tbl_DI.TabIndex = 0;
            this.tbl_DI.TagString = null;
            // 
            // uiGroupBox2
            // 
            this.uiGroupBox2.BackColor = System.Drawing.Color.Transparent;
            this.uiGroupBox2.Controls.Add(this.tbl_DO);
            this.uiGroupBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiGroupBox2.FillColor = System.Drawing.Color.Transparent;
            this.uiGroupBox2.FillColor2 = System.Drawing.Color.Transparent;
            this.uiGroupBox2.FillColorGradient = true;
            this.uiGroupBox2.FillColorGradientDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.uiGroupBox2.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiGroupBox2.ForeColor = System.Drawing.Color.White;
            this.uiGroupBox2.Location = new System.Drawing.Point(0, 0);
            this.uiGroupBox2.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.uiGroupBox2.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiGroupBox2.Name = "uiGroupBox2";
            this.uiGroupBox2.Padding = new System.Windows.Forms.Padding(0, 32, 0, 0);
            this.uiGroupBox2.Size = new System.Drawing.Size(443, 512);
            this.uiGroupBox2.TabIndex = 4;
            this.uiGroupBox2.Text = "输出";
            this.uiGroupBox2.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // tbl_DO
            // 
            this.tbl_DO.BackColor = System.Drawing.Color.Transparent;
            this.tbl_DO.ColumnCount = 1;
            this.tbl_DO.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tbl_DO.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbl_DO.Location = new System.Drawing.Point(0, 32);
            this.tbl_DO.Name = "tbl_DO";
            this.tbl_DO.RowCount = 1;
            this.tbl_DO.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tbl_DO.Size = new System.Drawing.Size(443, 480);
            this.tbl_DO.TabIndex = 0;
            this.tbl_DO.TagString = null;
            // 
            // tabPage1
            // 
            this.tabPage1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.tabPage1.Controls.Add(this.uiPanel_信号模拟);
            this.tabPage1.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tabPage1.Location = new System.Drawing.Point(0, 40);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Size = new System.Drawing.Size(862, 512);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "信号模拟";
            // 
            // uiPanel_信号模拟
            // 
            this.uiPanel_信号模拟.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiPanel_信号模拟.FillColor = System.Drawing.Color.Transparent;
            this.uiPanel_信号模拟.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiPanel_信号模拟.Location = new System.Drawing.Point(0, 0);
            this.uiPanel_信号模拟.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.uiPanel_信号模拟.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiPanel_信号模拟.Name = "uiPanel_信号模拟";
            this.uiPanel_信号模拟.Size = new System.Drawing.Size(862, 512);
            this.uiPanel_信号模拟.TabIndex = 0;
            this.uiPanel_信号模拟.Text = null;
            this.uiPanel_信号模拟.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // SerialportControUI
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.uiTabControl1);
            this.Margin = new System.Windows.Forms.Padding(6);
            this.Name = "SerialportControUI";
            this.Size = new System.Drawing.Size(862, 552);
            this.uiTabControl1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.uiSplitContainer1.Panel1.ResumeLayout(false);
            this.uiSplitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.uiSplitContainer1)).EndInit();
            this.uiSplitContainer1.ResumeLayout(false);
            this.uiGroupBox1.ResumeLayout(false);
            this.uiGroupBox2.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private Sunny.UI.UITabControl uiTabControl1;
        private System.Windows.Forms.TabPage tabPage2;
        private Sunny.UI.UISplitContainer uiSplitContainer1;
        private Sunny.UI.UIGroupBox uiGroupBox1;
        private Sunny.UI.UITableLayoutPanel tbl_DI;
        private Sunny.UI.UIGroupBox uiGroupBox2;
        private Sunny.UI.UITableLayoutPanel tbl_DO;
        private System.Windows.Forms.TabPage tabPage1;
        private Sunny.UI.UIPanel uiPanel_信号模拟;
    }
}
