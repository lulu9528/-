using Cognex.VisionPro;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Module.Vision.Lib
{
    /// <summary>
    /// 图像信息结构体
    /// 用于存储视觉检测过程中的图像数据、检测点位信息及检测结果
    /// 包含从图像采集到检测分析的全流程数据
    /// </summary>
    public struct ImageInfo
    {
        #region 检测点位基本信息
        /// <summary>
        /// 检测点位的名称
        /// 用于标识和区分不同的检测位置，通常具有业务含义
        /// 例如："螺丝孔位置检测"、"边缘平整度检测"
        /// </summary>
        public string 检测点位名称;

        /// <summary>
        /// 检测点位的类型分类
        /// 用于区分检测的方法或目的，例如："位置检测"、"缺陷检测"、"尺寸测量"
        /// </summary>
        public string 检测点位类型;

        /// <summary>
        /// 检测点位的详细定义描述
        /// 包含该检测点的具体检测要求、判定标准等业务说明
        /// </summary>
        public string 检测点位定义;

        /// <summary>
        /// 该检测点位所使用的治具名称
        /// 用于关联物理检测设备，便于追溯和维护
        /// </summary>
        public string 检测治具名称;

        /// <summary>
        /// 检测点位的唯一索引编号
        /// 用于程序中快速定位和访问特定检测点，通常从0或1开始递增
        /// </summary>
        public int 检测点位索引;
        #endregion

        #region 工具绑定信息
        /// <summary>
        /// 绑定的图像采集工具名称
        /// 关联负责为该检测点提供图像的取像工具
        /// </summary>
        public string 绑定取像工具名称;

        /// <summary>
        /// 绑定的图像采集工具索引
        /// 用于快速定位对应的取像工具实例
        /// </summary>
        public int 绑定取像工具索引;

        /// <summary>
        /// 绑定的检测分析工具名称
        /// 关联负责处理该检测点图像并进行分析的工具
        /// </summary>
        public string 绑定检测工具名称;

        /// <summary>
        /// 绑定的检测分析工具索引
        /// 用于快速定位对应的检测工具实例
        /// </summary>
        public int 绑定检测工具索引;

        /// <summary>
        /// 绑定的标定工具名称
        /// 关联负责将像素坐标转换为实际物理坐标的标定工具
        /// </summary>
        public string 绑定标定工具名称;

        /// <summary>
        /// 绑定的标定工具索引
        /// 用于快速定位对应的标定工具实例
        /// </summary>
        public int 绑定标定工具索引;
        #endregion

        #region 图像采集参数
        /// <summary>
        /// 产品颜色定义
        /// 描述被检测产品的颜色特征，用于辅助图像分析和参数调整
        /// 例如："白色"、"黑色"、"透明"
        /// </summary>
        public string 产品颜色定义;

        /// <summary>
        /// 取像工具的曝光值设置
        /// 控制相机采集图像时的曝光时间，影响图像亮度
        /// 单位通常为毫秒(ms)或微秒(μs)
        /// </summary>
        public double 取像工具曝光值;

        /// <summary>
        /// 取像工具的对比度值设置
        /// 控制图像中明暗区域的差异程度，影响图像细节清晰度
        /// 通常为0-100的百分比值或特定范围的数值
        /// </summary>
        public double 取像工具对比度值;
        #endregion

        #region 图像数据
        /// <summary>
        /// Cognex VisionPro的图像接口对象
        /// 存储原始的机器视觉图像数据，用于VisionPro工具链处理
        /// 包含图像像素数据、尺寸信息、元数据等
        /// </summary>
        public ICogImage _CogImage;

        /// <summary>
        /// Cognex VisionPro的记录接口对象
        /// 存储与图像相关的检测记录和分析结果
        /// 可包含工具运行过程中的中间数据和最终结果
        /// </summary>
        public ICogRecord _Record;

        /// <summary>
        /// 转换后的JPG格式位图图像
        /// 用于显示和保存，通常是经过处理后的可视化图像
        /// 便于人工查看和日志记录
        /// </summary>
        public Image JpgBitmap;
        #endregion

        #region 坐标信息
        /// <summary>
        /// 检测点在世界坐标系中的X坐标
        /// 经过标定转换后的实际物理坐标，单位通常为毫米(mm)
        /// 用于实际生产中的位置控制和精度分析
        /// </summary>
        public double X_世界坐标;

        /// <summary>
        /// 检测点在世界坐标系中的Y坐标
        /// 经过标定转换后的实际物理坐标，单位通常为毫米(mm)
        /// </summary>
        public double Y_世界坐标;

        /// <summary>
        /// 检测点在图像像素坐标系中的X坐标
        /// 原始图像中的像素位置，单位为像素(pixel)
        /// </summary>
        public double X_像素坐标;

        /// <summary>
        /// 检测点在图像像素坐标系中的Y坐标
        /// 原始图像中的像素位置，单位为像素(pixel)
        /// </summary>
        public double Y_像素坐标;

        /// <summary>
        /// 检测点在像素坐标系中的旋转角度
        /// 用于表示检测对象的旋转方向，单位通常为度(°)
        /// </summary>
        public double R_像素坐标;

        /// <summary>
        /// 简化的X坐标字段
        /// 通常用于快速访问主要使用的X坐标值（可能是世界坐标或像素坐标）
        /// 具体含义取决于上下文
        /// </summary>
        public double X;

        /// <summary>
        /// 简化的Y坐标字段
        /// 通常用于快速访问主要使用的Y坐标值
        /// </summary>
        public double Y;

        /// <summary>
        /// 简化的旋转角度字段
        /// 通常用于快速访问主要使用的旋转角度值
        /// </summary>
        public double R;
        #endregion

        #region 检测结果信息
        /// <summary>
        /// 检测结果的文本描述
        /// 存储详细的检测结论，例如："OK"、"NG-尺寸超差"、"NG-位置偏移"
        /// 用于日志记录和用户界面显示
        /// </summary>
        public string Result;

        /// <summary>
        /// 检测运行状态标识
        /// 表示该检测点是否正在运行或已准备就绪
        /// true：正在运行；false：未运行/已停止
        /// </summary>
        public bool RunState;

        /// <summary>
        /// 检测运行结果标识
        /// 表示该检测点的检测结果是否合格
        /// true：合格(OK)；false：不合格(NG)
        /// </summary>
        public bool RunResult;
        #endregion
    }
}