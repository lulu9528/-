using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicClassLibrary
{
    /// <summary>
    /// 数据转换工具类，提供以下功能：
    /// 1. 字符串位数补足（前补零）
    /// 2. 不同进制间的数值转换（2/8/10/16进制）
    /// 3. 字符串与字节数组的编码转换（支持指定字符集）
    /// 4. 字节数组与整数的转换
    /// </summary>
    public class DataConvertUtils
    {
        #region 补足位数（字符串前补零）

        /// <summary>
        /// 将字符串补足到指定长度，不足部分在字符串前面补零
        /// （注：最大支持9位固定长度，超过可能需要调整逻辑）
        /// </summary>
        /// <param name="text">原始字符串（需补足的字符串）</param>
        /// <param name="limitedLength">目标固定长度（字符串最终的长度）</param>
        /// <returns>补足零后的字符串：若原始长度≥目标长度，返回原始字符串；否则前补零至目标长度</returns>
        public static string RepairZero(string text, int limitedLength)
        {
            // 存储补足零后的临时字符串
            string temp = "";

            // 计算需要补足的零的数量（目标长度 - 原始字符串长度）
            int zeroCount = limitedLength - text.Length;
            // 循环添加对应数量的零
            for (int i = 0; i < zeroCount; i++)
            {
                temp += "0";
            }

            // 将原始字符串拼接在补零之后
            temp += text;

            // 返回处理后的字符串
            return temp;
        }

        #endregion

        #region 各进制数间转换（2/8/10/16进制）

        /// <summary>
        /// 实现不同进制数值的转换（支持2、8、10、16进制）
        /// 转换逻辑：先将原始值转为10进制整数，再转为目标进制的字符串
        /// </summary>
        /// <param name="value">原始数值的字符串表示（如"15"、"FF"）</param>
        /// <param name="from">原始数值的进制（只能是2、8、10、16）</param>
        /// <param name="to">目标进制（只能是2、8、10、16）</param>
        /// <returns>目标进制的数值字符串；若转换失败（如格式错误），返回"0"</returns>
        public static string ConvertBase(string value, int from, int to)
        {
            try
            {
                // 第一步：将原始进制的字符串转为10进制整数
                // Convert.ToInt32支持指定进制解析（2-36进制）
                int intValue = Convert.ToInt32(value, from);

                // 第二步：将10进制整数转为目标进制的字符串
                string result = Convert.ToString(intValue, to);

                // 特殊处理：若目标是2进制，补足位数（针对7位及以下的情况补零至8位以内）
                if (to == 2)
                {
                    int resultLength = result.Length;
                    // 根据二进制长度补零，确保更规整（如7位补1个零变为8位，6位补2个零等）
                    switch (resultLength)
                    {
                        case 7:
                            result = "0" + result;       // 7位 → 8位
                            break;
                        case 6:
                            result = "00" + result;      // 6位 → 8位
                            break;
                        case 5:
                            result = "000" + result;     // 5位 → 8位
                            break;
                        case 4:
                            result = "0000" + result;    // 4位 → 8位
                            break;
                        case 3:
                            result = "00000" + result;   // 3位 → 8位
                            break;
                            // 注：长度为1-2位或≥8位时不处理，可根据需求扩展
                    }
                }

                return result;
            }
            catch
            {
                // 捕获转换异常（如原始字符串格式错误、进制不支持等）
                // 原代码日志功能已注释，可根据需要添加日志记录
                // LogHelper.WriteTraceLog(TraceLogLevel.Error, ex.StackTrace);
                return "0"; // 转换失败时返回默认值"0"
            }
        }

        #endregion

        #region 字符串与字节数组的编码转换

        /// <summary>
        /// 使用指定的字符编码将字符串转换为字节数组
        /// （如UTF-8、GB2312、Unicode等，需根据实际场景选择编码）
        /// </summary>
        /// <param name="text">要转换的字符串</param>
        /// <param name="encoding">字符编码（如Encoding.UTF8、Encoding.Default）</param>
        /// <returns>转换后的字节数组；若字符串为空，返回空数组</returns>
        public static byte[] StringToBytes(string text, Encoding encoding)
        {
            // 调用编码类的GetBytes方法直接转换
            return encoding.GetBytes(text);
        }

        /// <summary>
        /// 使用指定的字符编码将字节数组转换为字符串
        /// （需与字符串转字节数组时使用的编码一致，否则可能出现乱码）
        /// </summary>
        /// <param name="bytes">要转换的字节数组</param>
        /// <param name="encoding">字符编码（需与转换为字节数组时的编码相同）</param>
        /// <returns>转换后的字符串；若字节数组为空，返回空字符串</returns>
        public static string BytesToString(byte[] bytes, Encoding encoding)
        {
            // 调用编码类的GetString方法直接转换
            return encoding.GetString(bytes);
        }

        #endregion

        #region 字节数组与32位整数的转换

        /// <summary>
        /// 将字节数组转换为32位整数（int）
        /// （默认使用系统端序，通常为小端序；仅取字节数组的前4个字节进行转换）
        /// </summary>
        /// <param name="data">待转换的字节数组（长度需≥4，否则返回0）</param>
        /// <returns>转换后的32位整数；若字节数组长度不足4，返回0</returns>
        public static int BytesToInt32(byte[] data)
        {
            // 若字节数组长度小于4，无法转换为int（int为4字节），直接返回0
            if (data.Length < 4)
            {
                return 0;
            }

            // 定义要返回的整数
            int num = 0;

            // 若字节数组长度≥4，处理前4个字节
            if (data.Length >= 4)
            {
                // 创建临时缓冲区（固定4字节，匹配int的长度）
                byte[] tempBuffer = new byte[4];

                // 从data的起始位置（索引0）复制4个字节到临时缓冲区
                Buffer.BlockCopy(data, 0, tempBuffer, 0, 4);

                // 将临时缓冲区的字节转换为int（使用系统默认端序）
                num = BitConverter.ToInt32(tempBuffer, 0);
            }

            return num;
        }

        #endregion

    }
}