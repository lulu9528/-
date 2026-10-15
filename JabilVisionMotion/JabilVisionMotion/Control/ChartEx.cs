using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace JabilVisionMotion.Control
{
    /// <summary>
    /// 自定义图表控件，用于显示测力曲线，支持正向和反向曲线展示
    /// 提供数据刷新、图表配置等功能，支持线程安全的数据更新
    /// </summary>
    public partial class ChartEx : UserControl
    {
        #region 常量定义
        // 图表默认字体设置
        private const string DEFAULT_FONT_FAMILY = "微软雅黑";
        private const float SERIES_LABEL_FONT_SIZE = 6f;
        private const float CHART_TITLE_FONT_SIZE = 12f;

        // 图表系列索引常量（避免硬编码魔术数字）
        private const int POSITIVE_CURVE_SERIES_INDEX = 0;   // 正向曲线系列索引
        private const int POSITIVE_POINTS_SERIES_INDEX = 1;  // 正向曲线关键点系列索引
        private const int NEGATIVE_CURVE_SERIES_INDEX = 2;   // 反向曲线系列索引
        // private const int NEGATIVE_POINTS_SERIES_INDEX = 3;  // 反向曲线关键点系列索引（当前注释未使用）

        // 图表显示配置常量
        private const int MARKER_SIZE = 10;                  // 标记点大小
        private const int Y_AXIS_INTERVAL = 100;             // Y轴默认间隔
        private const int X_AXIS_VIEW_SIZE = 20;             // X轴视图大小
        private const int LAST_POINTS_COUNT = 4;             // 显示的最后数据点数量
        #endregion

        #region 字段
        // 参考线对象（可用于在图表上显示阈值线等）
        private StripLine _upperLimitLine = new StripLine();
        private StripLine _lowerLimitLine = new StripLine();
        #endregion

        #region 构造函数
        /// <summary>
        /// 初始化ChartEx控件实例
        /// 配置图表的初始样式和默认系列
        /// </summary>
        public ChartEx()
        {
            InitializeComponent();
            // 初始化图表默认配置
            InitializeDefaultChartSettings();
        }
        #endregion

        #region 初始化方法
        /// <summary>
        /// 初始化图表的默认设置
        /// 设置默认系列、样式、坐标轴等基础属性
        /// </summary>
        private void InitializeDefaultChartSettings()
        {
            // 清除默认系列
            chart_Ex.Series.Clear();

            // 添加默认曲线系列
            ChartHelper.AddSeries(
                chart_Ex,
                "曲线",
                SeriesChartType.Line,
                Color.Red,
                Color.Red);

            // 设置图表整体样式
            ChartHelper.SetStyle(chart_Ex, Color.Transparent, Color.White);

            // 设置图例位置和样式
            ChartHelper.SetLegend(
                chart_Ex,
                Docking.Top,
                StringAlignment.Center,
                Color.Transparent,
                Color.White);

            // 设置坐标轴标题和样式
            ChartHelper.SetXY(
                chart_Ex,
                "",
                "扭力(N*mm)",
                StringAlignment.Far,
                Color.White,
                Color.White,
                AxisArrowStyle.None,
                2,
                20);

            // 绑定 tooltip 事件，显示数据点详细信息
            chart_Ex.GetToolTipText += chart_Ex_GetToolTipText;
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 清除图表所有数据和标题
        /// 重置图表到初始状态
        /// </summary>
        public void ChartExClear()
        {
            chart_Ex.Series.Clear();
            ChartHelper.ClearTitle(chart_Ex);
        }

        /// <summary>
        /// 配置图表的显示参数和系列
        /// 设置坐标轴范围、网格样式、曲线样式等
        /// </summary>
        /// <param name="strSeries">系列基础名称</param>
        /// <param name="fMaxValue">Y轴最大值</param>
        /// <param name="fMinValue">Y轴最小值</param>
        /// <param name="fIntervalOffsetHigh">高阈值偏移</param>
        /// <param name="fIntervalOffsetLow">低阈值偏移</param>
        /// <param name="fStripWidth">参考线宽度</param>
        public void ChartExSet(
            string strSeries,
            double fMaxValue,
            double fMinValue,
            double fIntervalOffsetHigh,
            double fIntervalOffsetLow,
            double fStripWidth)
        {
            // 清除现有系列
            chart_Ex.Series.Clear();

            // 添加正向曲线系列（快速线类型，提高绘制性能）
            ChartHelper.AddSeries(
                chart_Ex,
                $"{strSeries}正向曲线",
                SeriesChartType.FastLine,
                Color.DarkOrange,
                Color.Red);

            // 添加正向曲线关键点系列（用于标记最后几个数据点）
            ChartHelper.AddSeries(
                chart_Ex,
                "  ",
                SeriesChartType.Point,
                Color.White,
                Color.White);

            // 添加反向曲线系列
            ChartHelper.AddSeries(
                chart_Ex,
                $"{strSeries}反向曲线",
                SeriesChartType.FastLine,
                Color.Purple,
                Color.Red);

            // 配置图表标题，显示测力范围
            ChartHelper.SetTitle(
                chart_Ex,
                $"测力曲线图  {fIntervalOffsetLow} ~ {fIntervalOffsetHigh}N*mm",
                new Font(DEFAULT_FONT_FAMILY, CHART_TITLE_FONT_SIZE),
                Docking.Bottom,
                Color.DarkGreen);

            // 设置图表整体样式
            ChartHelper.SetStyle(chart_Ex, Color.Transparent, Color.White);

            // 配置图例
            ChartHelper.SetLegend(
                chart_Ex,
                Docking.Top,
                StringAlignment.Center,
                Color.Transparent,
                Color.White);

            // 配置坐标轴
            ChartHelper.SetXY(
                chart_Ex,
                "",
                "扭力(N)",
                StringAlignment.Far,
                Color.White,
                Color.White,
                AxisArrowStyle.None,
                2,
                20);

            // 配置网格线
            ChartHelper.SetMajorGrid(chart_Ex, Color.Gray, 20, 2);

            // 进一步细化Y轴网格样式
            chart_Ex.ChartAreas[0].AxisY.MajorGrid.Enabled = true;
            chart_Ex.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.Yellow;
            chart_Ex.ChartAreas[0].AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dash;

            // 设置Y轴范围和交点
            chart_Ex.ChartAreas[0].AxisY.Maximum = fMaxValue;
            chart_Ex.ChartAreas[0].AxisY.Minimum = fMinValue;
            chart_Ex.ChartAreas[0].AxisY.Crossing = 0;  // 设置XY轴在0点交叉
            chart_Ex.ChartAreas[0].AxisY.Interval = Y_AXIS_INTERVAL;

            // 配置X轴样式
            chart_Ex.ChartAreas[0].AxisX.MajorTickMark.Enabled = false;  // 隐藏刻度线
            chart_Ex.ChartAreas[0].AxisX.Interval = 2.5;

            // 配置X轴滚动和视图
            chart_Ex.ChartAreas[0].CursorX.IsUserEnabled = true;  // 允许用户启用光标
            chart_Ex.ChartAreas[0].CursorX.IsUserSelectionEnabled = true;  // 允许用户选择区域
            chart_Ex.ChartAreas[0].AxisX.ScrollBar.Enabled = false;  // 禁用滚动条
            chart_Ex.ChartAreas[0].AxisX.ScaleView.SmallScrollSizeType = DateTimeIntervalType.Number;
            chart_Ex.ChartAreas[0].AxisX.ScaleView.SmallScrollMinSize = 1;
            chart_Ex.ChartAreas[0].AxisX.ScaleView.SizeType = DateTimeIntervalType.Number;
            chart_Ex.ChartAreas[0].AxisX.ScaleView.Size = X_AXIS_VIEW_SIZE;

            // 配置正向曲线样式
            ConfigureSeriesStyle(
                POSITIVE_CURVE_SERIES_INDEX,
                true,
                Color.Black,
                MarkerStyle.None);

            // 配置正向曲线关键点样式
            ConfigureSeriesStyle(
                POSITIVE_POINTS_SERIES_INDEX,
                true,
                Color.White,
                MarkerStyle.Cross,
                MARKER_SIZE);

            // 配置反向曲线样式
            ConfigureSeriesStyle(
                NEGATIVE_CURVE_SERIES_INDEX,
                true,
                Color.Black,
                MarkerStyle.None);

            //// 配置反向曲线关键点样式（当前注释未使用）
            //ConfigureSeriesStyle(
            //    NEGATIVE_POINTS_SERIES_INDEX, 
            //    true, 
            //    Color.White, 
            //    MarkerStyle.Cross, 
            //    MARKER_SIZE);
        }

        /// <summary>
        /// 刷新正向曲线数据
        /// 线程安全的方法，支持跨线程调用
        /// </summary>
        /// <param name="x">X轴数据列表</param>
        /// <param name="y">Y轴数据列表</param>
        /// <param name="type">数据类型（预留参数）</param>
        public void RefreshChart_正向(List<string> x, List<string> y, string type)
        {
            // 检查是否需要跨线程调用
            if (this.chart_Ex.InvokeRequired)
            {
                // 使用委托进行跨线程调用
                RefreshChartDelegate stcb = new RefreshChartDelegate(RefreshChart_正向);
                this.Invoke(stcb, new object[] { x, y, type });
            }
            else
            {
                // 清除现有数据点
                chart_Ex.Series[POSITIVE_CURVE_SERIES_INDEX].Points.Clear();
                chart_Ex.Series[POSITIVE_POINTS_SERIES_INDEX].Points.Clear();

                // 添加所有数据点到正向曲线
                for (int i = 0; i < x.Count; i++)
                {
                    chart_Ex.Series[POSITIVE_CURVE_SERIES_INDEX]
                           .Points.AddXY(Convert.ToDouble(x[i]), Convert.ToDouble(y[i]));
                }

                // 添加最后N个点到关键点系列（用于突出显示）
                int startIndex = Math.Max(0, x.Count - LAST_POINTS_COUNT);
                for (int i = startIndex; i < x.Count; i++)
                {
                    chart_Ex.Series[POSITIVE_POINTS_SERIES_INDEX]
                           .Points.AddXY(Convert.ToDouble(x[i]), Convert.ToDouble(y[i]));
                }
            }
        }

        /// <summary>
        /// 刷新反向曲线数据
        /// 线程安全的方法，支持跨线程调用
        /// </summary>
        /// <param name="x">X轴数据列表</param>
        /// <param name="y">Y轴数据列表</param>
        /// <param name="type">数据类型（预留参数）</param>
        public void RefreshChart_反向(List<string> x, List<string> y, string type)
        {
            // 检查是否需要跨线程调用
            if (this.chart_Ex.InvokeRequired)
            {
                // 使用委托进行跨线程调用
                RefreshChartDelegate stcb = new RefreshChartDelegate(RefreshChart_反向);
                this.Invoke(stcb, new object[] { x, y, type });
            }
            else
            {
                // 清除现有数据点
                chart_Ex.Series[NEGATIVE_CURVE_SERIES_INDEX].Points.Clear();
                // chart_Ex.Series[NEGATIVE_POINTS_SERIES_INDEX].Points.Clear();  // 当前注释未使用

                // 添加所有数据点到反向曲线
                for (int i = 0; i < x.Count; i++)
                {
                    chart_Ex.Series[NEGATIVE_CURVE_SERIES_INDEX]
                           .Points.AddXY(Convert.ToDouble(x[i]), Convert.ToDouble(y[i]));
                }

                //// 添加最后N个点到关键点系列（当前注释未使用）
                //int startIndex = Math.Max(0, x.Count - LAST_POINTS_COUNT);
                //for (int i = startIndex; i < x.Count; i++)
                //{
                //    chart_Ex.Series[NEGATIVE_POINTS_SERIES_INDEX]
                //           .Points.AddXY(Convert.ToDouble(x[i]), Convert.ToDouble(y[i]));
                //}
            }
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 配置图表系列的样式
        /// 统一设置数据点标签、字体、标记样式等
        /// </summary>
        /// <param name="seriesIndex">系列索引</param>
        /// <param name="showValue">是否显示数据点值</param>
        /// <param name="labelColor">标签颜色</param>
        /// <param name="markerStyle">标记样式</param>
        /// <param name="markerSize">标记大小（默认0）</param>
        private void ConfigureSeriesStyle(
            int seriesIndex,
            bool showValue,
            Color labelColor,
            MarkerStyle markerStyle,
            int markerSize = 0)
        {
            var series = chart_Ex.Series[seriesIndex];
            series.IsValueShownAsLabel = showValue;
            series.Font = new Font(DEFAULT_FONT_FAMILY, SERIES_LABEL_FONT_SIZE);
            series.LabelForeColor = labelColor;
            series.MarkerStyle = markerStyle;

            if (markerSize > 0)
            {
                series.MarkerSize = markerSize;
            }
        }
        #endregion

        #region 事件处理
        /// <summary>
        /// 图表Tooltip事件处理方法
        /// 显示数据点的角度和力值信息
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数，包含Tooltip信息</param>
        public void chart_Ex_GetToolTipText(object sender, ToolTipEventArgs e)
        {
            // 仅处理数据点的Tooltip
            if (e.HitTestResult.ChartElementType == ChartElementType.DataPoint)
            {
                int pointIndex = e.HitTestResult.PointIndex;
                DataPoint dataPoint = e.HitTestResult.Series.Points[pointIndex];

                // 计算并格式化显示值
                double angle = dataPoint.XValue;
                double forceValue = dataPoint.YValues[0];

                // 对负值力值进行角度调整（业务逻辑）
                if (forceValue < 0)
                {
                    angle -= 100;
                }

                // 设置Tooltip文本
                e.Text = $"角度: {angle:F2}\n力值: {forceValue:F2}";
            }
        }
        #endregion

        #region 委托定义
        /// <summary>
        /// 刷新图表的委托
        /// 用于跨线程安全地更新图表数据
        /// </summary>
        /// <param name="x">X轴数据列表</param>
        /// <param name="y">Y轴数据列表</param>
        /// <param name="type">数据类型</param>
        public delegate void RefreshChartDelegate(List<string> x, List<string> y, string type);
        #endregion
    }

    /// <summary>
    /// 图表辅助工具类
    /// 提供一系列静态方法用于配置图表的样式、系列、标题等
    /// </summary>
    public class ChartHelper
    {
        /// <summary>
        /// 向图表添加数据系列
        /// </summary>
        /// <param name="chart">目标图表对象</param>
        /// <param name="seriesName">系列名称</param>
        /// <param name="chartType">图表类型</param>
        /// <param name="color">系列颜色</param>
        /// <param name="markColor">标记点颜色</param>
        /// <param name="showValue">是否显示数据点值（默认false）</param>
        public static void AddSeries(
            Chart chart,
            string seriesName,
            SeriesChartType chartType,
            Color color,
            Color markColor,
            bool showValue = false)
        {
            chart.Series.Add(seriesName);
            var series = chart.Series[seriesName];

            // 设置系列基本属性
            series.ChartType = chartType;
            series.Color = color;

            // 配置数据点显示
            if (showValue)
            {
                series.IsValueShownAsLabel = true;
                series.MarkerStyle = MarkerStyle.Circle;
                series.MarkerColor = markColor;
                series.LabelForeColor = color;
                series.LabelAngle = -90;  // 标签旋转角度
            }
        }

        /// <summary>
        /// 为图表设置标题
        /// </summary>
        /// <param name="chart">目标图表对象</param>
        /// <param name="chartName">标题文本</param>
        /// <param name="font">标题字体</param>
        /// <param name="docking">标题停靠位置</param>
        /// <param name="foreColor">标题文字颜色</param>
        public static void SetTitle(
            Chart chart,
            string chartName,
            Font font,
            Docking docking,
            Color foreColor)
        {
            chart.Titles.Add(chartName);
            var title = chart.Titles[0];

            title.Font = font;
            title.Docking = docking;
            title.ForeColor = foreColor;
        }

        /// <summary>
        /// 清除图表的所有标题
        /// </summary>
        /// <param name="chart">目标图表对象</param>
        public static void ClearTitle(Chart chart)
        {
            chart.Titles.Clear();
        }

        /// <summary>
        /// 设置图表的整体样式
        /// </summary>
        /// <param name="chart">目标图表对象</param>
        /// <param name="backColor">背景颜色</param>
        /// <param name="foreColor">前景文字颜色</param>
        public static void SetStyle(Chart chart, Color backColor, Color foreColor)
        {
            chart.BackColor = backColor;
            chart.ChartAreas[0].BackColor = backColor;
            chart.ForeColor = foreColor;
        }

        /// <summary>
        /// 设置图表的图例样式
        /// </summary>
        /// <param name="chart">目标图表对象</param>
        /// <param name="docking">图例停靠位置</param>
        /// <param name="align">图例对齐方式</param>
        /// <param name="backColor">图例背景颜色</param>
        /// <param name="foreColor">图例文字颜色</param>
        public static void SetLegend(
            Chart chart,
            Docking docking,
            StringAlignment align,
            Color backColor,
            Color foreColor)
        {
            var legend = chart.Legends[0];

            legend.Docking = docking;
            legend.Alignment = align;
            legend.BackColor = backColor;
            legend.ForeColor = foreColor;
        }

        /// <summary>
        /// 设置图表的XY轴样式和属性
        /// </summary>
        /// <param name="chart">目标图表对象</param>
        /// <param name="xTitle">X轴标题</param>
        /// <param name="yTitle">Y轴标题</param>
        /// <param name="align">坐标轴标题对齐方式</param>
        /// <param name="foreColor">坐标轴文字颜色</param>
        /// <param name="lineColor">坐标轴线条颜色</param>
        /// <param name="arrowStyle">坐标轴箭头样式</param>
        /// <param name="xInterval">X轴刻度间隔</param>
        /// <param name="yInterval">Y轴刻度间隔</param>
        public static void SetXY(
            Chart chart,
            string xTitle,
            string yTitle,
            StringAlignment align,
            Color foreColor,
            Color lineColor,
            AxisArrowStyle arrowStyle,
            double xInterval,
            double yInterval)
        {
            var axisX = chart.ChartAreas[0].AxisX;
            var axisY = chart.ChartAreas[0].AxisY;

            // 设置坐标轴标题
            axisX.Title = xTitle;
            axisY.Title = yTitle;

            // 设置标题对齐方式
            axisX.TitleAlignment = align;
            axisY.TitleAlignment = align;

            // 设置文字颜色
            axisX.TitleForeColor = foreColor;
            axisY.TitleForeColor = foreColor;
            axisX.LabelStyle = new LabelStyle() { ForeColor = foreColor };
            axisY.LabelStyle = new LabelStyle() { ForeColor = foreColor };

            // 设置坐标轴线条样式
            axisX.LineColor = lineColor;
            axisY.LineColor = lineColor;
            axisX.ArrowStyle = arrowStyle;
            axisY.ArrowStyle = arrowStyle;

            // 设置刻度间隔
            axisX.Interval = xInterval;
            axisY.Interval = yInterval;
        }

        /// <summary>
        /// 设置图表的网格线样式
        /// </summary>
        /// <param name="chart">目标图表对象</param>
        /// <param name="lineColor">网格线颜色</param>
        /// <param name="xInterval">X轴网格间隔</param>
        /// <param name="yInterval">Y轴网格间隔</param>
        public static void SetMajorGrid(
            Chart chart,
            Color lineColor,
            double xInterval,
            double yInterval)
        {
            var gridX = chart.ChartAreas[0].AxisX.MajorGrid;
            var gridY = chart.ChartAreas[0].AxisY.MajorGrid;

            gridX.LineColor = lineColor;
            gridY.LineColor = lineColor;
            gridX.Interval = xInterval;
            gridY.Interval = yInterval;
        }
    }
}
