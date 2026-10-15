using System.Windows.Forms;

namespace Jabil.Module.Vision.Frm
{
    partial class Frm_VisionControl
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Frm_VisionControl));
            this.uiPanel1 = new Sunny.UI.UIPanel();
            this.uiTableLayoutPanel1 = new Sunny.UI.UITableLayoutPanel();
            this.uiTableLayoutPanel2 = new Sunny.UI.UITableLayoutPanel();
            this.uiPanel2 = new Sunny.UI.UIPanel();
            this.cogRecordDisplay0 = new Cognex.VisionPro.CogRecordDisplay();
            this.cogDisplayStatusBar0 = new Cognex.VisionPro.CogDisplayStatusBarV2();
            this.uiPanel3 = new Sunny.UI.UIPanel();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.btn_CamGrab = new System.Windows.Forms.ToolStripButton();
            this.btn_CamLive = new System.Windows.Forms.ToolStripButton();
            this.btn_CamOpenImage = new System.Windows.Forms.ToolStripButton();
            this.btn_CamSaveImage = new System.Windows.Forms.ToolStripButton();
            this.btn_CamRun = new System.Windows.Forms.ToolStripButton();
            this.uiPanel4 = new Sunny.UI.UIPanel();
            this.cbb_点位名称 = new Sunny.UI.UIComboBox();
            this.cbb_点位定义 = new Sunny.UI.UIComboBox();
            this.cbb_点位类型 = new Sunny.UI.UIComboBox();
            this.miniToolStrip = new System.Windows.Forms.ToolStrip();
            this.uiPanel1.SuspendLayout();
            this.uiTableLayoutPanel1.SuspendLayout();
            this.uiTableLayoutPanel2.SuspendLayout();
            this.uiPanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.cogRecordDisplay0)).BeginInit();
            this.uiPanel3.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.uiPanel4.SuspendLayout();
            this.SuspendLayout();
            // 
            // uiPanel1
            // 
            this.uiPanel1.Controls.Add(this.uiTableLayoutPanel1);
            this.uiPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiPanel1.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.uiPanel1.FillColorGradient = true;
            this.uiPanel1.FillColorGradientDirection = System.Windows.Forms.FlowDirection.BottomUp;
            this.uiPanel1.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiPanel1.Location = new System.Drawing.Point(0, 0);
            this.uiPanel1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.uiPanel1.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiPanel1.Name = "uiPanel1";
            this.uiPanel1.Size = new System.Drawing.Size(650, 514);
            this.uiPanel1.TabIndex = 0;
            this.uiPanel1.Text = "uiPanel1";
            this.uiPanel1.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // uiTableLayoutPanel1
            // 
            this.uiTableLayoutPanel1.BackColor = System.Drawing.Color.Transparent;
            this.uiTableLayoutPanel1.ColumnCount = 1;
            this.uiTableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.uiTableLayoutPanel1.Controls.Add(this.uiTableLayoutPanel2, 0, 1);
            this.uiTableLayoutPanel1.Controls.Add(this.uiPanel4, 0, 0);
            this.uiTableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiTableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.uiTableLayoutPanel1.Name = "uiTableLayoutPanel1";
            this.uiTableLayoutPanel1.RowCount = 2;
            this.uiTableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.uiTableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.uiTableLayoutPanel1.Size = new System.Drawing.Size(650, 514);
            this.uiTableLayoutPanel1.TabIndex = 1;
            this.uiTableLayoutPanel1.TagString = null;
            // 
            // uiTableLayoutPanel2
            // 
            this.uiTableLayoutPanel2.BackColor = System.Drawing.Color.Transparent;
            this.uiTableLayoutPanel2.ColumnCount = 2;
            this.uiTableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 91.29771F));
            this.uiTableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8.702291F));
            this.uiTableLayoutPanel2.Controls.Add(this.uiPanel2, 0, 0);
            this.uiTableLayoutPanel2.Controls.Add(this.uiPanel3, 1, 0);
            this.uiTableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiTableLayoutPanel2.Location = new System.Drawing.Point(3, 38);
            this.uiTableLayoutPanel2.Name = "uiTableLayoutPanel2";
            this.uiTableLayoutPanel2.RowCount = 1;
            this.uiTableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.uiTableLayoutPanel2.Size = new System.Drawing.Size(644, 473);
            this.uiTableLayoutPanel2.TabIndex = 0;
            this.uiTableLayoutPanel2.TagString = null;
            // 
            // uiPanel2
            // 
            this.uiPanel2.Controls.Add(this.cogRecordDisplay0);
            this.uiPanel2.Controls.Add(this.cogDisplayStatusBar0);
            this.uiPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiPanel2.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiPanel2.Location = new System.Drawing.Point(4, 5);
            this.uiPanel2.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.uiPanel2.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiPanel2.Name = "uiPanel2";
            this.uiPanel2.Size = new System.Drawing.Size(579, 463);
            this.uiPanel2.TabIndex = 0;
            this.uiPanel2.Text = "uiPanel2";
            this.uiPanel2.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // cogRecordDisplay0
            // 
            this.cogRecordDisplay0.ColorMapLowerClipColor = System.Drawing.Color.Black;
            this.cogRecordDisplay0.ColorMapLowerRoiLimit = 0D;
            this.cogRecordDisplay0.ColorMapPredefined = Cognex.VisionPro.Display.CogDisplayColorMapPredefinedConstants.None;
            this.cogRecordDisplay0.ColorMapUpperClipColor = System.Drawing.Color.Black;
            this.cogRecordDisplay0.ColorMapUpperRoiLimit = 1D;
            this.cogRecordDisplay0.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cogRecordDisplay0.DoubleTapZoomCycleLength = 2;
            this.cogRecordDisplay0.DoubleTapZoomSensitivity = 2.5D;
            this.cogRecordDisplay0.Location = new System.Drawing.Point(0, 0);
            this.cogRecordDisplay0.MouseWheelMode = Cognex.VisionPro.Display.CogDisplayMouseWheelModeConstants.Zoom1;
            this.cogRecordDisplay0.MouseWheelSensitivity = 1D;
            this.cogRecordDisplay0.Name = "cogRecordDisplay0";
            this.cogRecordDisplay0.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("cogRecordDisplay0.OcxState")));
            this.cogRecordDisplay0.Size = new System.Drawing.Size(579, 434);
            this.cogRecordDisplay0.TabIndex = 6;
            // 
            // cogDisplayStatusBar0
            // 
            this.cogDisplayStatusBar0.CoordinateSpaceName = "*\\#";
            this.cogDisplayStatusBar0.CoordinateSpaceName3D = "*\\#";
            this.cogDisplayStatusBar0.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.cogDisplayStatusBar0.Location = new System.Drawing.Point(0, 434);
            this.cogDisplayStatusBar0.Margin = new System.Windows.Forms.Padding(155, 155, 155, 155);
            this.cogDisplayStatusBar0.Name = "cogDisplayStatusBar0";
            this.cogDisplayStatusBar0.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.cogDisplayStatusBar0.Size = new System.Drawing.Size(579, 29);
            this.cogDisplayStatusBar0.TabIndex = 5;
            this.cogDisplayStatusBar0.Use3DCoordinateSpaceTree = false;
            // 
            // uiPanel3
            // 
            this.uiPanel3.Controls.Add(this.toolStrip1);
            this.uiPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiPanel3.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiPanel3.Location = new System.Drawing.Point(591, 5);
            this.uiPanel3.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.uiPanel3.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiPanel3.Name = "uiPanel3";
            this.uiPanel3.Size = new System.Drawing.Size(49, 463);
            this.uiPanel3.TabIndex = 1;
            this.uiPanel3.Text = null;
            this.uiPanel3.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // toolStrip1
            // 
            this.toolStrip1.AllowItemReorder = true;
            this.toolStrip1.AllowMerge = false;
            this.toolStrip1.BackColor = System.Drawing.Color.Silver;
            this.toolStrip1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.toolStrip1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.toolStrip1.Font = new System.Drawing.Font("新宋体", 9F);
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btn_CamGrab,
            this.btn_CamLive,
            this.btn_CamOpenImage,
            this.btn_CamSaveImage,
            this.btn_CamRun});
            this.toolStrip1.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.VerticalStackWithOverflow;
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Padding = new System.Windows.Forms.Padding(0, 0, 2, 0);
            this.toolStrip1.ShowItemToolTips = false;
            this.toolStrip1.Size = new System.Drawing.Size(49, 463);
            this.toolStrip1.Stretch = true;
            this.toolStrip1.TabIndex = 83;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // btn_CamGrab
            // 
            this.btn_CamGrab.AutoSize = false;
            this.btn_CamGrab.ForeColor = System.Drawing.Color.White;
            this.btn_CamGrab.Image = ((System.Drawing.Image)(resources.GetObject("btn_CamGrab.Image")));
            this.btn_CamGrab.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
            this.btn_CamGrab.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btn_CamGrab.Name = "btn_CamGrab";
            this.btn_CamGrab.Size = new System.Drawing.Size(70, 50);
            this.btn_CamGrab.Text = "取像";
            this.btn_CamGrab.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.btn_CamGrab.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            // 
            // btn_CamLive
            // 
            this.btn_CamLive.AutoSize = false;
            this.btn_CamLive.ForeColor = System.Drawing.Color.White;
            this.btn_CamLive.Image = ((System.Drawing.Image)(resources.GetObject("btn_CamLive.Image")));
            this.btn_CamLive.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
            this.btn_CamLive.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btn_CamLive.Name = "btn_CamLive";
            this.btn_CamLive.Size = new System.Drawing.Size(70, 50);
            this.btn_CamLive.Text = "实时";
            this.btn_CamLive.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.btn_CamLive.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            // 
            // btn_CamOpenImage
            // 
            this.btn_CamOpenImage.AutoSize = false;
            this.btn_CamOpenImage.ForeColor = System.Drawing.Color.White;
            this.btn_CamOpenImage.Image = ((System.Drawing.Image)(resources.GetObject("btn_CamOpenImage.Image")));
            this.btn_CamOpenImage.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
            this.btn_CamOpenImage.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.btn_CamOpenImage.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btn_CamOpenImage.Name = "btn_CamOpenImage";
            this.btn_CamOpenImage.Size = new System.Drawing.Size(70, 50);
            this.btn_CamOpenImage.Text = "打开";
            this.btn_CamOpenImage.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.btn_CamOpenImage.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            // 
            // btn_CamSaveImage
            // 
            this.btn_CamSaveImage.AutoSize = false;
            this.btn_CamSaveImage.ForeColor = System.Drawing.Color.White;
            this.btn_CamSaveImage.Image = ((System.Drawing.Image)(resources.GetObject("btn_CamSaveImage.Image")));
            this.btn_CamSaveImage.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
            this.btn_CamSaveImage.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.btn_CamSaveImage.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btn_CamSaveImage.Name = "btn_CamSaveImage";
            this.btn_CamSaveImage.Size = new System.Drawing.Size(70, 50);
            this.btn_CamSaveImage.Text = "保存";
            this.btn_CamSaveImage.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.btn_CamSaveImage.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            // 
            // btn_CamRun
            // 
            this.btn_CamRun.AccessibleRole = System.Windows.Forms.AccessibleRole.ScrollBar;
            this.btn_CamRun.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.btn_CamRun.AutoSize = false;
            this.btn_CamRun.ForeColor = System.Drawing.Color.White;
            this.btn_CamRun.Image = ((System.Drawing.Image)(resources.GetObject("btn_CamRun.Image")));
            this.btn_CamRun.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
            this.btn_CamRun.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.btn_CamRun.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btn_CamRun.Name = "btn_CamRun";
            this.btn_CamRun.Size = new System.Drawing.Size(70, 50);
            this.btn_CamRun.Text = "运行";
            this.btn_CamRun.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.btn_CamRun.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            // 
            // uiPanel4
            // 
            this.uiPanel4.Controls.Add(this.cbb_点位名称);
            this.uiPanel4.Controls.Add(this.cbb_点位定义);
            this.uiPanel4.Controls.Add(this.cbb_点位类型);
            this.uiPanel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiPanel4.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.uiPanel4.FillColorGradient = true;
            this.uiPanel4.FillColorGradientDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.uiPanel4.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiPanel4.Location = new System.Drawing.Point(4, 5);
            this.uiPanel4.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.uiPanel4.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiPanel4.Name = "uiPanel4";
            this.uiPanel4.Size = new System.Drawing.Size(642, 25);
            this.uiPanel4.TabIndex = 1;
            this.uiPanel4.Text = "uiPanel4";
            this.uiPanel4.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // cbb_点位名称
            // 
            this.cbb_点位名称.BackColor = System.Drawing.Color.Transparent;
            this.cbb_点位名称.DataSource = null;
            this.cbb_点位名称.Dock = System.Windows.Forms.DockStyle.Left;
            this.cbb_点位名称.FillColor = System.Drawing.Color.White;
            this.cbb_点位名称.FillColor2 = System.Drawing.Color.White;
            this.cbb_点位名称.FillColorGradientDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.cbb_点位名称.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.cbb_点位名称.ItemHoverColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(200)))), ((int)(((byte)(255)))));
            this.cbb_点位名称.ItemSelectForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.cbb_点位名称.Location = new System.Drawing.Point(328, 0);
            this.cbb_点位名称.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cbb_点位名称.MinimumSize = new System.Drawing.Size(63, 0);
            this.cbb_点位名称.Name = "cbb_点位名称";
            this.cbb_点位名称.Padding = new System.Windows.Forms.Padding(0, 0, 30, 2);
            this.cbb_点位名称.Size = new System.Drawing.Size(134, 25);
            this.cbb_点位名称.SymbolSize = 24;
            this.cbb_点位名称.TabIndex = 4;
            this.cbb_点位名称.Text = "点位名称";
            this.cbb_点位名称.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.cbb_点位名称.Watermark = "点位名称";
            // 
            // cbb_点位定义
            // 
            this.cbb_点位定义.BackColor = System.Drawing.Color.Transparent;
            this.cbb_点位定义.DataSource = null;
            this.cbb_点位定义.Dock = System.Windows.Forms.DockStyle.Left;
            this.cbb_点位定义.FillColor = System.Drawing.Color.White;
            this.cbb_点位定义.FillColor2 = System.Drawing.Color.White;
            this.cbb_点位定义.FillColorGradientDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.cbb_点位定义.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.cbb_点位定义.ItemHoverColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(200)))), ((int)(((byte)(255)))));
            this.cbb_点位定义.ItemSelectForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.cbb_点位定义.Location = new System.Drawing.Point(164, 0);
            this.cbb_点位定义.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cbb_点位定义.MinimumSize = new System.Drawing.Size(63, 0);
            this.cbb_点位定义.Name = "cbb_点位定义";
            this.cbb_点位定义.Padding = new System.Windows.Forms.Padding(0, 0, 30, 2);
            this.cbb_点位定义.Size = new System.Drawing.Size(164, 25);
            this.cbb_点位定义.SymbolSize = 24;
            this.cbb_点位定义.TabIndex = 3;
            this.cbb_点位定义.Text = "点位定义";
            this.cbb_点位定义.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.cbb_点位定义.Watermark = "点位定义";
            // 
            // cbb_点位类型
            // 
            this.cbb_点位类型.BackColor = System.Drawing.Color.Transparent;
            this.cbb_点位类型.DataSource = null;
            this.cbb_点位类型.Dock = System.Windows.Forms.DockStyle.Left;
            this.cbb_点位类型.FillColor = System.Drawing.Color.White;
            this.cbb_点位类型.FillColor2 = System.Drawing.Color.White;
            this.cbb_点位类型.FillColorGradientDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.cbb_点位类型.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.cbb_点位类型.ItemHoverColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(200)))), ((int)(((byte)(255)))));
            this.cbb_点位类型.ItemSelectForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.cbb_点位类型.Location = new System.Drawing.Point(0, 0);
            this.cbb_点位类型.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cbb_点位类型.MinimumSize = new System.Drawing.Size(63, 0);
            this.cbb_点位类型.Name = "cbb_点位类型";
            this.cbb_点位类型.Padding = new System.Windows.Forms.Padding(0, 0, 30, 2);
            this.cbb_点位类型.Size = new System.Drawing.Size(164, 25);
            this.cbb_点位类型.SymbolSize = 24;
            this.cbb_点位类型.TabIndex = 2;
            this.cbb_点位类型.Text = "点位类型";
            this.cbb_点位类型.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.cbb_点位类型.TrimFilter = true;
            this.cbb_点位类型.Watermark = "点位类型";
            // 
            // miniToolStrip
            // 
            this.miniToolStrip.AccessibleName = "新项选择";
            this.miniToolStrip.AccessibleRole = System.Windows.Forms.AccessibleRole.ButtonDropDown;
            this.miniToolStrip.AllowItemReorder = true;
            this.miniToolStrip.AllowMerge = false;
            this.miniToolStrip.AutoSize = false;
            this.miniToolStrip.BackColor = System.Drawing.Color.Silver;
            this.miniToolStrip.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.miniToolStrip.CanOverflow = false;
            this.miniToolStrip.Dock = System.Windows.Forms.DockStyle.None;
            this.miniToolStrip.Font = new System.Drawing.Font("新宋体", 9F);
            this.miniToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.miniToolStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.miniToolStrip.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.VerticalStackWithOverflow;
            this.miniToolStrip.Location = new System.Drawing.Point(8, 212);
            this.miniToolStrip.Name = "miniToolStrip";
            this.miniToolStrip.Padding = new System.Windows.Forms.Padding(0, 0, 2, 0);
            this.miniToolStrip.ShowItemToolTips = false;
            this.miniToolStrip.Size = new System.Drawing.Size(50, 358);
            this.miniToolStrip.Stretch = true;
            this.miniToolStrip.TabIndex = 83;
            // 
            // FrmVisionControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.Silver;
            this.ClientSize = new System.Drawing.Size(650, 514);
            this.Controls.Add(this.uiPanel1);
            this.Name = "FrmVisionControl";
            this.Load += new System.EventHandler(this.Frm_VisionControl_Load);
            this.uiPanel1.ResumeLayout(false);
            this.uiTableLayoutPanel1.ResumeLayout(false);
            this.uiTableLayoutPanel2.ResumeLayout(false);
            this.uiPanel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.cogRecordDisplay0)).EndInit();
            this.uiPanel3.ResumeLayout(false);
            this.uiPanel3.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.uiPanel4.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private Sunny.UI.UIPanel uiPanel1;
        private Sunny.UI.UITableLayoutPanel uiTableLayoutPanel1;
        private Sunny.UI.UITableLayoutPanel uiTableLayoutPanel2;
        private Sunny.UI.UIPanel uiPanel2;
        public Cognex.VisionPro.CogRecordDisplay cogRecordDisplay0;
        private Cognex.VisionPro.CogDisplayStatusBarV2 cogDisplayStatusBar0;
        private Sunny.UI.UIPanel uiPanel3;
        public ToolStrip toolStrip1;
        public ToolStripButton btn_CamGrab;
        public ToolStripButton btn_CamLive;
        public ToolStripButton btn_CamOpenImage;
        public ToolStripButton btn_CamSaveImage;
        public ToolStripButton btn_CamRun;
        private Sunny.UI.UIPanel uiPanel4;
        private Sunny.UI.UIComboBox cbb_点位名称;
        private Sunny.UI.UIComboBox cbb_点位定义;
        private Sunny.UI.UIComboBox cbb_点位类型;
        private ToolStrip miniToolStrip;
    }
}