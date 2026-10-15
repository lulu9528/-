using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BasicClassLibrary
{
    /// <summary>
    /// 扩展方法类，提供一系列通用工具方法，包括：
    /// - 网络ping检测
    /// - 跨线程控件属性修改
    /// - 验证码生成
    /// - 图片处理（缩放、压缩、裁剪、添加文字）
    /// - 阵列点坐标计算
    /// </summary>
    public class ExtensionMethods
    {
        /// <summary>
        /// 检测与目标IP的网络连通性（ping操作）
        /// </summary>
        /// <param name="ip">目标IP地址或主机名（如"192.168.1.1"、"www.baidu.com"）</param>
        /// <returns>连通返回true，否则返回false</returns>
        public bool Collect(string ip)
        {
            // 创建Ping实例
            System.Net.NetworkInformation.Ping p = new System.Net.NetworkInformation.Ping();
            // 配置ping选项（不允许分片）
            System.Net.NetworkInformation.PingOptions options = new System.Net.NetworkInformation.PingOptions();
            options.DontFragment = true;
            // 发送的测试数据（ASCII编码的"hellow"）
            byte[] buffer = Encoding.ASCII.GetBytes("hellow");
            // 发送ping请求（超时时间2000毫秒）
            System.Net.NetworkInformation.PingReply reply = p.Send(ip, 2000, buffer);
            // 根据响应状态判断是否连通
            if (reply.Status == System.Net.NetworkInformation.IPStatus.Success)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 跨线程安全修改控件属性（解决WinForm跨线程操作控件的异常）
        /// </summary>
        /// <param name="control">目标控件（如TextBox、Label等）</param>
        /// <param name="pname">要修改的属性名（如"Text"、"Visible"）</param>
        /// <param name="k">属性的目标值（需与属性类型匹配）</param>
        public void ChangeCtlP(Control control, string pname, object k)
        {
            // 判断当前线程是否为控件创建线程
            if (control.InvokeRequired)
            {
                // 跨线程：通过Invoke切换到控件线程执行修改
                control.Invoke(new Action(() =>
                {
                    // 反射获取属性并设置值
                    control.GetType().GetProperty(pname).SetValue(control, k);
                }));
            }
            else
            {
                // 同线程：直接修改属性
                control.GetType().GetProperty(pname).SetValue(control, k);
            }
        }

        /// <summary>
        /// 在PictureBox中生成随机数字验证码（含干扰线和噪点）
        /// </summary>
        /// <param name="pictureBox1">显示验证码的PictureBox控件</param>
        /// <param name="weidth">验证码图片宽度（默认200）</param>
        /// <param name="height">验证码图片高度（默认50）</param>
        /// <param name="numcount">验证码数字个数（默认5）</param>
        /// <param name="interva">字符之间的间隔（默认35像素）</param>
        /// <param name="fontsize">字符大小（默认30）</param>
        /// <param name="linecount">干扰线数量（默认50）</param>
        /// <param name="pixelcount">噪点数量（默认800）</param>
        /// <returns>生成的验证码字符串（纯数字）</returns>
        public static string Generatecaptcha(PictureBox pictureBox1, int weidth = 200, int height = 50, int numcount = 5, int interva = 35, int fontsize = 30, int linecount = 50, int pixelcount = 800)
        {
            Random r = new Random(); // 随机数生成器
            string str = null; // 存储生成的验证码字符串

            // 生成指定长度的随机数字（0-9）
            for (int i = 0; i < numcount; i++)
            {
                int rNumber = r.Next(0, 10);
                str += rNumber;
            }

            // 创建验证码图片（指定宽高）
            Bitmap bmp = new Bitmap(weidth, height);
            Graphics g = Graphics.FromImage(bmp); // 从图片创建绘图对象

            // 绘制每个数字（随机字体、颜色）
            for (int i = 0; i < numcount; i++)
            {
                Point p = new Point(i * interva, 0); // 每个字符的绘制位置
                string[] fonts = { "微软雅黑", "微软雅黑", "黑体", "隶书", "仿宋" }; // 可选字体
                Color[] colors = { Color.Orange, Color.Blue, Color.Black, Color.Red, Color.Green, Color.DeepPink }; // 可选颜色
                // 绘制字符（随机字体、颜色、加粗）
                g.DrawString(
                    str[i].ToString(),
                    new Font(fonts[r.Next(0, 5)], fontsize, FontStyle.Bold),
                    new SolidBrush(colors[r.Next(0, 6)]),
                    p
                );
            }

            // 绘制干扰线（随机位置的绿色线段）
            for (int i = 0; i < linecount; i++)
            {
                Point p1 = new Point(r.Next(0, bmp.Width), r.Next(0, bmp.Height)); // 线段起点
                Point p2 = new Point(r.Next(0, bmp.Width), r.Next(0, bmp.Height)); // 线段终点
                g.DrawLine(new Pen(Brushes.Green), p1, p2);
            }

            // 绘制噪点（随机位置的黑色像素）
            for (int i = 0; i < pixelcount; i++)
            {
                Point p = new Point(r.Next(0, bmp.Width), r.Next(0, bmp.Height));
                bmp.SetPixel(p.X, p.Y, Color.Black);
            }

            // 将生成的图片显示在PictureBox中
            pictureBox1.Image = bmp;
            return str; // 返回验证码字符串
        }

        /// <summary>
        /// 将图片按比例缩放，确保宽度不超过1200像素（宽度小于1200则保持原尺寸）
        /// </summary>
        /// <param name="srcImage">原始图片</param>
        /// <returns>缩放后的图片（Bitmap）；异常时返回null</returns>
        public static Bitmap PercentImage(System.Drawing.Image srcImage)
        {
            // 计算新宽度（不超过1200）
            int newW = srcImage.Width < 1200 ? srcImage.Width : 1200;
            // 按比例计算新高度（保持宽高比）
            int newH = int.Parse(Math.Round(srcImage.Height * (double)newW / srcImage.Width).ToString());

            try
            {
                // 创建新尺寸的位图
                Bitmap b = new Bitmap(newW, newH);
                Graphics g = Graphics.FromImage(b);
                // 设置插值模式（默认缩放质量）
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Default;
                // 绘制缩放后的图片（从原图完整区域缩放至新尺寸）
                g.DrawImage(
                    srcImage,
                    new System.Drawing.Rectangle(0, 0, newW, newH), // 目标区域
                    new System.Drawing.Rectangle(0, 0, srcImage.Width, srcImage.Height), // 原图区域
                    GraphicsUnit.Pixel
                );
                g.Dispose(); // 释放绘图资源
                return b;
            }
            catch (Exception ex)
            {
                return null; // 异常时返回null
            }
        }

        /// <summary>
        /// 按质量百分比压缩图片（仅支持JPEG格式）
        /// </summary>
        /// <param name="iSource">原始图片</param>
        /// <param name="outPath">压缩后图片的保存路径（含文件名，如"D:\\img.jpg"）</param>
        /// <param name="flag">压缩质量（1-100，值越小压缩比越大，质量越低，默认60）</param>
        /// <returns>压缩成功返回true，失败返回false</returns>
        public static bool Compress(System.Drawing.Image iSource, string outPath, int flag = 60)
        {
            ImageFormat tFormat = iSource.RawFormat; // 原始图片格式
            EncoderParameters ep = new EncoderParameters(); // 编码器参数
            long[] qy = new long[1];
            qy[0] = flag; // 质量参数（1-100）
            // 创建质量编码器参数
            EncoderParameter eParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, qy);
            ep.Param[0] = eParam;

            try
            {
                // 获取所有图片解码器
                ImageCodecInfo[] arrayICI = ImageCodecInfo.GetImageDecoders();
                ImageCodecInfo jpegICIinfo = null;
                // 筛选JPEG解码器（仅支持JPEG格式压缩）
                for (int x = 0; x < arrayICI.Length; x++)
                {
                    if (arrayICI[x].FormatDescription.Equals("JPEG"))
                    {
                        jpegICIinfo = arrayICI[x];
                        break;
                    }
                }

                // 用JPEG编码器保存图片（指定质量）
                if (jpegICIinfo != null)
                    iSource.Save(outPath, jpegICIinfo, ep);
                else
                    iSource.Save(outPath); // 无JPEG编码器时按原始格式保存

                iSource.Dispose(); // 释放原始图片资源
                return true;
            }
            catch (Exception ex)
            {
                iSource.Dispose();
                return false;
            }
        }

        /// <summary>
        /// 裁剪图片的指定矩形区域
        /// </summary>
        /// <param name="originImage">原始图片</param>
        /// <param name="region">裁剪区域（包含起点坐标和宽高，如new Rectangle(10,10,100,100)）</param>
        /// <returns>裁剪后的图片（Image）</returns>
        public static Image CropImage(Image originImage, Rectangle region)
        {
            // 创建裁剪后尺寸的位图
            Bitmap result = new Bitmap(region.Width, region.Height);
            Graphics graphics = Graphics.FromImage(result);
            // 从原始图片的指定区域裁剪并绘制到新图片
            graphics.DrawImage(
                originImage,
                new Rectangle(0, 0, region.Width, region.Height), // 新图片的绘制区域（完整填充）
                region, // 原始图片的裁剪区域
                GraphicsUnit.Pixel
            );
            return result;
        }

        /// <summary>
        /// 按目标尺寸缩放图片（保持宽高比，不足部分留白）
        /// </summary>
        /// <param name="SourceImage">原始图片</param>
        /// <param name="TargetWidth">目标宽度</param>
        /// <param name="TargetHeight">目标高度</param>
        /// <returns>缩放后的图片（Image）；异常时返回null</returns>
        public static Image ZoomImage(Image SourceImage, int TargetWidth, int TargetHeight)
        {
            int IntWidth; // 计算后的实际宽度
            int IntHeight; // 计算后的实际高度

            try
            {
                ImageFormat format = SourceImage.RawFormat; // 原始图片格式
                // 创建目标尺寸的位图（背景默认为透明，后续用白色填充）
                Bitmap SaveImage = new Bitmap(TargetWidth, TargetHeight);
                Graphics g = Graphics.FromImage(SaveImage);
                g.Clear(Color.White); // 填充白色背景

                // 根据原始图片与目标尺寸的比例计算缩放后的实际尺寸（保持宽高比）
                if (SourceImage.Width > TargetWidth && SourceImage.Height <= TargetHeight)
                {
                    // 宽度超目标，高度未超：按宽度缩放
                    IntWidth = TargetWidth;
                    IntHeight = (IntWidth * SourceImage.Height) / SourceImage.Width;
                }
                else if (SourceImage.Width <= TargetWidth && SourceImage.Height > TargetHeight)
                {
                    // 高度超目标，宽度未超：按高度缩放
                    IntHeight = TargetHeight;
                    IntWidth = (IntHeight * SourceImage.Width) / SourceImage.Height;
                }
                else if (SourceImage.Width <= TargetWidth && SourceImage.Height <= TargetHeight)
                {
                    // 长宽均未超目标：保持原尺寸
                    IntHeight = SourceImage.Height;
                    IntWidth = SourceImage.Width;
                }
                else
                {
                    // 长宽均超目标：先按宽度缩放，若高度仍超则按高度重新计算
                    IntWidth = TargetWidth;
                    IntHeight = (IntWidth * SourceImage.Height) / SourceImage.Width;
                    if (IntHeight > TargetHeight)
                    {
                        IntHeight = TargetHeight;
                        IntWidth = (IntHeight * SourceImage.Width) / SourceImage.Height;
                    }
                }

                // 在目标图片中央绘制缩放后的图片（居中显示，留白部分为白色）
                g.DrawImage(
                    SourceImage,
                    (TargetWidth - IntWidth) / 2, // X方向居中偏移
                    (TargetHeight - IntHeight) / 2, // Y方向居中偏移
                    IntWidth,
                    IntHeight
                );
                SourceImage.Dispose(); // 释放原始图片资源

                return SaveImage;
            }
            catch (Exception ex)
            {
                return null; // 异常时返回null
            }
        }

        /// <summary>
        /// 给图片添加文字（默认微软雅黑、加粗）
        /// </summary>
        /// <param name="image">原始图片（会被直接修改）</param>
        /// <param name="str">要添加的文字内容</param>
        /// <param name="p">文字绘制的位置（PointF，支持浮点数坐标）</param>
        /// <param name="color">文字颜色（默认黑色）</param>
        /// <param name="fontsize">文字大小（默认30）</param>
        /// <returns>添加文字后的图片（与输入图片为同一实例）</returns>
        public static Image AddGraphics_string(Image image, string str, PointF p, Color color = default(Color), int fontsize = 30)
        {
            // 若未指定颜色，默认使用黑色
            if (color == default(Color))
                color = Color.Black;

            Graphics g = Graphics.FromImage(image); // 从图片创建绘图对象
            // 绘制文字（微软雅黑、指定大小、加粗）
            g.DrawString(
                str,
                new Font("微软雅黑", fontsize, FontStyle.Bold),
                new SolidBrush(color),
                p
            );
            return image;
        }

        /// <summary>
        /// 计算四边形区域内的阵列点坐标（按行列均匀分布）
        /// </summary>
        /// <typeparam name="T">占位类型（未实际使用，可忽略）</typeparam>
        /// <param name="t">占位参数（未实际使用）</param>
        /// <param name="lefttopp">四边形左上角点坐标</param>
        /// <param name="righttopp">四边形右上角点坐标</param>
        /// <param name="leftdownp">四边形左下角点坐标</param>
        /// <param name="rightdownp">四边形右下角点坐标</param>
        /// <param name="row">行数（垂直方向的点数）</param>
        /// <param name="column">列数（水平方向的点数）</param>
        /// <returns>二维数组的点坐标（[行,列]对应阵列中的位置）</returns>
        public static PointD[,] GetPoints<T>(T t, PointD lefttopp, PointD righttopp, PointD leftdownp, PointD rightdownp, int row, int column)
        {
            // 初始化行列数组（row行column列）
            PointD[,] points = new PointD[row, column];

            // 遍历每行，计算该行的首尾点及中间点
            for (int i = 0; i < row; i++)
            {
                // 计算第i行的左端点（基于左上角和左下角的线性插值）
                points[i, 0].X = (leftdownp.X - lefttopp.X) / (row - 1) * i + lefttopp.X;
                points[i, 0].Y = (leftdownp.Y - lefttopp.Y) / (row - 1) * i + lefttopp.Y;

                // 计算第i行的右端点（基于右上角和右下角的线性插值）
                points[i, column - 1].X = (rightdownp.X - righttopp.X) / (row - 1) * i + righttopp.X;
                points[i, column - 1].Y = (rightdownp.Y - righttopp.Y) / (row - 1) * i + righttopp.Y;

                // 计算第i行中每列的点（基于该行的左右端点线性插值）
                for (int j = 0; j < column; j++)
                {
                    points[i, j].X = (points[i, column - 1].X - points[i, 0].X) / (column - 1) * j + points[i, 0].X;
                    points[i, j].Y = (points[i, column - 1].Y - points[i, 0].Y) / (column - 1) * j + points[i, 0].Y;
                }
            }

            return points;
        }
    }

    /// <summary>
    /// 用于保存值的历史记录，可获取当前值和上一次的值
    /// </summary>
    /// <typeparam name="T">值的类型（如int、string等）</typeparam>
    public class GetLastValue<T>
    {
        /// <summary>
        /// 上一次的值（更新Now时自动同步）
        /// </summary>
        public T Last { get; private set; }
        /// <summary>
        /// 当前值（私有字段，通过属性访问）
        /// </summary>
        private T now;

        /// <summary>
        /// 当前值（设置时自动将旧值保存到Last）
        /// </summary>
        public T Now
        {
            get
            {
                return now;
            }
            set
            {
                Last = now; // 先保存旧值
                now = value; // 再更新当前值
            }
        }
    }

    /// <summary>
    /// 上升沿检测（当输入从false变为true时，输出一次true）
    /// </summary>
    public class UpTttigger
    {
        /// <summary>
        /// 上一次的输入值
        /// </summary>
        public bool Last { get; private set; }

        /// <summary>
        /// 当前输入值（设置时检测上升沿）
        /// </summary>
        public bool Now
        {
            set
            {
                // 上升沿条件：当前为true且上一次为false
                OutPut = value && !Last;
                Last = value; // 更新上一次的值
            }
        }

        /// <summary>
        /// 输出信号（上升沿时为true，仅维持一次更新）
        /// </summary>
        public bool OutPut { get; private set; }
    }

    /// <summary>
    /// 下降沿检测（当输入从true变为false时，输出一次true）
    /// </summary>
    public class LowTttigger
    {
        /// <summary>
        /// 上一次的输入值
        /// </summary>
        public bool Last { get; private set; }

        /// <summary>
        /// 当前输入值（设置时检测下降沿）
        /// </summary>
        public bool Now
        {
            set
            {
                // 下降沿条件：当前为false且上一次为true
                OutPut = !value && Last;
                Last = value; // 更新上一次的值
            }
        }

        /// <summary>
        /// 输出信号（下降沿时为true，仅维持一次更新）
        /// </summary>
        public bool OutPut { get; private set; }
    }
}