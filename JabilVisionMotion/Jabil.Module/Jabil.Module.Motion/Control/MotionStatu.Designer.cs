namespace Jabil.Module.Motion.Control
{
    partial class MotionStatu
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
            this.uiPanel1 = new Sunny.UI.UIPanel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.uiLight_缓停触发 = new Sunny.UI.UILight();
            this.uiLight1_报警 = new Sunny.UI.UILight();
            this.uiLight1_掉线 = new Sunny.UI.UILight();
            this.uiLight_到位 = new Sunny.UI.UILight();
            this.uiLight1_使能 = new Sunny.UI.UILight();
            this.uiLight_运动 = new Sunny.UI.UILight();
            this.uiLight_原点 = new Sunny.UI.UILight();
            this.uiLight_负极限 = new Sunny.UI.UILight();
            this.uiLight_正极限 = new Sunny.UI.UILight();
            this.uiDigitalLabel_轴规划位置 = new Sunny.UI.UIDigitalLabel();
            this.uiDigitalLabel_编码器位置 = new Sunny.UI.UIDigitalLabel();
            this.uiLabel_轴类型 = new Sunny.UI.UILabel();
            this.uiLabel_轴名称 = new Sunny.UI.UILabel();
            this.uiPanel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // uiPanel1
            // 
            this.uiPanel1.Controls.Add(this.panel2);
            this.uiPanel1.Controls.Add(this.uiDigitalLabel_轴规划位置);
            this.uiPanel1.Controls.Add(this.uiDigitalLabel_编码器位置);
            this.uiPanel1.Controls.Add(this.uiLabel_轴类型);
            this.uiPanel1.Controls.Add(this.uiLabel_轴名称);
            this.uiPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiPanel1.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiPanel1.Location = new System.Drawing.Point(0, 0);
            this.uiPanel1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.uiPanel1.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiPanel1.Name = "uiPanel1";
            this.uiPanel1.Size = new System.Drawing.Size(126, 325);
            this.uiPanel1.TabIndex = 1;
            this.uiPanel1.Text = null;
            this.uiPanel1.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.tableLayoutPanel1);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 90);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(126, 235);
            this.panel2.TabIndex = 79;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.uiLight_缓停触发, 0, 8);
            this.tableLayoutPanel1.Controls.Add(this.uiLight1_报警, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.uiLight1_掉线, 0, 7);
            this.tableLayoutPanel1.Controls.Add(this.uiLight_到位, 0, 6);
            this.tableLayoutPanel1.Controls.Add(this.uiLight1_使能, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.uiLight_运动, 0, 5);
            this.tableLayoutPanel1.Controls.Add(this.uiLight_原点, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.uiLight_负极限, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.uiLight_正极限, 0, 2);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 10;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.11111F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.11111F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.11111F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.11111F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.11111F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.11111F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.11111F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.11111F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11.11111F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 2F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(126, 236);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // uiLight_缓停触发
            // 
            this.uiLight_缓停触发.BackColor = System.Drawing.Color.Transparent;
            this.uiLight_缓停触发.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiLight_缓停触发.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiLight_缓停触发.Location = new System.Drawing.Point(3, 211);
            this.uiLight_缓停触发.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiLight_缓停触发.Name = "uiLight_缓停触发";
            this.uiLight_缓停触发.OffColor = System.Drawing.Color.Gainsboro;
            this.uiLight_缓停触发.OnColor = System.Drawing.Color.Green;
            this.uiLight_缓停触发.Radius = 20;
            this.uiLight_缓停触发.ShowLightLine = true;
            this.uiLight_缓停触发.Size = new System.Drawing.Size(120, 20);
            this.uiLight_缓停触发.TabIndex = 58;
            // 
            // uiLight1_报警
            // 
            this.uiLight1_报警.BackColor = System.Drawing.Color.Transparent;
            this.uiLight1_报警.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiLight1_报警.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiLight1_报警.Location = new System.Drawing.Point(3, 3);
            this.uiLight1_报警.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiLight1_报警.Name = "uiLight1_报警";
            this.uiLight1_报警.OffColor = System.Drawing.Color.Gainsboro;
            this.uiLight1_报警.OnColor = System.Drawing.Color.Green;
            this.uiLight1_报警.Radius = 20;
            this.uiLight1_报警.ShowLightLine = true;
            this.uiLight1_报警.ShowText = true;
            this.uiLight1_报警.Size = new System.Drawing.Size(120, 20);
            this.uiLight1_报警.TabIndex = 77;
            // 
            // uiLight1_掉线
            // 
            this.uiLight1_掉线.BackColor = System.Drawing.Color.Transparent;
            this.uiLight1_掉线.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiLight1_掉线.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiLight1_掉线.Location = new System.Drawing.Point(3, 185);
            this.uiLight1_掉线.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiLight1_掉线.Name = "uiLight1_掉线";
            this.uiLight1_掉线.OffColor = System.Drawing.Color.Gainsboro;
            this.uiLight1_掉线.OnColor = System.Drawing.Color.Green;
            this.uiLight1_掉线.Radius = 20;
            this.uiLight1_掉线.ShowLightLine = true;
            this.uiLight1_掉线.Size = new System.Drawing.Size(120, 20);
            this.uiLight1_掉线.TabIndex = 48;
            // 
            // uiLight_到位
            // 
            this.uiLight_到位.BackColor = System.Drawing.Color.Transparent;
            this.uiLight_到位.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiLight_到位.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiLight_到位.Location = new System.Drawing.Point(3, 159);
            this.uiLight_到位.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiLight_到位.Name = "uiLight_到位";
            this.uiLight_到位.OffColor = System.Drawing.Color.Gainsboro;
            this.uiLight_到位.OnColor = System.Drawing.Color.Green;
            this.uiLight_到位.Radius = 20;
            this.uiLight_到位.ShowLightLine = true;
            this.uiLight_到位.Size = new System.Drawing.Size(120, 20);
            this.uiLight_到位.TabIndex = 57;
            // 
            // uiLight1_使能
            // 
            this.uiLight1_使能.BackColor = System.Drawing.Color.Transparent;
            this.uiLight1_使能.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiLight1_使能.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiLight1_使能.Location = new System.Drawing.Point(3, 29);
            this.uiLight1_使能.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiLight1_使能.Name = "uiLight1_使能";
            this.uiLight1_使能.OffColor = System.Drawing.Color.Gainsboro;
            this.uiLight1_使能.OnColor = System.Drawing.Color.Green;
            this.uiLight1_使能.Radius = 20;
            this.uiLight1_使能.ShowLightLine = true;
            this.uiLight1_使能.Size = new System.Drawing.Size(120, 20);
            this.uiLight1_使能.TabIndex = 50;
            // 
            // uiLight_运动
            // 
            this.uiLight_运动.BackColor = System.Drawing.Color.Transparent;
            this.uiLight_运动.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiLight_运动.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiLight_运动.Location = new System.Drawing.Point(3, 133);
            this.uiLight_运动.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiLight_运动.Name = "uiLight_运动";
            this.uiLight_运动.OffColor = System.Drawing.Color.Gainsboro;
            this.uiLight_运动.OnColor = System.Drawing.Color.Green;
            this.uiLight_运动.Radius = 20;
            this.uiLight_运动.ShowLightLine = true;
            this.uiLight_运动.Size = new System.Drawing.Size(120, 20);
            this.uiLight_运动.TabIndex = 55;
            // 
            // uiLight_原点
            // 
            this.uiLight_原点.BackColor = System.Drawing.Color.Transparent;
            this.uiLight_原点.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiLight_原点.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiLight_原点.Location = new System.Drawing.Point(3, 81);
            this.uiLight_原点.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiLight_原点.Name = "uiLight_原点";
            this.uiLight_原点.OffColor = System.Drawing.Color.Gainsboro;
            this.uiLight_原点.OnColor = System.Drawing.Color.Green;
            this.uiLight_原点.Radius = 20;
            this.uiLight_原点.ShowLightLine = true;
            this.uiLight_原点.Size = new System.Drawing.Size(120, 20);
            this.uiLight_原点.TabIndex = 52;
            // 
            // uiLight_负极限
            // 
            this.uiLight_负极限.BackColor = System.Drawing.Color.Transparent;
            this.uiLight_负极限.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiLight_负极限.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiLight_负极限.Location = new System.Drawing.Point(3, 107);
            this.uiLight_负极限.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiLight_负极限.Name = "uiLight_负极限";
            this.uiLight_负极限.OffColor = System.Drawing.Color.Gainsboro;
            this.uiLight_负极限.OnColor = System.Drawing.Color.Green;
            this.uiLight_负极限.Radius = 20;
            this.uiLight_负极限.ShowLightLine = true;
            this.uiLight_负极限.Size = new System.Drawing.Size(120, 20);
            this.uiLight_负极限.TabIndex = 53;
            // 
            // uiLight_正极限
            // 
            this.uiLight_正极限.BackColor = System.Drawing.Color.Transparent;
            this.uiLight_正极限.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiLight_正极限.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiLight_正极限.Location = new System.Drawing.Point(3, 55);
            this.uiLight_正极限.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiLight_正极限.Name = "uiLight_正极限";
            this.uiLight_正极限.OffColor = System.Drawing.Color.Gainsboro;
            this.uiLight_正极限.OnColor = System.Drawing.Color.Green;
            this.uiLight_正极限.Radius = 20;
            this.uiLight_正极限.ShowLightLine = true;
            this.uiLight_正极限.Size = new System.Drawing.Size(120, 20);
            this.uiLight_正极限.TabIndex = 51;
            // 
            // uiDigitalLabel_轴规划位置
            // 
            this.uiDigitalLabel_轴规划位置.BackColor = System.Drawing.Color.Black;
            this.uiDigitalLabel_轴规划位置.DecimalPlaces = 3;
            this.uiDigitalLabel_轴规划位置.DigitalSize = 15;
            this.uiDigitalLabel_轴规划位置.Dock = System.Windows.Forms.DockStyle.Top;
            this.uiDigitalLabel_轴规划位置.Font = new System.Drawing.Font("宋体", 7.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiDigitalLabel_轴规划位置.ForeColor = System.Drawing.Color.Lime;
            this.uiDigitalLabel_轴规划位置.Location = new System.Drawing.Point(0, 66);
            this.uiDigitalLabel_轴规划位置.Margin = new System.Windows.Forms.Padding(0);
            this.uiDigitalLabel_轴规划位置.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiDigitalLabel_轴规划位置.Name = "uiDigitalLabel_轴规划位置";
            this.uiDigitalLabel_轴规划位置.Size = new System.Drawing.Size(126, 24);
            this.uiDigitalLabel_轴规划位置.TabIndex = 60;
            this.uiDigitalLabel_轴规划位置.Text = "uiDigitalLabel1";
            this.uiDigitalLabel_轴规划位置.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            // 
            // uiDigitalLabel_编码器位置
            // 
            this.uiDigitalLabel_编码器位置.BackColor = System.Drawing.Color.Black;
            this.uiDigitalLabel_编码器位置.DecimalPlaces = 3;
            this.uiDigitalLabel_编码器位置.DigitalSize = 15;
            this.uiDigitalLabel_编码器位置.Dock = System.Windows.Forms.DockStyle.Top;
            this.uiDigitalLabel_编码器位置.Font = new System.Drawing.Font("宋体", 7.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiDigitalLabel_编码器位置.ForeColor = System.Drawing.Color.Lime;
            this.uiDigitalLabel_编码器位置.Location = new System.Drawing.Point(0, 42);
            this.uiDigitalLabel_编码器位置.Margin = new System.Windows.Forms.Padding(0);
            this.uiDigitalLabel_编码器位置.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiDigitalLabel_编码器位置.Name = "uiDigitalLabel_编码器位置";
            this.uiDigitalLabel_编码器位置.Size = new System.Drawing.Size(126, 24);
            this.uiDigitalLabel_编码器位置.TabIndex = 46;
            this.uiDigitalLabel_编码器位置.Text = "uiDigitalLabel1";
            this.uiDigitalLabel_编码器位置.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            // 
            // uiLabel_轴类型
            // 
            this.uiLabel_轴类型.Dock = System.Windows.Forms.DockStyle.Top;
            this.uiLabel_轴类型.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiLabel_轴类型.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.uiLabel_轴类型.Location = new System.Drawing.Point(0, 21);
            this.uiLabel_轴类型.Name = "uiLabel_轴类型";
            this.uiLabel_轴类型.Size = new System.Drawing.Size(126, 21);
            this.uiLabel_轴类型.TabIndex = 1;
            this.uiLabel_轴类型.Text = "X轴";
            // 
            // uiLabel_轴名称
            // 
            this.uiLabel_轴名称.Dock = System.Windows.Forms.DockStyle.Top;
            this.uiLabel_轴名称.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiLabel_轴名称.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.uiLabel_轴名称.Location = new System.Drawing.Point(0, 0);
            this.uiLabel_轴名称.Name = "uiLabel_轴名称";
            this.uiLabel_轴名称.Size = new System.Drawing.Size(126, 21);
            this.uiLabel_轴名称.TabIndex = 0;
            this.uiLabel_轴名称.Text = "移栽轴";
            // 
            // MotionStatu
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.uiPanel1);
            this.Margin = new System.Windows.Forms.Padding(1);
            this.Name = "MotionStatu";
            this.Size = new System.Drawing.Size(126, 325);
            this.uiPanel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private Sunny.UI.UIPanel uiPanel1;
        private Sunny.UI.UILabel uiLabel_轴类型;
        private Sunny.UI.UILabel uiLabel_轴名称;
        private Sunny.UI.UILight uiLight_缓停触发;
        private Sunny.UI.UILight uiLight_到位;
        private Sunny.UI.UILight uiLight_运动;
        private Sunny.UI.UILight uiLight_负极限;
        private Sunny.UI.UILight uiLight_原点;
        private Sunny.UI.UILight uiLight_正极限;
        private Sunny.UI.UILight uiLight1_使能;
        private Sunny.UI.UILight uiLight1_掉线;
        private Sunny.UI.UILight uiLight1_报警;
        private Sunny.UI.UIDigitalLabel uiDigitalLabel_编码器位置;
        private Sunny.UI.UIDigitalLabel uiDigitalLabel_轴规划位置;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
    }
}
