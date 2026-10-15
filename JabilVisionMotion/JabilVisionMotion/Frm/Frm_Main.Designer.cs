namespace JabilVisionMotion.Frm
{
    partial class Frm_Main
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Frm_Main));
            this.elementHost1 = new System.Windows.Forms.Integration.ElementHost();
            this.toolStrip_工具栏 = new System.Windows.Forms.ToolStrip();
            this.toolStripLabel_截屏 = new System.Windows.Forms.ToolStripLabel();
            this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripLabel_锁定 = new System.Windows.Forms.ToolStripLabel();
            this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripLabel_虚拟键盘 = new System.Windows.Forms.ToolStripLabel();
            this.toolStripLabel_LOG显示 = new System.Windows.Forms.ToolStripLabel();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripLabel_日志显示 = new System.Windows.Forms.ToolStripLabel();
            this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripLabel_下料安全门打开 = new System.Windows.Forms.ToolStripLabel();
            this.toolStripLabel_上料安全门打开 = new System.Windows.Forms.ToolStripLabel();
            this.toolStripLabel_机台安全门打开 = new System.Windows.Forms.ToolStripLabel();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripLabel_蜂鸣器抑制 = new System.Windows.Forms.ToolStripLabel();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripLabel_STO复位 = new System.Windows.Forms.ToolStripLabel();
            this.statusStrip_状态栏 = new System.Windows.Forms.StatusStrip();
            this.Run_Memory = new System.Windows.Forms.ToolStripStatusLabel();
            this.Run_CPU = new System.Windows.Forms.ToolStripStatusLabel();
            this.tss_CurTime = new System.Windows.Forms.ToolStripStatusLabel();
            this.ts_RunTime = new System.Windows.Forms.ToolStripStatusLabel();
            this.tss_PermissionInfo = new System.Windows.Forms.ToolStripStatusLabel();
            this.lbl_CurEngine = new System.Windows.Forms.ToolStripStatusLabel();
            this.tssButton_TCPIP_State = new System.Windows.Forms.ToolStripSplitButton();
            this.tss_生产模式 = new System.Windows.Forms.ToolStripStatusLabel();
            this.tssButton_Modbus_State = new System.Windows.Forms.ToolStripSplitButton();
            this.toolStripStatusLabel_单联机 = new System.Windows.Forms.ToolStripStatusLabel();
            this.tss_SystemStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripProgressBar1 = new System.Windows.Forms.ToolStripProgressBar();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.uiStyleManager1 = new Sunny.UI.UIStyleManager(this.components);
            this.uiPanel_window = new Sunny.UI.UIPanel();
            this.uiPanel_状态栏底色 = new Sunny.UI.UIPanel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.toolStrip_工具栏.SuspendLayout();
            this.statusStrip_状态栏.SuspendLayout();
            this.uiPanel_window.SuspendLayout();
            this.uiPanel_状态栏底色.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // elementHost1
            // 
            this.elementHost1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.elementHost1.Dock = System.Windows.Forms.DockStyle.Top;
            this.elementHost1.Location = new System.Drawing.Point(0, 35);
            this.elementHost1.Name = "elementHost1";
            this.elementHost1.Size = new System.Drawing.Size(1241, 74);
            this.elementHost1.TabIndex = 1;
            this.elementHost1.Text = "elementHost1";
            this.elementHost1.Child = null;
            // 
            // toolStrip_工具栏
            // 
            this.toolStrip_工具栏.AllowItemReorder = true;
            this.toolStrip_工具栏.AllowMerge = false;
            this.toolStrip_工具栏.AutoSize = false;
            this.toolStrip_工具栏.BackColor = System.Drawing.Color.Transparent;
            this.toolStrip_工具栏.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.toolStrip_工具栏.Font = new System.Drawing.Font("新宋体", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.toolStrip_工具栏.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip_工具栏.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripLabel_截屏,
            this.toolStripSeparator6,
            this.toolStripLabel_锁定,
            this.toolStripSeparator7,
            this.toolStripLabel_虚拟键盘,
            this.toolStripLabel_LOG显示,
            this.toolStripSeparator1,
            this.toolStripLabel_日志显示,
            this.toolStripSeparator8,
            this.toolStripLabel_下料安全门打开,
            this.toolStripLabel_上料安全门打开,
            this.toolStripLabel_机台安全门打开,
            this.toolStripSeparator2,
            this.toolStripLabel_蜂鸣器抑制,
            this.toolStripSeparator4,
            this.toolStripLabel_STO复位});
            this.toolStrip_工具栏.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow;
            this.toolStrip_工具栏.Location = new System.Drawing.Point(0, 0);
            this.toolStrip_工具栏.Name = "toolStrip_工具栏";
            this.toolStrip_工具栏.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this.toolStrip_工具栏.ShowItemToolTips = false;
            this.toolStrip_工具栏.Size = new System.Drawing.Size(1241, 29);
            this.toolStrip_工具栏.Stretch = true;
            this.toolStrip_工具栏.TabIndex = 136;
            this.toolStrip_工具栏.Text = "toolStrip2";
            // 
            // toolStripLabel_截屏
            // 
            this.toolStripLabel_截屏.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.toolStripLabel_截屏.ForeColor = System.Drawing.Color.Gray;
            this.toolStripLabel_截屏.Image = ((System.Drawing.Image)(resources.GetObject("toolStripLabel_截屏.Image")));
            this.toolStripLabel_截屏.Name = "toolStripLabel_截屏";
            this.toolStripLabel_截屏.Size = new System.Drawing.Size(61, 26);
            this.toolStripLabel_截屏.Text = "截图";
            this.toolStripLabel_截屏.Click += new System.EventHandler(this.toolStripLabel_截屏_Click);
            // 
            // toolStripSeparator6
            // 
            this.toolStripSeparator6.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.toolStripSeparator6.Name = "toolStripSeparator6";
            this.toolStripSeparator6.Size = new System.Drawing.Size(6, 29);
            // 
            // toolStripLabel_锁定
            // 
            this.toolStripLabel_锁定.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.toolStripLabel_锁定.ForeColor = System.Drawing.Color.Gray;
            this.toolStripLabel_锁定.Image = ((System.Drawing.Image)(resources.GetObject("toolStripLabel_锁定.Image")));
            this.toolStripLabel_锁定.Name = "toolStripLabel_锁定";
            this.toolStripLabel_锁定.Size = new System.Drawing.Size(61, 26);
            this.toolStripLabel_锁定.Text = "锁定";
            this.toolStripLabel_锁定.Click += new System.EventHandler(this.toolStripLabel_锁定_Click);
            // 
            // toolStripSeparator7
            // 
            this.toolStripSeparator7.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.toolStripSeparator7.Name = "toolStripSeparator7";
            this.toolStripSeparator7.Size = new System.Drawing.Size(6, 29);
            // 
            // toolStripLabel_虚拟键盘
            // 
            this.toolStripLabel_虚拟键盘.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.toolStripLabel_虚拟键盘.ForeColor = System.Drawing.Color.Gray;
            this.toolStripLabel_虚拟键盘.Image = global::JabilVisionMotion.Properties.Resources.虚拟键盘_未点击;
            this.toolStripLabel_虚拟键盘.Name = "toolStripLabel_虚拟键盘";
            this.toolStripLabel_虚拟键盘.Size = new System.Drawing.Size(91, 26);
            this.toolStripLabel_虚拟键盘.Text = "虚拟键盘";
            this.toolStripLabel_虚拟键盘.Click += new System.EventHandler(this.toolStripLabel_虚拟键盘_Click);
            // 
            // toolStripLabel_LOG显示
            // 
            this.toolStripLabel_LOG显示.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripLabel_LOG显示.BackColor = System.Drawing.Color.Transparent;
            this.toolStripLabel_LOG显示.ForeColor = System.Drawing.Color.Gray;
            this.toolStripLabel_LOG显示.Name = "toolStripLabel_LOG显示";
            this.toolStripLabel_LOG显示.Size = new System.Drawing.Size(87, 26);
            this.toolStripLabel_LOG显示.Text = "..........";
            this.toolStripLabel_LOG显示.MouseDown += new System.Windows.Forms.MouseEventHandler(this.toolStripLabel_LOG显示_MouseDown);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 29);
            // 
            // toolStripLabel_日志显示
            // 
            this.toolStripLabel_日志显示.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.toolStripLabel_日志显示.ForeColor = System.Drawing.Color.Gray;
            this.toolStripLabel_日志显示.Image = global::JabilVisionMotion.Properties.Resources.虚拟键盘_未点击;
            this.toolStripLabel_日志显示.Name = "toolStripLabel_日志显示";
            this.toolStripLabel_日志显示.Size = new System.Drawing.Size(91, 26);
            this.toolStripLabel_日志显示.Text = "显示日志";
            this.toolStripLabel_日志显示.Click += new System.EventHandler(this.toolStripLabel_日志显示_Click);
            // 
            // toolStripSeparator8
            // 
            this.toolStripSeparator8.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripSeparator8.Name = "toolStripSeparator8";
            this.toolStripSeparator8.Size = new System.Drawing.Size(6, 29);
            // 
            // toolStripLabel_下料安全门打开
            // 
            this.toolStripLabel_下料安全门打开.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripLabel_下料安全门打开.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.toolStripLabel_下料安全门打开.ForeColor = System.Drawing.Color.DarkGray;
            this.toolStripLabel_下料安全门打开.Image = global::JabilVisionMotion.Properties.Resources.虚拟键盘_未点击;
            this.toolStripLabel_下料安全门打开.Name = "toolStripLabel_下料安全门打开";
            this.toolStripLabel_下料安全门打开.Size = new System.Drawing.Size(121, 26);
            this.toolStripLabel_下料安全门打开.Text = "后安全门打开";
            this.toolStripLabel_下料安全门打开.Visible = false;
            // 
            // toolStripLabel_上料安全门打开
            // 
            this.toolStripLabel_上料安全门打开.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripLabel_上料安全门打开.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.toolStripLabel_上料安全门打开.ForeColor = System.Drawing.Color.DarkGray;
            this.toolStripLabel_上料安全门打开.Image = global::JabilVisionMotion.Properties.Resources.虚拟键盘_未点击;
            this.toolStripLabel_上料安全门打开.Name = "toolStripLabel_上料安全门打开";
            this.toolStripLabel_上料安全门打开.Size = new System.Drawing.Size(121, 26);
            this.toolStripLabel_上料安全门打开.Text = "前安全门打开";
            this.toolStripLabel_上料安全门打开.Visible = false;
            // 
            // toolStripLabel_机台安全门打开
            // 
            this.toolStripLabel_机台安全门打开.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripLabel_机台安全门打开.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.toolStripLabel_机台安全门打开.ForeColor = System.Drawing.Color.DarkGray;
            this.toolStripLabel_机台安全门打开.Image = global::JabilVisionMotion.Properties.Resources.虚拟键盘_未点击;
            this.toolStripLabel_机台安全门打开.Name = "toolStripLabel_机台安全门打开";
            this.toolStripLabel_机台安全门打开.Size = new System.Drawing.Size(136, 26);
            this.toolStripLabel_机台安全门打开.Text = "机台安全门打开";
            this.toolStripLabel_机台安全门打开.Click += new System.EventHandler(this.toolStripLabel_机台安全门打开_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 29);
            // 
            // toolStripLabel_蜂鸣器抑制
            // 
            this.toolStripLabel_蜂鸣器抑制.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripLabel_蜂鸣器抑制.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.toolStripLabel_蜂鸣器抑制.ForeColor = System.Drawing.Color.Gray;
            this.toolStripLabel_蜂鸣器抑制.Image = global::JabilVisionMotion.Properties.Resources.虚拟键盘_未点击;
            this.toolStripLabel_蜂鸣器抑制.Name = "toolStripLabel_蜂鸣器抑制";
            this.toolStripLabel_蜂鸣器抑制.Size = new System.Drawing.Size(76, 26);
            this.toolStripLabel_蜂鸣器抑制.Text = "蜂鸣器";
            this.toolStripLabel_蜂鸣器抑制.Click += new System.EventHandler(this.toolStripLabel_蜂鸣器抑制_Click);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(6, 29);
            // 
            // toolStripLabel_STO复位
            // 
            this.toolStripLabel_STO复位.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripLabel_STO复位.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.toolStripLabel_STO复位.ForeColor = System.Drawing.Color.Gray;
            this.toolStripLabel_STO复位.Image = global::JabilVisionMotion.Properties.Resources.虚拟键盘_未点击;
            this.toolStripLabel_STO复位.Name = "toolStripLabel_STO复位";
            this.toolStripLabel_STO复位.Size = new System.Drawing.Size(85, 26);
            this.toolStripLabel_STO复位.Text = "STO复位";
            this.toolStripLabel_STO复位.Click += new System.EventHandler(this.toolStripLabel_STO复位_Click);
            // 
            // statusStrip_状态栏
            // 
            this.statusStrip_状态栏.AllowItemReorder = true;
            this.statusStrip_状态栏.BackColor = System.Drawing.Color.Transparent;
            this.statusStrip_状态栏.Dock = System.Windows.Forms.DockStyle.Fill;
            this.statusStrip_状态栏.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.statusStrip_状态栏.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Run_Memory,
            this.Run_CPU,
            this.tss_CurTime,
            this.ts_RunTime,
            this.tss_PermissionInfo,
            this.lbl_CurEngine,
            this.tssButton_TCPIP_State,
            this.tss_生产模式,
            this.tssButton_Modbus_State,
            this.toolStripStatusLabel_单联机,
            this.tss_SystemStatus,
            this.toolStripProgressBar1,
            this.toolStripStatusLabel1});
            this.statusStrip_状态栏.Location = new System.Drawing.Point(0, 0);
            this.statusStrip_状态栏.Name = "statusStrip_状态栏";
            this.statusStrip_状态栏.Padding = new System.Windows.Forms.Padding(1, 0, 17, 0);
            this.statusStrip_状态栏.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this.statusStrip_状态栏.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.statusStrip_状态栏.Size = new System.Drawing.Size(1241, 30);
            this.statusStrip_状态栏.TabIndex = 140;
            this.statusStrip_状态栏.Text = "statusStrip1";
            // 
            // Run_Memory
            // 
            this.Run_Memory.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.Run_Memory.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)));
            this.Run_Memory.ForeColor = System.Drawing.Color.White;
            this.Run_Memory.Name = "Run_Memory";
            this.Run_Memory.Size = new System.Drawing.Size(88, 25);
            this.Run_Memory.Text = "Run_Memory";
            // 
            // Run_CPU
            // 
            this.Run_CPU.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.Run_CPU.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)));
            this.Run_CPU.ForeColor = System.Drawing.Color.White;
            this.Run_CPU.Name = "Run_CPU";
            this.Run_CPU.Size = new System.Drawing.Size(63, 25);
            this.Run_CPU.Text = "Run_CPU";
            // 
            // tss_CurTime
            // 
            this.tss_CurTime.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.tss_CurTime.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)));
            this.tss_CurTime.ForeColor = System.Drawing.Color.White;
            this.tss_CurTime.Name = "tss_CurTime";
            this.tss_CurTime.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.tss_CurTime.Size = new System.Drawing.Size(130, 25);
            this.tss_CurTime.Text = "2020/01/01 00:00:00";
            // 
            // ts_RunTime
            // 
            this.ts_RunTime.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.ts_RunTime.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)));
            this.ts_RunTime.ForeColor = System.Drawing.Color.White;
            this.ts_RunTime.Name = "ts_RunTime";
            this.ts_RunTime.Size = new System.Drawing.Size(62, 25);
            this.ts_RunTime.Text = "RunTime";
            // 
            // tss_PermissionInfo
            // 
            this.tss_PermissionInfo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.tss_PermissionInfo.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)));
            this.tss_PermissionInfo.ForeColor = System.Drawing.Color.White;
            this.tss_PermissionInfo.Name = "tss_PermissionInfo";
            this.tss_PermissionInfo.Size = new System.Drawing.Size(108, 25);
            this.tss_PermissionInfo.Text = "当前用户：未登录";
            // 
            // lbl_CurEngine
            // 
            this.lbl_CurEngine.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.lbl_CurEngine.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)));
            this.lbl_CurEngine.ForeColor = System.Drawing.Color.White;
            this.lbl_CurEngine.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.lbl_CurEngine.Name = "lbl_CurEngine";
            this.lbl_CurEngine.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.lbl_CurEngine.Size = new System.Drawing.Size(108, 25);
            this.lbl_CurEngine.Text = "当前方案：未命名";
            this.lbl_CurEngine.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tssButton_TCPIP_State
            // 
            this.tssButton_TCPIP_State.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tssButton_TCPIP_State.DoubleClickEnabled = true;
            this.tssButton_TCPIP_State.ForeColor = System.Drawing.Color.Red;
            this.tssButton_TCPIP_State.Image = ((System.Drawing.Image)(resources.GetObject("tssButton_TCPIP_State.Image")));
            this.tssButton_TCPIP_State.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tssButton_TCPIP_State.Name = "tssButton_TCPIP_State";
            this.tssButton_TCPIP_State.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.tssButton_TCPIP_State.Size = new System.Drawing.Size(81, 28);
            this.tssButton_TCPIP_State.Text = "TCPIP通讯";
            this.tssButton_TCPIP_State.ButtonDoubleClick += new System.EventHandler(this.tssButton_TCPIP_State_ButtonDoubleClick);
            // 
            // tss_生产模式
            // 
            this.tss_生产模式.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.tss_生产模式.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)));
            this.tss_生产模式.Font = new System.Drawing.Font("楷体", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tss_生产模式.ForeColor = System.Drawing.Color.Black;
            this.tss_生产模式.Name = "tss_生产模式";
            this.tss_生产模式.Size = new System.Drawing.Size(4, 25);
            // 
            // tssButton_Modbus_State
            // 
            this.tssButton_Modbus_State.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tssButton_Modbus_State.DoubleClickEnabled = true;
            this.tssButton_Modbus_State.ForeColor = System.Drawing.Color.Red;
            this.tssButton_Modbus_State.Image = ((System.Drawing.Image)(resources.GetObject("tssButton_Modbus_State.Image")));
            this.tssButton_Modbus_State.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tssButton_Modbus_State.Name = "tssButton_Modbus_State";
            this.tssButton_Modbus_State.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.tssButton_Modbus_State.Size = new System.Drawing.Size(97, 28);
            this.tssButton_Modbus_State.Text = "Modbus通讯";
            this.tssButton_Modbus_State.ButtonDoubleClick += new System.EventHandler(this.tssButton_Modbus_State_ButtonDoubleClick);
            // 
            // toolStripStatusLabel_单联机
            // 
            this.toolStripStatusLabel_单联机.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.toolStripStatusLabel_单联机.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)));
            this.toolStripStatusLabel_单联机.Font = new System.Drawing.Font("楷体", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.toolStripStatusLabel_单联机.ForeColor = System.Drawing.Color.Black;
            this.toolStripStatusLabel_单联机.Name = "toolStripStatusLabel_单联机";
            this.toolStripStatusLabel_单联机.Size = new System.Drawing.Size(61, 25);
            this.toolStripStatusLabel_单联机.Text = "单机模式";
            // 
            // tss_SystemStatus
            // 
            this.tss_SystemStatus.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.tss_SystemStatus.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)));
            this.tss_SystemStatus.Font = new System.Drawing.Font("楷体", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tss_SystemStatus.ForeColor = System.Drawing.Color.Black;
            this.tss_SystemStatus.Name = "tss_SystemStatus";
            this.tss_SystemStatus.Size = new System.Drawing.Size(51, 25);
            this.tss_SystemStatus.Text = "Online";
            // 
            // toolStripProgressBar1
            // 
            this.toolStripProgressBar1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.toolStripProgressBar1.Name = "toolStripProgressBar1";
            this.toolStripProgressBar1.Size = new System.Drawing.Size(100, 24);
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.toolStripStatusLabel1.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)));
            this.toolStripStatusLabel1.Font = new System.Drawing.Font("楷体", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.toolStripStatusLabel1.ForeColor = System.Drawing.Color.Black;
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(4, 25);
            // 
            // uiStyleManager1
            // 
            this.uiStyleManager1.DPIScale = true;
            this.uiStyleManager1.GlobalFont = true;
            this.uiStyleManager1.GlobalFontName = "Tahoma";
            // 
            // uiPanel_window
            // 
            this.uiPanel_window.Controls.Add(this.toolStrip_工具栏);
            this.uiPanel_window.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiPanel_window.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.uiPanel_window.FillColorGradient = true;
            this.uiPanel_window.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiPanel_window.Location = new System.Drawing.Point(0, 0);
            this.uiPanel_window.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.uiPanel_window.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiPanel_window.Name = "uiPanel_window";
            this.uiPanel_window.Size = new System.Drawing.Size(1241, 442);
            this.uiPanel_window.TabIndex = 141;
            this.uiPanel_window.Text = null;
            this.uiPanel_window.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // uiPanel_状态栏底色
            // 
            this.uiPanel_状态栏底色.Controls.Add(this.statusStrip_状态栏);
            this.uiPanel_状态栏底色.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.uiPanel_状态栏底色.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.uiPanel_状态栏底色.FillColorGradient = true;
            this.uiPanel_状态栏底色.FillColorGradientDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.uiPanel_状态栏底色.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiPanel_状态栏底色.Location = new System.Drawing.Point(0, 551);
            this.uiPanel_状态栏底色.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.uiPanel_状态栏底色.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiPanel_状态栏底色.Name = "uiPanel_状态栏底色";
            this.uiPanel_状态栏底色.Size = new System.Drawing.Size(1241, 30);
            this.uiPanel_状态栏底色.TabIndex = 141;
            this.uiPanel_状态栏底色.Text = null;
            this.uiPanel_状态栏底色.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.uiPanel_window);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 109);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1241, 442);
            this.panel1.TabIndex = 144;
            // 
            // Frm_Main
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1241, 581);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.uiPanel_状态栏底色);
            this.Controls.Add(this.elementHost1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Frm_Main";
            this.ShowIcon = false;
            this.Text = "捷普（中国）自动化有限公司";
            this.TitleColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.ZoomScaleRect = new System.Drawing.Rectangle(15, 15, 800, 450);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Frm_Main_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Frm_Main_FormClosed);
            this.Load += new System.EventHandler(this.Frm_Main_Load);
            this.Shown += new System.EventHandler(this.Frm_Main_Shown);
            this.toolStrip_工具栏.ResumeLayout(false);
            this.toolStrip_工具栏.PerformLayout();
            this.statusStrip_状态栏.ResumeLayout(false);
            this.statusStrip_状态栏.PerformLayout();
            this.uiPanel_window.ResumeLayout(false);
            this.uiPanel_状态栏底色.ResumeLayout(false);
            this.uiPanel_状态栏底色.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Integration.ElementHost elementHost1;
        public System.Windows.Forms.ToolStrip toolStrip_工具栏;
        public System.Windows.Forms.ToolStripLabel toolStripLabel_截屏;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
        public System.Windows.Forms.ToolStripLabel toolStripLabel_锁定;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
        public System.Windows.Forms.ToolStripLabel toolStripLabel_虚拟键盘;
        public System.Windows.Forms.ToolStripLabel toolStripLabel_LOG显示;
        public System.Windows.Forms.ToolStripLabel toolStripLabel_日志显示;
        private System.Windows.Forms.ToolStripStatusLabel Run_Memory;
        private System.Windows.Forms.ToolStripStatusLabel Run_CPU;
        public System.Windows.Forms.ToolStripStatusLabel tss_CurTime;
        private System.Windows.Forms.ToolStripStatusLabel ts_RunTime;
        public System.Windows.Forms.ToolStripStatusLabel tss_PermissionInfo;
        public System.Windows.Forms.ToolStripStatusLabel lbl_CurEngine;
        public System.Windows.Forms.ToolStripStatusLabel tss_生产模式;
        public System.Windows.Forms.ToolStripStatusLabel tss_SystemStatus;
        private System.Windows.Forms.ToolStripProgressBar toolStripProgressBar1;
        private Sunny.UI.UIStyleManager uiStyleManager1;
        public Sunny.UI.UIPanel uiPanel_window;
        private Sunny.UI.UIPanel uiPanel_状态栏底色;
        public System.Windows.Forms.StatusStrip statusStrip_状态栏;
        public System.Windows.Forms.ToolStripSplitButton tssButton_TCPIP_State;
        public System.Windows.Forms.ToolStripSplitButton tssButton_Modbus_State;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        public System.Windows.Forms.ToolStripLabel toolStripLabel_蜂鸣器抑制;
        public System.Windows.Forms.ToolStripLabel toolStripLabel_STO复位;
        public System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel_单联机;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        public System.Windows.Forms.ToolStripLabel toolStripLabel_下料安全门打开;
        public System.Windows.Forms.ToolStripLabel toolStripLabel_机台安全门打开;
        public System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.Panel panel1;
        public System.Windows.Forms.ToolStripLabel toolStripLabel_上料安全门打开;
    }
}