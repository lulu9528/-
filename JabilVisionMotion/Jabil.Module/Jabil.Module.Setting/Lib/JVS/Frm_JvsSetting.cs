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
    /// 保存后：映射 JSON 即时持久化；同时写入 VisionTool 表——
    /// 所有取像相机记录切换为 JVS 取图模式（类型=JvsCameraTool、路径=取图工具 .jvt）
    /// 界面按功能分三组：方案配置 / 取图工具 / 输出数据格式
    /// </summary>
    public class Frm_JvsSetting : UIForm
    {
        private UITextBox txtWorkspacePath;
        private UIButton btnWorkspaceBrowse;
        private UITextBox txtCaptureToolPath;
        private UIButton btnCaptureBrowse;
        private UITextBox txtToolBlock;
        private UITextBox txtInputPin;
        private UITextBox txtOutputPin;
        private UITextBox txtPointSeparator;
        private UITextBox txtFieldSeparator;
        private UITextBox txtFieldOrder;
        private UIButton btnSave;
        private UIButton btnCancel;
        private UILabel lblStatus;

        public Frm_JvsSetting()
        {
            InitializeUi();
            LoadConfig();
        }

        private void InitializeUi()
        {
            this.Text = "视觉设置（JVS）";
            this.ClientSize = new Size(640, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(600, 480);
            this.Padding = new Padding(10);
            this.ShowInTaskbar = false;

            // ===== ① 加载程序设置 =====
            var grpScheme = MakeGroup("加载程序设置", 0, 0, 628, 132);
            var schemeGrid = MakeGrid(3, 3);
            schemeGrid.RowStyles.Clear();
            for (int i = 0; i < 3; i++)
                schemeGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            schemeGrid.Controls.Add(MakeLabel("JVS 方案路径(.jvws)"), 0, 0);
            txtWorkspacePath = MakeTextBox(true);
            schemeGrid.Controls.Add(txtWorkspacePath, 1, 0);
            btnWorkspaceBrowse = MakeBrowseButton("浏览…", () =>
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
            });
            schemeGrid.Controls.Add(btnWorkspaceBrowse, 2, 0);

            schemeGrid.Controls.Add(MakeLabel("ToolBlock 名称"), 0, 1);
            txtToolBlock = MakeTextBox(false);
            schemeGrid.Controls.Add(txtToolBlock, 1, 1);

            schemeGrid.Controls.Add(MakeLabel("输入图像 pin"), 0, 2);
            txtInputPin = MakeTextBox(false);
            schemeGrid.Controls.Add(txtInputPin, 1, 2);
            grpScheme.Controls.Add(schemeGrid);

            // ===== ② 取像设置 =====
            var grpCapture = MakeGroup("取像设置", 0, 140, 628, 80);
            var captureGrid = MakeGrid(3, 1);
            captureGrid.RowStyles.Clear();
            captureGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            captureGrid.Controls.Add(MakeLabel("取图工具(.jvt)（相机算子导出）"), 0, 0);
            txtCaptureToolPath = MakeTextBox(true);
            captureGrid.Controls.Add(txtCaptureToolPath, 1, 0);
            btnCaptureBrowse = MakeBrowseButton("浏览…", () =>
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
            });
            captureGrid.Controls.Add(btnCaptureBrowse, 2, 0);
            grpCapture.Controls.Add(captureGrid);

            // ===== ③ 输出数据格式 =====
            var grpFormat = MakeGroup("输出数据格式", 0, 228, 628, 156);
            var formatGrid = MakeGrid(3, 4);
            formatGrid.RowStyles.Clear();
            for (int i = 0; i < 4; i++)
                formatGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            formatGrid.Controls.Add(MakeLabel("输出数据 pin"), 0, 0);
            txtOutputPin = MakeTextBox(false);
            formatGrid.Controls.Add(txtOutputPin, 1, 0);
            formatGrid.Controls.Add(MakeLabel("点分隔符"), 0, 1);
            txtPointSeparator = MakeTextBox(false);
            formatGrid.Controls.Add(txtPointSeparator, 1, 1);
            formatGrid.Controls.Add(MakeLabel("字段分隔符"), 0, 2);
            txtFieldSeparator = MakeTextBox(false);
            formatGrid.Controls.Add(txtFieldSeparator, 1, 2);
            formatGrid.Controls.Add(MakeLabel("字段顺序(X,Y,R)"), 0, 3);
            txtFieldOrder = MakeTextBox(false);
            formatGrid.Controls.Add(txtFieldOrder, 1, 3);
            grpFormat.Controls.Add(formatGrid);

            // ===== 将三个分组加入窗体（否则分组不显示） =====
            this.Controls.Add(grpScheme);
            this.Controls.Add(grpCapture);
            this.Controls.Add(grpFormat);

            // ===== 状态提示 =====
            lblStatus = new UILabel
            {
                Location = new Point(0, 392),
                Size = new Size(628, 28),
                Text = "",
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("宋体", 11F, FontStyle.Regular)
            };
            this.Controls.Add(lblStatus);

            // ===== 底部说明 =====
            var tip = new UILabel
            {
                Location = new Point(0, 424),
                Size = new Size(628, 46),
                Text = "说明：取图由 JVS 取图工具（.jvt）驱动，点胶机进程不驱动任何硬件相机；" +
                       "输出 pin 数据格式为“X,Y,R;X,Y,R;…”，字段顺序与分隔符按下发方案实际格式调整。" +
                       "保存后，取像相机记录（VisionTool 表）自动切换为 JVS 取图模式。",
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.TopLeft,
                Font = new Font("宋体", 10F, FontStyle.Regular)
            };
            this.Controls.Add(tip);

            // ===== 保存 / 取消（右下角） =====
            btnCancel = new UIButton
            {
                Text = "取消",
                Location = new Point(440, 478),
                Size = new Size(90, 34),
                Font = new Font("宋体", 12F, FontStyle.Regular)
            };
            btnCancel.Click += (s, e) => Close();
            btnSave = new UIButton
            {
                Text = "保存",
                Location = new Point(538, 478),
                Size = new Size(90, 34),
                Font = new Font("宋体", 12F, FontStyle.Regular)
            };
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnCancel);
            this.Controls.Add(btnSave);
        }

        private static GroupBox MakeGroup(string title, int x, int y, int width, int height)
        {
            return new GroupBox
            {
                Text = title,
                Location = new Point(x, y),
                Size = new Size(width, height),
                Font = new Font("宋体", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 55, 100)
            };
        }

        private static TableLayoutPanel MakeGrid(int columns, int rows)
        {
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = columns,
                RowCount = rows,
                Padding = new Padding(6, 4, 6, 4)
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            return grid;
        }

        private static UILabel MakeLabel(string text)
        {
            return new UILabel
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("宋体", 12F, FontStyle.Regular)
            };
        }

        private static UITextBox MakeTextBox(bool readOnly)
        {
            return new UITextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = readOnly,
                Font = new Font("宋体", 12F, FontStyle.Regular)
            };
        }

        private static UIButton MakeBrowseButton(string text, Action browseAction)
        {
            var btn = new UIButton
            {
                Text = text,
                Dock = DockStyle.Fill,
                Font = new Font("宋体", 11F, FontStyle.Regular)
            };
            btn.Click += (s, e) => browseAction();
            return btn;
        }

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

                // 2) 表写入成功后，映射 JSON 持久化 + 内存单例刷新（新配置即时生效）
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
