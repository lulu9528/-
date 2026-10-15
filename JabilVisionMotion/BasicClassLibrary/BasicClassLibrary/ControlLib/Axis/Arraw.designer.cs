namespace BasicClassLibrary.Controls.Axis
{
    partial class Arraw
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
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.btn_Right = new System.Windows.Forms.Button();
            this.btn_Down = new System.Windows.Forms.Button();
            this.btn_Left = new System.Windows.Forms.Button();
            this.btn_Up = new System.Windows.Forms.Button();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btn_Right
            // 
            this.btn_Right.AutoSize = true;
            this.btn_Right.BackColor = System.Drawing.Color.Transparent;
            this.btn_Right.BackgroundImage = global::BasicClassLibrary.Properties.Resources.右;
            this.btn_Right.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_Right.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_Right.FlatAppearance.BorderSize = 0;
            this.btn_Right.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_Right.Location = new System.Drawing.Point(123, 58);
            this.btn_Right.Name = "btn_Right";
            this.btn_Right.Size = new System.Drawing.Size(55, 49);
            this.btn_Right.TabIndex = 3;
            this.btn_Right.Text = "X轴+";
            this.btn_Right.UseVisualStyleBackColor = true;
            this.btn_Right.MouseDown += new System.Windows.Forms.MouseEventHandler(this.btn_Right_MouseDown);
            this.btn_Right.MouseUp += new System.Windows.Forms.MouseEventHandler(this.btn_Right_MouseUp);
            // 
            // btn_Down
            // 
            this.btn_Down.AutoSize = true;
            this.btn_Down.BackColor = System.Drawing.Color.Transparent;
            this.btn_Down.BackgroundImage = global::BasicClassLibrary.Properties.Resources.下;
            this.btn_Down.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_Down.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_Down.FlatAppearance.BorderSize = 0;
            this.btn_Down.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_Down.Location = new System.Drawing.Point(63, 113);
            this.btn_Down.Name = "btn_Down";
            this.btn_Down.Size = new System.Drawing.Size(54, 50);
            this.btn_Down.TabIndex = 2;
            this.btn_Down.Text = "Y轴-";
            this.btn_Down.UseVisualStyleBackColor = true;
            this.btn_Down.MouseDown += new System.Windows.Forms.MouseEventHandler(this.btn_Down_MouseDown);
            this.btn_Down.MouseUp += new System.Windows.Forms.MouseEventHandler(this.btn_Down_MouseUp);
            // 
            // btn_Left
            // 
            this.btn_Left.AutoSize = true;
            this.btn_Left.BackColor = System.Drawing.Color.Transparent;
            this.btn_Left.BackgroundImage = global::BasicClassLibrary.Properties.Resources.左;
            this.btn_Left.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_Left.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_Left.FlatAppearance.BorderSize = 0;
            this.btn_Left.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_Left.Location = new System.Drawing.Point(3, 58);
            this.btn_Left.Name = "btn_Left";
            this.btn_Left.Size = new System.Drawing.Size(54, 49);
            this.btn_Left.TabIndex = 1;
            this.btn_Left.Text = "X轴-";
            this.btn_Left.UseVisualStyleBackColor = true;
            this.btn_Left.MouseDown += new System.Windows.Forms.MouseEventHandler(this.btn_Left_MouseDown);
            this.btn_Left.MouseUp += new System.Windows.Forms.MouseEventHandler(this.btn_Left_MouseUp);
            // 
            // btn_Up
            // 
            this.btn_Up.AutoSize = true;
            this.btn_Up.BackColor = System.Drawing.Color.Transparent;
            this.btn_Up.BackgroundImage = global::BasicClassLibrary.Properties.Resources.上;
            this.btn_Up.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.btn_Up.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_Up.FlatAppearance.BorderSize = 0;
            this.btn_Up.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_Up.Location = new System.Drawing.Point(63, 3);
            this.btn_Up.Name = "btn_Up";
            this.btn_Up.Size = new System.Drawing.Size(54, 49);
            this.btn_Up.TabIndex = 0;
            this.btn_Up.Text = "Y轴+";
            this.btn_Up.UseVisualStyleBackColor = true;
            this.btn_Up.MouseDown += new System.Windows.Forms.MouseEventHandler(this.btn_Up_MouseDown);
            this.btn_Up.MouseUp += new System.Windows.Forms.MouseEventHandler(this.btn_Up_MouseUp);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.BackColor = System.Drawing.Color.Transparent;
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel1.Controls.Add(this.btn_Up, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.btn_Left, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.btn_Down, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.btn_Right, 2, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(181, 166);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // Arraw
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.DoubleBuffered = true;
            this.Name = "Arraw";
            this.Size = new System.Drawing.Size(181, 166);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        public System.Windows.Forms.Button btn_Right;
        public System.Windows.Forms.Button btn_Down;
        public System.Windows.Forms.Button btn_Left;
        public System.Windows.Forms.Button btn_Up;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
    }
}
