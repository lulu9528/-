using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices; // 提供结构体与内存交互的API（如Marshal）
using System.Text;
using System.Threading.Tasks;

namespace BasicClassLibrary
{
    public class BitmapFormat
    {
        #region BMP格式核心结构体定义
        /// <summary>
        /// BMP文件头结构体（14字节）
        /// 描述BMP文件的整体信息，如文件类型、大小、像素数据偏移量等
        /// </summary>
        public struct BITMAPFILEHEADER
        {
            public ushort bfType;         // 文件类型标识，BMP格式固定为0x4D42（即"BM"）
            public int bfSize;            // 整个文件的大小（字节）
            public ushort bfReserved1;    // 保留字段1，通常为0
            public ushort bfReserved2;    // 保留字段2，通常为0
            public int bfOffBits;         // 像素数据相对于文件开头的偏移量（字节）
        }

        /// <summary>
        /// 颜色掩码结构体（4字节）
        /// 用于定义8位灰度图的调色板，每个条目对应一个灰度值的RGB分量
        /// </summary>
        public struct MASK
        {
            public byte redmask;      // 红色分量值（灰度图中R=G=B）
            public byte greenmask;    // 绿色分量值
            public byte bluemask;     // 蓝色分量值
            public byte rgbReserved;  // 保留字段，通常为0
        }

        /// <summary>
        /// BMP信息头结构体（40字节）
        /// 描述图片的详细信息，如尺寸、位深度、压缩方式等
        /// </summary>
        public struct BITMAPINFOHEADER
        {
            public int biSize;             // 本结构体的大小（字节，固定为40）
            public int biWidth;            // 图片宽度（像素）
            public int biHeight;           // 图片高度（像素，BMP中正数表示图像倒立存储）
            public ushort biPlanes;        // 色彩平面数，固定为1
            public ushort biBitCount;      // 每个像素的位数（位深度），此处为8（灰度图）
            public int biCompression;      // 压缩方式，0表示不压缩
            public int biSizeImage;        // 像素数据大小（字节），不压缩时可设为0
            public int biXPelsPerMeter;    // 水平分辨率（像素/米），0表示不指定
            public int biYPelsPerMeter;    // 垂直分辨率（像素/米），0表示不指定
            public int biClrUsed;          // 实际使用的调色板颜色数，8位图通常为256
            public int biClrImportant;     // 重要的调色板颜色数，通常与biClrUsed相同
        }
        #endregion

        #region 核心方法实现
        /*******************************************
        * 函数名称：RotatePic       
        * 函数功能：旋转图片（180度），解决指纹图片显示与实际方向颠倒的问题     
        * 函数逻辑：通过反转像素数组的顺序实现旋转，原数组第N个像素对应旋转后倒数第N个像素
        * 函数入参：
        *   BmpBuf---旋转前的像素数据（字节数组，每个字节代表一个像素的灰度值）
        *   width---图片宽度（像素）
        *   height---图片高度（像素）
        * 函数出参：
        *   ResBuf---旋转后的像素数据（输出参数，需提前初始化）
        * 函数返回：无
        *********************************************/
        public static void RotatePic(byte[] BmpBuf, int width, int height, ref byte[] ResBuf)
        {
            int RowLoop = 0;          // 行循环变量（按行遍历像素）
            int ColLoop = 0;          // 列循环变量（按列遍历像素）
            int BmpBuflen = width * height;  // 像素总数量（宽×高）

            try
            {
                // 按行遍历原图（每行像素数为width）
                for (RowLoop = 0; RowLoop < BmpBuflen;)
                {
                    // 遍历当前行的每个像素
                    for (ColLoop = 0; ColLoop < width; ColLoop++)
                    {
                        // 核心旋转逻辑：
                        // 原图中第(RowLoop + ColLoop)个像素，对应旋转后第(BmpBuflen - RowLoop - width + ColLoop)个像素
                        // 原理：反转整个像素数组的顺序（实现180度旋转）
                        ResBuf[RowLoop + ColLoop] = BmpBuf[BmpBuflen - RowLoop - width + ColLoop];
                    }
                    // 移动到下一行（步长为宽度）
                    RowLoop = RowLoop + width;
                }
            }
            catch (Exception ex)
            {
                // 异常捕获（原代码日志功能已注释，可根据需要补充）
                // ZKCE.SysException.ZKCELogger logger = new ZKCE.SysException.ZKCELogger(ex);
                // logger.Append();
            }
        }

        /*******************************************
        * 函数名称：StructToBytes       
        * 函数功能：将结构体转换为字节数组（用于BMP头信息的序列化）
        * 函数逻辑：通过Marshal类将结构体写入非托管内存，再复制到托管字节数组
        * 函数入参：
        *   StructObj---待转换的结构体（如BITMAPFILEHEADER、BITMAPINFOHEADER等）
        *   Size---目标字节数组的长度（用于特殊处理BMP文件头的14字节格式）
        * 函数出参：无
        * 函数返回：转换后的字节数组
        *********************************************/
        public static byte[] StructToBytes(object StructObj, int Size)
        {
            int StructSize = Marshal.SizeOf(StructObj);  // 获取结构体的实际大小（字节）
            byte[] GetBytes = new byte[StructSize];      // 初始化临时字节数组

            try
            {
                // 1. 分配非托管内存（大小为结构体大小）
                IntPtr StructPtr = Marshal.AllocHGlobal(StructSize);
                // 2. 将结构体复制到非托管内存
                Marshal.StructureToPtr(StructObj, StructPtr, false);
                // 3. 将非托管内存中的数据复制到托管字节数组
                Marshal.Copy(StructPtr, GetBytes, 0, StructSize);
                // 4. 释放非托管内存（避免内存泄漏）
                Marshal.FreeHGlobal(StructPtr);

                // 特殊处理：BMP文件头（BITMAPFILEHEADER）实际应为14字节，但结构体默认会因对齐补位到16字节
                // 此处移除补位的2字节（索引2和3），确保文件头符合BMP格式标准
                if (Size == 14)
                {
                    byte[] NewBytes = new byte[Size];  // 目标14字节数组
                    int Count = 0;                     // 新数组的索引计数器

                    // 遍历结构体转换后的字节数组，跳过索引2和3的补位字节
                    for (int Loop = 0; Loop < StructSize; Loop++)
                    {
                        if (Loop != 2 && Loop != 3)
                        {
                            NewBytes[Count] = GetBytes[Loop];
                            Count++;
                        }
                    }
                    return NewBytes;  // 返回14字节的BMP文件头
                }
                else
                {
                    return GetBytes;  // 其他结构体（如信息头）直接返回原数组
                }
            }
            catch (Exception ex)
            {
                // 异常捕获（原代码日志功能已注释）
                // ZKCE.SysException.ZKCELogger logger = new ZKCE.SysException.ZKCELogger(ex);
                // logger.Append();
                return GetBytes;
            }
        }

        /*******************************************
        * 函数名称：GetBitmap       
        * 函数功能：将像素数据转换为BMP格式的内存流（用于内存中处理图片，如显示）
        * 函数逻辑：构造BMP文件头、信息头、调色板，旋转像素数据后写入内存流
        * 函数入参：
        *   buffer---原始像素数据（字节数组，每个字节为灰度值）
        *   nWidth---图片宽度（像素）
        *   nHeight---图片高度（像素）
        * 函数出参：
        *   ms---输出的内存流（包含完整BMP格式数据，需提前初始化）
        * 函数返回：无
        *********************************************/
        public static void GetBitmap(byte[] buffer, int nWidth, int nHeight, ref MemoryStream ms)
        {
            int ColorIndex = 0;               // 调色板索引
            ushort m_nBitCount = 8;           // 位深度：8位（灰度图）
            int m_nColorTableEntries = 256;   // 调色板颜色数：256（8位图最大值）
            byte[] ResBuf = new byte[nWidth * nHeight * 2];  // 旋转后的像素数组（临时缓冲区）

            try
            {
                // 初始化BMP头信息结构体
                BITMAPFILEHEADER BmpHeader = new BITMAPFILEHEADER();
                BITMAPINFOHEADER BmpInfoHeader = new BITMAPINFOHEADER();
                MASK[] ColorMask = new MASK[m_nColorTableEntries];  // 256个灰度调色板

                // 计算行对齐后的宽度：BMP要求每行字节数为4的倍数，不足则补位
                int w = (((nWidth + 3) / 4) * 4);

                // 填充BMP信息头（描述图片属性）
                BmpInfoHeader.biSize = Marshal.SizeOf(BmpInfoHeader);  // 结构体大小（40字节）
                BmpInfoHeader.biWidth = nWidth;                        // 图片宽度
                BmpInfoHeader.biHeight = nHeight;                      // 图片高度
                BmpInfoHeader.biPlanes = 1;                            // 色彩平面数（固定为1）
                BmpInfoHeader.biBitCount = m_nBitCount;                // 8位灰度图
                BmpInfoHeader.biCompression = 0;                       // 不压缩
                BmpInfoHeader.biSizeImage = 0;                         // 不压缩时可设为0
                BmpInfoHeader.biXPelsPerMeter = 0;                     // 分辨率未指定
                BmpInfoHeader.biYPelsPerMeter = 0;
                BmpInfoHeader.biClrUsed = m_nColorTableEntries;         // 使用256种颜色
                BmpInfoHeader.biClrImportant = m_nColorTableEntries;   // 所有颜色均重要

                // 填充BMP文件头（描述文件属性）
                BmpHeader.bfType = 0x4D42;  // "BM"标识，BMP文件标志
                // 像素数据偏移量：文件头(14) + 信息头(40) + 调色板(256×4)
                BmpHeader.bfOffBits = 14 + Marshal.SizeOf(BmpInfoHeader) + BmpInfoHeader.biClrUsed * 4;
                // 整个文件大小：偏移量 + 像素数据大小（行对齐后的宽度×高度）
                BmpHeader.bfSize = BmpHeader.bfOffBits + ((((w * BmpInfoHeader.biBitCount + 31) / 32) * 4) * BmpInfoHeader.biHeight);
                BmpHeader.bfReserved1 = 0;  // 保留字段
                BmpHeader.bfReserved2 = 0;

                // 将文件头转换为字节数组并写入内存流（14字节）
                ms.Write(StructToBytes(BmpHeader, 14), 0, 14);
                // 将信息头转换为字节数组并写入内存流（40字节）
                ms.Write(StructToBytes(BmpInfoHeader, Marshal.SizeOf(BmpInfoHeader)), 0, Marshal.SizeOf(BmpInfoHeader));

                // 构建8位灰度图的调色板（每个灰度值对应R=G=B）
                for (ColorIndex = 0; ColorIndex < m_nColorTableEntries; ColorIndex++)
                {
                    ColorMask[ColorIndex].redmask = (byte)ColorIndex;    // 红色分量=灰度值
                    ColorMask[ColorIndex].greenmask = (byte)ColorIndex;  // 绿色分量=灰度值
                    ColorMask[ColorIndex].bluemask = (byte)ColorIndex;   // 蓝色分量=灰度值
                    ColorMask[ColorIndex].rgbReserved = 0;

                    // 将调色板条目写入内存流（每个条目4字节）
                    ms.Write(StructToBytes(ColorMask[ColorIndex], Marshal.SizeOf(ColorMask[ColorIndex])), 0, Marshal.SizeOf(ColorMask[ColorIndex]));
                }

                // 旋转像素数据（解决指纹图片倒立问题）
                RotatePic(buffer, nWidth, nHeight, ref ResBuf);

                // 处理行对齐：计算补位字节数（使每行字节数为4的倍数）
                byte[] filter = null;
                if (w - nWidth > 0)
                {
                    filter = new byte[w - nWidth];  // 补位字节数组（默认值0）
                }

                // 按行写入像素数据（含补位）
                for (int i = 0; i < nHeight; i++)
                {
                    // 写入当前行的有效像素（nWidth字节）
                    ms.Write(ResBuf, i * nWidth, nWidth);
                    // 写入补位字节（使每行总长度为w）
                    if (w - nWidth > 0)
                    {
                        ms.Write(filter, 0, w - nWidth);  // 补位字节为0（黑色）
                    }
                }
            }
            catch (Exception ex)
            {
                // 异常捕获（原代码日志功能已注释）
                // ZKCE.SysException.ZKCELogger logger = new ZKCE.SysException.ZKCELogger(ex);
                // logger.Append();
            }
        }

        /*******************************************
        * 函数名称：WriteBitmap       
        * 函数功能：将像素数据写入BMP文件（保存为本地文件）
        * 函数逻辑：与GetBitmap类似，差异在于输出目标为文件流而非内存流
        * 函数入参：
        *   buffer---原始像素数据（字节数组，每个字节为灰度值）
        *   nWidth---图片宽度（像素）
        *   nHeight---图片高度（像素）
        * 函数出参：无
        * 函数返回：无
        *********************************************/
        public static void WriteBitmap(byte[] buffer, int nWidth, int nHeight)
        {
            int ColorIndex = 0;               // 调色板索引
            ushort m_nBitCount = 8;           // 位深度：8位（灰度图）
            int m_nColorTableEntries = 256;   // 调色板颜色数：256
            byte[] ResBuf = new byte[nWidth * nHeight];  // 旋转后的像素数组

            try
            {
                // 初始化BMP头信息结构体（同GetBitmap）
                BITMAPFILEHEADER BmpHeader = new BITMAPFILEHEADER();
                BITMAPINFOHEADER BmpInfoHeader = new BITMAPINFOHEADER();
                MASK[] ColorMask = new MASK[m_nColorTableEntries];
                int w = (((nWidth + 3) / 4) * 4);  // 行对齐后的宽度

                // 填充信息头（同GetBitmap）
                BmpInfoHeader.biSize = Marshal.SizeOf(BmpInfoHeader);
                BmpInfoHeader.biWidth = nWidth;
                BmpInfoHeader.biHeight = nHeight;
                BmpInfoHeader.biPlanes = 1;
                BmpInfoHeader.biBitCount = m_nBitCount;
                BmpInfoHeader.biCompression = 0;
                BmpInfoHeader.biSizeImage = 0;
                BmpInfoHeader.biXPelsPerMeter = 0;
                BmpInfoHeader.biYPelsPerMeter = 0;
                BmpInfoHeader.biClrUsed = m_nColorTableEntries;
                BmpInfoHeader.biClrImportant = m_nColorTableEntries;

                // 填充文件头（同GetBitmap）
                BmpHeader.bfType = 0x4D42;
                BmpHeader.bfOffBits = 14 + Marshal.SizeOf(BmpInfoHeader) + BmpInfoHeader.biClrUsed * 4;
                BmpHeader.bfSize = BmpHeader.bfOffBits + ((((w * BmpInfoHeader.biBitCount + 31) / 32) * 4) * BmpInfoHeader.biHeight);
                BmpHeader.bfReserved1 = 0;
                BmpHeader.bfReserved2 = 0;

                // 创建文件流（保存路径为"finger.bmp"，覆盖模式）
                Stream FileStream = File.Open("finger.bmp", FileMode.Create, FileAccess.Write);
                BinaryWriter TmpBinaryWriter = new BinaryWriter(FileStream);  // 二进制写入器

                // 写入文件头和信息头（同GetBitmap）
                TmpBinaryWriter.Write(StructToBytes(BmpHeader, 14));
                TmpBinaryWriter.Write(StructToBytes(BmpInfoHeader, Marshal.SizeOf(BmpInfoHeader)));

                // 写入调色板（同GetBitmap）
                for (ColorIndex = 0; ColorIndex < m_nColorTableEntries; ColorIndex++)
                {
                    ColorMask[ColorIndex].redmask = (byte)ColorIndex;
                    ColorMask[ColorIndex].greenmask = (byte)ColorIndex;
                    ColorMask[ColorIndex].bluemask = (byte)ColorIndex;
                    ColorMask[ColorIndex].rgbReserved = 0;

                    TmpBinaryWriter.Write(StructToBytes(ColorMask[ColorIndex], Marshal.SizeOf(ColorMask[ColorIndex])));
                }

                // 旋转像素数据
                RotatePic(buffer, nWidth, nHeight, ref ResBuf);

                // 处理行对齐补位
                byte[] filter = null;
                if (w - nWidth > 0)
                {
                    filter = new byte[w - nWidth];
                }

                // 按行写入像素数据（含补位）
                for (int i = 0; i < nHeight; i++)
                {
                    TmpBinaryWriter.Write(ResBuf, i * nWidth, nWidth);  // 写入有效像素
                    if (w - nWidth > 0)
                    {
                        TmpBinaryWriter.Write(filter, 0, w - nWidth);  // 写入补位字节
                    }
                }

                // 释放资源（关闭流和写入器）
                FileStream.Close();
                TmpBinaryWriter.Close();
            }
            catch (Exception ex)
            {
                // 异常捕获（原代码日志功能已注释）
                // ZKCE.SysException.ZKCELogger logger = new ZKCE.SysException.ZKCELogger(ex);
                // logger.Append();
            }
        }
        #endregion
    }
}