using Cognex.VisionPro;
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
