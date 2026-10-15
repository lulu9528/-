using BasicClassLibrary;
using Jabil.Lib.Common;
using Jabil.Lib.Parameter;
using STTech.CodePlus.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Jabil.Lib
{
    /// <summary>
    /// 项目配置管理核心类，负责系统参数、配置文件、全局状态的集中管理
    /// 采用单例设计模式，确保在应用程序生命周期内全局唯一实例
    /// 提供XML配置读写、CSV文件操作、文件占用检测等核心功能
    /// </summary>
    public class Project
    {
        /// <summary>
        /// 静态实例对象，用于实现单例模式的核心存储
        /// </summary>
        private static Project _instance;

        /// <summary>
        /// 线程同步锁对象，用于在多线程环境下保证单例初始化的线程安全性
        /// </summary>
        private static readonly object _lock = new object();

        /// <summary>
        /// 全局唯一实例访问属性，通过双重锁定机制实现线程安全的单例模式
        /// </summary>
        public static Project Instance
        {
            get
            {
                // 双重锁定检查：
                // 1. 外层检查避免每次获取实例都加锁，提高性能
                // 2. 内层检查确保在多线程同时通过外层检查时，只有一个线程创建实例
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new Project();
                        }
                    }
                }
                return _instance;
            }
            set => _instance = value;  // 允许手动设置实例（主要用于测试或特殊场景）
        }

        public string ImagePath = @"D:\VPTool\Image";


        public string CSVPath = @"D:\VPTool\Config\Data\生产数据";
        /// <summary>
        /// 系统核心参数对象，存储应用程序的基础配置信息（如运行模式、超时设置等）
        /// </summary>
        public SystemParameters SystemParm = new SystemParameters();

        /// <summary>
        /// 参数路径管理对象，统一管理系统中各类配置文件的存储路径
        /// 避免硬编码路径，便于路径统一修改和维护
        /// </summary>
        public ParameterPath ParameterPath = new ParameterPath();

        /// <summary>
        /// 视觉系统参数对象，存储与机器视觉相关的配置（如相机参数、图像处理参数等）
        /// </summary>
        public VisionParameters visionParm = new VisionParameters();

        /// <summary>
        ///焊接机参数   压力  振幅   深圳捷普需要把这个参数传到MES
        /// </summary>
        public WeldParameters weldParm = new WeldParameters();


        public Postion postion左Epson = new Postion();



        public Postion postion右Epson = new Postion();


        public Postion 飞拍基准位 = new Postion();
        /// <summary>
        /// 全局变量容器，用于存储应用程序运行过程中的动态变量和状态信息
        /// 提供跨模块的数据共享能力
        /// </summary>
        public GlobalVariables GlobalVariables { get; } = new GlobalVariables();

        /// <summary>
        /// 工位信息对象，存储当前工位的静态信息（如工位编号、名称、类型等）
        /// </summary>
        public JStationInfo StationInfo { get; set; }

        /// <summary>
        /// 工位控制器接口，定义了工位的核心操作契约
        /// 通过依赖注入可实现不同类型工位的控制逻辑
        /// </summary>
        public IStationController Controller { get; set; }

        #region XML配置文件操作
        /// <summary>
        /// 加载系统XML配置文件，并将配置数据反序列化为指定类型的对象
        /// 若加载失败，会自动创建默认配置文件并重新尝试加载
        /// </summary>
        /// <typeparam name="T">配置对象的类型（必须是可序列化的）</typeparam>
        /// <param name="obj">接收配置数据的对象引用（通过ref关键字实现输出参数）</param>
        /// <param name="relativePath">配置文件相对于应用程序启动目录的路径</param>
        /// <returns>配置加载是否成功（true：成功，false：失败）</returns>
        /// <exception cref="ArgumentNullException">当输入参数obj为null时抛出</exception>
        /// <exception cref="ArgumentException">当配置文件路径为空或空白时抛出</exception>
        public bool LoadSystemConfig<T>(ref T obj, string relativePath)
        {
            // 参数合法性验证，确保方法输入符合预期
            if (obj == null)
                throw new ArgumentNullException(nameof(obj), "接收配置的对象不能为null");
            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException("配置文件路径不能为空或空白", nameof(relativePath));

            try
            {
                // 组合完整路径（应用程序启动目录 + 相对路径）
                //string fullPath = Application.StartupPath + relativePath;
                string fullPath = Path.Combine(Application.StartupPath, relativePath);

                // 尝试加载配置文件
                bool loadSuccess = XMLReadWrite.ToTryLoad(ref obj, fullPath);

                // 若加载失败（文件不存在或格式错误），则创建默认配置并重新加载
                if (!loadSuccess)
                {
                    XMLReadWrite.ToSave(obj, fullPath);  // 保存默认配置
                    loadSuccess = XMLReadWrite.ToTryLoad(ref obj, fullPath);  // 重新加载
                }

                return loadSuccess;
            }
            catch (Exception ex)
            {
                // 记录异常信息（实际项目中建议使用日志框架如log4net、NLog等）
                Debug.WriteLine($"加载配置文件失败: {ex.Message}，堆栈信息: {ex.StackTrace}");

                // 异常情况下的最后尝试：创建默认配置并加载
                try
                {
                    string fullPath = Path.Combine(Application.StartupPath, relativePath);
                    XMLReadWrite.ToSave(obj, fullPath);
                    return XMLReadWrite.ToTryLoad(ref obj, fullPath);
                }
                catch (Exception saveEx)
                {
                    Debug.WriteLine($"创建默认配置文件失败: {saveEx.Message}，堆栈信息: {saveEx.StackTrace}");
                    return false;
                }
            }
        }

        /// <summary>
        /// 将指定的配置对象序列化为XML格式并保存到指定路径
        /// </summary>
        /// <typeparam name="T">配置对象的类型（必须是可序列化的）</typeparam>
        /// <param name="obj">要保存的配置对象</param>
        /// <param name="relativePath">配置文件相对于应用程序启动目录的路径</param>
        /// <returns>配置保存是否成功（true：成功，false：失败）</returns>
        /// <exception cref="ArgumentNullException">当配置对象为null时抛出</exception>
        /// <exception cref="ArgumentException">当配置文件路径为空或空白时抛出</exception>
        public bool SaveSystemConfig<T>(T obj, string relativePath)
        {
            // 参数合法性验证
            if (obj == null)
                throw new ArgumentNullException(nameof(obj), "要保存的配置对象不能为null");
            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException("配置文件路径不能为空或空白", nameof(relativePath));

            try
            {
                // 组合完整路径并保存配置
                string fullPath = Path.Combine(Application.StartupPath, relativePath);

                //string fullPath = Application.StartupPath + relativePath;

                XMLReadWrite.ToSave(obj, fullPath);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"保存配置文件失败: {ex.Message}，堆栈信息: {ex.StackTrace}");
                return false;
            }
        }
        #endregion

        #region CSV文件操作
        /// <summary>
        /// 读取CSV文件数据，并以二维列表形式返回（行→列结构）
        /// 支持跳过表头行，适用于读取结构化数据文件
        /// </summary>
        /// <param name="relativePath">CSV文件相对于应用程序启动目录的路径</param>
        /// <param name="skipHeader">是否跳过表头行（true：跳过第一行，false：不跳过）</param>
        /// <returns>CSV文件中的数据，以List<List<object>>形式返回（外层List表示行，内层List表示列）</returns>
        /// <exception cref="ArgumentException">当CSV文件路径为空或空白时抛出</exception>
        /// <exception cref="IOException">当文件被占用或无法访问时抛出</exception>
        public List<List<object>> GetCsvData(string relativePath, bool skipHeader = false)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException("CSV文件路径不能为空或空白", nameof(relativePath));

            try
            {
                string fullPath = Path.Combine(Application.StartupPath, relativePath);

                // 检查文件是否被其他进程占用
                if (IsFileInUse(fullPath))
                {
                    throw new IOException($"文件正在被使用，无法读取: {fullPath}");
                }

                // 使用using语句确保StreamReader自动释放资源，避免文件句柄泄露
                using (var streamReader = new StreamReader(fullPath, Encoding.Default, false))
                {
                    // 读取全部内容并按行分割（支持\r\n和\n两种换行符）
                    string data = streamReader.ReadToEnd();
                    string[] rowsData = data.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    var result = new List<List<object>>();

                    // 解析每行数据，按逗号分割列
                    foreach (var rowData in rowsData)
                    {
                        string[] columnsData = rowData.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        result.Add(columnsData.Cast<object>().ToList());
                    }

                    // 根据参数决定是否跳过表头行
                    if (skipHeader && result.Count > 0)
                    {
                        result.RemoveAt(0);
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"读取CSV文件失败: {ex.Message}，堆栈信息: {ex.StackTrace}");
                throw;  // 向上抛出异常，由调用者处理
            }
        }

        /// <summary>
        /// 向CSV文件写入完整数据（包含表头和内容），覆盖原有文件
        /// 使用StringBuilder优化字符串拼接性能，适用于批量写入数据
        /// </summary>
        /// <param name="relativePath">CSV文件相对于应用程序启动目录的路径</param>
        /// <param name="header">表头数据列表（第一行）</param>
        /// <param name="body">内容数据，二维列表结构（行→列）</param>
        /// <exception cref="ArgumentException">当CSV文件路径为空或空白时抛出</exception>
        /// <exception cref="ArgumentNullException">当表头或内容为null时抛出</exception>
        /// <exception cref="IOException">当文件被占用或无法写入时抛出</exception>
        public void WriteCsvDataAll(string relativePath, List<object> header, List<List<object>> body)
        {
            // 参数合法性验证
            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException("CSV文件路径不能为空或空白", nameof(relativePath));
            if (header == null)
                throw new ArgumentNullException(nameof(header), "表头数据不能为null");
            if (body == null)
                throw new ArgumentNullException(nameof(body), "内容数据不能为null");

            try
            {
                string fullPath = Path.Combine(Application.StartupPath, relativePath);

                // 检查文件是否被其他进程占用
                if (IsFileInUse(fullPath))
                {
                    throw new IOException($"文件正在被使用，无法写入: {fullPath}");
                }

                // 使用StringBuilder减少字符串拼接时的内存分配，提高性能
                var stringBuilder = new StringBuilder();

                // 拼接表头行
                foreach (var headerItem in header)
                {
                    stringBuilder.Append($"{headerItem},");
                }
                stringBuilder.AppendLine();  // 换行

                // 拼接内容行
                foreach (var row in body)
                {
                    foreach (var item in row)
                    {
                        stringBuilder.Append($"{item},");
                    }
                    stringBuilder.AppendLine();  // 换行
                }

                // 使用using语句确保StreamWriter自动释放资源
                using (var streamWriter = new StreamWriter(fullPath, false, Encoding.Default))
                {
                    streamWriter.Write(stringBuilder.ToString());
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"写入CSV文件失败: {ex.Message}，堆栈信息: {ex.StackTrace}");
                throw;  // 向上抛出异常，由调用者处理
            }
        }

        /// <summary>
        /// 检查指定文件是否被其他进程占用（无法以独占方式打开）
        /// 若文件被Excel占用，会尝试关闭关联的Excel进程
        /// </summary>
        /// <param name="filePath">文件的完整路径</param>
        /// <returns>true：文件被占用；false：文件未被占用或不存在</returns>
        public bool IsFileInUse(string filePath)
        {
            // 文件不存在时视为未被占用
            if (!File.Exists(filePath))
                return false;

            try
            {
                // 尝试以独占方式打开文件（不允许其他进程读写）
                // FileShare.None：拒绝其他进程的任何访问
                using (File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    return false;  // 成功打开，文件未被占用
                }
            }
            catch (IOException)
            {
                // 捕获到IO异常，说明文件被占用
                // 尝试关闭关联的Excel进程（主要处理Excel锁定文件的场景）
                KillExcel.KillRelatedProcesses(filePath);
                return true;
            }
        }
        #endregion
    }

    /// <summary>
    /// Excel进程管理工具类，用于关闭与指定文件关联的Excel进程
    /// 解决Excel文件被锁定导致无法读写的问题
    /// </summary>
    public static class KillExcel
    {
        /// <summary>
        /// 导入Windows API函数，用于获取窗口所属的进程ID
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="processId">输出参数，接收进程ID</param>
        /// <returns>窗口线程ID</returns>
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowThreadProcessId(IntPtr hwnd, out int processId);

        /// <summary>
        /// 关闭与指定文件相关联的Excel进程（精确匹配，减少误杀风险）
        /// 仅关闭打开了目标文件的Excel进程，避免影响其他Excel窗口
        /// </summary>
        /// <param name="filePath">文件的完整路径</param>
        public static void KillRelatedProcesses(string filePath)
        {
            // 路径为空时直接返回
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            try
            {
                // 提取文件名（不含路径）用于匹配窗口标题
                string fileName = Path.GetFileName(filePath);

                // 获取所有正在运行的Excel进程
                var excelProcesses = Process.GetProcessesByName("excel");

                // 遍历Excel进程，查找打开了目标文件的进程
                foreach (var process in excelProcesses)
                {
                    try
                    {
                        // 通过窗口标题判断是否关联目标文件（忽略大小写）
                        // 窗口标题格式通常为"文件名 - Excel"
                        if (process.MainWindowTitle.IndexOf(fileName, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            process.Kill();  // 强制关闭进程

                            // 等待进程退出（最多等待3秒）
                            if (!process.WaitForExit(3000))
                            {
                                Debug.WriteLine($"强制关闭Excel进程超时，进程ID: {process.Id}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // 单个进程处理失败不影响其他进程
                        Debug.WriteLine($"关闭Excel进程（ID: {process.Id}）失败: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"处理Excel进程时发生错误: {ex.Message}，堆栈信息: {ex.StackTrace}");
            }
        }
    }

    /// <summary>
    /// 工位控制器接口，定义了工业设备工位的标准操作契约
    /// 所有工位控制器都应实现此接口，保证操作的一致性
    /// </summary>
    public interface IStationController
    {
        /// <summary>
        /// 初始化工位，包括硬件初始化、参数加载、状态重置等
        /// 应在工位启动前调用，确保工位处于就绪状态
        /// </summary>
        void InitStation();

        /// <summary>
        /// 停止工位运行，终止当前所有操作并释放资源
        /// 通常在程序退出或紧急停止时调用
        /// </summary>
        void Stop();

        /// <summary>
        /// 开始工位运行，启动正常的生产流程
        /// 需在InitStation之后调用
        /// </summary>
        void StartStation();

        /// <summary>
        /// 暂停工位运行，暂时停止当前操作但不释放资源
        /// 可通过Resume方法恢复运行
        /// </summary>
        void Pause();

        /// <summary>
        /// 复位工位，将工位恢复到初始状态（类似重启但不重新初始化）
        /// 用于处理异常后的状态恢复
        /// </summary>
        void Reset();

        /// <summary>
        /// 恢复工位运行，继续执行Pause前的操作
        /// </summary>
        void Resume();
    }
}