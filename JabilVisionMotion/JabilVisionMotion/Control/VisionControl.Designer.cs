namespace JabilVisionMotion.Control
{
    partial class VisionControl
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VisionControl));
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.cogRecordDisplay_1 = new Cognex.VisionPro.CogRecordDisplay();
            this.label_Cam = new System.Windows.Forms.Label();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.cogRecordDisplay_1)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.cogRecordDisplay_1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.label_Cam, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 9.264305F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 90.73569F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(446, 367);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // cogRecordDisplay_1
            // 
            this.cogRecordDisplay_1.ColorMapLowerClipColor = System.Drawing.Color.Black;
            this.cogRecordDisplay_1.ColorMapLowerRoiLimit = 0D;
            this.cogRecordDisplay_1.ColorMapPredefined = Cognex.VisionPro.Display.CogDisplayColorMapPredefinedConstants.None;
            this.cogRecordDisplay_1.ColorMapUpperClipColor = System.Drawing.Color.Black;
            this.cogRecordDisplay_1.ColorMapUpperRoiLimit = 1D;
            this.cogRecordDisplay_1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cogRecordDisplay_1.DoubleTapZoomCycleLength = 2;
            this.cogRecordDisplay_1.DoubleTapZoomSensitivity = 2.5D;
            this.cogRecordDisplay_1.Location = new System.Drawing.Point(3, 37);
            this.cogRecordDisplay_1.MouseWheelMode = Cognex.VisionPro.Display.CogDisplayMouseWheelModeConstants.Zoom1;
            this.cogRecordDisplay_1.MouseWheelSensitivity = 1D;
            this.cogRecordDisplay_1.Name = "cogRecordDisplay_1";
            this.cogRecordDisplay_1.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("cogRecordDisplay_1.OcxState")));
            this.cogRecordDisplay_1.Size = new System.Drawing.Size(440, 327);
            this.cogRecordDisplay_1.TabIndex = 83;
            // 
            // label_Cam
            // 
            this.label_Cam.AutoSize = true;
            this.label_Cam.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label_Cam.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label_Cam.ForeColor = System.Drawing.Color.White;
            this.label_Cam.Location = new System.Drawing.Point(3, 0);
            this.label_Cam.Name = "label_Cam";
            this.label_Cam.Size = new System.Drawing.Size(440, 34);
            this.label_Cam.TabIndex = 82;
            this.label_Cam.Text = "左侧相机";
            this.label_Cam.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // VisionControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(55)))), ((int)(((byte)(100)))));
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "VisionControl";
            this.Size = new System.Drawing.Size(446, 367);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.cogRecordDisplay_1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private Cognex.VisionPro.CogRecordDisplay cogRecordDisplay_1;
        private System.Windows.Forms.Label label_Cam;
    }
}
