namespace Jabil.Module.Serialport.Control
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
            this.uiTableLayoutPanel1 = new Sunny.UI.UITableLayoutPanel();
            this.labelPointName = new Sunny.UI.UIScrollingText();
            this.uiLightState = new Sunny.UI.UILight();
            this.labelMessageFormat = new Sunny.UI.UIScrollingText();
            this.uiTableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // uiTableLayoutPanel1
            // 
            this.uiTableLayoutPanel1.BackColor = System.Drawing.Color.Transparent;
            this.uiTableLayoutPanel1.ColumnCount = 2;
            this.uiTableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 76.22378F));
            this.uiTableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 23.77622F));
            this.uiTableLayoutPanel1.Controls.Add(this.labelPointName, 0, 0);
            this.uiTableLayoutPanel1.Controls.Add(this.uiLightState, 1, 0);
            this.uiTableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.uiTableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.uiTableLayoutPanel1.Name = "uiTableLayoutPanel1";
            this.uiTableLayoutPanel1.RowCount = 1;
            this.uiTableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.uiTableLayoutPanel1.Size = new System.Drawing.Size(143, 30);
            this.uiTableLayoutPanel1.TabIndex = 1;
            this.uiTableLayoutPanel1.TagString = null;
            // 
            // label_点位名称
            // 
            this.labelPointName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelPointName.FillColor = System.Drawing.Color.Transparent;
            this.labelPointName.FillDisableColor = System.Drawing.Color.Transparent;
            this.labelPointName.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.labelPointName.ForeColor = System.Drawing.Color.White;
            this.labelPointName.Interval = 3000;
            this.labelPointName.Location = new System.Drawing.Point(3, 3);
            this.labelPointName.MinimumSize = new System.Drawing.Size(1, 1);
            this.labelPointName.Name = "label_点位名称";
            this.labelPointName.RectColor = System.Drawing.Color.Transparent;
            this.labelPointName.Size = new System.Drawing.Size(103, 24);
            this.labelPointName.TabIndex = 7;
            this.labelPointName.Text = "点位名称";
            // 
            // uiLight_状态
            // 
            this.uiLightState.BackColor = System.Drawing.Color.Transparent;
            this.uiLightState.CenterColor = System.Drawing.Color.Green;
            this.uiLightState.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiLightState.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiLightState.Location = new System.Drawing.Point(112, 3);
            this.uiLightState.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiLightState.Name = "uiLight_状态";
            this.uiLightState.OffCenterColor = System.Drawing.Color.Red;
            this.uiLightState.OffColor = System.Drawing.Color.Red;
            this.uiLightState.OnCenterColor = System.Drawing.Color.Green;
            this.uiLightState.OnColor = System.Drawing.Color.Green;
            this.uiLightState.Radius = 24;
            this.uiLightState.Size = new System.Drawing.Size(28, 24);
            this.uiLightState.State = Sunny.UI.UILightState.Blink;
            this.uiLightState.TabIndex = 6;
            this.uiLightState.Text = "uiLight1";
            // 
            // Label_报文格式
            // 
            this.labelMessageFormat.BackColor = System.Drawing.Color.Transparent;
            this.labelMessageFormat.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelMessageFormat.FillColor = System.Drawing.Color.Transparent;
            this.labelMessageFormat.FillDisableColor = System.Drawing.Color.Transparent;
            this.labelMessageFormat.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.labelMessageFormat.ForeColor = System.Drawing.Color.White;
            this.labelMessageFormat.Interval = 3000;
            this.labelMessageFormat.Location = new System.Drawing.Point(0, 30);
            this.labelMessageFormat.MinimumSize = new System.Drawing.Size(1, 1);
            this.labelMessageFormat.Name = "Label_报文格式";
            this.labelMessageFormat.RectColor = System.Drawing.Color.Transparent;
            this.labelMessageFormat.Size = new System.Drawing.Size(143, 27);
            this.labelMessageFormat.TabIndex = 8;
            this.labelMessageFormat.Text = "报文格式";
            // 
            // DIO
            // 
            this.Controls.Add(this.labelMessageFormat);
            this.Controls.Add(this.uiTableLayoutPanel1);
            this.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.FillColor2 = System.Drawing.Color.Gray;
            this.FillColorGradient = true;
            this.FillColorGradientDirection = System.Windows.Forms.FlowDirection.BottomUp;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "DIO";
            this.Size = new System.Drawing.Size(143, 57);
            this.uiTableLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private Sunny.UI.UITableLayoutPanel uiTableLayoutPanel1;
        private Sunny.UI.UILight uiLightState;
        private Sunny.UI.UIScrollingText labelPointName;
        private Sunny.UI.UIScrollingText labelMessageFormat;
    }
}
