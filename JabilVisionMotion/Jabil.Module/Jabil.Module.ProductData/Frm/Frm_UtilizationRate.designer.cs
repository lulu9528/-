namespace Jabil.Module.ProductData.Frm
{
    partial class Frm_UtilizationRate
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle9 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle10 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle11 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle12 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle13 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle14 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle15 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle16 = new System.Windows.Forms.DataGridViewCellStyle();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.uiSplitContainer1 = new Sunny.UI.UISplitContainer();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.previousShiftGroupBox = new Sunny.UI.UIGroupBox();
            this.previousShiftGridView = new Sunny.UI.UIDataGridView();
            this.currentShiftGroupBox = new Sunny.UI.UIGroupBox();
            this.currentShiftGridView = new Sunny.UI.UIDataGridView();
            this.ctGroupBox = new Sunny.UI.UIGroupBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.datePicker = new System.Windows.Forms.DateTimePicker();
            this.utilizationChart = new Sunny.UI.UIBarChart();
            this._refreshTimer = new System.Windows.Forms.Timer(this.components);
            this.btnClearData = new System.Windows.Forms.Button();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.uiSplitContainer1)).BeginInit();
            this.uiSplitContainer1.Panel1.SuspendLayout();
            this.uiSplitContainer1.Panel2.SuspendLayout();
            this.uiSplitContainer1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.previousShiftGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.previousShiftGridView)).BeginInit();
            this.currentShiftGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.currentShiftGridView)).BeginInit();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 85F));
            this.tableLayoutPanel1.Controls.Add(this.uiSplitContainer1, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(711, 546);
            this.tableLayoutPanel1.TabIndex = 1;
            // 
            // uiSplitContainer1
            // 
            this.uiSplitContainer1.Cursor = System.Windows.Forms.Cursors.Default;
            this.uiSplitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiSplitContainer1.IsSplitterFixed = true;
            this.uiSplitContainer1.Location = new System.Drawing.Point(3, 3);
            this.uiSplitContainer1.MinimumSize = new System.Drawing.Size(20, 20);
            this.uiSplitContainer1.Name = "uiSplitContainer1";
            this.uiSplitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // uiSplitContainer1.Panel1
            // 
            this.uiSplitContainer1.Panel1.Controls.Add(this.tableLayoutPanel2);
            // 
            // uiSplitContainer1.Panel2
            // 
            this.uiSplitContainer1.Panel2.Controls.Add(this.utilizationChart);
            this.uiSplitContainer1.Size = new System.Drawing.Size(705, 540);
            this.uiSplitContainer1.SplitterDistance = 493;
            this.uiSplitContainer1.SplitterWidth = 11;
            this.uiSplitContainer1.TabIndex = 4;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 1;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Controls.Add(this.tableLayoutPanel3, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.panel2, 0, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 2;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 4F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 58F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(705, 493);
            this.tableLayoutPanel2.TabIndex = 1;
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 3;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 44F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 44F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12F));
            this.tableLayoutPanel3.Controls.Add(this.previousShiftGroupBox, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.currentShiftGroupBox, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.ctGroupBox, 2, 0);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(0, 31);
            this.tableLayoutPanel3.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 1;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(705, 462);
            this.tableLayoutPanel3.TabIndex = 0;
            // 
            // previousShiftGroupBox
            // 
            this.previousShiftGroupBox.Controls.Add(this.previousShiftGridView);
            this.previousShiftGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.previousShiftGroupBox.Font = new System.Drawing.Font("宋体", 10.5F);
            this.previousShiftGroupBox.Location = new System.Drawing.Point(310, 0);
            this.previousShiftGroupBox.Margin = new System.Windows.Forms.Padding(0);
            this.previousShiftGroupBox.MinimumSize = new System.Drawing.Size(1, 1);
            this.previousShiftGroupBox.Name = "previousShiftGroupBox";
            this.previousShiftGroupBox.Padding = new System.Windows.Forms.Padding(0, 32, 0, 0);
            this.previousShiftGroupBox.Size = new System.Drawing.Size(310, 462);
            this.previousShiftGroupBox.TabIndex = 2;
            this.previousShiftGroupBox.Text = "上一班";
            this.previousShiftGroupBox.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // previousShiftGridView
            // 
            dataGridViewCellStyle9.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.previousShiftGridView.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle9;
            this.previousShiftGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.previousShiftGridView.BackgroundColor = System.Drawing.Color.White;
            this.previousShiftGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle10.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle10.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle10.Font = new System.Drawing.Font("宋体", 12F);
            dataGridViewCellStyle10.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle10.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle10.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle10.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.previousShiftGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle10;
            this.previousShiftGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.previousShiftGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.previousShiftGridView.EnableHeadersVisualStyles = false;
            this.previousShiftGridView.Font = new System.Drawing.Font("宋体", 12F);
            this.previousShiftGridView.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            this.previousShiftGridView.Location = new System.Drawing.Point(0, 32);
            this.previousShiftGridView.Name = "previousShiftGridView";
            this.previousShiftGridView.ReadOnly = true;
            dataGridViewCellStyle11.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle11.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle11.Font = new System.Drawing.Font("宋体", 12F);
            dataGridViewCellStyle11.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle11.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle11.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle11.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.previousShiftGridView.RowHeadersDefaultCellStyle = dataGridViewCellStyle11;
            dataGridViewCellStyle12.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle12.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.previousShiftGridView.RowsDefaultCellStyle = dataGridViewCellStyle12;
            this.previousShiftGridView.RowTemplate.Height = 23;
            this.previousShiftGridView.SelectedIndex = -1;
            this.previousShiftGridView.Size = new System.Drawing.Size(310, 430);
            this.previousShiftGridView.StripeOddColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.previousShiftGridView.TabIndex = 1;
            // 
            // currentShiftGroupBox
            // 
            this.currentShiftGroupBox.Controls.Add(this.currentShiftGridView);
            this.currentShiftGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.currentShiftGroupBox.Font = new System.Drawing.Font("宋体", 10.5F);
            this.currentShiftGroupBox.Location = new System.Drawing.Point(0, 0);
            this.currentShiftGroupBox.Margin = new System.Windows.Forms.Padding(0);
            this.currentShiftGroupBox.MinimumSize = new System.Drawing.Size(1, 1);
            this.currentShiftGroupBox.Name = "currentShiftGroupBox";
            this.currentShiftGroupBox.Padding = new System.Windows.Forms.Padding(0, 32, 0, 0);
            this.currentShiftGroupBox.Size = new System.Drawing.Size(310, 462);
            this.currentShiftGroupBox.TabIndex = 1;
            this.currentShiftGroupBox.Text = "当班";
            this.currentShiftGroupBox.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // currentShiftGridView
            // 
            dataGridViewCellStyle13.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.currentShiftGridView.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle13;
            this.currentShiftGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.currentShiftGridView.BackgroundColor = System.Drawing.Color.White;
            this.currentShiftGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle14.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle14.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle14.Font = new System.Drawing.Font("宋体", 12F);
            dataGridViewCellStyle14.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle14.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle14.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle14.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.currentShiftGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle14;
            this.currentShiftGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.currentShiftGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.currentShiftGridView.EnableHeadersVisualStyles = false;
            this.currentShiftGridView.Font = new System.Drawing.Font("宋体", 12F);
            this.currentShiftGridView.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            this.currentShiftGridView.Location = new System.Drawing.Point(0, 32);
            this.currentShiftGridView.Name = "currentShiftGridView";
            this.currentShiftGridView.ReadOnly = true;
            dataGridViewCellStyle15.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle15.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle15.Font = new System.Drawing.Font("宋体", 12F);
            dataGridViewCellStyle15.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle15.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle15.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle15.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.currentShiftGridView.RowHeadersDefaultCellStyle = dataGridViewCellStyle15;
            dataGridViewCellStyle16.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle16.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.currentShiftGridView.RowsDefaultCellStyle = dataGridViewCellStyle16;
            this.currentShiftGridView.RowTemplate.Height = 23;
            this.currentShiftGridView.SelectedIndex = -1;
            this.currentShiftGridView.Size = new System.Drawing.Size(310, 430);
            this.currentShiftGridView.StripeOddColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.currentShiftGridView.TabIndex = 0;
            // 
            // ctGroupBox
            // 
            this.ctGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ctGroupBox.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.ctGroupBox.Location = new System.Drawing.Point(624, 5);
            this.ctGroupBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.ctGroupBox.MinimumSize = new System.Drawing.Size(1, 1);
            this.ctGroupBox.Name = "ctGroupBox";
            this.ctGroupBox.Padding = new System.Windows.Forms.Padding(0, 32, 0, 0);
            this.ctGroupBox.Size = new System.Drawing.Size(77, 452);
            this.ctGroupBox.TabIndex = 3;
            this.ctGroupBox.Text = "CT";
            this.ctGroupBox.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.btnClearData);
            this.panel2.Controls.Add(this.datePicker);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Margin = new System.Windows.Forms.Padding(0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(705, 31);
            this.panel2.TabIndex = 1;
            // 
            // datePicker
            // 
            this.datePicker.Location = new System.Drawing.Point(3, 0);
            this.datePicker.Name = "datePicker";
            this.datePicker.Size = new System.Drawing.Size(200, 26);
            this.datePicker.TabIndex = 0;
            this.datePicker.ValueChanged += new System.EventHandler(this.DatePicker_ValueChanged);
            // 
            // utilizationChart
            // 
            this.utilizationChart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.utilizationChart.Font = new System.Drawing.Font("宋体", 9F);
            this.utilizationChart.LegendFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.utilizationChart.Location = new System.Drawing.Point(0, 0);
            this.utilizationChart.Margin = new System.Windows.Forms.Padding(0);
            this.utilizationChart.MinimumSize = new System.Drawing.Size(1, 1);
            this.utilizationChart.Name = "utilizationChart";
            this.utilizationChart.Size = new System.Drawing.Size(705, 36);
            this.utilizationChart.SubFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.utilizationChart.TabIndex = 3;
            this.utilizationChart.Text = "uiBarChart1";
            // 
            // _refreshTimer
            // 
            this._refreshTimer.Enabled = true;
            // 
            // btnClearData
            // 
            this.btnClearData.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(249)))), ((int)(((byte)(255)))));
            this.btnClearData.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnClearData.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClearData.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Bold);
            this.btnClearData.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.btnClearData.Location = new System.Drawing.Point(630, 0);
            this.btnClearData.Name = "btnClearData";
            this.btnClearData.Size = new System.Drawing.Size(75, 31);
            this.btnClearData.TabIndex = 2;
            this.btnClearData.Text = "重置";
            this.btnClearData.UseVisualStyleBackColor = false;
            this.btnClearData.Click += new System.EventHandler(this.btnClearData_Click);
            // 
            // Frm_UtilizationRate
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(711, 546);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "Frm_UtilizationRate";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.uiSplitContainer1.Panel1.ResumeLayout(false);
            this.uiSplitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.uiSplitContainer1)).EndInit();
            this.uiSplitContainer1.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel3.ResumeLayout(false);
            this.previousShiftGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.previousShiftGridView)).EndInit();
            this.currentShiftGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.currentShiftGridView)).EndInit();
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }


        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private Sunny.UI.UISplitContainer uiSplitContainer1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private Sunny.UI.UIGroupBox previousShiftGroupBox;
        private Sunny.UI.UIDataGridView previousShiftGridView;
        private Sunny.UI.UIGroupBox currentShiftGroupBox;
        private Sunny.UI.UIDataGridView currentShiftGridView;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.DateTimePicker datePicker;
        private Sunny.UI.UIBarChart utilizationChart;
        private System.Windows.Forms.Timer _refreshTimer;
        private Sunny.UI.UIGroupBox ctGroupBox;
        private System.Windows.Forms.Button btnClearData;
    }
}
