using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Jabil.Module.Permission
{
    /// <summary>
    /// 位图工厂类
    /// 提供应用程序中使用的图像资源的统一访问入口
    /// 负责加载和缓存图像资源，简化图像使用流程
    /// </summary>
    public class BitmapFactory
    {
        /// <summary>
        /// 加载图像资源的私有方法
        /// 根据图像名称和尺寸获取指定的BitmapImage对象
        /// </summary>
        /// <param name="iconName">图像文件名（包含扩展名）</param>
        /// <param name="size">图像的解码尺寸（宽和高）</param>
        /// <returns>加载完成的BitmapImage对象</returns>
        private static BitmapImage GetImage(string iconName, int size)
        {
            // 构建图像资源的URI路径
            // 格式说明：pack://application:,,,/程序集名称;component/资源路径
            var uri = new Uri($@"pack://application:,,,/Jabil.Module.Permission;component/Images/{iconName}", UriKind.RelativeOrAbsolute);

            // 创建BitmapImage对象并加载图像
            var icon = new BitmapImage(uri);

            // 设置图像的解码尺寸，控制图像大小
            icon.DecodePixelHeight = size;
            icon.DecodePixelWidth = size;

            return icon;
        }

        /// <summary>
        /// 登录标题图像
        /// 静态属性，首次访问时通过GetImage方法加载
        /// 对应Images目录下的JabilLogo.png，尺寸为12x12
        /// </summary>
        public static BitmapImage LoginTitle { get; } = GetImage("JabilLogo.png", 12);

        /// <summary>
        /// 背景图像
        /// 静态属性，首次访问时通过GetImage方法加载
        /// 对应Images目录下的background.jpg，尺寸为12x12
        /// </summary>
        public static BitmapImage BackGround { get; } = GetImage("background.jpg", 12);
    }
}