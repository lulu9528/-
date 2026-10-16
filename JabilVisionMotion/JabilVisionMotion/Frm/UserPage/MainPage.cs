using Jabil.Lib;
using Jabil.Module.Log.Lib;
using Sunny.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using JabilVisionMotion.Lib;
using System.Threading;
using Jabil.Module.ProductData;
using Jabil.Module.ProductData.Lib;
using JabilVisionMotion.Edit;
using Jabil.Lib.Parameter;
using System.Reflection;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net;
using JabilVisionMotion.Edit.Process;

namespace JabilVisionMotion.Frm.UserPage
{
    /// <summary>
    /// 应用程序主页面类
    /// 继承自Sunny.UI的UIPage控件，作为系统的主要交互界面
    /// 包含系统核心功能模块的入口和主要操作区域
    /// </summary>
    public partial class MainPage : UIPage
    {
        /// <summary>
        /// 初始化MainPage的新实例
        /// 调用InitializeComponent()方法加载设计器生成的控件和布局
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
            InitJvsDisplayGrid(); // JVS 检测结果图 6 宫格显示区（替代原 Cognex 显示控件）
        }

        /// <summary>
        /// JVS 检测结果图显示格字典（标题 → PictureBox）
        /// 键：飞拍相机/左上相机/左下相机/右上相机/右下相机/当前检测/结果图保存
        /// </summary>
        private readonly Dictionary<string, PictureBox> _jvsDisplayBoxes =
            new Dictionary<string, PictureBox>(StringComparer.Ordinal);

        /// <summary>
        /// 构建 JVS 检测结果图显示区（6 宫格 + 结果图保存页大图）
        /// 布局沿用原 VisionPro 版视觉页骨架（tableLayoutPanel1 3列×4行）：
        /// 行1 = 飞拍/左上/左下；行3 = 右上/右下/当前检测
        /// </summary>
        private void InitJvsDisplayGrid()
        {
            try
            {
                // panel1（Epson/飞拍测试按钮）移出宫格，固定到视觉页右上角，空出 (2,3) 给"当前检测"格
                if (tableLayoutPanel1.Controls.Contains(panel1))
                {
                    tableLayoutPanel1.Controls.Remove(panel1);
                    tabPage2.Controls.Add(panel1);
                    panel1.Location = new Point(tabPage2.ClientSize.Width - panel1.Width - 8, 8);
                    panel1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                }

                // 6 宫格
                AddJvsCell(0, 1, "飞拍相机");
                AddJvsCell(1, 1, "左上相机");
                AddJvsCell(2, 1, "左下相机");
                AddJvsCell(0, 3, "右上相机");
                AddJvsCell(1, 3, "右下相机");
                AddJvsCell(2, 3, "当前检测");

                // 结果图保存页（tabPage1）：1 个大显示格，与"当前检测"同步显示最新检测结果图
                var bigBox = CreateJvsPictureBox();
                bigBox.Dock = DockStyle.Fill;
                tabPage1.Controls.Add(bigBox);
                _jvsDisplayBoxes["结果图保存"] = bigBox;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 在视觉页宫格 (col,row) 添加一个显示格（标题栏 + 图像区）。
        /// </summary>
        private void AddJvsCell(int col, int row, string title)
        {
            var cell = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(30, 40, 60) };
            var label = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 24,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font("宋体", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 55, 100)
            };
            var box = CreateJvsPictureBox();
            // Dock 布局顺序：先 Top 标题后 Fill 图像（先添加的先生成 Dock 布局）
            cell.Controls.Add(label);
            cell.Controls.Add(box);
            tableLayoutPanel1.Controls.Add(cell, col, row);
            _jvsDisplayBoxes[title] = box;
        }

        private static PictureBox CreateJvsPictureBox()
        {
            return new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(20, 28, 40)
            };
        }

        /// <summary>
        /// 刷新 JVS 检测结果图（每次检测完成后由 VisionProcess 调用）。
        /// 相机归属映射：绑定取像工具名称（如"左上相机"）→ 对应宫格；
        /// 其它检测（复检等）→ "当前检测"格。结果图保存页大图同步刷新。
        /// 线程安全：VisionProcess 在工作线程调用，自动切换到 UI 线程。
        /// Bitmap 所有权：本方法接管并负责释放旧图；传入后调用方不要再 Dispose。
        /// </summary>
        /// <param name="cameraKey">取像工具名称或检测名称（含 飞拍/左上/右上/左下/右下 关键字）</param>
        /// <param name="bitmap">JVS 检测结果图（image_with_shapes 转换）</param>
        public void RefreshJvsDisplay(string cameraKey, Bitmap bitmap)
        {
            if (bitmap == null)
                return;
            if (IsDisposed || _jvsDisplayBoxes.Count == 0)
            {
                bitmap.Dispose();
                return;
            }
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, Bitmap>(RefreshJvsDisplay), cameraKey, bitmap);
                return;
            }

            PictureBox mainBox = ResolveDisplayBox(cameraKey);
            if (mainBox == null)
            {
                bitmap.Dispose();
                return;
            }
            var old = mainBox.Image;
            mainBox.Image = bitmap;
            old?.Dispose();

            // 结果图保存页大图：独立拷贝显示（避免与宫格共享引用引发 Dispose 冲突）
            if (_jvsDisplayBoxes.TryGetValue("结果图保存", out var big) && big != mainBox)
            {
                var oldBig = big.Image;
                big.Image = (Bitmap)bitmap.Clone();
                oldBig?.Dispose();
            }
        }

        /// <summary>
        /// 按取像工具/检测名称解析对应显示格。
        /// </summary>
        private PictureBox ResolveDisplayBox(string cameraKey)
        {
            if (string.IsNullOrWhiteSpace(cameraKey))
                return null;
            if (_jvsDisplayBoxes.TryGetValue(cameraKey, out var direct))
                return direct;
            if (cameraKey.Contains("飞拍"))
                return GetBox("飞拍相机");
            if (cameraKey.Contains("左上"))
                return GetBox("左上相机");
            if (cameraKey.Contains("右上"))
                return GetBox("右上相机");
            if (cameraKey.Contains("左下"))
                return GetBox("左下相机");
            if (cameraKey.Contains("右下"))
                return GetBox("右下相机");
            return GetBox("当前检测");
        }

        private PictureBox GetBox(string key)
        {
            return _jvsDisplayBoxes.TryGetValue(key, out var box) ? box : null;
        }

        /// <summary>
        /// 窗体对象实例
        /// 用于存储MainPage的单例实例，确保全局只有一个主页面实例
        /// </summary>
        private static MainPage _instance;

        /// <summary>
        /// 获取MainPage的单例实例属性
        /// 提供全局访问点，确保在应用程序生命周期内只存在一个主页面实例
        /// </summary>
        public static MainPage Instance
        {
            get
            {
                // 检查实例是否为空或已被释放，如果是则创建新实例
                // 这种实现方式确保了在实例被意外释放后能重新创建
                if (_instance == null || _instance.IsDisposed)
                    _instance = new MainPage();
                return _instance;
            }
        }

        private void uiRadioButton1_CheckedChanged(object sender, EventArgs e)
        {
           
            //Controller.Instance.StationInfo.治具选择 = 1;
        }

        private void uiRadioButton2_CheckedChanged(object sender, EventArgs e)
        {
           
            //Controller.Instance.StationInfo.治具选择 = 2;
        }

        private void uiRadioButton3_CheckedChanged(object sender, EventArgs e)
        {
           
            //Controller.Instance.StationInfo.治具选择 = 3;
        }

        //private void uiButton1_Click(object sender, EventArgs e)
        //{
        //    uiRadioButton1.Checked = false;
        //    uiRadioButton2.Checked = false;
        //    uiRadioButton3.Checked = false;

        //}



        //private void uiButton6_Click(object sender, EventArgs e)
        //{
        //    Project.Instance.weldParm.前焊接机压力 =Convert.ToInt32(uiTextBox1.Text);
        //    Project.Instance.weldParm.前焊接机振幅 = Convert.ToInt32(uiTextBox2.Text);
        //    Project.Instance.weldParm.后焊接机压力 = Convert.ToInt32(uiTextBox3.Text);
        //    Project.Instance.weldParm.后焊接机振幅 = Convert.ToInt32(uiTextBox4.Text);
        //    Project.Instance.SaveSystemConfig(Project.Instance.weldParm, ParameterPath.WeldMachine);
        //}

        //private void uiTextBox1_Load(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        Project.Instance.LoadSystemConfig(ref Project.Instance.weldParm, ParameterPath.WeldMachine);
        //        uiTextBox1.Text = Project.Instance.weldParm.前焊接机压力.ToString();
        //        uiTextBox2.Text = Project.Instance.weldParm.前焊接机振幅.ToString();
        //        uiTextBox3.Text = Project.Instance.weldParm.后焊接机压力.ToString();
        //        uiTextBox4.Text = Project.Instance.weldParm.后焊接机振幅.ToString();
               
        //    }
        //    catch (Exception ex)
        //    {
        //        RLog.SaveError(ex);
        //    }

        //}



    

        /// <summary>
        /// 追加一行 JVS 视觉检测数据到主界面"JVS 视觉数据"页
        /// （VisionProcess 可能在后台线程调用，自动切换到 UI 线程）
        /// </summary>
        /// <param name="result">检测结果（OK/ERR）</param>
        /// <param name="pointName">检测点位名称</param>
        /// <param name="pointCount">检测点数</param>
        /// <param name="elapsedMs">本次检测总耗时（毫秒）</param>
        /// <param name="roundTripMs">往返耗时（毫秒，进程内模式与总耗时一致）</param>
        /// <param name="jvsTotalMs">JVS 总耗时（毫秒）</param>
        /// <param name="processMs">检测处理耗时（毫秒）</param>
        /// <param name="dataText">检测数据文本（X,Y,R;…）</param>
        public void AddVisionDataRow(string result, string pointName, int pointCount, int elapsedMs, int roundTripMs, int jvsTotalMs, int processMs, string dataText)
        {
            if (dataGridViewVision == null || dataGridViewVision.IsDisposed)
                return;

            // 确保在 UI 线程操作（VisionProcess 可能在后台线程调用）
            if (dataGridViewVision.InvokeRequired)
            {
                dataGridViewVision.Invoke(new Action<string, string, int, int, int, int, int, string>(AddVisionDataRow),
                    result, pointName, pointCount, elapsedMs, roundTripMs, jvsTotalMs, processMs, dataText);
                return;
            }

            dataGridViewVision.SuspendLayout();
            try
            {
                // 按拍照顺序追加，不清除上一次结果
                dataGridViewVision.Rows.Add(
                    DateTime.Now.ToString("HH:mm:ss"),
                    result ?? "",
                    pointName ?? "",
                    pointCount,
                    elapsedMs,
                    roundTripMs,
                    jvsTotalMs,
                    processMs,
                    dataText ?? "");
                // 限制行数，避免无界增长（保留最近 500 条）
                while (dataGridViewVision.Rows.Count > 500)
                    dataGridViewVision.Rows.RemoveAt(0);
                // 自动滚动到最新一行
                if (dataGridViewVision.Rows.Count > 0)
                    dataGridViewVision.FirstDisplayedScrollingRowIndex = dataGridViewVision.Rows.Count - 1;
            }
            finally
            {
                dataGridViewVision.ResumeLayout();
            }
        }

        public void AddDataRow(DataGridView dgv, 获取最后焊接结果 data)
        {
            if (dgv == null)
                throw new ArgumentNullException(nameof(dgv), "DataGridView控件不能为空");

            if (data == null)
                throw new ArgumentNullException(nameof(data), "添加的数据行不能为空");

            // 确保在UI线程操作
            if (dgv.InvokeRequired)  //dgv.InvokeRequired
            {
                dgv.Invoke(new Action<DataGridView, 获取最后焊接结果>(AddDataRow), dgv, data);
                return;
            }

            // 保存当前选中行索引
            int selectedIndex = dgv.SelectedRows.Count > 0 ? dgv.SelectedRows[0].Index : -1;

            // 暂停绘制以提高性能
            dgv.SuspendLayout();

            try
            {
                // 添加新行
                int rowIndex = dgv.Rows.Add(data.ToString().Split(','));
                dgv.Rows[rowIndex].Cells[0].Style.BackColor = Color.White;
                dgv.Rows[rowIndex].Cells[1].Style.BackColor = Color.White;

                // 检查是否超过100行
                if (dgv.Rows.Count > 83)
                {
                    dgv.Rows.RemoveAt(3);
                }

                // 恢复选中状态
                if (selectedIndex >= 0 && selectedIndex < dgv.Rows.Count)
                {
                    dgv.Rows[selectedIndex].Selected = true;
                }
                // 自动滚动到最后一行
                else if (dgv.Rows.Count > 0)
                {
                    dgv.FirstDisplayedScrollingRowIndex = dgv.Rows.Count - 1;
                }
                dgv.ClearSelection();
            }
            finally
            {
                // 恢复绘制
                dgv.ResumeLayout();
            }
        }

        private void uiButton7_Click(object sender, EventArgs e)
        {
            
            //Controller.Instance.StationInfo.手动流程选择 = 1;
            //Controller.Instance.StationInfo.手动流程选择等待 = 1;
        }

        private void uiButton8_Click(object sender, EventArgs e)
        {
            //Controller.Instance.StationInfo.手动流程选择 = 2;
            //Controller.Instance.StationInfo.手动流程选择等待 = 1;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int a = Convert.ToInt16(textBox1.Text);
            Controller.Instance.StationInfo.Epson1下相机请求位置号 = a;
            new VisionProcess("Vision|定位|定位相机|右下定位|5", "隐藏").Run();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            int a = Convert.ToInt16(textBox1.Text);
            Controller.Instance.StationInfo.Epson1下相机请求位置号 = a;
            new VisionProcess("Vision|定位|定位相机|右下定位|5", "隐藏").Run();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            int a = Convert.ToInt16(textBox1.Text);
            Controller.Instance.StationInfo.Epson2下相机请求位置号 = a;
            new VisionProcess("Vision|定位|定位相机|左下定位|5", "隐藏").Run();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            new VisionProcess("Vision|定位|定位相机|飞拍定位|5", "隐藏").Run();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            new StateJudge("StateJudge|LOMA|飞拍开始", "隐藏").Run();
        }
    }
}
