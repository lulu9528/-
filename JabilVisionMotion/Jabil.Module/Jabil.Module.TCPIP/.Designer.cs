
namespace Jabil.Module.TCPIP
{
    partial class TCPIPPage
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
            this.uiPanel_window = new Sunny.UI.UIPanel();
            this.contentPanel = new Sunny.UI.UIPanel();
            this.toolStrip_工具栏 = new System.Windows.Forms.ToolStrip();
            this.tsBtnTcpIpControl = new System.Windows.Forms.ToolStripButton();
            this.tsBtnTcpIpConfig = new System.Windows.Forms.ToolStripButton();
            this.contentPanel.SuspendLayout();
            this.toolStrip_工具栏.SuspendLayout();
            this.SuspendLayout();
            // 
            // uiPanel_window
            // 
            this.uiPanel_window.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiPanel_window.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.uiPanel_window.FillColorGradient = true;
            this.uiPanel_window.FillColorGradientDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.uiPanel_window.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiPanel_window.Location = new System.Drawing.Point(0, 0);
            this.uiPanel_window.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.uiPanel_window.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiPanel_window.Name = "uiPanel_window";
            this.uiPanel_window.Size = new System.Drawing.Size(717, 450);
            this.uiPanel_window.TabIndex = 85;
            this.uiPanel_window.Text = null;
            this.uiPanel_window.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // uiPanel_底色
            // 
            this.contentPanel.BackColor = System.Drawing.Color.Transparent;
            this.contentPanel.Controls.Add(this.uiPanel_window);
            this.contentPanel.Controls.Add(this.toolStrip_工具栏);
            this.contentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentPanel.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.contentPanel.FillColor2 = System.Drawing.Color.Gray;
            this.contentPanel.FillColorGradient = true;
            this.contentPanel.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.contentPanel.Location = new System.Drawing.Point(0, 0);
            this.contentPanel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.contentPanel.MinimumSize = new System.Drawing.Size(1, 1);
            this.contentPanel.Name = "uiPanel_底色";
            this.contentPanel.Size = new System.Drawing.Size(800, 450);
            this.contentPanel.TabIndex = 4;
            this.contentPanel.Text = null;
            this.contentPanel.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // toolStrip_工具栏
            // 
            this.toolStrip_工具栏.AllowItemReorder = true;
            this.toolStrip_工具栏.AllowMerge = false;
            this.toolStrip_工具栏.AutoSize = false;
            this.toolStrip_工具栏.BackColor = System.Drawing.Color.Transparent;
            this.toolStrip_工具栏.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.toolStrip_工具栏.Dock = System.Windows.Forms.DockStyle.Right;
            this.toolStrip_工具栏.Font = new System.Drawing.Font("新宋体", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.toolStrip_工具栏.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip_工具栏.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip_工具栏.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsBtnTcpIpControl,
            this.tsBtnTcpIpConfig});
            this.toolStrip_工具栏.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.VerticalStackWithOverflow;
            this.toolStrip_工具栏.Location = new System.Drawing.Point(717, 0);
            this.toolStrip_工具栏.Name = "toolStrip_工具栏";
            this.toolStrip_工具栏.ShowItemToolTips = false;
            this.toolStrip_工具栏.Size = new System.Drawing.Size(83, 450);
            this.toolStrip_工具栏.Stretch = true;
            this.toolStrip_工具栏.TabIndex = 83;
            this.toolStrip_工具栏.Text = "toolStrip1";
            // 
            // toolStripButton_TCPIPControl
            // 
            this.tsBtnTcpIpControl.AccessibleRole = System.Windows.Forms.AccessibleRole.ScrollBar;
            this.tsBtnTcpIpControl.AutoSize = false;
            this.tsBtnTcpIpControl.BackColor = System.Drawing.Color.Transparent;
            this.tsBtnTcpIpControl.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsBtnTcpIpControl.Font = new System.Drawing.Font("黑体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tsBtnTcpIpControl.ForeColor = System.Drawing.Color.White;
            this.tsBtnTcpIpControl.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
            this.tsBtnTcpIpControl.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.tsBtnTcpIpControl.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsBtnTcpIpControl.Name = "toolStripButton_TCPIPControl";
            this.tsBtnTcpIpControl.Size = new System.Drawing.Size(80, 70);
            this.tsBtnTcpIpControl.Text = "监控";
            this.tsBtnTcpIpControl.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            // 
            // toolStripButton_TCPIPConfig
            // 
            this.tsBtnTcpIpConfig.AccessibleRole = System.Windows.Forms.AccessibleRole.ScrollBar;
            this.tsBtnTcpIpConfig.AutoSize = false;
            this.tsBtnTcpIpConfig.BackColor = System.Drawing.Color.Transparent;
            this.tsBtnTcpIpConfig.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsBtnTcpIpConfig.Font = new System.Drawing.Font("黑体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tsBtnTcpIpConfig.ForeColor = System.Drawing.Color.White;
            this.tsBtnTcpIpConfig.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
            this.tsBtnTcpIpConfig.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.tsBtnTcpIpConfig.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsBtnTcpIpConfig.Name = "toolStripButton_TCPIPConfig";
            this.tsBtnTcpIpConfig.Size = new System.Drawing.Size(80, 70);
            this.tsBtnTcpIpConfig.Text = "配置";
            this.tsBtnTcpIpConfig.TextImageRelation = System.Windows.Forms.TextImageRelation.Overlay;
            this.tsBtnTcpIpConfig.Visible = false;
            // 
            // TCPIPPage
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.contentPanel);
            this.Name = "TCPIPPage";
            this.contentPanel.ResumeLayout(false);
            this.toolStrip_工具栏.ResumeLayout(false);
            this.toolStrip_工具栏.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private Sunny.UI.UIPanel uiPanel_window;
        private Sunny.UI.UIPanel contentPanel;
        private System.Windows.Forms.ToolStrip toolStrip_工具栏;
        public System.Windows.Forms.ToolStripButton tsBtnTcpIpControl;
        public System.Windows.Forms.ToolStripButton tsBtnTcpIpConfig;
    }
}