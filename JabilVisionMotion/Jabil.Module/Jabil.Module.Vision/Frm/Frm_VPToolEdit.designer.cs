namespace Jabil.Module.Vision.Frm
{
    partial class Frm_VpToolEdit
    {
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false</param>


        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容
        /// </summary>
        private void InitializeComponent()
        {
            this.lblToolName = new System.Windows.Forms.Label();
            this.txtBoxToolName = new System.Windows.Forms.TextBox();
            this.lblToolType = new System.Windows.Forms.Label();
            this.uiComboBoxToolType = new Sunny.UI.UIComboBox();
            this.btnAdd = new Sunny.UI.UIButton();
            this.btnCancel = new Sunny.UI.UIButton();
            this.SuspendLayout();
            // 
            // lblToolName
            // 
            this.lblToolName.BackColor = System.Drawing.Color.Transparent;
            this.lblToolName.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblToolName.ForeColor = System.Drawing.Color.Black;
            this.lblToolName.Location = new System.Drawing.Point(23, 56);
            this.lblToolName.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblToolName.Name = "lblToolName";
            this.lblToolName.Size = new System.Drawing.Size(121, 35);
            this.lblToolName.TabIndex = 39;
            this.lblToolName.Text = "检测工具名称：";
            this.lblToolName.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtBoxToolName
            // 
            this.txtBoxToolName.Location = new System.Drawing.Point(152, 55);
            this.txtBoxToolName.Margin = new System.Windows.Forms.Padding(4);
            this.txtBoxToolName.MaxLength = 50;
            this.txtBoxToolName.Multiline = true;
            this.txtBoxToolName.Name = "txtBoxToolName";
            this.txtBoxToolName.Size = new System.Drawing.Size(220, 36);
            this.txtBoxToolName.TabIndex = 0;
            // 
            // lblToolType
            // 
            this.lblToolType.BackColor = System.Drawing.Color.Transparent;
            this.lblToolType.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblToolType.ForeColor = System.Drawing.Color.Black;
            this.lblToolType.Location = new System.Drawing.Point(23, 112);
            this.lblToolType.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblToolType.Name = "lblToolType";
            this.lblToolType.Size = new System.Drawing.Size(121, 35);
            this.lblToolType.TabIndex = 41;
            this.lblToolType.Text = "检测工具类型：";
            this.lblToolType.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // uiComboBoxToolType
            // 
            this.uiComboBoxToolType.DataSource = null;
            this.uiComboBoxToolType.FillColor = System.Drawing.Color.White;
            this.uiComboBoxToolType.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiComboBoxToolType.ItemHoverColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(200)))), ((int)(((byte)(255)))));
            this.uiComboBoxToolType.ItemSelectForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.uiComboBoxToolType.Location = new System.Drawing.Point(152, 110);
            this.uiComboBoxToolType.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.uiComboBoxToolType.MinimumSize = new System.Drawing.Size(63, 0);
            this.uiComboBoxToolType.Name = "uiComboBoxToolType";
            this.uiComboBoxToolType.Padding = new System.Windows.Forms.Padding(0, 0, 30, 2);
            this.uiComboBoxToolType.Size = new System.Drawing.Size(220, 39);
            this.uiComboBoxToolType.SymbolSize = 24;
            this.uiComboBoxToolType.TabIndex = 1;
            this.uiComboBoxToolType.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.uiComboBoxToolType.Watermark = "选择工具类型";
            // 
            // btnAdd
            // 
            this.btnAdd.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAdd.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnAdd.Location = new System.Drawing.Point(43, 184);
            this.btnAdd.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(100, 35);
            this.btnAdd.TabIndex = 2;
            this.btnAdd.Text = "保存";
            this.btnAdd.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnAdd.Click += new System.EventHandler(this.BtnAdd_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCancel.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnCancel.Location = new System.Drawing.Point(253, 184);
            this.btnCancel.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 35);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "取消";
            this.btnCancel.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            // 
            // FrmVpToolEdit
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(394, 255);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.uiComboBoxToolType);
            this.Controls.Add(this.lblToolType);
            this.Controls.Add(this.txtBoxToolName);
            this.Controls.Add(this.lblToolName);
            this.Name = "FrmVpToolEdit";
            this.ZoomScaleRect = new System.Drawing.Rectangle(19, 19, 394, 255);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblToolName;
        private System.Windows.Forms.Label lblToolType;
        private Sunny.UI.UIButton btnAdd;
        private Sunny.UI.UIButton btnCancel;
        public System.Windows.Forms.TextBox txtBoxToolName;
        public Sunny.UI.UIComboBox uiComboBoxToolType;
    }
}
