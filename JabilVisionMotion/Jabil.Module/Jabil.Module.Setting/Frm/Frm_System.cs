using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using Sunny.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Jabil.Lib.Parameter;
using Jabil.Lib;
using Jabil.Module.Setting.Lib.JVS;

namespace Jabil.Module.Setting.Frm
{
    public partial class Frm_System : UIForm
    {
        public Frm_System()
        {
            InitializeComponent();
        }
        /// <summary>
        /// 窗体对象实例
        /// </summary>
        private static Frm_System _instance;
        public static Frm_System Instance
        {
            get
            {
                if (_instance == null || _instance.IsDisposed)
                    _instance = new Frm_System();
                return _instance;
            }
        }

        // JVS 视觉运行状态面板控件（配置摘要 + 运行状态）
        private Label _lblSchemePath;
        private Label _lblCaptureTool;
        private Label _lblToolBlock;
        private Label _lblInputPin;
        private Label _lblOutputPin;
        private Label _lblSeparators;
        private Label _lblFieldOrder;
        private Label _lblEngineState;
        private Label _lblLastResult;

        private void Frm_Data_Load(object sender, EventArgs e)
        {
            try
            {
                BuildJvsStatusPanel();
                RefreshConfigSummary();
                JvsVisionBridge.LastResultChanged += OnJvsLastResultChanged;
            }
            catch (Exception ex)
            {
                Log.Lib.RLog.SaveError(ex);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try
            {
                JvsVisionBridge.LastResultChanged -= OnJvsLastResultChanged;
            }
            catch (Exception)
            {
                // 退订失败不影响窗体关闭
            }
            base.OnFormClosed(e);
        }

        /// <summary>
        /// 构建 JVS 视觉运行状态面板（配置摘要 + 引擎/检测运行状态）。
        /// </summary>
        private void BuildJvsStatusPanel()
        {
            panel1.SuspendLayout();
            panel1.Controls.Clear();

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(8)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 0));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 210));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // ===== 上区：JVS 配置摘要 =====
            var grpConfig = CreateGroup("JVS 视觉配置摘要");
            var cfgGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 8,
                Padding = new Padding(6)
            };
            cfgGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            cfgGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            AddConfigRow(cfgGrid, "JVS 方案路径(.jvws)", out _lblSchemePath, 0);
            AddConfigRow(cfgGrid, "取图工具(.jvt)", out _lblCaptureTool, 1);
            AddConfigRow(cfgGrid, "ToolBlock 名称", out _lblToolBlock, 2);
            AddConfigRow(cfgGrid, "输入图像 pin", out _lblInputPin, 3);
            AddConfigRow(cfgGrid, "输出数据 pin", out _lblOutputPin, 4);
            AddConfigRow(cfgGrid, "分隔符(点/字段)", out _lblSeparators, 5);
            AddConfigRow(cfgGrid, "字段顺序", out _lblFieldOrder, 6);
            AddConfigRow(cfgGrid, "JVS 引擎状态", out _lblEngineState, 7);
            grpConfig.Controls.Add(cfgGrid);
            root.Controls.Add(grpConfig, 0, 0);

            // ===== 下区：检测运行状态 =====
            var grpState = CreateGroup("检测运行状态");
            var stateGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(6)
            };
            stateGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            stateGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            var lbl = MakeValueLabel();
            lbl.Text = "暂无检测记录（触发检测后自动刷新）";
            _lblLastResult = lbl;
            var lblTitle = MakeFieldLabel();
            lblTitle.Text = "最近检测结果";
            lblTitle.Font = new Font("宋体", 12F, FontStyle.Bold);
            stateGrid.Controls.Add(lblTitle, 0, 0);
            stateGrid.Controls.Add(lbl, 1, 0);
            grpState.Controls.Add(stateGrid);
            root.Controls.Add(grpState, 0, 1);

            panel1.Controls.Add(root);
            panel1.ResumeLayout();
        }

        private static GroupBox CreateGroup(string title)
        {
            return new GroupBox
            {
                Dock = DockStyle.Fill,
                Text = title,
                Font = new Font("宋体", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 55, 100)
            };
        }

        private void AddConfigRow(TableLayoutPanel grid, string fieldName, out Label valueLabel, int row)
        {
            var lbl = MakeFieldLabel();
            lbl.Text = fieldName;
            var value = MakeValueLabel();
            valueLabel = value;
            grid.Controls.Add(lbl, 0, row);
            grid.Controls.Add(value, 1, row);
        }

        private static Label MakeFieldLabel()
        {
            return new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false,
                Font = new Font("宋体", 12F, FontStyle.Regular),
                ForeColor = Color.FromArgb(48, 48, 48)
            };
        }

        private static Label MakeValueLabel()
        {
            return new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false,
                Font = new Font("宋体", 12F, FontStyle.Regular),
                ForeColor = Color.FromArgb(0, 55, 100),
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        /// <summary>
        /// 从 JvsWorkspaceMapping 刷新配置摘要 + 引擎状态。
        /// </summary>
        private void RefreshConfigSummary()
        {
            var m = JvsVisionBridge.Instance.Mapping;
            _lblSchemePath.Text = m?.WorkspacePath ?? "（未配置）";
            _lblCaptureTool.Text = m?.CaptureToolPath ?? "（未配置）";
            _lblToolBlock.Text = m?.ToolBlock ?? "（未配置）";
            _lblInputPin.Text = m?.InputPin ?? "（未配置）";
            _lblOutputPin.Text = m?.OutputPin ?? "（未配置）";
            _lblSeparators.Text = $"点=“{m?.PointSeparator}” 字段=“{m?.FieldSeparator}”";
            _lblFieldOrder.Text = m?.FieldOrder ?? "（未配置）";
            _lblEngineState.Text = JvsRuntime.IsInitialized ? "已初始化（进程内引擎就绪）" : "未初始化";
            _lblEngineState.ForeColor = JvsRuntime.IsInitialized ? Color.Green : Color.OrangeRed;
        }

        /// <summary>
        /// JVS 检测结果事件回调（可能在工作线程，切回 UI 线程刷新）。
        /// </summary>
        private void OnJvsLastResultChanged(string resultText)
        {
            if (IsDisposed)
                return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(OnJvsLastResultChanged), resultText);
                return;
            }
            if (_lblLastResult != null)
                _lblLastResult.Text = resultText;
        }

        /// <summary>
        /// 打开视觉设置页（JVS 取像/加载程序等视觉参数统一在此编辑保存）
        /// </summary>
        private void uiButton_JvsSetting_Click(object sender, EventArgs e)
        {
            try
            {
                using (var frm = new Frm_JvsSetting())
                {
                    frm.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                Log.Lib.RLog.SaveError(ex);
                MessageBox.Show(this, "打开 JVS 设置页失败：" + ex.Message,
                    "JVS 设置", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
