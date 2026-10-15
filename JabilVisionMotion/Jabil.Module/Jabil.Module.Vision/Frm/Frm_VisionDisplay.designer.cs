

namespace Jabil.Module.Vision.Frm
{
    partial class Frm_VisionDisplay
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
            this.uiPanel_window = new Sunny.UI.UIPanel();
            this.SuspendLayout();
            // 
            // uiPanel_window
            // 
            this.uiPanel_window.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiPanel_window.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.uiPanel_window.FillColorGradient = true;
            this.uiPanel_window.FillColorGradientDirection = System.Windows.Forms.FlowDirection.BottomUp;
            this.uiPanel_window.Font = new System.Drawing.Font("宋体", 42F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiPanel_window.Location = new System.Drawing.Point(0, 0);
            this.uiPanel_window.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.uiPanel_window.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiPanel_window.Name = "uiPanel_window";
            this.uiPanel_window.Size = new System.Drawing.Size(999, 493);
            this.uiPanel_window.TabIndex = 86;
            this.uiPanel_window.Text = "主页面显示";
            this.uiPanel_window.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // Frm_VisionDisplay
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(999, 493);
            this.Controls.Add(this.uiPanel_window);
            this.Name = "Frm_VisionDisplay";
            this.ResumeLayout(false);

        }

        #endregion

        private Sunny.UI.UIPanel uiPanel_window;
    }
}
