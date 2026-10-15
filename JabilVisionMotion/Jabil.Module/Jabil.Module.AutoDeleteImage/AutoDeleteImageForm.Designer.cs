namespace Jabil.Module.AutoDeleteImage
{
    partial class AutoDeleteImageForm
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

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AutoDeleteImageForm));
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.groupSettings = new System.Windows.Forms.GroupBox();
            this.chkAutoHideOnStart = new System.Windows.Forms.CheckBox();
            this.chkAutoStartMonitoring = new System.Windows.Forms.CheckBox();
            this.chkAutoStart = new System.Windows.Forms.CheckBox();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.numericCheckInterval = new System.Windows.Forms.NumericUpDown();
            this.chkConfirmDeletion = new System.Windows.Forms.CheckBox();
            this.chkIncludeSubfolders = new System.Windows.Forms.CheckBox();
            this.btnSaveSettings = new System.Windows.Forms.Button();
            this.txtNGKeyword = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.txtOKKeyword = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.btnBrowseFolder = new System.Windows.Forms.Button();
            this.txtFolderPath = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.numericNGDays = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.numericOKDays = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.groupOperations = new System.Windows.Forms.GroupBox();
            this.btnManualDelete = new System.Windows.Forms.Button();
            this.btnStopMonitoring = new System.Windows.Forms.Button();
            this.btnStartMonitoring = new System.Windows.Forms.Button();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.groupLog = new System.Windows.Forms.GroupBox();
            this.btnClearLog = new System.Windows.Forms.Button();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.groupSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericCheckInterval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericNGDays)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericOKDays)).BeginInit();
            this.groupOperations.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.groupLog.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(800, 550);
            this.tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.groupSettings);
            this.tabPage1.Controls.Add(this.groupOperations);
            this.tabPage1.Location = new System.Drawing.Point(4, 25);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(792, 521);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "设置与操作";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // groupSettings
            // 
            this.groupSettings.Controls.Add(this.chkAutoHideOnStart);
            this.groupSettings.Controls.Add(this.chkAutoStartMonitoring);
            this.groupSettings.Controls.Add(this.chkAutoStart);
            this.groupSettings.Controls.Add(this.label8);
            this.groupSettings.Controls.Add(this.label7);
            this.groupSettings.Controls.Add(this.numericCheckInterval);
            this.groupSettings.Controls.Add(this.chkConfirmDeletion);
            this.groupSettings.Controls.Add(this.chkIncludeSubfolders);
            this.groupSettings.Controls.Add(this.btnSaveSettings);
            this.groupSettings.Controls.Add(this.txtNGKeyword);
            this.groupSettings.Controls.Add(this.label6);
            this.groupSettings.Controls.Add(this.txtOKKeyword);
            this.groupSettings.Controls.Add(this.label5);
            this.groupSettings.Controls.Add(this.btnBrowseFolder);
            this.groupSettings.Controls.Add(this.txtFolderPath);
            this.groupSettings.Controls.Add(this.label4);
            this.groupSettings.Controls.Add(this.numericNGDays);
            this.groupSettings.Controls.Add(this.label3);
            this.groupSettings.Controls.Add(this.numericOKDays);
            this.groupSettings.Controls.Add(this.label2);
            this.groupSettings.Controls.Add(this.label1);
            this.groupSettings.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupSettings.Location = new System.Drawing.Point(3, 103);
            this.groupSettings.Name = "groupSettings";
            this.groupSettings.Padding = new System.Windows.Forms.Padding(10);
            this.groupSettings.Size = new System.Drawing.Size(786, 320);
            this.groupSettings.TabIndex = 1;
            this.groupSettings.TabStop = false;
            this.groupSettings.Text = "设置";
            // 
            // chkAutoHideOnStart
            // 
            this.chkAutoHideOnStart.AutoSize = true;
            this.chkAutoHideOnStart.Checked = true;
            this.chkAutoHideOnStart.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkAutoHideOnStart.Location = new System.Drawing.Point(526, 245);
            this.chkAutoHideOnStart.Name = "chkAutoHideOnStart";
            this.chkAutoHideOnStart.Size = new System.Drawing.Size(119, 19);
            this.chkAutoHideOnStart.TabIndex = 19;
            this.chkAutoHideOnStart.Text = "开机自动隐藏";
            this.chkAutoHideOnStart.UseVisualStyleBackColor = true;
            this.chkAutoHideOnStart.CheckedChanged += new System.EventHandler(this.chkAutoHideOnStart_CheckedChanged);
            // 
            // chkAutoStartMonitoring
            // 
            this.chkAutoStartMonitoring.AutoSize = true;
            this.chkAutoStartMonitoring.Location = new System.Drawing.Point(402, 281);
            this.chkAutoStartMonitoring.Name = "chkAutoStartMonitoring";
            this.chkAutoStartMonitoring.Size = new System.Drawing.Size(119, 19);
            this.chkAutoStartMonitoring.TabIndex = 18;
            this.chkAutoStartMonitoring.Text = "自动开启监控";
            this.chkAutoStartMonitoring.UseVisualStyleBackColor = true;
            this.chkAutoStartMonitoring.CheckedChanged += new System.EventHandler(this.chkAutoStartMonitoring_CheckedChanged);
            // 
            // chkAutoStart
            // 
            this.chkAutoStart.AutoSize = true;
            this.chkAutoStart.Location = new System.Drawing.Point(402, 245);
            this.chkAutoStart.Name = "chkAutoStart";
            this.chkAutoStart.Size = new System.Drawing.Size(104, 19);
            this.chkAutoStart.TabIndex = 17;
            this.chkAutoStart.Text = "开机自启动";
            this.chkAutoStart.UseVisualStyleBackColor = true;
            this.chkAutoStart.CheckedChanged += new System.EventHandler(this.chkAutoStart_CheckedChanged);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(361, 133);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(347, 15);
            this.label8.TabIndex = 16;
            this.label8.Text = "文件名关键字可以通过逗号 (,) 来分隔多个关键字";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(13, 245);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(151, 15);
            this.label7.TabIndex = 15;
            this.label7.Text = "自动检查间隔(分钟):";
            // 
            // numericCheckInterval
            // 
            this.numericCheckInterval.Location = new System.Drawing.Point(170, 243);
            this.numericCheckInterval.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericCheckInterval.Name = "numericCheckInterval";
            this.numericCheckInterval.Size = new System.Drawing.Size(120, 25);
            this.numericCheckInterval.TabIndex = 14;
            this.numericCheckInterval.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // chkConfirmDeletion
            // 
            this.chkConfirmDeletion.AutoSize = true;
            this.chkConfirmDeletion.Location = new System.Drawing.Point(140, 212);
            this.chkConfirmDeletion.Name = "chkConfirmDeletion";
            this.chkConfirmDeletion.Size = new System.Drawing.Size(134, 19);
            this.chkConfirmDeletion.TabIndex = 13;
            this.chkConfirmDeletion.Text = "删除前进行确认";
            this.chkConfirmDeletion.UseVisualStyleBackColor = true;
            // 
            // chkIncludeSubfolders
            // 
            this.chkIncludeSubfolders.AutoSize = true;
            this.chkIncludeSubfolders.Location = new System.Drawing.Point(140, 187);
            this.chkIncludeSubfolders.Name = "chkIncludeSubfolders";
            this.chkIncludeSubfolders.Size = new System.Drawing.Size(119, 19);
            this.chkIncludeSubfolders.TabIndex = 12;
            this.chkIncludeSubfolders.Text = "包含子文件夹";
            this.chkIncludeSubfolders.UseVisualStyleBackColor = true;
            // 
            // btnSaveSettings
            // 
            this.btnSaveSettings.Location = new System.Drawing.Point(650, 270);
            this.btnSaveSettings.Name = "btnSaveSettings";
            this.btnSaveSettings.Size = new System.Drawing.Size(120, 30);
            this.btnSaveSettings.TabIndex = 11;
            this.btnSaveSettings.Text = "保存设置";
            this.btnSaveSettings.UseVisualStyleBackColor = true;
            this.btnSaveSettings.Click += new System.EventHandler(this.btnSaveSettings_Click);
            // 
            // txtNGKeyword
            // 
            this.txtNGKeyword.Location = new System.Drawing.Point(140, 154);
            this.txtNGKeyword.Name = "txtNGKeyword";
            this.txtNGKeyword.Size = new System.Drawing.Size(200, 25);
            this.txtNGKeyword.TabIndex = 10;
            this.txtNGKeyword.Text = "NG";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(13, 157);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(106, 15);
            this.label6.TabIndex = 9;
            this.label6.Text = "NG图片关键字:";
            // 
            // txtOKKeyword
            // 
            this.txtOKKeyword.Location = new System.Drawing.Point(140, 123);
            this.txtOKKeyword.Name = "txtOKKeyword";
            this.txtOKKeyword.Size = new System.Drawing.Size(200, 25);
            this.txtOKKeyword.TabIndex = 8;
            this.txtOKKeyword.Text = "OK";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(13, 126);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(106, 15);
            this.label5.TabIndex = 7;
            this.label5.Text = "OK图片关键字:";
            // 
            // btnBrowseFolder
            // 
            this.btnBrowseFolder.Location = new System.Drawing.Point(650, 87);
            this.btnBrowseFolder.Name = "btnBrowseFolder";
            this.btnBrowseFolder.Size = new System.Drawing.Size(120, 30);
            this.btnBrowseFolder.TabIndex = 6;
            this.btnBrowseFolder.Text = "浏览...";
            this.btnBrowseFolder.UseVisualStyleBackColor = true;
            this.btnBrowseFolder.Click += new System.EventHandler(this.btnBrowseFolder_Click);
            // 
            // txtFolderPath
            // 
            this.txtFolderPath.Location = new System.Drawing.Point(140, 90);
            this.txtFolderPath.Name = "txtFolderPath";
            this.txtFolderPath.Size = new System.Drawing.Size(490, 25);
            this.txtFolderPath.TabIndex = 5;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(13, 93);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(90, 15);
            this.label4.TabIndex = 4;
            this.label4.Text = "监控文件夹:";
            // 
            // numericNGDays
            // 
            this.numericNGDays.Location = new System.Drawing.Point(140, 59);
            this.numericNGDays.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericNGDays.Name = "numericNGDays";
            this.numericNGDays.Size = new System.Drawing.Size(120, 25);
            this.numericNGDays.TabIndex = 3;
            this.numericNGDays.Value = new decimal(new int[] {
            7,
            0,
            0,
            0});
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(13, 61);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(121, 15);
            this.label3.TabIndex = 2;
            this.label3.Text = "NG图片保留天数:";
            // 
            // numericOKDays
            // 
            this.numericOKDays.Location = new System.Drawing.Point(140, 28);
            this.numericOKDays.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericOKDays.Name = "numericOKDays";
            this.numericOKDays.Size = new System.Drawing.Size(120, 25);
            this.numericOKDays.TabIndex = 1;
            this.numericOKDays.Value = new decimal(new int[] {
            30,
            0,
            0,
            0});
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 30);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(121, 15);
            this.label2.TabIndex = 0;
            this.label2.Text = "OK图片保留天数:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 187);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(75, 15);
            this.label1.TabIndex = 0;
            this.label1.Text = "高级选项:";
            // 
            // groupOperations
            // 
            this.groupOperations.Controls.Add(this.btnManualDelete);
            this.groupOperations.Controls.Add(this.btnStopMonitoring);
            this.groupOperations.Controls.Add(this.btnStartMonitoring);
            this.groupOperations.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupOperations.Location = new System.Drawing.Point(3, 3);
            this.groupOperations.Name = "groupOperations";
            this.groupOperations.Padding = new System.Windows.Forms.Padding(10);
            this.groupOperations.Size = new System.Drawing.Size(786, 100);
            this.groupOperations.TabIndex = 0;
            this.groupOperations.TabStop = false;
            this.groupOperations.Text = "操作";
            // 
            // btnManualDelete
            // 
            this.btnManualDelete.Location = new System.Drawing.Point(450, 30);
            this.btnManualDelete.Name = "btnManualDelete";
            this.btnManualDelete.Size = new System.Drawing.Size(150, 40);
            this.btnManualDelete.TabIndex = 2;
            this.btnManualDelete.Text = "手动删除过期图片";
            this.btnManualDelete.UseVisualStyleBackColor = true;
            this.btnManualDelete.Click += new System.EventHandler(this.btnManualDelete_Click);
            // 
            // btnStopMonitoring
            // 
            this.btnStopMonitoring.Location = new System.Drawing.Point(250, 30);
            this.btnStopMonitoring.Name = "btnStopMonitoring";
            this.btnStopMonitoring.Size = new System.Drawing.Size(150, 40);
            this.btnStopMonitoring.TabIndex = 1;
            this.btnStopMonitoring.Text = "停止自动监控";
            this.btnStopMonitoring.UseVisualStyleBackColor = true;
            this.btnStopMonitoring.Click += new System.EventHandler(this.btnStopMonitoring_Click);
            // 
            // btnStartMonitoring
            // 
            this.btnStartMonitoring.Location = new System.Drawing.Point(50, 30);
            this.btnStartMonitoring.Name = "btnStartMonitoring";
            this.btnStartMonitoring.Size = new System.Drawing.Size(150, 40);
            this.btnStartMonitoring.TabIndex = 0;
            this.btnStartMonitoring.Text = "开始自动监控";
            this.btnStartMonitoring.UseVisualStyleBackColor = true;
            this.btnStartMonitoring.Click += new System.EventHandler(this.btnStartMonitoring_Click);
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.groupLog);
            this.tabPage2.Location = new System.Drawing.Point(4, 25);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(792, 521);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "操作日志";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // groupLog
            // 
            this.groupLog.Controls.Add(this.btnClearLog);
            this.groupLog.Controls.Add(this.txtLog);
            this.groupLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupLog.Location = new System.Drawing.Point(3, 3);
            this.groupLog.Name = "groupLog";
            this.groupLog.Padding = new System.Windows.Forms.Padding(10);
            this.groupLog.Size = new System.Drawing.Size(786, 515);
            this.groupLog.TabIndex = 0;
            this.groupLog.TabStop = false;
            this.groupLog.Text = "日志记录";
            // 
            // btnClearLog
            // 
            this.btnClearLog.Location = new System.Drawing.Point(650, 20);
            this.btnClearLog.Name = "btnClearLog";
            this.btnClearLog.Size = new System.Drawing.Size(120, 30);
            this.btnClearLog.TabIndex = 1;
            this.btnClearLog.Text = "清空日志";
            this.btnClearLog.UseVisualStyleBackColor = true;
            this.btnClearLog.Click += new System.EventHandler(this.btnClearLog_Click);
            // 
            // txtLog
            // 
            this.txtLog.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.txtLog.Location = new System.Drawing.Point(10, 60);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(766, 445);
            this.txtLog.TabIndex = 0;
            // 
            // AutoDeleteImageForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 550);
            this.Controls.Add(this.tabControl1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "AutoDeleteImageForm";
            this.ShowInTaskbar = false;
            this.Text = "图片自动删除工具";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.AutoDeleteImageForm_Load);
            this.Shown += new System.EventHandler(this.AutoDeleteImageForm_Shown);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.groupSettings.ResumeLayout(false);
            this.groupSettings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericCheckInterval)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericNGDays)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericOKDays)).EndInit();
            this.groupOperations.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.groupLog.ResumeLayout(false);
            this.groupLog.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.GroupBox groupSettings;
        private System.Windows.Forms.Button btnSaveSettings;
        private System.Windows.Forms.TextBox txtNGKeyword;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtOKKeyword;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button btnBrowseFolder;
        private System.Windows.Forms.TextBox txtFolderPath;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown numericNGDays;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown numericOKDays;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.GroupBox groupOperations;
        private System.Windows.Forms.Button btnManualDelete;
        private System.Windows.Forms.Button btnStopMonitoring;
        private System.Windows.Forms.Button btnStartMonitoring;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.GroupBox groupLog;
        private System.Windows.Forms.Button btnClearLog;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.CheckBox chkIncludeSubfolders;
        private System.Windows.Forms.CheckBox chkConfirmDeletion;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.NumericUpDown numericCheckInterval;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.CheckBox chkAutoStart;
        private System.Windows.Forms.CheckBox chkAutoStartMonitoring;
        private System.Windows.Forms.CheckBox chkAutoHideOnStart;
    }
}
    