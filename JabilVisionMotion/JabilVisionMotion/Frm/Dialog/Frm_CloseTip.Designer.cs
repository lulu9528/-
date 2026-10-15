namespace JabilVisionMotion.Frm.Dialog
{
    partial class Frm_CloseTip
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
            this.lbl_Message = new Sunny.UI.UILabel();
            this.uiPanel_底色 = new Sunny.UI.UIPanel();
            this.uiPanel_底色.SuspendLayout();
            this.SuspendLayout();
            // 
            // lbl_Message
            // 
            this.lbl_Message.BackColor = System.Drawing.Color.Transparent;
            this.lbl_Message.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbl_Message.Font = new System.Drawing.Font("黑体", 15F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lbl_Message.ForeColor = System.Drawing.Color.White;
            this.lbl_Message.Location = new System.Drawing.Point(0, 0);
            this.lbl_Message.Name = "lbl_Message";
            this.lbl_Message.Size = new System.Drawing.Size(538, 71);
            this.lbl_Message.Style = Sunny.UI.UIStyle.Custom;
            this.lbl_Message.TabIndex = 2;
            this.lbl_Message.Text = "正在保存相关数据，请稍后......";
            this.lbl_Message.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // uiPanel_底色
            // 
            this.uiPanel_底色.Controls.Add(this.lbl_Message);
            this.uiPanel_底色.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiPanel_底色.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.uiPanel_底色.FillColor2 = System.Drawing.Color.Gainsboro;
            this.uiPanel_底色.FillColorGradient = true;
            this.uiPanel_底色.FillColorGradientDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.uiPanel_底色.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiPanel_底色.Location = new System.Drawing.Point(0, 0);
            this.uiPanel_底色.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.uiPanel_底色.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiPanel_底色.Name = "uiPanel_底色";
            this.uiPanel_底色.Size = new System.Drawing.Size(538, 71);
            this.uiPanel_底色.TabIndex = 3;
            this.uiPanel_底色.Text = null;
            this.uiPanel_底色.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // Frm_CloseTip
            // 
            this.AllowShowTitle = false;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.ClientSize = new System.Drawing.Size(538, 71);
            this.Controls.Add(this.uiPanel_底色);
            this.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "Frm_CloseTip";
            this.Padding = new System.Windows.Forms.Padding(0);
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.ShowTitle = false;
            this.Text = "Vision && Motion Pro";
            this.TopMost = true;
            this.ZoomScaleRect = new System.Drawing.Rectangle(15, 15, 538, 71);
            this.uiPanel_底色.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private Sunny.UI.UILabel lbl_Message;
        private Sunny.UI.UIPanel uiPanel_底色;
    }
}