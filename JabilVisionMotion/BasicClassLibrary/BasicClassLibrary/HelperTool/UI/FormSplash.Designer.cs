namespace BasicClassLibrary.HelperTool.UI
{
    partial class FormSplash
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
            this.label_Percent = new System.Windows.Forms.Label();
            this.label_Msg = new System.Windows.Forms.Label();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            // 
            // label_Percent
            // 
            this.label_Percent.AutoSize = true;
            this.label_Percent.ForeColor = System.Drawing.Color.SeaGreen;
            this.label_Percent.Location = new System.Drawing.Point(446, 49);
            this.label_Percent.Name = "label_Percent";
            this.label_Percent.Size = new System.Drawing.Size(23, 12);
            this.label_Percent.TabIndex = 5;
            this.label_Percent.Text = "10%";
            // 
            // label_Msg
            // 
            this.label_Msg.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.label_Msg.ForeColor = System.Drawing.Color.Blue;
            this.label_Msg.Location = new System.Drawing.Point(-1, 9);
            this.label_Msg.Name = "label_Msg";
            this.label_Msg.Size = new System.Drawing.Size(489, 21);
            this.label_Msg.TabIndex = 4;
            this.label_Msg.Text = "label_Msg";
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(-1, 37);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(444, 44);
            this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressBar1.TabIndex = 3;
            // 
            // FormSplash
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.label_Percent);
            this.Controls.Add(this.label_Msg);
            this.Controls.Add(this.progressBar1);
            this.Name = "FormSplash";
            this.Text = "FormSplash";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label_Percent;
        private System.Windows.Forms.Label label_Msg;
        private System.Windows.Forms.ProgressBar progressBar1;
    }
}