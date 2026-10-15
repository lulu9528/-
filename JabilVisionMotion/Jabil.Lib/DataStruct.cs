using Cognex.VisionPro;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Lib
{
    /// <summary>
    /// 坐标数据结构，用于精确存储和传递二维空间中的位置信息及旋转角度
    /// 适用于机器视觉、运动控制等需要精确定位的场景
    /// </summary>
    public struct CoordinateData
    {
        /// <summary>
        /// X轴坐标值（单位由具体应用场景决定，如毫米、像素等）
        /// </summary>
        public double X;

        /// <summary>
        /// Y轴坐标值（单位与X轴保持一致）
        /// </summary>
        public double Y;

        /// <summary>
        /// 旋转角度（单位需根据具体应用场景确定，通常为弧度或度）
        /// 表示绕Z轴旋转的角度，用于描述物体的朝向
        /// </summary>
        public double Rotation;

        /// <summary>
        /// 初始化坐标数据结构的构造函数
        /// </summary>
        /// <param name="x">X轴坐标值</param>
        /// <param name="y">Y轴坐标值</param>
        /// <param name="rotation">旋转角度值</param>
        public CoordinateData(double x, double y, double rotation)
        {
            X = x;
            Y = y;
            Rotation = rotation;
        }

        /// <summary>
        /// 重写ToString方法，提供格式化的坐标信息字符串
        /// 便于调试输出、日志记录和用户界面显示
        /// </summary>
        /// <returns>包含X、Y坐标和旋转角度的格式化字符串（保留两位小数）</returns>
        public override string ToString()
        {
            return $"X: {X:F2}, Y: {Y:F2}, Rotation: {Rotation:F2}";
        }
    }

    /// <summary>
    /// 工位信息类，集中管理工业设备工位的各种运行状态、控制参数和流程信息
    /// 作为工位状态数据的容器，提供统一的访问接口
    /// </summary>
    public class JStationInfo
    {
        #region 流程控制属性
        /// <summary>
        /// 复位流程执行触发标志
        /// true：请求执行复位流程；false：不执行
        /// 通常由用户操作或系统自动触发
        /// </summary>
        public bool ExecuteResetProcess { get; set; }

        /// <summary>
        /// 复位流程的当前运行状态
        /// 取值范围：NotRunning（未运行）、Running（运行中）、Completed（已完成）、Error（出错）
        /// </summary>
        public ProcessStatus ResetProcessStatus { get; set; } = ProcessStatus.NotRunning;

        /// <summary>
        /// 九点标定流程执行触发标志
        /// true：请求执行九点标定；false：不执行
        /// 九点标定用于建立相机坐标系与机械坐标系的转换关系
        /// </summary>
        public bool ExecuteNinePointCalibration { get; set; }

        /// <summary>
        /// 九点标定流程的当前运行状态
        /// 取值范围：NotRunning（未运行）、Running（运行中）、Completed（已完成）、Error（出错）
        /// </summary>
        public ProcessStatus NinePointCalibrationStatus { get; set; } = ProcessStatus.NotRunning;

        /// <summary>
        /// 自动对针流程执行触发标志
        /// true：请求执行自动对针；false：不执行
        /// 自动对针用于校准点胶针头、焊接头等执行部件的位置精度
        /// </summary>
        public bool ExecuteAutoNeedleAlignment { get; set; }

        /// <summary>
        /// 自动对针流程的当前运行状态
        /// 取值范围：NotRunning（未运行）、Running（运行中）、Completed（已完成）、Error（出错）
        /// </summary>
        public ProcessStatus AutoNeedleAlignmentStatus { get; set; } = ProcessStatus.NotRunning;

        /// <summary>
        /// 自动擦胶流程执行触发标志
        /// true：请求执行自动擦胶；false：不执行
        /// 自动擦胶用于清洁点胶针头残留的胶水，保证点胶质量
        /// </summary>
        public bool ExecuteAutoGlueCleaning { get; set; }

        /// <summary>
        /// 自动擦胶流程的当前运行状态
        /// 取值范围：NotRunning（未运行）、Running（运行中）、Completed（已完成）、Error（出错）
        /// </summary>
        public ProcessStatus AutoGlueCleaningStatus { get; set; } = ProcessStatus.NotRunning;

        /// <summary>
        /// 点胶测试流程执行触发标志
        /// true：请求执行点胶测试；false：不执行
        /// 点胶测试用于验证点胶参数设置是否合理，通常在正式生产前执行
        /// </summary>
        public bool ExecuteGlueTestProcess { get; set; }

        /// <summary>
        /// 点胶测试流程的当前运行状态
        /// 取值范围：NotRunning（未运行）、Running（运行中）、Completed（已完成）、Error（出错）
        /// </summary>
        public ProcessStatus GlueTestProcessStatus { get; set; } = ProcessStatus.NotRunning;
        #endregion

        #region 运动控制属性
        private int _runningSpeedRatio = 100; // 默认以100%速度运行

        /// <summary>
        /// 运行速度比例（百分比）
        /// 取值范围被限制在1-100之间，用于调节设备运动速度
        /// 100表示全速运行，50表示半速运行
        /// </summary>
        public int RunningSpeedRatio
        {
            get => _runningSpeedRatio;
            set
            {
                // 确保速度比例在有效范围内，防止设置无效值导致设备异常
                if (value < 1)
                    _runningSpeedRatio = 1;       // 低于最小值时强制设为1
                else if (value > 100)
                    _runningSpeedRatio = 100;     // 高于最大值时强制设为100
                else
                    _runningSpeedRatio = value;   // 在有效范围内时直接使用输入值
            }
        }

        /// <summary>
        /// X轴实时位置反馈值（单位由设备决定，通常为毫米）
        /// 用于实时显示和监控X轴的当前位置
        /// </summary>
        public double XAxisPosition { get; set; }

        /// <summary>
        /// Y轴实时位置反馈值（单位与X轴保持一致）
        /// 用于实时显示和监控Y轴的当前位置
        /// </summary>
        public double YAxisPosition { get; set; }

        /// <summary>
        /// Z轴实时位置反馈值（单位与X、Y轴保持一致）
        /// 用于实时显示和监控Z轴（通常为垂直方向）的当前位置
        /// </summary>
        public double ZAxisPosition { get; set; }

        /// <summary>
        /// 激光头实时位置反馈值（单位由设备决定）
        /// 专门用于监控激光头组件的当前位置，适用于带激光功能的工位
        /// </summary>
        public double LaserHeadPosition { get; set; }
        #endregion

        /// <summary>
        /// 初始化工位信息实例，设置所有流程状态的默认值
        /// 确保工位在创建时处于已知的初始状态
        /// </summary>
        public JStationInfo()
        {
            // 初始化所有流程状态为未运行（NotRunning）
            ResetProcessStatus = ProcessStatus.NotRunning;
            NinePointCalibrationStatus = ProcessStatus.NotRunning;
            AutoNeedleAlignmentStatus = ProcessStatus.NotRunning;
            AutoGlueCleaningStatus = ProcessStatus.NotRunning;
            GlueTestProcessStatus = ProcessStatus.NotRunning;
        }

        /// <summary>
        /// 重置所有流程的执行状态和触发标志
        /// 将所有流程恢复到初始状态，通常在工位复位或紧急停止时调用
        /// </summary>
        public void ResetAllProcessStatus()
        {
            // 清除所有流程的执行请求
            ExecuteResetProcess = false;
            ExecuteNinePointCalibration = false;
            ExecuteAutoNeedleAlignment = false;
            ExecuteAutoGlueCleaning = false;
            ExecuteGlueTestProcess = false;

            // 将所有流程状态重置为未运行
            ResetProcessStatus = ProcessStatus.NotRunning;
            NinePointCalibrationStatus = ProcessStatus.NotRunning;
            AutoNeedleAlignmentStatus = ProcessStatus.NotRunning;
            AutoGlueCleaningStatus = ProcessStatus.NotRunning;
            GlueTestProcessStatus = ProcessStatus.NotRunning;
        }
    }
}