using Jabil.Lib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Jabil.Module.ProductData.Lib
{
    /// <summary>
    /// CSV日志工具类，用于记录CT值变化
    /// </summary>
    public static class CtCsvLogger
    {
        // 日志目录（应用根目录下的CTLogs）
        private static readonly string _logDirectory = System.IO.Path.Combine(Project.Instance?.ParameterPath?.DataPath ?? Application.StartupPath, "Config", "Data", "CT统计");
        // 线程锁确保写入安全
        private static readonly object _writeLock = new object();

        /// <summary>
        /// 记录CT值（仅时间戳和当前值）
        /// </summary>
        /// <param name="timestamp">记录时间</param>
        /// <param name="ctValue">当前CT值</param>
        public static void LogCtValue(DateTime timestamp, double ctValue)
        {
            try
            {
                // 确保目录存在
                if (!Directory.Exists(_logDirectory))
                {
                    Directory.CreateDirectory(_logDirectory);
                }

                // 按日期生成文件（每天一个）
                string fileName = $"CT_Log_{timestamp:yyyyMMdd}.csv";
                string filePath = Path.Combine(_logDirectory, fileName);
                bool isNewFile = !File.Exists(filePath);

                lock (_writeLock)
                {
                    using (var writer = new StreamWriter(filePath, true))
                    {
                        // 新文件写表头
                        if (isNewFile)
                        {
                            writer.WriteLine("Timestamp,CTValue");
                        }

                        // 写入记录（时间精确到秒，CT值保留2位小数）
                        writer.WriteLine($"{timestamp:yyyy-MM-dd HH:mm:ss},{ctValue:F2}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CT日志写入失败：{ex.Message}");
            }
        }
    }
}
