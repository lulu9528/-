namespace Jabil.Module.TemporaryVariables.Control
{
    partial class VariablesDIO
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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.label_更新时间 = new Sunny.UI.UIScrollingText();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.label_点位索引 = new System.Windows.Forms.Label();
            this.uiLight_点位状态 = new Sunny.UI.UILight();
            this.label_点位名称 = new Sunny.UI.UIScrollingText();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.tableLayoutPanel1.Controls.Add(this.label_更新时间, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.label_点位名称, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(4);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 60.81081F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 39.18919F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 29F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(337, 104);
            this.tableLayoutPanel1.TabIndex = 2;
            // 
            // label_更新时间
            // 
            this.label_更新时间.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label_更新时间.FillColor = System.Drawing.Color.Transparent;
            this.label_更新时间.FillDisableColor = System.Drawing.Color.Transparent;
            this.label_更新时间.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label_更新时间.ForeColor = System.Drawing.Color.White;
            this.label_更新时间.Interval = 3000;
            this.label_更新时间.Location = new System.Drawing.Point(3, 77);
            this.label_更新时间.MinimumSize = new System.Drawing.Size(1, 1);
            this.label_更新时间.Name = "label_更新时间";
            this.label_更新时间.Radius = 2;
            this.label_更新时间.RectColor = System.Drawing.Color.Transparent;
            this.label_更新时间.RectDisableColor = System.Drawing.Color.Transparent;
            this.label_更新时间.Size = new System.Drawing.Size(331, 24);
            this.label_更新时间.TabIndex = 5;
            this.label_更新时间.Text = "更新时间";
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 3;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel2.Controls.Add(this.label_点位索引, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.uiLight_点位状态, 0, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tableLayoutPanel2.Location = new System.Drawing.Point(4, 4);
            this.tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(4);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(329, 37);
            this.tableLayoutPanel2.TabIndex = 3;
            // 
            // label_点位索引
            // 
            this.label_点位索引.BackColor = System.Drawing.Color.Transparent;
            this.label_点位索引.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label_点位索引.Font = new System.Drawing.Font("微软雅黑", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label_点位索引.ForeColor = System.Drawing.Color.White;
            this.label_点位索引.Location = new System.Drawing.Point(44, 0);
            this.label_点位索引.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label_点位索引.Name = "label_点位索引";
            this.label_点位索引.Size = new System.Drawing.Size(72, 37);
            this.label_点位索引.TabIndex = 8;
            this.label_点位索引.Text = "1";
            this.label_点位索引.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // uiLight_点位状态
            // 
            this.uiLight_点位状态.BackColor = System.Drawing.Color.Transparent;
            this.uiLight_点位状态.CenterColor = System.Drawing.Color.Green;
            this.uiLight_点位状态.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiLight_点位状态.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiLight_点位状态.Location = new System.Drawing.Point(3, 3);
            this.uiLight_点位状态.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiLight_点位状态.Name = "uiLight_点位状态";
            this.uiLight_点位状态.OffCenterColor = System.Drawing.Color.Red;
            this.uiLight_点位状态.OffColor = System.Drawing.Color.Red;
            this.uiLight_点位状态.OnCenterColor = System.Drawing.Color.Green;
            this.uiLight_点位状态.OnColor = System.Drawing.Color.Green;
            this.uiLight_点位状态.Radius = 31;
            this.uiLight_点位状态.ShowLightLine = true;
            this.uiLight_点位状态.Size = new System.Drawing.Size(34, 31);
            this.uiLight_点位状态.TabIndex = 7;
            this.uiLight_点位状态.Click += new System.EventHandler(this.uiLight_点位状态_Click);
            // 
            // label_点位名称
            // 
            this.label_点位名称.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label_点位名称.FillColor = System.Drawing.Color.Transparent;
            this.label_点位名称.FillDisableColor = System.Drawing.Color.Transparent;
            this.label_点位名称.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label_点位名称.ForeColor = System.Drawing.Color.White;
            this.label_点位名称.Interval = 3000;
            this.label_点位名称.Location = new System.Drawing.Point(3, 48);
            this.label_点位名称.MinimumSize = new System.Drawing.Size(1, 1);
            this.label_点位名称.Name = "label_点位名称";
            this.label_点位名称.Radius = 2;
            this.label_点位名称.RectColor = System.Drawing.Color.Transparent;
            this.label_点位名称.RectDisableColor = System.Drawing.Color.Transparent;
            this.label_点位名称.Size = new System.Drawing.Size(331, 23);
            this.label_点位名称.TabIndex = 4;
            this.label_点位名称.Text = "点位名称";
            this.label_点位名称.Click += new System.EventHandler(this.label_点位名称_Click);
            // 
            // VariablesDIO
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.tableLayoutPanel1);
            this.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.FillColor2 = System.Drawing.Color.Gray;
            this.FillColorGradient = true;
            this.FillColorGradientDirection = System.Windows.Forms.FlowDirection.BottomUp;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "VariablesDIO";
            this.Size = new System.Drawing.Size(337, 104);
            this.Load += new System.EventHandler(this.DIO_Load);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private Sunny.UI.UILight uiLight_点位状态;
        private System.Windows.Forms.Label label_点位索引;
        private Sunny.UI.UIScrollingText label_点位名称;
        private Sunny.UI.UIScrollingText label_更新时间;
    }
}
