using Jabil.Lib;
using Jabil.Module.Log.Lib;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jabil.Module.ProductData.Lib
{
    /// <summary>
    /// 输出统计工具类
    /// 核心功能：提供CSV文件的读写操作、时间索引计算（用于班次内小时段定位）、文件占用检测与释放管理
    /// 采用单例模式确保证系统中统计数据处理逻辑的一致性
    /// </summary>
    public class OutputStatistics
    {
        #region 线程安全单例模式
        /// <summary>
        /// 懒加载单例实例，通过Lazy实现线程安全初始化
        /// LazyThreadSafetyMode.ExecutionAndPublication确保多线程环境下仅初始化一次实例
        /// </summary>
        private static readonly Lazy<OutputStatistics> _lazyInstance =
            new Lazy<OutputStatistics>(() => new OutputStatistics(), LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// 全局唯一实例访问入口
        /// 外部通过此属性获取工具类实例，保证统计逻辑的一致性
        /// </summary>
        public static OutputStatistics Instance => _lazyInstance.Value;

        /// <summary>
        /// 私有构造函数，防止外部直接实例化
        /// 确保单例模式的唯一性
        /// </summary>
        private OutputStatistics()
        {
            RestartTime = 6; // 默认班次开始时间为早上8:00（与抛料统计的班次定义保持一致）
        }
        #endregion

        /// <summary>
        /// 班次开始时间（小时数，0-23）
        /// 例如：8表示8:00开始，用于计算当前时间在班次内的相对索引
        /// 可根据实际生产排班调整
        /// </summary>
        public int RestartTime { get; set; }

        /// <summary>
        /// 计算当前时间在所属班次中的时间段索引（0-11）
        /// 每个班次分为12个小时段，每个时段对应一个索引，用于数据按小时分组统计
        /// </summary>
        /// <returns>返回0-11的时间段索引，计算失败返回-1</returns>
        public int GetTimeIndex()
        {
            try
            {
                //判断当前班次
                List<int> listTime = new List<int>(12);
                if (RestartTime <= DateTime.Now.Hour && DateTime.Now.Hour < RestartTime + 12)
                {
                    return DateTime.Now.Hour - RestartTime;
                }
                else if (RestartTime > DateTime.Now.Hour)
                {
                    return 12 - (RestartTime - DateTime.Now.Hour);

                }
                else
                {
                    return (DateTime.Now.Hour - RestartTime) - 12;
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return -1;
            }
        }

        /// <summary>
        /// 将数据列表写入CSV文件（支持泛型类型）
        /// 自动处理目录创建、文件占用检测，并使用UTF8编码确保多语言兼容性
        /// </summary>
        /// <typeparam name="T">数据实体类型（需与CSV列匹配）</typeparam>
        /// <param name="filePath">目标CSV文件的完整路径</param>
        /// <param name="dataList">要写入的数据列表</param>
        public void WriteCsv<T>(string filePath, List<T> dataList)
        {
            // 参数合法性校验
            if (string.IsNullOrEmpty(filePath) || dataList == null)
            {
                RLog.SaveError(new ArgumentNullException("文件路径或数据列表不能为空"));
                return;
            }

            try
            {
                // 确保目标目录存在，不存在则自动创建
                string directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 等待文件释放占用（最多等待3秒），避免因文件被锁定导致写入失败
                if (!WaitForFileRelease(filePath, 3000))
                {
                    RLog.SaveError(new IOException($"文件被占用且无法释放：{filePath}"));
                    return;
                }

                // 使用CsvHelper写入数据，采用UTF8编码支持多语言
                using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, Encoding.UTF8))
                using (var csv = new CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture))
                {
                    // 调试输出：是否包含表头（CsvHelper默认会写入表头）
                    Console.WriteLine(csv.Configuration.HasHeaderRecord.ToString());
                    // 写入整个数据列表
                    csv.WriteRecords(dataList);
                }
            }
            catch (Exception ex)
            {
                // 记录写入失败的异常信息，包含文件路径便于排查
                RLog.SaveError(new Exception($"写入CSV失败：{filePath}", ex));
            }
        }

        /// <summary>
        /// 从CSV文件读取数据并反序列化为指定类型的列表
        /// 自动处理文件存在性检查、文件占用检测，支持UTF8编码
        /// </summary>
        /// <typeparam name="T">目标数据实体类型（需与CSV列匹配）</typeparam>
        /// <param name="filePath">要读取的CSV文件完整路径</param>
        /// <returns>读取成功返回数据列表，失败返回空列表</returns>
        public List<T> ReadCsv<T>(string filePath)
        {
            // 参数合法性校验：路径为空或文件不存在
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                RLog.SaveError(new FileNotFoundException("CSV文件不存在", filePath));
                return new List<T>();
            }

            try
            {
                // 等待文件释放占用（最多等待3秒），避免因文件被锁定导致读取失败
                if (!WaitForFileRelease(filePath, 3000))
                {
                    RLog.SaveError(new IOException($"文件被占用且无法释放：{filePath}"));
                    return new List<T>();
                }

                // 使用CsvHelper读取数据，采用UTF8编码
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                using (var csv = new CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture))
                {
                    // 调试输出：是否包含表头
                    Console.WriteLine(csv.Configuration.HasHeaderRecord.ToString());
                    // 读取并转换为指定类型的列表
                    return csv.GetRecords<T>().ToList();
                }
            }
            catch (Exception ex)
            {
                // 记录读取失败的异常信息，包含文件路径便于排查
                RLog.SaveError(new Exception($"读取CSV失败：{filePath}", ex));
                return new List<T>();
            }
        }

        /// <summary>
        /// 等待文件释放占用（最多等待指定时间）
        /// 解决文件被其他进程（如Excel打开）锁定导致的读写失败问题
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="timeout">最大等待时间（毫秒）</param>
        /// <returns>文件成功释放返回true，超时返回false</returns>
        private bool WaitForFileRelease(string filePath, int timeout)
        {
            // 文件不存在视为未被占用，直接返回成功
            if (!File.Exists(filePath))
                return true;

            int interval = 100; // 检查间隔（100毫秒）
            int attempts = timeout / interval; // 计算最大尝试次数

            // 循环检查文件是否被释放
            for (int i = 0; i < attempts; i++)
            {
                if (!IsFileInUse(filePath))
                    return true; // 文件已释放，返回成功

                Thread.Sleep(interval); // 等待下一次检查
            }

            // 超时后尝试强制释放（通过关闭相关进程，谨慎使用）
            try
            {
                KillExcel.KillRelatedProcesses(filePath); // 关闭占用文件的进程（如Excel）
                return !IsFileInUse(filePath); // 再次检查是否释放
            }
            catch
            {
                return false; // 强制释放失败
            }
        }

        /// <summary>
        /// 检查文件是否被其他进程占用
        /// 通过尝试以读写方式打开文件来判断是否被锁定
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>文件被占用返回true，否则返回false</returns>
        private bool IsFileInUse(string filePath)
        {
            try
            {
                // 尝试以独占方式打开文件（读写权限，不共享）
                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    return false; // 打开成功，文件未被占用
                }
            }
            catch (IOException)
            {
                return true; // 发生IO异常，文件被占用
            }
            catch
            {
                return true; // 其他异常，视为文件被占用
            }
        }
    }
}