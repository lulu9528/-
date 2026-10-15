namespace BasicClassLibrary.Controls.Axis
{
    partial class AxisState
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
            this.numericUpDownPosition = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.numericUpDownSpeed = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.lbLed_ALM = new BasicClassLibrary.Controls.Leds.LBLed();
            this.tableLayoutPanel3 = new BasicClassLibrary.Controls.HoverTableLayoutPanel();
            this.lbLed_SEVON = new BasicClassLibrary.Controls.Leds.LBLed();
            this.lbLed_INP = new BasicClassLibrary.Controls.Leds.LBLed();
            this.lbLed_EZ = new BasicClassLibrary.Controls.Leds.LBLed();
            this.lbLed_RDY = new BasicClassLibrary.Controls.Leds.LBLed();
            this.lbLed_ELN = new BasicClassLibrary.Controls.Leds.LBLed();
            this.lbLed_DSTP = new BasicClassLibrary.Controls.Leds.LBLed();
            this.lbLed_ELP = new BasicClassLibrary.Controls.Leds.LBLed();
            this.lbLed_SLN = new BasicClassLibrary.Controls.Leds.LBLed();
            this.lbLed_SLP = new BasicClassLibrary.Controls.Leds.LBLed();
            this.lbLed_ORG = new BasicClassLibrary.Controls.Leds.LBLed();
            this.lbLed_EMG = new BasicClassLibrary.Controls.Leds.LBLed();
            this.lbLed_ERC = new BasicClassLibrary.Controls.Leds.LBLed();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPosition)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownSpeed)).BeginInit();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.SuspendLayout();
            // 
            // numericUpDownPosition
            // 
            this.numericUpDownPosition.DecimalPlaces = 3;
            this.numericUpDownPosition.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numericUpDownPosition.Increment = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.numericUpDownPosition.InterceptArrowKeys = false;
            this.numericUpDownPosition.Location = new System.Drawing.Point(69, 3);
            this.numericUpDownPosition.Maximum = new decimal(new int[] {
            1410065407,
            2,
            0,
            0});
            this.numericUpDownPosition.Minimum = new decimal(new int[] {
            1410065407,
            2,
            0,
            -2147483648});
            this.numericUpDownPosition.Name = "numericUpDownPosition";
            this.numericUpDownPosition.ReadOnly = true;
            this.numericUpDownPosition.Size = new System.Drawing.Size(68, 21);
            this.numericUpDownPosition.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Location = new System.Drawing.Point(3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(60, 36);
            this.label1.TabIndex = 2;
            this.label1.Text = "位置";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // numericUpDownSpeed
            // 
            this.numericUpDownSpeed.DecimalPlaces = 3;
            this.numericUpDownSpeed.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numericUpDownSpeed.Increment = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.numericUpDownSpeed.InterceptArrowKeys = false;
            this.numericUpDownSpeed.Location = new System.Drawing.Point(69, 39);
            this.numericUpDownSpeed.Maximum = new decimal(new int[] {
            1410065407,
            2,
            0,
            0});
            this.numericUpDownSpeed.Minimum = new decimal(new int[] {
            1410065407,
            2,
            0,
            -2147483648});
            this.numericUpDownSpeed.Name = "numericUpDownSpeed";
            this.numericUpDownSpeed.ReadOnly = true;
            this.numericUpDownSpeed.Size = new System.Drawing.Size(68, 21);
            this.numericUpDownSpeed.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label2.Location = new System.Drawing.Point(3, 36);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(60, 21);
            this.label2.TabIndex = 2;
            this.label2.Text = "速度";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label18.Location = new System.Drawing.Point(143, 36);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(60, 21);
            this.label18.TabIndex = 15;
            this.label18.Text = "mm/s";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33332F));
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel3, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 63F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 21.42857F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(212, 263);
            this.tableLayoutPanel1.TabIndex = 17;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 3;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 47.2973F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 52.7027F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 65F));
            this.tableLayoutPanel2.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.numericUpDownPosition, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.label2, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.numericUpDownSpeed, 1, 1);
            this.tableLayoutPanel2.Controls.Add(this.label18, 2, 1);
            this.tableLayoutPanel2.Controls.Add(this.lbLed_ALM, 2, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 2;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 64.91228F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 35.08772F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(206, 57);
            this.tableLayoutPanel2.TabIndex = 0;
            // 
            // lbLed_ALM
            // 
            this.lbLed_ALM.BackColor = System.Drawing.Color.Transparent;
            this.lbLed_ALM.BlinkInterval = 500;
            this.lbLed_ALM.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbLed_ALM.Label = "报警/ALM";
            this.lbLed_ALM.LabelPosition = BasicClassLibrary.Controls.Leds.LBLed.LedLabelPosition.Top;
            this.lbLed_ALM.LedColor = System.Drawing.Color.Red;
            this.lbLed_ALM.LedSize = new System.Drawing.SizeF(10F, 10F);
            this.lbLed_ALM.Location = new System.Drawing.Point(143, 3);
            this.lbLed_ALM.Name = "lbLed_ALM";
            this.lbLed_ALM.Renderer = null;
            this.lbLed_ALM.Size = new System.Drawing.Size(60, 30);
            this.lbLed_ALM.State = BasicClassLibrary.Controls.Leds.LBLed.LedState.Off;
            this.lbLed_ALM.Style = BasicClassLibrary.Controls.Leds.LBLed.LedStyle.Circular;
            this.lbLed_ALM.TabIndex = 0;
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 3;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33334F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33334F));
            this.tableLayoutPanel3.Controls.Add(this.lbLed_SEVON, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.lbLed_INP, 1, 0);
            this.tableLayoutPanel3.Controls.Add(this.lbLed_EZ, 2, 3);
            this.tableLayoutPanel3.Controls.Add(this.lbLed_RDY, 2, 0);
            this.tableLayoutPanel3.Controls.Add(this.lbLed_ELN, 0, 1);
            this.tableLayoutPanel3.Controls.Add(this.lbLed_DSTP, 0, 3);
            this.tableLayoutPanel3.Controls.Add(this.lbLed_ELP, 1, 1);
            this.tableLayoutPanel3.Controls.Add(this.lbLed_SLN, 0, 2);
            this.tableLayoutPanel3.Controls.Add(this.lbLed_SLP, 1, 2);
            this.tableLayoutPanel3.Controls.Add(this.lbLed_ORG, 2, 2);
            this.tableLayoutPanel3.Controls.Add(this.lbLed_EMG, 1, 3);
            this.tableLayoutPanel3.Controls.Add(this.lbLed_ERC, 2, 1);
            this.tableLayoutPanel3.Delay = 3000D;
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.IsHoverEnabled = true;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(3, 66);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 4;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(206, 194);
            this.tableLayoutPanel3.TabIndex = 1;
            // 
            // lbLed_SEVON
            // 
            this.lbLed_SEVON.BackColor = System.Drawing.Color.Transparent;
            this.lbLed_SEVON.BlinkInterval = 500;
            this.lbLed_SEVON.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbLed_SEVON.Label = "使能";
            this.lbLed_SEVON.LabelPosition = BasicClassLibrary.Controls.Leds.LBLed.LedLabelPosition.Top;
            this.lbLed_SEVON.LedColor = System.Drawing.Color.Red;
            this.lbLed_SEVON.LedSize = new System.Drawing.SizeF(10F, 10F);
            this.lbLed_SEVON.Location = new System.Drawing.Point(3, 3);
            this.lbLed_SEVON.Name = "lbLed_SEVON";
            this.lbLed_SEVON.Renderer = null;
            this.lbLed_SEVON.Size = new System.Drawing.Size(62, 42);
            this.lbLed_SEVON.State = BasicClassLibrary.Controls.Leds.LBLed.LedState.Off;
            this.lbLed_SEVON.Style = BasicClassLibrary.Controls.Leds.LBLed.LedStyle.Circular;
            this.lbLed_SEVON.TabIndex = 13;
            // 
            // lbLed_INP
            // 
            this.lbLed_INP.BackColor = System.Drawing.Color.Transparent;
            this.lbLed_INP.BlinkInterval = 500;
            this.lbLed_INP.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbLed_INP.Label = "到位";
            this.lbLed_INP.LabelPosition = BasicClassLibrary.Controls.Leds.LBLed.LedLabelPosition.Top;
            this.lbLed_INP.LedColor = System.Drawing.Color.Red;
            this.lbLed_INP.LedSize = new System.Drawing.SizeF(10F, 10F);
            this.lbLed_INP.Location = new System.Drawing.Point(71, 3);
            this.lbLed_INP.Name = "lbLed_INP";
            this.lbLed_INP.Renderer = null;
            this.lbLed_INP.Size = new System.Drawing.Size(62, 42);
            this.lbLed_INP.State = BasicClassLibrary.Controls.Leds.LBLed.LedState.Off;
            this.lbLed_INP.Style = BasicClassLibrary.Controls.Leds.LBLed.LedStyle.Circular;
            this.lbLed_INP.TabIndex = 0;
            // 
            // lbLed_EZ
            // 
            this.lbLed_EZ.BackColor = System.Drawing.Color.Transparent;
            this.lbLed_EZ.BlinkInterval = 500;
            this.lbLed_EZ.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbLed_EZ.Label = "EZ";
            this.lbLed_EZ.LabelPosition = BasicClassLibrary.Controls.Leds.LBLed.LedLabelPosition.Top;
            this.lbLed_EZ.LedColor = System.Drawing.Color.Red;
            this.lbLed_EZ.LedSize = new System.Drawing.SizeF(10F, 10F);
            this.lbLed_EZ.Location = new System.Drawing.Point(139, 147);
            this.lbLed_EZ.Name = "lbLed_EZ";
            this.lbLed_EZ.Renderer = null;
            this.lbLed_EZ.Size = new System.Drawing.Size(64, 44);
            this.lbLed_EZ.State = BasicClassLibrary.Controls.Leds.LBLed.LedState.Off;
            this.lbLed_EZ.Style = BasicClassLibrary.Controls.Leds.LBLed.LedStyle.Circular;
            this.lbLed_EZ.TabIndex = 0;
            // 
            // lbLed_RDY
            // 
            this.lbLed_RDY.BackColor = System.Drawing.Color.Transparent;
            this.lbLed_RDY.BlinkInterval = 500;
            this.lbLed_RDY.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbLed_RDY.Label = "准备";
            this.lbLed_RDY.LabelPosition = BasicClassLibrary.Controls.Leds.LBLed.LedLabelPosition.Top;
            this.lbLed_RDY.LedColor = System.Drawing.Color.Red;
            this.lbLed_RDY.LedSize = new System.Drawing.SizeF(10F, 10F);
            this.lbLed_RDY.Location = new System.Drawing.Point(139, 3);
            this.lbLed_RDY.Name = "lbLed_RDY";
            this.lbLed_RDY.Renderer = null;
            this.lbLed_RDY.Size = new System.Drawing.Size(64, 42);
            this.lbLed_RDY.State = BasicClassLibrary.Controls.Leds.LBLed.LedState.Off;
            this.lbLed_RDY.Style = BasicClassLibrary.Controls.Leds.LBLed.LedStyle.Circular;
            this.lbLed_RDY.TabIndex = 5;
            // 
            // lbLed_ELN
            // 
            this.lbLed_ELN.BackColor = System.Drawing.Color.Transparent;
            this.lbLed_ELN.BlinkInterval = 500;
            this.lbLed_ELN.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbLed_ELN.Label = "负硬限位";
            this.lbLed_ELN.LabelPosition = BasicClassLibrary.Controls.Leds.LBLed.LedLabelPosition.Top;
            this.lbLed_ELN.LedColor = System.Drawing.Color.Red;
            this.lbLed_ELN.LedSize = new System.Drawing.SizeF(10F, 10F);
            this.lbLed_ELN.Location = new System.Drawing.Point(3, 51);
            this.lbLed_ELN.Name = "lbLed_ELN";
            this.lbLed_ELN.Renderer = null;
            this.lbLed_ELN.Size = new System.Drawing.Size(62, 42);
            this.lbLed_ELN.State = BasicClassLibrary.Controls.Leds.LBLed.LedState.Off;
            this.lbLed_ELN.Style = BasicClassLibrary.Controls.Leds.LBLed.LedStyle.Circular;
            this.lbLed_ELN.TabIndex = 3;
            // 
            // lbLed_DSTP
            // 
            this.lbLed_DSTP.BackColor = System.Drawing.Color.Transparent;
            this.lbLed_DSTP.BlinkInterval = 500;
            this.lbLed_DSTP.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbLed_DSTP.Label = "减速停止";
            this.lbLed_DSTP.LabelPosition = BasicClassLibrary.Controls.Leds.LBLed.LedLabelPosition.Top;
            this.lbLed_DSTP.LedColor = System.Drawing.Color.Red;
            this.lbLed_DSTP.LedSize = new System.Drawing.SizeF(10F, 10F);
            this.lbLed_DSTP.Location = new System.Drawing.Point(3, 147);
            this.lbLed_DSTP.Name = "lbLed_DSTP";
            this.lbLed_DSTP.Renderer = null;
            this.lbLed_DSTP.Size = new System.Drawing.Size(62, 44);
            this.lbLed_DSTP.State = BasicClassLibrary.Controls.Leds.LBLed.LedState.Off;
            this.lbLed_DSTP.Style = BasicClassLibrary.Controls.Leds.LBLed.LedStyle.Circular;
            this.lbLed_DSTP.TabIndex = 9;
            // 
            // lbLed_ELP
            // 
            this.lbLed_ELP.BackColor = System.Drawing.Color.Transparent;
            this.lbLed_ELP.BlinkInterval = 500;
            this.lbLed_ELP.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbLed_ELP.Label = "正硬限位";
            this.lbLed_ELP.LabelPosition = BasicClassLibrary.Controls.Leds.LBLed.LedLabelPosition.Top;
            this.lbLed_ELP.LedColor = System.Drawing.Color.Red;
            this.lbLed_ELP.LedSize = new System.Drawing.SizeF(10F, 10F);
            this.lbLed_ELP.Location = new System.Drawing.Point(71, 51);
            this.lbLed_ELP.Name = "lbLed_ELP";
            this.lbLed_ELP.Renderer = null;
            this.lbLed_ELP.Size = new System.Drawing.Size(62, 42);
            this.lbLed_ELP.State = BasicClassLibrary.Controls.Leds.LBLed.LedState.Off;
            this.lbLed_ELP.Style = BasicClassLibrary.Controls.Leds.LBLed.LedStyle.Circular;
            this.lbLed_ELP.TabIndex = 10;
            // 
            // lbLed_SLN
            // 
            this.lbLed_SLN.BackColor = System.Drawing.Color.Transparent;
            this.lbLed_SLN.BlinkInterval = 500;
            this.lbLed_SLN.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbLed_SLN.Label = "负软限位";
            this.lbLed_SLN.LabelPosition = BasicClassLibrary.Controls.Leds.LBLed.LedLabelPosition.Top;
            this.lbLed_SLN.LedColor = System.Drawing.Color.Red;
            this.lbLed_SLN.LedSize = new System.Drawing.SizeF(10F, 10F);
            this.lbLed_SLN.Location = new System.Drawing.Point(3, 99);
            this.lbLed_SLN.Name = "lbLed_SLN";
            this.lbLed_SLN.Renderer = null;
            this.lbLed_SLN.Size = new System.Drawing.Size(62, 42);
            this.lbLed_SLN.State = BasicClassLibrary.Controls.Leds.LBLed.LedState.Off;
            this.lbLed_SLN.Style = BasicClassLibrary.Controls.Leds.LBLed.LedStyle.Circular;
            this.lbLed_SLN.TabIndex = 11;
            // 
            // lbLed_SLP
            // 
            this.lbLed_SLP.BackColor = System.Drawing.Color.Transparent;
            this.lbLed_SLP.BlinkInterval = 500;
            this.lbLed_SLP.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbLed_SLP.Label = "正软限位";
            this.lbLed_SLP.LabelPosition = BasicClassLibrary.Controls.Leds.LBLed.LedLabelPosition.Top;
            this.lbLed_SLP.LedColor = System.Drawing.Color.Red;
            this.lbLed_SLP.LedSize = new System.Drawing.SizeF(10F, 10F);
            this.lbLed_SLP.Location = new System.Drawing.Point(71, 99);
            this.lbLed_SLP.Name = "lbLed_SLP";
            this.lbLed_SLP.Renderer = null;
            this.lbLed_SLP.Size = new System.Drawing.Size(62, 42);
            this.lbLed_SLP.State = BasicClassLibrary.Controls.Leds.LBLed.LedState.Off;
            this.lbLed_SLP.Style = BasicClassLibrary.Controls.Leds.LBLed.LedStyle.Circular;
            this.lbLed_SLP.TabIndex = 0;
            // 
            // lbLed_ORG
            // 
            this.lbLed_ORG.BackColor = System.Drawing.Color.Transparent;
            this.lbLed_ORG.BlinkInterval = 500;
            this.lbLed_ORG.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbLed_ORG.Label = "原点";
            this.lbLed_ORG.LabelPosition = BasicClassLibrary.Controls.Leds.LBLed.LedLabelPosition.Top;
            this.lbLed_ORG.LedColor = System.Drawing.Color.Red;
            this.lbLed_ORG.LedSize = new System.Drawing.SizeF(10F, 10F);
            this.lbLed_ORG.Location = new System.Drawing.Point(139, 99);
            this.lbLed_ORG.Name = "lbLed_ORG";
            this.lbLed_ORG.Renderer = null;
            this.lbLed_ORG.Size = new System.Drawing.Size(64, 42);
            this.lbLed_ORG.State = BasicClassLibrary.Controls.Leds.LBLed.LedState.Off;
            this.lbLed_ORG.Style = BasicClassLibrary.Controls.Leds.LBLed.LedStyle.Circular;
            this.lbLed_ORG.TabIndex = 6;
            // 
            // lbLed_EMG
            // 
            this.lbLed_EMG.BackColor = System.Drawing.Color.Transparent;
            this.lbLed_EMG.BlinkInterval = 500;
            this.lbLed_EMG.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbLed_EMG.Label = "急停";
            this.lbLed_EMG.LabelPosition = BasicClassLibrary.Controls.Leds.LBLed.LedLabelPosition.Top;
            this.lbLed_EMG.LedColor = System.Drawing.Color.Red;
            this.lbLed_EMG.LedSize = new System.Drawing.SizeF(10F, 10F);
            this.lbLed_EMG.Location = new System.Drawing.Point(71, 147);
            this.lbLed_EMG.Name = "lbLed_EMG";
            this.lbLed_EMG.Renderer = null;
            this.lbLed_EMG.Size = new System.Drawing.Size(62, 44);
            this.lbLed_EMG.State = BasicClassLibrary.Controls.Leds.LBLed.LedState.Off;
            this.lbLed_EMG.Style = BasicClassLibrary.Controls.Leds.LBLed.LedStyle.Circular;
            this.lbLed_EMG.TabIndex = 4;
            // 
            // lbLed_ERC
            // 
            this.lbLed_ERC.BackColor = System.Drawing.Color.Transparent;
            this.lbLed_ERC.BlinkInterval = 500;
            this.lbLed_ERC.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbLed_ERC.Label = "ERC";
            this.lbLed_ERC.LabelPosition = BasicClassLibrary.Controls.Leds.LBLed.LedLabelPosition.Top;
            this.lbLed_ERC.LedColor = System.Drawing.Color.Red;
            this.lbLed_ERC.LedSize = new System.Drawing.SizeF(10F, 10F);
            this.lbLed_ERC.Location = new System.Drawing.Point(139, 51);
            this.lbLed_ERC.Name = "lbLed_ERC";
            this.lbLed_ERC.Renderer = null;
            this.lbLed_ERC.Size = new System.Drawing.Size(64, 42);
            this.lbLed_ERC.State = BasicClassLibrary.Controls.Leds.LBLed.LedState.Off;
            this.lbLed_ERC.Style = BasicClassLibrary.Controls.Leds.LBLed.LedStyle.Circular;
            this.lbLed_ERC.TabIndex = 14;
            // 
            // AxisState
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.DoubleBuffered = true;
            this.Name = "AxisState";
            this.Size = new System.Drawing.Size(212, 263);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPosition)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownSpeed)).EndInit();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.tableLayoutPanel3.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        public Leds.LBLed lbLed_EZ;
        public Leds.LBLed lbLed_SLP;
        public System.Windows.Forms.NumericUpDown numericUpDownPosition;
        public Leds.LBLed lbLed_INP;
        public System.Windows.Forms.NumericUpDown numericUpDownSpeed;
        public Leds.LBLed lbLed_ALM;
        public Leds.LBLed lbLed_ELN;
        public Leds.LBLed lbLed_EMG;
        public Leds.LBLed lbLed_RDY;
        public Leds.LBLed lbLed_ORG;
        public Leds.LBLed lbLed_DSTP;
        public Leds.LBLed lbLed_ELP;
        public Leds.LBLed lbLed_SLN;
        public Leds.LBLed lbLed_SEVON;
        public Leds.LBLed lbLed_ERC;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private HoverTableLayoutPanel tableLayoutPanel3;
    }
}
