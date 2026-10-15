namespace Jabil.Module.ProductData.Frm
{
    partial class Frm_NgStatistics
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Frm_NgStatistics));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
            this._refreshTimer = new System.Windows.Forms.Timer(this.components);
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this._currentShiftGroupBox = new Sunny.UI.UIGroupBox();
            this._currentShiftGridView = new Sunny.UI.UIDataGridView();
            this._previousShiftGroupBox = new Sunny.UI.UIGroupBox();
            this._previousShiftGridView = new Sunny.UI.UIDataGridView();
            this.panel2 = new System.Windows.Forms.Panel();
            this._datePicker = new System.Windows.Forms.DateTimePicker();
            this.btnClearData = new System.Windows.Forms.Button();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this._currentShiftGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._currentShiftGridView)).BeginInit();
            this._previousShiftGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._previousShiftGridView)).BeginInit();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // _refreshTimer
            // 
            this._refreshTimer.Enabled = true;
            this._refreshTimer.Interval = 1000;
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.panel2, 0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // tableLayoutPanel2
            // 
            resources.ApplyResources(this.tableLayoutPanel2, "tableLayoutPanel2");
            this.tableLayoutPanel2.Controls.Add(this._currentShiftGroupBox, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this._previousShiftGroupBox, 1, 0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            // 
            // _currentShiftGroupBox
            // 
            this._currentShiftGroupBox.Controls.Add(this._currentShiftGridView);
            resources.ApplyResources(this._currentShiftGroupBox, "_currentShiftGroupBox");
            this._currentShiftGroupBox.Name = "_currentShiftGroupBox";
            this._currentShiftGroupBox.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _currentShiftGridView
            // 
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this._currentShiftGridView.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            this._currentShiftGridView.BackgroundColor = System.Drawing.Color.White;
            this._currentShiftGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle2.Font = new System.Drawing.Font("宋体", 12F);
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this._currentShiftGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this._currentShiftGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            resources.ApplyResources(this._currentShiftGridView, "_currentShiftGridView");
            this._currentShiftGridView.EnableHeadersVisualStyles = false;
            this._currentShiftGridView.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            this._currentShiftGridView.Name = "_currentShiftGridView";
            this._currentShiftGridView.ReadOnly = true;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle3.Font = new System.Drawing.Font("宋体", 12F);
            dataGridViewCellStyle3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this._currentShiftGridView.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
            dataGridViewCellStyle4.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle4.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this._currentShiftGridView.RowsDefaultCellStyle = dataGridViewCellStyle4;
            this._currentShiftGridView.RowTemplate.Height = 23;
            this._currentShiftGridView.SelectedIndex = -1;
            this._currentShiftGridView.StripeOddColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            // 
            // _previousShiftGroupBox
            // 
            this._previousShiftGroupBox.Controls.Add(this._previousShiftGridView);
            resources.ApplyResources(this._previousShiftGroupBox, "_previousShiftGroupBox");
            this._previousShiftGroupBox.Name = "_previousShiftGroupBox";
            this._previousShiftGroupBox.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _previousShiftGridView
            // 
            dataGridViewCellStyle5.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this._previousShiftGridView.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle5;
            this._previousShiftGridView.BackgroundColor = System.Drawing.Color.White;
            this._previousShiftGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle6.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle6.Font = new System.Drawing.Font("宋体", 12F);
            dataGridViewCellStyle6.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle6.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle6.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle6.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this._previousShiftGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle6;
            this._previousShiftGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            resources.ApplyResources(this._previousShiftGridView, "_previousShiftGridView");
            this._previousShiftGridView.EnableHeadersVisualStyles = false;
            this._previousShiftGridView.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            this._previousShiftGridView.Name = "_previousShiftGridView";
            this._previousShiftGridView.ReadOnly = true;
            dataGridViewCellStyle7.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle7.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle7.Font = new System.Drawing.Font("宋体", 12F);
            dataGridViewCellStyle7.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle7.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle7.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle7.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this._previousShiftGridView.RowHeadersDefaultCellStyle = dataGridViewCellStyle7;
            dataGridViewCellStyle8.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle8.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this._previousShiftGridView.RowsDefaultCellStyle = dataGridViewCellStyle8;
            this._previousShiftGridView.RowTemplate.Height = 23;
            this._previousShiftGridView.SelectedIndex = -1;
            this._previousShiftGridView.StripeOddColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.btnClearData);
            this.panel2.Controls.Add(this._datePicker);
            resources.ApplyResources(this.panel2, "panel2");
            this.panel2.Name = "panel2";
            // 
            // _datePicker
            // 
            resources.ApplyResources(this._datePicker, "_datePicker");
            this._datePicker.Name = "_datePicker";
            this._datePicker.ValueChanged += new System.EventHandler(this.DatePicker_ValueChanged);
            // 
            // btnClearData
            // 
            this.btnClearData.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(249)))), ((int)(((byte)(255)))));
            resources.ApplyResources(this.btnClearData, "btnClearData");
            this.btnClearData.Name = "btnClearData";
            this.btnClearData.UseVisualStyleBackColor = false;
            this.btnClearData.Click += new System.EventHandler(this.btnClearData_Click);
            // 
            // Frm_NgStatistics
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            resources.ApplyResources(this, "$this");
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "Frm_NgStatistics";
            this.TitleFont = new System.Drawing.Font("宋体", 5.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this._currentShiftGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._currentShiftGridView)).EndInit();
            this._previousShiftGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._previousShiftGridView)).EndInit();
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Timer _refreshTimer;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private Sunny.UI.UIGroupBox _currentShiftGroupBox;
        private Sunny.UI.UIDataGridView _currentShiftGridView;
        private Sunny.UI.UIGroupBox _previousShiftGroupBox;
        private Sunny.UI.UIDataGridView _previousShiftGridView;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.DateTimePicker _datePicker;
        private System.Windows.Forms.Button btnClearData;
    }
}
