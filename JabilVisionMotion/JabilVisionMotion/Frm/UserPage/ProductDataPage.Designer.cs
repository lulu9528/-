namespace JabilVisionMotion.Frm.UserPage
{
    partial class ProductDataPage
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
            this.uiPanel_底色 = new Sunny.UI.UIPanel();
            this.uiPanel_window = new Sunny.UI.UIPanel();
            this.toolStrip_工具栏 = new System.Windows.Forms.ToolStrip();
            this.toolStripButton_Yield = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton_UtilizationRate = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton_MaterialThrowingRate = new System.Windows.Forms.ToolStripButton();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.uiPanel_底色.SuspendLayout();
            this.toolStrip_工具栏.SuspendLayout();
            this.SuspendLayout();
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
            this.uiPanel_底色.TabIndex = 2;
            this.uiPanel_底色.Text = null;
            this.uiPanel_底色.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // uiPanel_window
            // 
            this.uiPanel_window.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiPanel_window.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.uiPanel_window.FillColorGradient = true;
            this.uiPanel_window.FillColorGradientDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.uiPanel_window.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiPanel_window.Location = new System.Drawing.Point(83, 0);
            this.uiPanel_window.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.uiPanel_window.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiPanel_window.Name = "uiPanel_window";
            this.uiPanel_window.Size = new System.Drawing.Size(717, 450);
            this.uiPanel_window.TabIndex = 85;
            this.uiPanel_window.Text = null;
            this.uiPanel_window.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
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
            this.toolStripButton_Yield,
            this.toolStripButton_UtilizationRate,
            this.toolStripButton_MaterialThrowingRate});
            this.toolStrip_工具栏.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.VerticalStackWithOverflow;
            this.toolStrip_工具栏.Location = new System.Drawing.Point(0, 0);
            this.toolStrip_工具栏.Name = "toolStrip_工具栏";
            this.toolStrip_工具栏.ShowItemToolTips = false;
            this.toolStrip_工具栏.Size = new System.Drawing.Size(83, 450);
            this.toolStrip_工具栏.Stretch = true;
            this.toolStrip_工具栏.TabIndex = 83;
            this.toolStrip_工具栏.Text = "toolStrip1";
            // 
            // toolStripButton_Yield
            // 
            this.toolStripButton_Yield.AccessibleRole = System.Windows.Forms.AccessibleRole.ScrollBar;
            this.toolStripButton_Yield.AutoSize = false;
            this.toolStripButton_Yield.BackColor = System.Drawing.Color.Transparent;
            this.toolStripButton_Yield.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButton_Yield.Font = new System.Drawing.Font("黑体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.toolStripButton_Yield.ForeColor = System.Drawing.Color.White;
            this.toolStripButton_Yield.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
            this.toolStripButton_Yield.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripButton_Yield.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton_Yield.Name = "toolStripButton_Yield";
            this.toolStripButton_Yield.Size = new System.Drawing.Size(80, 70);
            this.toolStripButton_Yield.Text = "产能";
            this.toolStripButton_Yield.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            // 
            // toolStripButton_UtilizationRate
            // 
            this.toolStripButton_UtilizationRate.AccessibleRole = System.Windows.Forms.AccessibleRole.ScrollBar;
            this.toolStripButton_UtilizationRate.AutoSize = false;
            this.toolStripButton_UtilizationRate.BackColor = System.Drawing.Color.Transparent;
            this.toolStripButton_UtilizationRate.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButton_UtilizationRate.Font = new System.Drawing.Font("黑体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.toolStripButton_UtilizationRate.ForeColor = System.Drawing.Color.White;
            this.toolStripButton_UtilizationRate.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
            this.toolStripButton_UtilizationRate.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripButton_UtilizationRate.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton_UtilizationRate.Name = "toolStripButton_UtilizationRate";
            this.toolStripButton_UtilizationRate.Size = new System.Drawing.Size(80, 70);
            this.toolStripButton_UtilizationRate.Text = "稼动率";
            this.toolStripButton_UtilizationRate.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            // 
            // toolStripButton_MaterialThrowingRate
            // 
            this.toolStripButton_MaterialThrowingRate.AccessibleRole = System.Windows.Forms.AccessibleRole.ScrollBar;
            this.toolStripButton_MaterialThrowingRate.AutoSize = false;
            this.toolStripButton_MaterialThrowingRate.BackColor = System.Drawing.Color.Transparent;
            this.toolStripButton_MaterialThrowingRate.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButton_MaterialThrowingRate.Font = new System.Drawing.Font("黑体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.toolStripButton_MaterialThrowingRate.ForeColor = System.Drawing.Color.White;
            this.toolStripButton_MaterialThrowingRate.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
            this.toolStripButton_MaterialThrowingRate.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripButton_MaterialThrowingRate.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton_MaterialThrowingRate.Name = "toolStripButton_MaterialThrowingRate";
            this.toolStripButton_MaterialThrowingRate.Size = new System.Drawing.Size(80, 70);
            this.toolStripButton_MaterialThrowingRate.Text = "抛料率";
            this.toolStripButton_MaterialThrowingRate.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            this.toolStripButton_MaterialThrowingRate.Visible = false;
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 1000;
            // 
            // ProductDataPage
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.uiPanel_底色);
            this.Name = "ProductDataPage";
            this.Text = "ProductDataPage";
            this.Load += new System.EventHandler(this.ProductDataPage_Load);
            this.uiPanel_底色.ResumeLayout(false);
            this.toolStrip_工具栏.ResumeLayout(false);
            this.toolStrip_工具栏.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private Sunny.UI.UIPanel uiPanel_底色;
        private Sunny.UI.UIPanel uiPanel_window;
        private System.Windows.Forms.ToolStrip toolStrip_工具栏;
        public System.Windows.Forms.ToolStripButton toolStripButton_Yield;
        public System.Windows.Forms.ToolStripButton toolStripButton_UtilizationRate;
        private System.Windows.Forms.Timer timer1;
        public System.Windows.Forms.ToolStripButton toolStripButton_MaterialThrowingRate;
    }
}