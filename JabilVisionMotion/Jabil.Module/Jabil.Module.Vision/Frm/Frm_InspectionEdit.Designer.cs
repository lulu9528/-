
namespace Jabil.Module.Vision.Frm
{
    partial class Frm_InspectionEdit
    {
        #region Windows 窗体设计器生成的代码
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容
        /// </summary>
        private void InitializeComponent()
        {
            this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.cbxAcqName = new Sunny.UI.UIComboBox();
            this.lblAcqTool = new System.Windows.Forms.Label();
            this.txtInspectionName = new System.Windows.Forms.TextBox();
            this.lblInspectionName = new System.Windows.Forms.Label();
            this.txtInspectionType = new System.Windows.Forms.TextBox();
            this.lblInspectionType = new System.Windows.Forms.Label();
            this.txtInspectionIndex = new System.Windows.Forms.TextBox();
            this.lblInspectionIndex = new System.Windows.Forms.Label();
            this.txtInspectionDefinition = new System.Windows.Forms.TextBox();
            this.lblInspectionDefinition = new System.Windows.Forms.Label();
            this.cbxToolBlockName = new Sunny.UI.UIComboBox();
            this.lblToolBlock = new System.Windows.Forms.Label();
            this.cbxCalibName = new Sunny.UI.UIComboBox();
            this.lblCalibTool = new System.Windows.Forms.Label();
            this.txtProductColor = new System.Windows.Forms.TextBox();
            this.lblProductColor = new System.Windows.Forms.Label();
            this.txtContrast = new System.Windows.Forms.TextBox();
            this.lblContrast = new System.Windows.Forms.Label();
            this.txtExposure = new System.Windows.Forms.TextBox();
            this.lblExposure = new System.Windows.Forms.Label();
            this.buttonsPanel = new System.Windows.Forms.Panel();
            this.btnCancel = new Sunny.UI.UIButton();
            this.btnSave = new Sunny.UI.UIButton();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel.SuspendLayout();
            this.buttonsPanel.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel
            // 
            this.tableLayoutPanel.ColumnCount = 4;
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 260F));
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 260F));
            this.tableLayoutPanel.Controls.Add(this.cbxAcqName, 1, 2);
            this.tableLayoutPanel.Controls.Add(this.lblAcqTool, 0, 2);
            this.tableLayoutPanel.Controls.Add(this.txtInspectionName, 1, 0);
            this.tableLayoutPanel.Controls.Add(this.lblInspectionName, 0, 0);
            this.tableLayoutPanel.Controls.Add(this.txtInspectionType, 3, 0);
            this.tableLayoutPanel.Controls.Add(this.lblInspectionType, 2, 0);
            this.tableLayoutPanel.Controls.Add(this.txtInspectionIndex, 3, 1);
            this.tableLayoutPanel.Controls.Add(this.lblInspectionIndex, 2, 1);
            this.tableLayoutPanel.Controls.Add(this.txtInspectionDefinition, 1, 1);
            this.tableLayoutPanel.Controls.Add(this.lblInspectionDefinition, 0, 1);
            this.tableLayoutPanel.Controls.Add(this.cbxToolBlockName, 3, 2);
            this.tableLayoutPanel.Controls.Add(this.lblToolBlock, 2, 2);
            this.tableLayoutPanel.Controls.Add(this.cbxCalibName, 1, 3);
            this.tableLayoutPanel.Controls.Add(this.lblCalibTool, 0, 3);
            this.tableLayoutPanel.Controls.Add(this.txtProductColor, 3, 3);
            this.tableLayoutPanel.Controls.Add(this.lblProductColor, 2, 3);
            this.tableLayoutPanel.Controls.Add(this.txtContrast, 3, 4);
            this.tableLayoutPanel.Controls.Add(this.lblContrast, 2, 4);
            this.tableLayoutPanel.Controls.Add(this.txtExposure, 1, 4);
            this.tableLayoutPanel.Controls.Add(this.lblExposure, 0, 4);
            this.tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel.Location = new System.Drawing.Point(5, 5);
            this.tableLayoutPanel.Margin = new System.Windows.Forms.Padding(5);
            this.tableLayoutPanel.Name = "tableLayoutPanel";
            this.tableLayoutPanel.RowCount = 5;
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tableLayoutPanel.Size = new System.Drawing.Size(850, 242);
            this.tableLayoutPanel.TabIndex = 0;
            // 
            // cbxAcqName
            // 
            this.cbxAcqName.DataSource = null;
            this.cbxAcqName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cbxAcqName.FillColor = System.Drawing.Color.White;
            this.cbxAcqName.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.cbxAcqName.ItemHoverColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(200)))), ((int)(((byte)(255)))));
            this.cbxAcqName.ItemSelectForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.cbxAcqName.Location = new System.Drawing.Point(155, 105);
            this.cbxAcqName.Margin = new System.Windows.Forms.Padding(5);
            this.cbxAcqName.MinimumSize = new System.Drawing.Size(63, 0);
            this.cbxAcqName.Name = "cbxAcqName";
            this.cbxAcqName.Padding = new System.Windows.Forms.Padding(0, 0, 30, 2);
            this.cbxAcqName.Size = new System.Drawing.Size(250, 40);
            this.cbxAcqName.SymbolSize = 24;
            this.cbxAcqName.TabIndex = 43;
            this.cbxAcqName.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.cbxAcqName.Watermark = "选择取像工具";
            // 
            // lblAcqTool
            // 
            this.lblAcqTool.BackColor = System.Drawing.Color.Transparent;
            this.lblAcqTool.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAcqTool.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblAcqTool.ForeColor = System.Drawing.Color.Black;
            this.lblAcqTool.Location = new System.Drawing.Point(5, 100);
            this.lblAcqTool.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.lblAcqTool.Name = "lblAcqTool";
            this.lblAcqTool.Size = new System.Drawing.Size(140, 50);
            this.lblAcqTool.TabIndex = 45;
            this.lblAcqTool.Text = "绑定取像工具：";
            this.lblAcqTool.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtInspectionName
            // 
            this.txtInspectionName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtInspectionName.Location = new System.Drawing.Point(155, 5);
            this.txtInspectionName.Margin = new System.Windows.Forms.Padding(5);
            this.txtInspectionName.Name = "txtInspectionName";
            this.txtInspectionName.Size = new System.Drawing.Size(250, 26);
            this.txtInspectionName.TabIndex = 42;
            // 
            // lblInspectionName
            // 
            this.lblInspectionName.BackColor = System.Drawing.Color.Transparent;
            this.lblInspectionName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblInspectionName.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblInspectionName.ForeColor = System.Drawing.Color.Black;
            this.lblInspectionName.Location = new System.Drawing.Point(5, 0);
            this.lblInspectionName.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.lblInspectionName.Name = "lblInspectionName";
            this.lblInspectionName.Size = new System.Drawing.Size(140, 50);
            this.lblInspectionName.TabIndex = 44;
            this.lblInspectionName.Text = "检测点位名称：";
            this.lblInspectionName.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtInspectionType
            // 
            this.txtInspectionType.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtInspectionType.Location = new System.Drawing.Point(565, 5);
            this.txtInspectionType.Margin = new System.Windows.Forms.Padding(5);
            this.txtInspectionType.Name = "txtInspectionType";
            this.txtInspectionType.Size = new System.Drawing.Size(280, 26);
            this.txtInspectionType.TabIndex = 46;
            // 
            // lblInspectionType
            // 
            this.lblInspectionType.BackColor = System.Drawing.Color.Transparent;
            this.lblInspectionType.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblInspectionType.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblInspectionType.ForeColor = System.Drawing.Color.Black;
            this.lblInspectionType.Location = new System.Drawing.Point(415, 0);
            this.lblInspectionType.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.lblInspectionType.Name = "lblInspectionType";
            this.lblInspectionType.Size = new System.Drawing.Size(140, 50);
            this.lblInspectionType.TabIndex = 47;
            this.lblInspectionType.Text = "检测点位类型：";
            this.lblInspectionType.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtInspectionIndex
            // 
            this.txtInspectionIndex.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtInspectionIndex.Location = new System.Drawing.Point(565, 55);
            this.txtInspectionIndex.Margin = new System.Windows.Forms.Padding(5);
            this.txtInspectionIndex.Name = "txtInspectionIndex";
            this.txtInspectionIndex.Size = new System.Drawing.Size(280, 26);
            this.txtInspectionIndex.TabIndex = 50;
            // 
            // lblInspectionIndex
            // 
            this.lblInspectionIndex.BackColor = System.Drawing.Color.Transparent;
            this.lblInspectionIndex.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblInspectionIndex.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblInspectionIndex.ForeColor = System.Drawing.Color.Black;
            this.lblInspectionIndex.Location = new System.Drawing.Point(415, 50);
            this.lblInspectionIndex.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.lblInspectionIndex.Name = "lblInspectionIndex";
            this.lblInspectionIndex.Size = new System.Drawing.Size(140, 50);
            this.lblInspectionIndex.TabIndex = 51;
            this.lblInspectionIndex.Text = "检测点位索引:";
            this.lblInspectionIndex.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtInspectionDefinition
            // 
            this.txtInspectionDefinition.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtInspectionDefinition.Location = new System.Drawing.Point(155, 55);
            this.txtInspectionDefinition.Margin = new System.Windows.Forms.Padding(5);
            this.txtInspectionDefinition.Name = "txtInspectionDefinition";
            this.txtInspectionDefinition.Size = new System.Drawing.Size(250, 26);
            this.txtInspectionDefinition.TabIndex = 48;
            // 
            // lblInspectionDefinition
            // 
            this.lblInspectionDefinition.BackColor = System.Drawing.Color.Transparent;
            this.lblInspectionDefinition.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblInspectionDefinition.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblInspectionDefinition.ForeColor = System.Drawing.Color.Black;
            this.lblInspectionDefinition.Location = new System.Drawing.Point(5, 50);
            this.lblInspectionDefinition.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.lblInspectionDefinition.Name = "lblInspectionDefinition";
            this.lblInspectionDefinition.Size = new System.Drawing.Size(140, 50);
            this.lblInspectionDefinition.TabIndex = 49;
            this.lblInspectionDefinition.Text = "检测点位定义：";
            this.lblInspectionDefinition.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cbxToolBlockName
            // 
            this.cbxToolBlockName.DataSource = null;
            this.cbxToolBlockName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cbxToolBlockName.FillColor = System.Drawing.Color.White;
            this.cbxToolBlockName.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.cbxToolBlockName.ItemHoverColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(200)))), ((int)(((byte)(255)))));
            this.cbxToolBlockName.ItemSelectForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.cbxToolBlockName.Location = new System.Drawing.Point(565, 105);
            this.cbxToolBlockName.Margin = new System.Windows.Forms.Padding(5);
            this.cbxToolBlockName.MinimumSize = new System.Drawing.Size(63, 0);
            this.cbxToolBlockName.Name = "cbxToolBlockName";
            this.cbxToolBlockName.Padding = new System.Windows.Forms.Padding(0, 0, 30, 2);
            this.cbxToolBlockName.Size = new System.Drawing.Size(280, 40);
            this.cbxToolBlockName.SymbolSize = 24;
            this.cbxToolBlockName.TabIndex = 52;
            this.cbxToolBlockName.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.cbxToolBlockName.Watermark = "选择检测工具";
            // 
            // lblToolBlock
            // 
            this.lblToolBlock.BackColor = System.Drawing.Color.Transparent;
            this.lblToolBlock.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblToolBlock.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblToolBlock.ForeColor = System.Drawing.Color.Black;
            this.lblToolBlock.Location = new System.Drawing.Point(415, 100);
            this.lblToolBlock.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.lblToolBlock.Name = "lblToolBlock";
            this.lblToolBlock.Size = new System.Drawing.Size(140, 50);
            this.lblToolBlock.TabIndex = 53;
            this.lblToolBlock.Text = "绑定检测工具：";
            this.lblToolBlock.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cbxCalibName
            // 
            this.cbxCalibName.DataSource = null;
            this.cbxCalibName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cbxCalibName.FillColor = System.Drawing.Color.White;
            this.cbxCalibName.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.cbxCalibName.ItemHoverColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(200)))), ((int)(((byte)(255)))));
            this.cbxCalibName.ItemSelectForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.cbxCalibName.Location = new System.Drawing.Point(155, 155);
            this.cbxCalibName.Margin = new System.Windows.Forms.Padding(5);
            this.cbxCalibName.MinimumSize = new System.Drawing.Size(63, 0);
            this.cbxCalibName.Name = "cbxCalibName";
            this.cbxCalibName.Padding = new System.Windows.Forms.Padding(0, 0, 30, 2);
            this.cbxCalibName.Size = new System.Drawing.Size(250, 40);
            this.cbxCalibName.SymbolSize = 24;
            this.cbxCalibName.TabIndex = 54;
            this.cbxCalibName.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.cbxCalibName.Watermark = "选择标定工具";
            // 
            // lblCalibTool
            // 
            this.lblCalibTool.BackColor = System.Drawing.Color.Transparent;
            this.lblCalibTool.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCalibTool.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCalibTool.ForeColor = System.Drawing.Color.Black;
            this.lblCalibTool.Location = new System.Drawing.Point(5, 150);
            this.lblCalibTool.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.lblCalibTool.Name = "lblCalibTool";
            this.lblCalibTool.Size = new System.Drawing.Size(140, 50);
            this.lblCalibTool.TabIndex = 55;
            this.lblCalibTool.Text = "绑定标定工具：";
            this.lblCalibTool.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtProductColor
            // 
            this.txtProductColor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtProductColor.Location = new System.Drawing.Point(565, 155);
            this.txtProductColor.Margin = new System.Windows.Forms.Padding(5);
            this.txtProductColor.Name = "txtProductColor";
            this.txtProductColor.Size = new System.Drawing.Size(280, 26);
            this.txtProductColor.TabIndex = 56;
            // 
            // lblProductColor
            // 
            this.lblProductColor.BackColor = System.Drawing.Color.Transparent;
            this.lblProductColor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblProductColor.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblProductColor.ForeColor = System.Drawing.Color.Black;
            this.lblProductColor.Location = new System.Drawing.Point(415, 150);
            this.lblProductColor.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.lblProductColor.Name = "lblProductColor";
            this.lblProductColor.Size = new System.Drawing.Size(140, 50);
            this.lblProductColor.TabIndex = 57;
            this.lblProductColor.Text = "产品颜色定义：";
            this.lblProductColor.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtContrast
            // 
            this.txtContrast.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtContrast.Location = new System.Drawing.Point(565, 205);
            this.txtContrast.Margin = new System.Windows.Forms.Padding(5);
            this.txtContrast.Name = "txtContrast";
            this.txtContrast.Size = new System.Drawing.Size(280, 26);
            this.txtContrast.TabIndex = 60;
            // 
            // lblContrast
            // 
            this.lblContrast.BackColor = System.Drawing.Color.Transparent;
            this.lblContrast.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblContrast.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblContrast.ForeColor = System.Drawing.Color.Black;
            this.lblContrast.Location = new System.Drawing.Point(415, 200);
            this.lblContrast.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.lblContrast.Name = "lblContrast";
            this.lblContrast.Size = new System.Drawing.Size(140, 50);
            this.lblContrast.TabIndex = 61;
            this.lblContrast.Text = "取像工具对比度：";
            this.lblContrast.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtExposure
            // 
            this.txtExposure.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtExposure.Location = new System.Drawing.Point(155, 205);
            this.txtExposure.Margin = new System.Windows.Forms.Padding(5);
            this.txtExposure.Name = "txtExposure";
            this.txtExposure.Size = new System.Drawing.Size(250, 26);
            this.txtExposure.TabIndex = 58;
            // 
            // lblExposure
            // 
            this.lblExposure.BackColor = System.Drawing.Color.Transparent;
            this.lblExposure.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblExposure.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblExposure.ForeColor = System.Drawing.Color.Black;
            this.lblExposure.Location = new System.Drawing.Point(5, 200);
            this.lblExposure.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.lblExposure.Name = "lblExposure";
            this.lblExposure.Size = new System.Drawing.Size(140, 50);
            this.lblExposure.TabIndex = 59;
            this.lblExposure.Text = "取像工具曝光值：";
            this.lblExposure.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // buttonsPanel
            // 
            this.buttonsPanel.Controls.Add(this.btnCancel);
            this.buttonsPanel.Controls.Add(this.btnSave);
            this.buttonsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonsPanel.Location = new System.Drawing.Point(5, 257);
            this.buttonsPanel.Margin = new System.Windows.Forms.Padding(5);
            this.buttonsPanel.Name = "buttonsPanel";
            this.buttonsPanel.Size = new System.Drawing.Size(850, 59);
            this.buttonsPanel.TabIndex = 1;
            // 
            // btnCancel
            // 
            this.btnCancel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCancel.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnCancel.Location = new System.Drawing.Point(670, 10);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(5);
            this.btnCancel.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(120, 35);
            this.btnCancel.TabIndex = 63;
            this.btnCancel.Text = "取消";
            this.btnCancel.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            // 
            // btnSave
            // 
            this.btnSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSave.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnSave.Location = new System.Drawing.Point(540, 10);
            this.btnSave.Margin = new System.Windows.Forms.Padding(5);
            this.btnSave.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(120, 35);
            this.btnSave.TabIndex = 62;
            this.btnSave.Text = "保存";
            this.btnSave.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnSave.Click += new System.EventHandler(this.BtnSave_Click);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.buttonsPanel, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(30, 30);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 78.75F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 21.25F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(860, 321);
            this.tableLayoutPanel1.TabIndex = 2;
            // 
            // FrmInspectionEdit
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(920, 381);
            this.Controls.Add(this.tableLayoutPanel1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmInspectionEdit";
            this.Padding = new System.Windows.Forms.Padding(30);
            this.Text = "检测点位编辑";
            this.ZoomScaleRect = new System.Drawing.Rectangle(19, 19, 860, 380);
            this.tableLayoutPanel.ResumeLayout(false);
            this.tableLayoutPanel.PerformLayout();
            this.buttonsPanel.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
        private System.Windows.Forms.Panel buttonsPanel;
        private Sunny.UI.UIComboBox cbxAcqName;
        private System.Windows.Forms.Label lblAcqTool;
        private System.Windows.Forms.TextBox txtInspectionName;
        private System.Windows.Forms.Label lblInspectionName;
        private System.Windows.Forms.TextBox txtInspectionType;
        private System.Windows.Forms.Label lblInspectionType;
        private System.Windows.Forms.TextBox txtInspectionIndex;
        private System.Windows.Forms.Label lblInspectionIndex;
        private System.Windows.Forms.TextBox txtInspectionDefinition;
        private System.Windows.Forms.Label lblInspectionDefinition;
        private Sunny.UI.UIComboBox cbxToolBlockName;
        private System.Windows.Forms.Label lblToolBlock;
        private Sunny.UI.UIComboBox cbxCalibName;
        private System.Windows.Forms.Label lblCalibTool;
        private System.Windows.Forms.TextBox txtProductColor;
        private System.Windows.Forms.Label lblProductColor;
        private System.Windows.Forms.TextBox txtContrast;
        private System.Windows.Forms.Label lblContrast;
        private System.Windows.Forms.TextBox txtExposure;
        private System.Windows.Forms.Label lblExposure;
        private Sunny.UI.UIButton btnCancel;
        private Sunny.UI.UIButton btnSave;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
    }

}