namespace JabilVisionMotion.Frm.Dialog
{
    partial class Frm_UpdateParam
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            this.btnDownload = new System.Windows.Forms.Button();
            this.btnSearch = new System.Windows.Forms.Button();
            this.dtView = new System.Windows.Forms.DataGridView();
            this.Name1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Value = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ValueTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.NewValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.NewValueTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dtView)).BeginInit();
            this.SuspendLayout();
            // 
            // btnDownload
            // 
            this.btnDownload.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDownload.Font = new System.Drawing.Font("宋体", 12F);
            this.btnDownload.Location = new System.Drawing.Point(673, 119);
            this.btnDownload.Margin = new System.Windows.Forms.Padding(4);
            this.btnDownload.Name = "btnDownload";
            this.btnDownload.Size = new System.Drawing.Size(114, 51);
            this.btnDownload.TabIndex = 17;
            this.btnDownload.Text = "导出";
            this.btnDownload.UseVisualStyleBackColor = true;
            // 
            // btnSearch
            // 
            this.btnSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSearch.Font = new System.Drawing.Font("宋体", 12F);
            this.btnSearch.Location = new System.Drawing.Point(673, 60);
            this.btnSearch.Margin = new System.Windows.Forms.Padding(4);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(114, 51);
            this.btnSearch.TabIndex = 16;
            this.btnSearch.Text = "查询";
            this.btnSearch.UseVisualStyleBackColor = true;
            // 
            // dtView
            // 
            this.dtView.AllowUserToAddRows = false;
            this.dtView.BackgroundColor = System.Drawing.SystemColors.ButtonHighlight;
            this.dtView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(140)))), ((int)(((byte)(220)))));
            dataGridViewCellStyle1.Font = new System.Drawing.Font("宋体", 10F, System.Drawing.FontStyle.Bold);
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dtView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dtView.ColumnHeadersHeight = 22;
            this.dtView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dtView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Name1,
            this.Value,
            this.ValueTime,
            this.NewValue,
            this.NewValueTime});
            this.dtView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dtView.EnableHeadersVisualStyles = false;
            this.dtView.Location = new System.Drawing.Point(0, 0);
            this.dtView.Margin = new System.Windows.Forms.Padding(4);
            this.dtView.Name = "dtView";
            this.dtView.RowHeadersVisible = false;
            this.dtView.RowHeadersWidth = 51;
            this.dtView.RowTemplate.Height = 23;
            this.dtView.Size = new System.Drawing.Size(800, 450);
            this.dtView.TabIndex = 15;
            // 
            // Name1
            // 
            this.Name1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.Name1.DataPropertyName = "Name";
            this.Name1.HeaderText = "参数名称";
            this.Name1.MinimumWidth = 6;
            this.Name1.Name = "Name";
            this.Name1.ReadOnly = true;
            this.Name1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.Name1.Width = 85;
            // 
            // Value
            // 
            this.Value.DataPropertyName = "Value";
            this.Value.HeaderText = "原参数";
            this.Value.MinimumWidth = 6;
            this.Value.Name = "Value";
            this.Value.Width = 160;
            // 
            // ValueTime
            // 
            this.ValueTime.DataPropertyName = "ValueTime";
            dataGridViewCellStyle2.Format = "yyyy-MM-dd HH:mm:ss.fff";
            this.ValueTime.DefaultCellStyle = dataGridViewCellStyle2;
            this.ValueTime.HeaderText = "原参数操作时间 ";
            this.ValueTime.MinimumWidth = 6;
            this.ValueTime.Name = "ValueTime";
            this.ValueTime.Width = 180;
            // 
            // NewValue
            // 
            this.NewValue.DataPropertyName = "NewValue";
            this.NewValue.HeaderText = "现参数";
            this.NewValue.MinimumWidth = 6;
            this.NewValue.Name = "NewValue";
            this.NewValue.Width = 120;
            // 
            // NewValueTime
            // 
            this.NewValueTime.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.NewValueTime.DataPropertyName = "NewValueTime";
            dataGridViewCellStyle3.Format = "yyyy-MM-dd HH:mm:ss.fff";
            this.NewValueTime.DefaultCellStyle = dataGridViewCellStyle3;
            this.NewValueTime.HeaderText = "现参数操作时间";
            this.NewValueTime.MinimumWidth = 6;
            this.NewValueTime.Name = "NewValueTime";
            this.NewValueTime.ReadOnly = true;
            this.NewValueTime.Width = 162;
            // 
            // Frm_UpdateParam
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.btnDownload);
            this.Controls.Add(this.btnSearch);
            this.Controls.Add(this.dtView);
            this.Name = "Frm_UpdateParam";
            this.Text = "修改参数记录";
            ((System.ComponentModel.ISupportInitialize)(this.dtView)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnDownload;
        private System.Windows.Forms.Button btnSearch;
        private System.Windows.Forms.DataGridView dtView;
        private System.Windows.Forms.DataGridViewTextBoxColumn Name1;
        private System.Windows.Forms.DataGridViewTextBoxColumn Value;
        private System.Windows.Forms.DataGridViewTextBoxColumn ValueTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn NewValue;
        private System.Windows.Forms.DataGridViewTextBoxColumn NewValueTime;
    }
}