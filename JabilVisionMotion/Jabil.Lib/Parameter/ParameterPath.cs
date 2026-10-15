using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Lib.Parameter
{
    [Serializable]
    public class ParameterPath  //路径设置
    {
        /// <summary>
        /// 数据输出路径
        /// </summary>
        public string DataPath = "D:\\VPTool";
        /// <summary>
        /// 系统配置路径
        /// </summary>
        public const string SystemParmPath = "Config\\SystemParm.Xml";
        /// <summary>
        /// 配置文件配置路径
        /// </summary>
        public const string ParameterPathPath = "Config\\ParameterPath.Xml";
        /// <summary>
        /// 视觉检测配置路径
        /// </summary>
        public const string VisionParmPath = "Config\\VisionParm.Xml";
        /// <summary>
        /// DispenseSetting配置路径
        /// </summary>
        public const string DispenseSettingDataPath = "Config\\DispenseSetting.Xml";
        /// <summary>
        /// 保存图片的相关路径
        /// </summary>
        public const string SaveImagePath = "D:\\VPTool\\Image";
        /// <summary>
        /// 标准位置配置路径
        /// </summary>
        public const string StationPostion左Epson = "Config\\StationPostion左Epson.Xml";


        public const string StationPostion右Epson = "Config\\StationPostion右Epson.Xml";


        public const string 飞拍Postion = "Config\\飞拍Postion.Xml";
    }
}
