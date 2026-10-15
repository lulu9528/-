namespace Jabil.Module.TCPIP.Frm
{
    partial class Frm_TCPIPControl
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
            this.tcpipTabControl = new Sunny.UI.UITabControl();
            this.SuspendLayout();
            // 
            // uiTabControl1
            // 
            this.tcpipTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tcpipTabControl.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            this.tcpipTabControl.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tcpipTabControl.ItemSize = new System.Drawing.Size(150, 40);
            this.tcpipTabControl.Location = new System.Drawing.Point(0, 0);
            this.tcpipTabControl.MainPage = "";
            this.tcpipTabControl.MenuStyle = Sunny.UI.UIMenuStyle.Custom;
            this.tcpipTabControl.Name = "uiTabControl1";
            this.tcpipTabControl.SelectedIndex = 0;
            this.tcpipTabControl.Size = new System.Drawing.Size(873, 521);
            this.tcpipTabControl.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            this.tcpipTabControl.TabBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.tcpipTabControl.TabIndex = 1;
            this.tcpipTabControl.TabSelectedColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.tcpipTabControl.TipsFont = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // Frm_TCPIPFanuc
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(873, 521);
            this.Controls.Add(this.tcpipTabControl);
            this.Name = "Frm_TCPIPFanuc";
            this.ResumeLayout(false);

        }

        #endregion

        private Sunny.UI.UITabControl tcpipTabControl;
    }
}