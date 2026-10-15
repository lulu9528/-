
namespace Jabil.Module.Help.Frm
{
    partial class Frm_故障处理
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
            this.uiPanel11 = new Sunny.UI.UIPanel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.TsbtnOpenCircuitDiagram = new System.Windows.Forms.ToolStripLabel();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.TsbtnOpenOperationGuide = new System.Windows.Forms.ToolStripLabel();
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.TsbtnOpenEpsonAlarmCode = new System.Windows.Forms.ToolStripLabel();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.TsbtnOpenDocFolder = new System.Windows.Forms.ToolStripLabel();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.TsbtnOpenInspectionGuide = new System.Windows.Forms.ToolStripLabel();
            this.webView21 = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.uiPanel11.SuspendLayout();
            this.panel1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.webView21)).BeginInit();
            this.SuspendLayout();
            // 
            // uiPanel11
            // 
            this.uiPanel11.Controls.Add(this.panel1);
            this.uiPanel11.Controls.Add(this.toolStrip1);
            this.uiPanel11.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiPanel11.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.uiPanel11.FillColor2 = System.Drawing.Color.Gray;
            this.uiPanel11.FillColorGradient = true;
            this.uiPanel11.Font = new System.Drawing.Font("宋体", 12F);
            this.uiPanel11.ForeColor = System.Drawing.Color.White;
            this.uiPanel11.Location = new System.Drawing.Point(0, 0);
            this.uiPanel11.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.uiPanel11.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiPanel11.Name = "uiPanel11";
            this.uiPanel11.RectColor = System.Drawing.Color.FromArgb(((int)(((byte)(130)))), ((int)(((byte)(58)))), ((int)(((byte)(183)))));
            this.uiPanel11.Size = new System.Drawing.Size(1009, 594);
            this.uiPanel11.Style = Sunny.UI.UIStyle.Custom;
            this.uiPanel11.StyleCustomMode = true;
            this.uiPanel11.TabIndex = 15;
            this.uiPanel11.Text = null;
            this.uiPanel11.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.Transparent;
            this.panel1.Controls.Add(this.webView21);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 25);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1009, 569);
            this.panel1.TabIndex = 1;
            // 
            // toolStrip1
            // 
            this.toolStrip1.BackColor = System.Drawing.Color.Transparent;
            this.toolStrip1.Font = new System.Drawing.Font("新宋体", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.TsbtnOpenCircuitDiagram,
            this.toolStripSeparator1,
            this.TsbtnOpenOperationGuide,
            this.toolStripSeparator5,
            this.TsbtnOpenEpsonAlarmCode,
            this.toolStripSeparator3,
            this.TsbtnOpenDocFolder,
            this.toolStripSeparator4,
            this.TsbtnOpenInspectionGuide});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1009, 25);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // TsbtnOpenCircuitDiagram
            // 
            this.TsbtnOpenCircuitDiagram.Font = new System.Drawing.Font("新宋体", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.TsbtnOpenCircuitDiagram.ForeColor = System.Drawing.Color.Gray;
            this.TsbtnOpenCircuitDiagram.Name = "TsbtnOpenCircuitDiagram";
            this.TsbtnOpenCircuitDiagram.Size = new System.Drawing.Size(58, 22);
            this.TsbtnOpenCircuitDiagram.Text = "电路图";
            this.TsbtnOpenCircuitDiagram.Click += new System.EventHandler(this.TsbtnOpenCircuitDiagram_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // TsbtnOpenOperationGuide
            // 
            this.TsbtnOpenOperationGuide.ForeColor = System.Drawing.Color.Gray;
            this.TsbtnOpenOperationGuide.Name = "TsbtnOpenOperationGuide";
            this.TsbtnOpenOperationGuide.Size = new System.Drawing.Size(75, 22);
            this.TsbtnOpenOperationGuide.Text = "操作说明";
            this.TsbtnOpenOperationGuide.Click += new System.EventHandler(this.TsbtnOpenOperationGuide_Click);
            // 
            // toolStripSeparator5
            // 
            this.toolStripSeparator5.Name = "toolStripSeparator5";
            this.toolStripSeparator5.Size = new System.Drawing.Size(6, 25);
            // 
            // TsbtnOpenEpsonAlarmCode
            // 
            this.TsbtnOpenEpsonAlarmCode.ForeColor = System.Drawing.Color.Gray;
            this.TsbtnOpenEpsonAlarmCode.Name = "TsbtnOpenEpsonAlarmCode";
            this.TsbtnOpenEpsonAlarmCode.Size = new System.Drawing.Size(120, 22);
            this.TsbtnOpenEpsonAlarmCode.Text = "Epson报警代码";
            this.TsbtnOpenEpsonAlarmCode.Click += new System.EventHandler(this.TsbtnOpenEpsonAlarmCode_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
            // 
            // TsbtnOpenDocFolder
            // 
            this.TsbtnOpenDocFolder.ForeColor = System.Drawing.Color.Gray;
            this.TsbtnOpenDocFolder.Name = "TsbtnOpenDocFolder";
            this.TsbtnOpenDocFolder.Size = new System.Drawing.Size(92, 22);
            this.TsbtnOpenDocFolder.Text = "打开文件夹";
            this.TsbtnOpenDocFolder.Click += new System.EventHandler(this.TsbtnOpenDocFolder_Click);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(6, 25);
            // 
            // TsbtnOpenInspectionGuide
            // 
            this.TsbtnOpenInspectionGuide.ForeColor = System.Drawing.Color.Gray;
            this.TsbtnOpenInspectionGuide.Name = "TsbtnOpenInspectionGuide";
            this.TsbtnOpenInspectionGuide.Size = new System.Drawing.Size(109, 22);
            this.TsbtnOpenInspectionGuide.Text = "点检操作说明";
            this.TsbtnOpenInspectionGuide.Click += new System.EventHandler(this.TsbtnOpenInspectionGuide_Click);
            // 
            // webView21
            // 
            this.webView21.AllowExternalDrop = true;
            this.webView21.CreationProperties = null;
            this.webView21.DefaultBackgroundColor = System.Drawing.Color.White;
            this.webView21.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webView21.Location = new System.Drawing.Point(0, 0);
            this.webView21.Name = "webView21";
            this.webView21.Size = new System.Drawing.Size(1009, 569);
            this.webView21.TabIndex = 1;
            this.webView21.ZoomFactor = 1D;
            // 
            // Frm_故障处理
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(1009, 594);
            this.Controls.Add(this.uiPanel11);
            this.Name = "Frm_故障处理";
            this.Text = "Frm_Operation";
            this.uiPanel11.ResumeLayout(false);
            this.uiPanel11.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.webView21)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private Sunny.UI.UIPanel uiPanel11;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripLabel TsbtnOpenCircuitDiagram;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripLabel TsbtnOpenEpsonAlarmCode;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripLabel TsbtnOpenDocFolder;
        private System.Windows.Forms.ToolStripLabel TsbtnOpenOperationGuide;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripLabel TsbtnOpenInspectionGuide;
        private Microsoft.Web.WebView2.WinForms.WebView2 webView21;
    }
}