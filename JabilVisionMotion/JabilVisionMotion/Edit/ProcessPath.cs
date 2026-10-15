using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace JabilVisionMotion.Edit
{
    /// <summary>
    /// 流程文件路径管理类<br/>
    /// 集中定义和管理所有业务流程文件（如CSV格式的流程配置）的路径，<br/>
    /// 避免路径硬编码在业务逻辑中，便于统一维护和修改路径结构。
    /// </summary>
    public class ProcessPath
    {

        public static string dataPath = "D:\\Data\\";


        //复位流程
        public static string _复位流程_ProcessPath = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\复位流程.csv";

        #region  联机

        #region 前滑台焊接流程
        public static string 前滑台焊接_主流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\前滑台\\前滑台焊接主流程.csv";

        public static string 前滑台上下料穴位选择_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\前滑台\\副流程\\前滑台上下料穴位选择.csv";

        public static string 前滑台13上料焊接13下料_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\前滑台\\副流程\\前滑台13上料焊接13下料.csv";

        public static string 前滑台24上料焊接24下料_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\前滑台\\副流程\\前滑台24上料焊接24下料.csv";

        #endregion

        #region 后滑台焊接流程
        public static string 后滑台焊接_主流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\后滑台\\后滑台焊接主流程.csv";

        public static string 后滑台上下料穴位选择_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\后滑台\\副流程\\后滑台上下料穴位选择.csv";

        public static string 后滑台13上料焊接13下料_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\后滑台\\副流程\\后滑台13上料焊接13下料.csv";

        public static string 后滑台24上料焊接24下料_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\后滑台\\副流程\\后滑台24上料焊接24下料.csv";

        #endregion

        #region 前覆膜机

        public static string 前覆膜机_主流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\前覆膜机\\前覆膜机主流程.csv";

        public static string 前覆膜机_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\前覆膜机\\副流程\\前覆膜机.csv";

        #endregion

        #region 后覆膜机

        public static string 后覆膜机_主流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\后覆膜机\\后覆膜机主流程.csv";

        public static string 后覆膜机_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\后覆膜机\\副流程\\后覆膜机.csv";

        #endregion

        #region 搬运轴流程

        public static string 搬运轴_主流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\搬运轴\\搬运轴主流程.csv";

        public static string 搬运轴_流程选择_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\搬运轴\\副流程\\流程选择.csv";

        public static string 搬运轴_先取前滑台13再取后滑台24_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\搬运轴\\副流程\\先取前滑台13再取后滑台24.csv";

        public static string 搬运轴_先取后滑台24再取前滑台13_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\搬运轴\\副流程\\先取后滑台24再取前滑台13.csv";

        public static string 搬运轴_屏蔽前滑台_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\搬运轴\\副流程\\屏蔽前滑台.csv";

        public static string 搬运轴_屏蔽后滑台_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\搬运轴\\副流程\\屏蔽后滑台.csv";

        #endregion

        #region 换治具
        public static string 治具选择_主流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\单机\\换治具\\换治具主流程.csv";

        public static string 取治具1放治具2_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\单机\\换治具\\副流程\\取治具1放治具2.csv";
        public static string 取治具1放治具3_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\单机\\换治具\\副流程\\取治具1放治具3.csv";

        public static string 取治具2放治具1_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\单机\\换治具\\副流程\\取治具2放治具1.csv";
        public static string 取治具2放治具3_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\单机\\换治具\\副流程\\取治具2放治具3.csv";

        public static string 取治具3放治具1_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\单机\\换治具\\副流程\\取治具3放治具1.csv";
        public static string 取治具3放治具2_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\单机\\换治具\\副流程\\取治具3放治具2.csv";

        public static string 治具选择_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\单机\\换治具\\副流程\\治具选择.csv";
        #endregion

        #region 手动测试
        public static string 手动测试_主流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\手动测试\\手动测试主流程.csv";

        public static string 手动测试_流程选择path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\手动测试\\副流程\\流程选择.csv";

        public static string 手动测试_前滑台path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\手动测试\\副流程\\前滑台手动.csv";

        public static string 手动测试_后滑台path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\手动测试\\副流程\\后滑台手动.csv";
        #endregion






        #region 飞拍
        public static string 飞拍组装流程_主流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\飞拍组装流程\\飞拍组装主流程.csv";

        public static string 飞拍流程_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\飞拍组装流程\\副流程\\飞拍流程.csv";
        public static string 组装流程_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\飞拍组装流程\\副流程\\组装流程.csv";
        public static string 复检流程_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\飞拍组装流程\\副流程\\复检流程.csv";


        #endregion


        #region EPSON2

        #region EPSON2上相机

        public static string EPSON2上相机_主流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON2上相机\\EPSON2上相机主流程.csv";

        public static string EPSON2判断是否需要拍照_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON2上相机\\副流程\\判断是否需要拍照.csv";

        public static string EPSON2无点位拍照_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON2上相机\\副流程\\无点位拍照.csv";

        public static string EPSON2有点位直接发送_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON2上相机\\副流程\\有点位直接发送.csv";

        #endregion


        #region EPSON2下相机


        public static string EPSON2下相机_主流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON2下相机\\EPSON2下相机主流程.csv";

        public static string EPSON2下相机拍照点位确认_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON2下相机\\副流程\\下相机拍照点位确认.csv";

        public static string EPSON2下相机位置1拍照_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON2下相机\\副流程\\一点位拍照.csv";

        public static string EPSON2下相机位置2拍照_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON2下相机\\副流程\\二点位拍照.csv";

        public static string EPSON2下相机位置3拍照_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON2下相机\\副流程\\三点位拍照.csv";

        public static string EPSON2下相机位置4拍照_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON2下相机\\副流程\\四点位拍照.csv";

        public static string EPSON2下相机位置5拍照_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON2下相机\\副流程\\五点位拍照.csv";

        public static string EPSON2下相机位置6拍照_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON2下相机\\副流程\\六点位拍照.csv";
        #endregion


        #region EPSON2请求最终结果

        public static string EPSON2请求最终结果_主流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON2发送请求最终结果\\EPSON2发送请求最终结果.csv";

        public static string EPSON2请求发送最终结果_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON2发送请求最终结果\\副流程\\等待请求最终结果.csv";

        public static string EPSON2请求结果1_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON2发送请求最终结果\\副流程\\请求位置1结果.csv";

        public static string EPSON2请求结果2_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON2发送请求最终结果\\副流程\\请求位置2结果.csv";

        public static string EPSON2请求结果3_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON2发送请求最终结果\\副流程\\请求位置3结果.csv";

        public static string EPSON2请求结果4_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON2发送请求最终结果\\副流程\\请求位置4结果.csv";

        public static string EPSON2请求结果5_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON2发送请求最终结果\\副流程\\请求位置5结果.csv";

        public static string EPSON2请求结果6_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON2发送请求最终结果\\副流程\\请求位置6结果.csv";



        #endregion
        #endregion



        #region EPSON1

        #region EPSON1上相机

        public static string EPSON1上相机_主流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON1上相机\\EPSON1上相机主流程.csv";

        public static string EPSON1判断是否需要拍照_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON1上相机\\副流程\\判断是否需要拍照.csv";

        public static string EPSON1无点位拍照_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON1上相机\\副流程\\无点位拍照.csv";

        public static string EPSON1有点位直接发送_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON1上相机\\副流程\\有点位直接发送.csv";

        #endregion


        #region EPSON1下相机


        public static string EPSON1下相机_主流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON1下相机\\EPSON1下相机主流程.csv";

        public static string EPSON1下相机拍照点位确认_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON1下相机\\副流程\\下相机拍照点位确认.csv";

        public static string EPSON1下相机位置1拍照_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON1下相机\\副流程\\一点位拍照.csv";

        public static string EPSON1下相机位置2拍照_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON1下相机\\副流程\\二点位拍照.csv";

        public static string EPSON1下相机位置3拍照_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON1下相机\\副流程\\三点位拍照.csv";

        public static string EPSON1下相机位置4拍照_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON1下相机\\副流程\\四点位拍照.csv";

        public static string EPSON1下相机位置5拍照_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON1下相机\\副流程\\五点位拍照.csv";

        public static string EPSON1下相机位置6拍照_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON1下相机\\副流程\\六点位拍照.csv";
        #endregion


        #region EPSON1请求最终结果

        public static string EPSON1请求最终结果_主流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON1发送请求最终结果\\EPSON1发送请求最终结果.csv";

        public static string EPSON1请求发送最终结果_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON1发送请求最终结果\\副流程\\等待请求最终结果.csv";

        public static string EPSON1请求结果1_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON1发送请求最终结果\\副流程\\请求位置1结果.csv";

        public static string EPSON1请求结果2_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON1发送请求最终结果\\副流程\\请求位置2结果.csv";

        public static string EPSON1请求结果3_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON1发送请求最终结果\\副流程\\请求位置3结果.csv";

        public static string EPSON1请求结果4_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON1发送请求最终结果\\副流程\\请求位置4结果.csv";

        public static string EPSON1请求结果5_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON1发送请求最终结果\\副流程\\请求位置5结果.csv";

        public static string EPSON1请求结果6_副流程path = Environment.CurrentDirectory + "\\Config\\Project\\Profile\\Process\\联机\\EPSON1发送请求最终结果\\副流程\\请求位置6结果.csv";



        #endregion
        #endregion







        #endregion

    }
}
