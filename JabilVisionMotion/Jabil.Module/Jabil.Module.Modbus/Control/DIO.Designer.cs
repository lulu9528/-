namespace Jabil.Module.Modbus.Control
{
    partial class DIO
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
            if (_isDisposed) return;

            if (disposing)
            {
                // 释放托管资源
                if (_statusTimer != null)
                {
                    _statusTimer.Tick -= StatusTimer_Tick;
                    _statusTimer.Dispose();
                }

                if (_toolTip != null)
                {
                    _toolTip.Dispose();
                }

                // 移除事件订阅
                DoubleClick -= DIO_DoubleClick;
                MouseEnter -= DIO_MouseEnter;
                MouseLeave -= DIO_MouseLeave;

                // 释放子控件事件
                label_PointName.Click -= label_PointName_Click;
                uiLight_状态.Click -= uiLight_状态_Click;
                Label_PointIndex.Click -= Label_PointIndex_Click;
                Label_PointValue.Click -= Label_PointValue_Click;

                // 释放组件资源
                components?.Dispose();
            }

            // 标记为已释放
            _isDisposed = true;
        }

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.uiTableLayoutPanel1 = new Sunny.UI.UITableLayoutPanel();
            this.Label_PointIndex = new Sunny.UI.UILabel();
            this.label_PointName = new Sunny.UI.UIScrollingText();
            this.uiLight_状态 = new Sunny.UI.UILight();
            this.Label_PointValue = new Sunny.UI.UILabel();
            this.uiTableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // uiTableLayoutPanel1
            // 
            this.uiTableLayoutPanel1.BackColor = System.Drawing.Color.Transparent;
            this.uiTableLayoutPanel1.ColumnCount = 3;
            this.uiTableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 70F));
            this.uiTableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 14.82759F));
            this.uiTableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15.17241F));
            this.uiTableLayoutPanel1.Controls.Add(this.Label_PointIndex, 0, 1);
            this.uiTableLayoutPanel1.Controls.Add(this.label_PointName, 0, 0);
            this.uiTableLayoutPanel1.Controls.Add(this.uiLight_状态, 1, 0);
            this.uiTableLayoutPanel1.Controls.Add(this.Label_PointValue, 2, 0);
            this.uiTableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiTableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.uiTableLayoutPanel1.Name = "uiTableLayoutPanel1";
            this.uiTableLayoutPanel1.RowCount = 2;
            this.uiTableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 70F));
            this.uiTableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.uiTableLayoutPanel1.Size = new System.Drawing.Size(290, 80);
            this.uiTableLayoutPanel1.TabIndex = 1;
            this.uiTableLayoutPanel1.TagString = null;
            // 
            // Label_PointIndex
            // 
            this.Label_PointIndex.BackColor = System.Drawing.Color.Transparent;
            this.Label_PointIndex.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Label_PointIndex.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Label_PointIndex.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.Label_PointIndex.Location = new System.Drawing.Point(3, 56);
            this.Label_PointIndex.Name = "Label_PointIndex";
            this.Label_PointIndex.Size = new System.Drawing.Size(197, 24);
            this.Label_PointIndex.TabIndex = 12;
            this.Label_PointIndex.Text = "PointIndex";
            this.Label_PointIndex.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.Label_PointIndex.Click += new System.EventHandler(this.Label_PointIndex_Click);
            // 
            // label_PointName
            // 
            this.label_PointName.Active = true;
            this.label_PointName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label_PointName.FillColor = System.Drawing.Color.Transparent;
            this.label_PointName.FillDisableColor = System.Drawing.Color.Transparent;
            this.label_PointName.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label_PointName.ForeColor = System.Drawing.Color.White;
            this.label_PointName.Interval = 3000;
            this.label_PointName.Location = new System.Drawing.Point(3, 3);
            this.label_PointName.MinimumSize = new System.Drawing.Size(1, 1);
            this.label_PointName.Name = "label_PointName";
            this.label_PointName.RectColor = System.Drawing.Color.Transparent;
            this.label_PointName.RectDisableColor = System.Drawing.Color.Transparent;
            this.label_PointName.Size = new System.Drawing.Size(197, 50);
            this.label_PointName.TabIndex = 5;
            this.label_PointName.Text = "PointName";
            this.label_PointName.Click += new System.EventHandler(this.label_PointName_Click);
            // 
            // uiLight_状态
            // 
            this.uiLight_状态.BackColor = System.Drawing.Color.Transparent;
            this.uiLight_状态.CenterColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(200)))), ((int)(((byte)(100)))));
            this.uiLight_状态.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiLight_状态.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiLight_状态.Location = new System.Drawing.Point(206, 3);
            this.uiLight_状态.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiLight_状态.Name = "uiLight_状态";
            this.uiLight_状态.OffCenterColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.uiLight_状态.OffColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.uiLight_状态.OnCenterColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(200)))), ((int)(((byte)(100)))));
            this.uiLight_状态.OnColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(220)))), ((int)(((byte)(60)))));
            this.uiLight_状态.Radius = 37;
            this.uiLight_状态.Size = new System.Drawing.Size(37, 50);
            this.uiLight_状态.TabIndex = 11;
            this.uiLight_状态.Text = "uiLight1";
            this.uiLight_状态.Click += new System.EventHandler(this.uiLight_状态_Click);
            // 
            // Label_PointValue
            // 
            this.Label_PointValue.BackColor = System.Drawing.Color.Transparent;
            this.Label_PointValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Label_PointValue.Font = new System.Drawing.Font("微软雅黑", 7.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Label_PointValue.ForeColor = System.Drawing.Color.White;
            this.Label_PointValue.Location = new System.Drawing.Point(249, 0);
            this.Label_PointValue.Name = "Label_PointValue";
            this.Label_PointValue.Size = new System.Drawing.Size(38, 56);
            this.Label_PointValue.TabIndex = 13;
            this.Label_PointValue.Text = "1";
            this.Label_PointValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.Label_PointValue.Click += new System.EventHandler(this.Label_PointValue_Click);
            // 
            // DIO
            // 
            this.Controls.Add(this.uiTableLayoutPanel1);
            this.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.FillColor2 = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(45)))), ((int)(((byte)(80)))));
            this.FillColorGradient = true;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "DIO";
            this.Size = new System.Drawing.Size(290, 80);
            this.Load += new System.EventHandler(this.DIO_Load);
            this.uiTableLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private Sunny.UI.UITableLayoutPanel uiTableLayoutPanel1;
        private Sunny.UI.UIScrollingText label_PointName;
        private Sunny.UI.UILight uiLight_状态;
        private Sunny.UI.UILabel Label_PointIndex;
        private Sunny.UI.UILabel Label_PointValue;
    }
}