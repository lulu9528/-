namespace JabilVisionMotion.Frm.UserPage
{
    partial class SettingPage
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingPage));
            this.uiPanel_window = new Sunny.UI.UIPanel();
            this.uiPanel_底色 = new Sunny.UI.UIPanel();
            this.toolStrip_工具栏 = new System.Windows.Forms.ToolStrip();
            this.toolStripButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
            this.uiPanel_底色.SuspendLayout();
            this.toolStrip_工具栏.SuspendLayout();
            this.SuspendLayout();
            // 
            // uiPanel_window
            // 
            this.uiPanel_window.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiPanel_window.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.uiPanel_window.FillColorGradient = true;
            this.uiPanel_window.FillColorGradientDirection = System.Windows.Forms.FlowDirection.BottomUp;
            this.uiPanel_window.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiPanel_window.Location = new System.Drawing.Point(81, 0);
            this.uiPanel_window.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.uiPanel_window.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiPanel_window.Name = "uiPanel_window";
            this.uiPanel_window.Size = new System.Drawing.Size(719, 450);
            this.uiPanel_window.TabIndex = 85;
            this.uiPanel_window.Text = null;
            this.uiPanel_window.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            this.uiPanel_window.Click += new System.EventHandler(this.tsbtnProjectSetting_Click);
            // 
            // uiPanel_底色
            // 
            this.uiPanel_底色.BackColor = System.Drawing.Color.Transparent;
            this.uiPanel_底色.Controls.Add(this.uiPanel_window);
            this.uiPanel_底色.Controls.Add(this.toolStrip_工具栏);
            this.uiPanel_底色.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiPanel_底色.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.uiPanel_底色.FillColorGradient = true;
            this.uiPanel_底色.FillColorGradientDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.uiPanel_底色.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiPanel_底色.Location = new System.Drawing.Point(0, 0);
            this.uiPanel_底色.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.uiPanel_底色.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiPanel_底色.Name = "uiPanel_底色";
            this.uiPanel_底色.Size = new System.Drawing.Size(800, 450);
            this.uiPanel_底色.TabIndex = 3;
            this.uiPanel_底色.Text = null;
            this.uiPanel_底色.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // toolStrip_工具栏
            // 
            this.toolStrip_工具栏.AllowItemReorder = true;
            this.toolStrip_工具栏.AllowMerge = false;
            this.toolStrip_工具栏.AutoSize = false;
            this.toolStrip_工具栏.BackColor = System.Drawing.Color.Transparent;
            this.toolStrip_工具栏.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.toolStrip_工具栏.Dock = System.Windows.Forms.DockStyle.Left;
            this.toolStrip_工具栏.Font = new System.Drawing.Font("新宋体", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.toolStrip_工具栏.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip_工具栏.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip_工具栏.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButton,
            this.toolStripButton1});
            this.toolStrip_工具栏.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.VerticalStackWithOverflow;
            this.toolStrip_工具栏.Location = new System.Drawing.Point(0, 0);
            this.toolStrip_工具栏.Name = "toolStrip_工具栏";
            this.toolStrip_工具栏.ShowItemToolTips = false;
            this.toolStrip_工具栏.Size = new System.Drawing.Size(81, 450);
            this.toolStrip_工具栏.Stretch = true;
            this.toolStrip_工具栏.TabIndex = 83;
            this.toolStrip_工具栏.Text = "toolStrip1";
            // 
            // toolStripButton
            // 
            this.toolStripButton.AccessibleRole = System.Windows.Forms.AccessibleRole.ScrollBar;
            this.toolStripButton.AutoSize = false;
            this.toolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButton.Font = new System.Drawing.Font("黑体", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.toolStripButton.ForeColor = System.Drawing.Color.White;
            this.toolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton.Image")));
            this.toolStripButton.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton.Name = "toolStripButton";
            this.toolStripButton.Size = new System.Drawing.Size(80, 70);
            this.toolStripButton.Text = "项目参数";
            this.toolStripButton.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            this.toolStripButton.Click += new System.EventHandler(this.tsbtnProjectSetting_Click);
            // 
            // toolStripButton1
            // 
            this.toolStripButton1.AccessibleRole = System.Windows.Forms.AccessibleRole.ScrollBar;
            this.toolStripButton1.AutoSize = false;
            this.toolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButton1.Font = new System.Drawing.Font("黑体", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.toolStripButton1.ForeColor = System.Drawing.Color.White;
            this.toolStripButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton1.Image")));
            this.toolStripButton1.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton1.Name = "toolStripButton1";
            this.toolStripButton1.Size = new System.Drawing.Size(80, 70);
            this.toolStripButton1.Text = "系统参数";
            this.toolStripButton1.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            this.toolStripButton1.Click += new System.EventHandler(this.tsbtnSystemSetting_Click);
            // 
            // SettingPage
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.uiPanel_底色);
            this.Name = "SettingPage";
            this.Text = "SettingPage";
            this.uiPanel_底色.ResumeLayout(false);
            this.toolStrip_工具栏.ResumeLayout(false);
            this.toolStrip_工具栏.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private Sunny.UI.UIPanel uiPanel_window;
        private Sunny.UI.UIPanel uiPanel_底色;
        private System.Windows.Forms.ToolStrip toolStrip_工具栏;
        public System.Windows.Forms.ToolStripButton toolStripButton;
        public System.Windows.Forms.ToolStripButton toolStripButton1;
    }
}