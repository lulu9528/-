using Jabil.Module.Vision;
using Sunny.UI;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Jabil.Module.Setting.Lib.JVS
{
    /// <summary>
    /// JVS 进程内嵌入设置页
    /// 编辑 JvsWorkspaceMapping.json（方案路径、取图工具 .jvt、ToolBlock/pin 名称、输出分隔符与字段顺序）
    /// 保存后：映射 JSON 即时持久化；同时按源框架的保存位置写入 VisionTool 表——
    /// 所有取像相机记录（CogAcqFifoTool）切换为 JVS 取图模式（类型=JvsCameraTool、路径=取图工具 .jvt）
    /// </summary>
    public class Frm_JvsSetting : UIForm
    {
        private TextBox txtWorkspacePath;
        private Button btnWorkspaceBrowse;
        private TextBox txtCaptureToolPath;
        private Button btnCaptureBrowse;
        private TextBox txtToolBlock;
        private TextBox txtInputPin;
        private TextBox txtOutputPin;
        private TextBox txtPointSeparator;
        private TextBox txtFieldSeparator;
        private TextBox txtFieldOrder;
        private Button btnSave;
        private Button btnCancel;
        private Label lblStatus;

        public Frm_JvsSetting()
        {
            InitializeUi();
            LoadConfig();
        }

        private void InitializeUi()
        {
            this.Text = "JVS 设置";
            this.Size = new Size(600, 480);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(560, 440);
            this.Padding = new Padding(12);
            this.ShowInTaskbar = false;

            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 9,
                Padding = new Padding(4)
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));

            // 方案路径（检测算法 .jvws）
            grid.Controls.Add(MakeLabel("JVS 方案路径(.jvws)"), 0, 0);
            txtWorkspacePath = new TextBox { Dock = DockStyle.Fill, ReadOnly = true };
            btnWorkspaceBrowse = new Button { Text = "浏览…", Dock = DockStyle.Fill };
            btnWorkspaceBrowse.Click += (s, e) =>
            {
                using (var dlg = new OpenFileDialog())
                {
                    dlg.Title = "选择 JVS 方案文件";
                    dlg.Filter = "JVS 方案(*.jvws)|*.jvws|所有文件(*.*)|*.*";
                    if (File.Exists(txtWorkspacePath.Text))
                        dlg.InitialDirectory = Path.GetDirectoryName(txtWorkspacePath.Text);
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                        txtWorkspacePath.Text = dlg.FileName;
                }
            };
            grid.Controls.Add(txtWorkspacePath, 1, 0);
            grid.Controls.Add(btnWorkspaceBrowse, 2, 0);

            // 取图工具（相机取图算子 .jvt，JVS 导出）
            grid.Controls.Add(MakeLabel("取图工具(.jvt)"), 0, 1);
            txtCaptureToolPath = new TextBox { Dock = DockStyle.Fill, ReadOnly = true };
            btnCaptureBrowse = new Button { Text = "浏览…", Dock = DockStyle.Fill };
            btnCaptureBrowse.Click += (s, e) =>
            {
                using (var dlg = new OpenFileDialog())
                {
                    dlg.Title = "选择 JVS 取图工具（相机算子导出）";
                    dlg.Filter = "JVS 工具(*.jvt)|*.jvt|所有文件(*.*)|*.*";
                    if (File.Exists(txtCaptureToolPath.Text))
                        dlg.InitialDirectory = Path.GetDirectoryName(txtCaptureToolPath.Text);
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                        txtCaptureToolPath.Text = dlg.FileName;
                }
            };
            grid.Controls.Add(txtCaptureToolPath, 1, 1);
            grid.Controls.Add(btnCaptureBrowse, 2, 1);

            // ToolBlock 名称
            grid.Controls.Add(MakeLabel("ToolBlock 名称"), 0, 2);
            txtToolBlock = new TextBox { Dock = DockStyle.Fill };
            grid.Controls.Add(txtToolBlock, 1, 2);

            // 输入 pin
            grid.Controls.Add(MakeLabel("输入图像 pin"), 0, 3);
            txtInputPin = new TextBox { Dock = DockStyle.Fill };
            grid.Controls.Add(txtInputPin, 1, 3);

            // 输出 pin
            grid.Controls.Add(MakeLabel("输出数据 pin"), 0, 4);
            txtOutputPin = new TextBox { Dock = DockStyle.Fill };
            grid.Controls.Add(txtOutputPin, 1, 4);

            // 点分隔符
            grid.Controls.Add(MakeLabel("点分隔符"), 0, 5);
            txtPointSeparator = new TextBox { Dock = DockStyle.Fill };
            grid.Controls.Add(txtPointSeparator, 1, 5);

            // 字段分隔符
            grid.Controls.Add(MakeLabel("字段分隔符"), 0, 6);
            txtFieldSeparator = new TextBox { Dock = DockStyle.Fill };
            grid.Controls.Add(txtFieldSeparator, 1, 6);

            // 字段顺序
            grid.Controls.Add(MakeLabel("字段顺序(X,Y,R)"), 0, 7);
            txtFieldOrder = new TextBox { Dock = DockStyle.Fill };
            grid.Controls.Add(txtFieldOrder, 1, 7);

            // 按钮行
            var btnPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
            btnPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            btnPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            btnPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            btnSave = new Button { Text = "保存", Dock = DockStyle.Fill, Height = 34 };
            btnCancel = new Button { Text = "取消", Dock = DockStyle.Fill, Height = 34 };
            lblStatus = new Label { Dock = DockStyle.Fill, Text = "", TextAlign = ContentAlignment.MiddleRight, AutoSize = false };
            btnPanel.Controls.Add(lblStatus, 0, 0);
            btnPanel.Controls.Add(btnSave, 1, 0);
            btnPanel.Controls.Add(btnCancel, 2, 0);
            grid.Controls.Add(btnPanel, 0, 8);
            grid.SetColumnSpan(btnPanel, 3);

            btnSave.Click += BtnSave_Click;
            btnCancel.Click += (s, e) => Close();

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            var tip = new Label
            {
                Dock = DockStyle.Fill,
                Text = "说明：取图由 JVS 取图工具（.jvt）触发，点胶机进程不驱动任何硬件相机；" +
                       "输出 pin 数据格式为 “X,Y,R;X,Y,R;…”，字段顺序与分隔符按下发方案实际格式调整。" +
                       "保存后，取像相机记录（CogAcqFifoTool）将按源框架位置写入 VisionTool 表并切换为 JVS 取图模式。",
                ForeColor = Color.DimGray,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft
            };
            root.Controls.Add(grid, 0, 0);
            root.Controls.Add(tip, 0, 1);

            this.Controls.Add(root);
        }

        private static Label MakeLabel(string text) => new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = false
        };

        private void LoadConfig()
        {
            var m = JvsVisionBridge.Instance.Mapping;
            txtWorkspacePath.Text = m.WorkspacePath ?? "";
            txtCaptureToolPath.Text = m.CaptureToolPath ?? "";
            txtToolBlock.Text = m.ToolBlock ?? "";
            txtInputPin.Text = m.InputPin ?? "";
            txtOutputPin.Text = m.OutputPin ?? "";
            txtPointSeparator.Text = m.PointSeparator ?? ";";
            txtFieldSeparator.Text = m.FieldSeparator ?? ",";
            txtFieldOrder.Text = m.FieldOrder ?? "X,Y,R";

            // 首装提示：VisionTool 表尚无 JvsCameraTool 记录时，需保存一次才切换为 JVS 取图模式
            // （否则启动仍按 Cognex 模式加载，无硬件相机时开机失败）
            var tools = VisionSystem.Instance?.ListVisionTool;
            if (tools == null || !tools.Any(t => t != null && t.检测工具类型 == "JvsCameraTool"))
            {
                lblStatus.Text = "提示：当前取像相机记录尚未切换为 JVS 取图模式（JvsCameraTool），保存后自动切换。";
                lblStatus.ForeColor = Color.Orange;
            }
        }

        private async void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                var m = JvsVisionBridge.Instance.Mapping;
                m.WorkspacePath = txtWorkspacePath.Text.Trim();
                m.CaptureToolPath = txtCaptureToolPath.Text.Trim();
                m.ToolBlock = txtToolBlock.Text.Trim();
                m.InputPin = txtInputPin.Text.Trim();
                m.OutputPin = txtOutputPin.Text.Trim();
                m.PointSeparator = txtPointSeparator.Text.Trim();
                m.FieldSeparator = txtFieldSeparator.Text.Trim();
                m.FieldOrder = txtFieldOrder.Text.Trim();

                // 前置校验（保存前一次性完成，避免写入中途失败留下不一致状态）
                if (string.IsNullOrWhiteSpace(m.WorkspacePath))
                    throw new FormatException("请选择 JVS 方案路径(.jvws)");
                if (string.IsNullOrWhiteSpace(m.CaptureToolPath))
                    throw new FormatException("请选择取图工具文件(.jvt)");
                if (!File.Exists(m.CaptureToolPath))
                    throw new FormatException("取图工具文件不存在：" + m.CaptureToolPath);

                // 1) 先写 VisionTool 表：取像相机记录（CogAcqFifoTool 或已切换的 JvsCameraTool，幂等）
                //    统一切换为 JVS 取图模式（类型=JvsCameraTool、路径=取图工具 .jvt）。
                //    批量写入：逐条 upsert、循环后统一刷新一次数据源。
                var visionSystem = VisionSystem.Instance;
                var targets = (visionSystem?.ListVisionTool ?? new System.Collections.Generic.List<Jabil.Module.Vision.Common.Dtos.VisionToolDto>())
                    .Where(t => t != null &&
                        (t.检测工具类型 == "CogAcqFifoTool" || t.检测工具类型 == "JvsCameraTool"))
                    .ToList();
                foreach (var tool in targets)
                {
                    tool.检测工具类型 = "JvsCameraTool";
                    tool.检测工具路径 = m.CaptureToolPath;
                }
                if (targets.Count > 0)
                    await visionSystem.AddOrUpdateVisionToolsAsync(targets).ConfigureAwait(true);

                // 2) 表写入成功后，映射 JSON 持久化 + 内存单例刷新（新配置即时生效）。
                //    若第 2 步失败，表已生效、JSON 未写——下次启动仍以 VisionTool 表路径优先，核心功能不受影响
                m.Save();
                JvsWorkspaceMapping.Reload();

                lblStatus.Text = targets.Count > 0
                    ? $"保存成功：{targets.Count} 个取像相机已切换为 JVS 取图模式，映射已保存"
                    : "保存成功：映射已保存（未找到取像相机记录）";
                lblStatus.ForeColor = Color.Green;
            }
            catch (Exception ex)
            {
                lblStatus.Text = "保存失败：" + ex.Message;
                lblStatus.ForeColor = Color.Red;
            }
        }
    }
}
