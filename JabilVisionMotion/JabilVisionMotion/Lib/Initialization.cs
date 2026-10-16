using BasicClassLibrary;
using BasicClassLibrary.RexHelps;
using csLTDMC;
using Jabil.Lib;
using Jabil.Module.Log.Frm;
using Jabil.Module.Log.Lib;
using Jabil.Module.Modbus.Lib;
using Jabil.Module.Motion.Common.Dtos;
using Jabil.Module.Motion.Lib;
using JabilVisionMotion.Frm;
using JabilVisionMotion.Frm.UserPage;
using Jabil.Module.Permission.Forms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Jabil.Module.IO;
using Jabil.Module.Motion;
using Jabil.Module.Vision;
using Jabil.Module.Modbus;
using Jabil.Module.TCPIP;
using Jabil.Module.TCPIP.Lib;
using Jabil.Module.ProductData.Lib;
using Jabil.Lib.Parameter;
using Jabil.Module.Permission;
using JabilVisionMotion.Edit;
using Jabil.Module.Vision.Frm;
using Jabil.Module.Serialport;
using System.Runtime.InteropServices;
using JabilVisionMotion.Frm.Dialog;
using lctdevice;

namespace JabilVisionMotion.Lib
{
    /// <summary>
    /// 程序初始化管理类<br/>
    /// 核心职责：协调程序启动过程中的所有初始化操作，确保各模块按依赖顺序加载，提供进度反馈和错误处理<br/>
    /// 设计特点：
    /// - 采用线程安全的单例模式，保证全局唯一的初始化入口
    /// - 分阶段执行初始化（配置检查→模块加载→数据清理→参数初始化）
    /// - 每个步骤包含状态反馈和错误处理，失败时自动释放资源并退出
    /// </summary>
    public class Initialization
    {
        #region 单例模式（线程安全）
        /// <summary>
        /// 线程安全的单例实例<br/>
        /// 使用Lazy延迟初始化，确保在第一次访问时才创建实例，避免程序启动时的资源浪费<br/>
        /// LazyThreadSafetyMode.ExecutionAndPublication：保证初始化方法在多线程环境下只执行一次，避免实例重复创建
        /// </summary>
        private static readonly Lazy<Initialization> _instance = new Lazy<Initialization>(() => new Initialization(), LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// 单例实例访问入口<br/>
        /// 外部通过Initialization.Instance调用初始化功能，确保全局唯一控制
        /// </summary>
        public static Initialization Instance => _instance.Value;

        /// <summary>
        /// 私有构造函数，防止外部实例化<br/>
        /// 确保单例模式的唯一性，避免创建多个初始化管理器导致的冲突
        /// </summary>
        private Initialization() { }
        #endregion

        #region 字段（遵循C#命名规范）
        /// <summary>
        /// 标识程序启动时是否初始化成功<br/>
        /// 外部可通过此属性判断程序是否正常启动（true=成功，false=失败）
        /// </summary>
        public bool InitSucceed { get; private set; } = false;

        /// <summary>
        /// 标识程序是否正在启动<br/>
        /// volatile关键字确保多线程环境下的可见性，避免线程缓存导致的状态不一致（如UI线程与初始化线程的状态同步）
        /// </summary>
        public static volatile bool IsLoading = true;

        // 常量定义（替代魔法数字，提高代码可维护性）
        private const int BackupRetentionDays = 5;       // 备份文件保留天数（只保留最近5天的备份，节省存储空间）
        private const int ImageRetentionDays = 7;       // 图片文件保留天数（保留7天的检测图片，用于追溯）
        private const int PermissionExpireSeconds = 600; // 权限过期时间（秒）（10分钟未操作自动失效，增强安全性）
        #endregion

        #region 核心初始化方法
        /// <summary>
        /// 初始化入口方法，协调整个程序的启动流程<br/>
        /// 执行步骤（按依赖顺序）：
        /// 1. 预初始化：创建核心系统实例，建立基础依赖
        /// 2. 配置检查：验证配置文件完整性，加载关键参数
        /// 3. 模块加载：按硬件依赖顺序初始化运动、IO、通讯等模块
        /// 4. 数据清理：定期删除过期备份和图片文件，释放存储空间
        /// 5. 参数初始化：加载业务参数，完成启动
        /// 每个步骤通过进度更新反馈状态，失败时触发资源释放并退出
        /// </summary>
        public async Task InitAsync()
        {
            try
            {
                // 预初始化：创建核心系统实例，建立基础依赖（如视觉、IO、通讯系统等）
                PreInit();
                // 设置产品名称（示例值，实际应从配置文件读取）
                Project.Instance.SystemParm.ProductName = "Test";
                // 更新初始化进度（2%：开始初始化）
                UpdateStep(2, "初始化", true);

                #region 配置文件检查与加载（按顺序加载关键配置）
                // 15%：检查配置文件目录是否存在（确保后续加载有可用的存储路径）
                UpdateStepWithCheck(15, "检查各配置文件是否存在", InitConfigDirectory);

                // 6%：加载左Epson点位参数
                UpdateStepWithCheck(6, "尝试加载机器人位置参数", () => Project.Instance.LoadSystemConfig(ref Project.Instance.postion左Epson, ParameterPath.StationPostion左Epson));


                UpdateStepWithCheck(6, "尝试加载机器人位置参数", () => Project.Instance.LoadSystemConfig(ref Project.Instance.postion右Epson, ParameterPath.StationPostion右Epson));


                UpdateStepWithCheck(6, "尝试加载机器人位置参数", () => Project.Instance.LoadSystemConfig(ref Project.Instance.飞拍基准位, ParameterPath.飞拍Postion));

                // 6%：加载配置路径文件（定义系统参数的存储路径，为后续加载提供基础）
                UpdateStepWithCheck(6, "尝试加载配置路径文件", () => Project.Instance.LoadSystemConfig(ref Project.Instance.ParameterPath, ParameterPath.ParameterPathPath));
                // 8%：加载系统设置参数（如语言、运行模式等全局配置）
                UpdateStepWithCheck(8, "尝试加载系统设置参数", () => Project.Instance.LoadSystemConfig(ref Project.Instance.SystemParm, ParameterPath.SystemParmPath));
                // 9%：加载视觉配置参数（相机参数、算法阈值等视觉相关配置）
                UpdateStepWithCheck(9, "尝试加载视觉配置参数", () => Project.Instance.LoadSystemConfig(ref Project.Instance.visionParm, ParameterPath.VisionParmPath));
                // 12%：刷新项目点表信息（同步IO点、通讯点等实时数据，确保参数一致性）
                bool isAllRefreshed = await RefreshParamAsync();
                UpdateStepWithCheck(12, "尝试加载项目相关点表信息", () => isAllRefreshed);
                Project.Instance.SystemParm.ProductName = "Test"; // 再次设置产品名称（可能被配置覆盖，此处强制设置）
                #endregion

                #region 模块初始化（按依赖顺序加载硬件/功能模块）
                // 20%：加载运动控制模块（轴卡、电机驱动等，运动控制是核心，需优先初始化）
                //UpdateStepWithCheck(20, "尝试运动控制模块加载", () => SysParm.Instance._hardwareInitializer.InitMotionCards().Success);
                // 25%：加载IO板卡（数字量输入输出模块，为后续控制提供硬件接口）
                //UpdateStepWithCheck(25, "尝试加载IO板卡", () => SysParm.Instance._hardwareInitializer.InitIOCards().Success);
                // 注释：原计划加载Modbus设备，此处保留注释用于说明扩展点
                // 25%：加载Modbus通讯设备（PLC、传感器等通过Modbus连接的设备）
                 UpdateStepWithCheck(25, "尝试加载Modbus通讯设备", () => SysParm.Instance._hardwareInitializer.InitPLCModbusDevices().Success);

                
                 UpdateStepWithCheck(25, "尝试加载Modbus通讯设备", () => SysParm.Instance._hardwareInitializer.InitRobotModbusDevices().Success);



                UpdateStepWithCheck(35, "尝试加载TCP网络通讯设备", () => SysParm.Instance._hardwareInitializer.InitTcpipDevices().Success);
                // 35%：轴卡使能初始化（激活运动轴，准备接收运动指令，必须在运动模块加载后执行）
                //UpdateStepWithCheck(35, "轴卡使能初始化", () => SysParm.Instance._hardwareInitializer.EnableAxisServo().Success);
                // 注释：原计划加载2D检测程序，此处保留注释用于说明扩展点
                // 45%：加载2D检测程序（视觉检测算法、模板等，依赖视觉配置参数）
                UpdateStepWithCheck(45, "尝试加载2D检测程序", () => SysParm.Instance._hardwareInitializer.InitVisionTools().Success);

                //UpdateStepWithCheck(55, "飞拍模块初始化", () => SysParm.Instance._hardwareInitializer.LoadFlyTrigger().Success);
                //UpdateStepWithCheck(55, "焊接机连接初始化", () => SysParm.Instance._hardwareInitializer.InitWeldingMachine().Success);

                //UpdateStepWithCheck(65, "Mes数据库检测", () => SysParm.Instance._hardwareInitializer.InitMesDb().Success);

                #endregion

                #region 数据清理与备份（定期维护操作）
                // 40%：清理过期备份文件（保留最近5天，避免备份文件过多占用空间）
                UpdateStepWithCheck(40, "定期备份清理备份程序，请耐心等待", () => RegularClear(BackupRetentionDays));
                // 清理过期图片文件（保留最近30天，平衡追溯需求和存储空间）
                DeleteImageFile(ImageRetentionDays);
                #endregion

                #region 完成初始化
                // 75%：初始化软件参数（业务参数、运行模式等，完成初始化的最后步骤）
                UpdateStepWithCheck(75, "尝试初始化软件参数", InitializeParameter);
                // 100%：启动成功，更新状态（标记初始化完成，通知外部程序可正常运行）
                UpdateStep(100, "启动成功", InitSucceed = true);
                IsLoading = false; // 标记加载完成（UI线程可据此切换界面状态）
                #endregion
            }
            catch (Exception ex)
            {
                // 记录初始化异常，标记加载失败（任何步骤抛出异常均视为初始化失败）
                RLog.SaveError(ex);
                IsLoading = false;
            }
        }
        #endregion

        #region 数据清理相关方法（维护系统存储空间）

        /// <summary>
        /// 递归删除目录及包含的所有子目录和文件<br/>
        /// 用于清理过期备份目录等场景
        /// </summary>
        /// <param name="directory">要删除的目录信息（DirectoryInfo实例）</param>
        private void DeleteDirectoryRecursively(DirectoryInfo directory)
        {
            if (directory == null || !directory.Exists) return;

            try
            {
                // 递归删除所有子目录
                foreach (var subDir in directory.GetDirectories())
                {
                    DeleteDirectoryRecursively(subDir);
                }

                // 删除目录中的所有文件（移除只读属性）
                foreach (var file in directory.GetFiles())
                {
                    file.Attributes = RemoveReadOnlyAttribute(file.Attributes);
                    file.Delete();
                }

                // 删除当前目录
                directory.Attributes = RemoveReadOnlyAttribute(directory.Attributes);
                directory.Delete();
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex); // 仅记录错误，不重复输出正常操作
            }
        }

        /// <summary>
        /// 按保留天数删除指定目录下的过期文件（递归处理子目录）<br/>
        /// 简化日志：只记录扫描结果和删除统计，不记录单个文件删除
        /// </summary>
        /// <param name="rootPath">要清理的根目录路径</param>
        /// <param name="retentionDays">文件保留天数</param>
        /// <returns>删除的文件总数</returns>
        private int DeleteExpiredFilesInDirectory(string rootPath, int retentionDays)
        {
            if (!Directory.Exists(rootPath))
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange, $"跳过不存在的目录：{rootPath}");
                return 0;
            }

            DateTime cutoffDate = DateTime.Now.AddDays(-retentionDays);
            int deletedCount = 0;

            try
            {
                // 递归获取目录下所有文件（排除NG文件夹）
                var filesToDelete = Directory.EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories)
                                             .Where(path => !path.Contains(@"\NG\") && !path.EndsWith(@"\NG"))
                                             .Select(path => new FileInfo(path))
                                             .Where(file => file.CreationTime < cutoffDate)
                                             .ToList();

                if (!filesToDelete.Any())
                {
                    return 0; // 无过期文件，不记录日志（避免冗余）
                }

                // 批量删除文件（不记录单个文件）
                foreach (var file in filesToDelete)
                {
                    try
                    {
                        if (file.IsReadOnly) file.IsReadOnly = false;
                        file.Delete();
                        deletedCount++;
                    }
                    catch (Exception ex)
                    {
                        RLog.SaveError(ex, $"删除文件失败：{file.FullName}"); // 仅记录失败的文件
                    }
                }

                // 只记录统计结果
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Green,
                    $"目录 '{rootPath}' 清理完成：删除 {deletedCount} 个过期文件");

                return deletedCount;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex, $"目录 '{rootPath}' 清理失败");
                return 0;
            }
        }

        /// <summary>
        /// 按保留天数删除过期图片文件<br/>
        /// 简化日志：合并OK文件夹查找和清理结果，只输出关键统计
        /// </summary>
        /// <param name="retentionDays">文件保留天数</param>
        private void DeleteImageFile(int retentionDays)
        {
            string rootSearchPath = ParameterPath.SaveImagePath;
            if (!Directory.Exists(rootSearchPath))
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange, $"图片根目录不存在，跳过清理：{rootSearchPath}");
                return;
            }

            // 关键步骤日志：开始查找OK文件夹
            RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Blue, $"开始清理图片：在 '{rootSearchPath}' 中查找所有OK文件夹");

            try
            {
                // 递归查找所有OK子目录
                var okDirectories = Directory.EnumerateDirectories(
                    rootSearchPath,
                    "OK",
                    SearchOption.AllDirectories)
                    .ToList();

                if (!okDirectories.Any())
                {
                    RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Green, "未找到任何OK文件夹，无需清理");
                    return;
                }

                // 统计总清理结果
                int totalDeleted = 0;
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Cyan, $"找到 {okDirectories.Count} 个OK文件夹，开始批量清理");

                // 遍历处理每个OK文件夹（不输出单个文件夹开始日志）
                foreach (var okDirPath in okDirectories)
                {
                    totalDeleted += DeleteExpiredFilesInDirectory(okDirPath, retentionDays);
                }

                // 最终汇总日志
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Green,
                    $"图片清理完成：共处理 {okDirectories.Count} 个OK文件夹，累计删除 {totalDeleted} 个过期文件");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex, "图片清理过程发生严重错误");
            }
        }

        #endregion

        #region 定期备份与清理

        // 定义定时器（全局变量，避免被回收）
        private System.Timers.Timer _backupTimer;

        /// <summary>
        /// 初始化备份定时器（在程序启动时调用）<br/>
        /// 定期执行程序备份和过期数据清理
        /// </summary>
        /// <returns>true：初始化成功；false：失败</returns>
        private bool InitBackupTimer()
        {
            try
            {
                _backupTimer = new System.Timers.Timer();
                // 设置间隔为12小时（12*60*60*1000毫秒），可根据需求调整
                _backupTimer.Interval = 12 * 3600 * 1000;
                // 定时器触发时执行备份和清理
                _backupTimer.Elapsed += (sender, e) =>
                {
                    // 执行程序备份和过期备份清理
                    RegularClear(BackupRetentionDays);
                    // 执行图片清理（假设保留7天，可根据需求调整）
                    DeleteImageFile(ImageRetentionDays);
                };
                // 启动时立即执行一次备份和清理
                _backupTimer.AutoReset = true; // 自动重复触发
                _backupTimer.Enabled = true;
                RegularClear(BackupRetentionDays);
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Green, "备份与清理定时器初始化成功。");
                return true;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex, "备份与清理定时器初始化失败。");
                return false;
            }
        }

        /// <summary>
        /// 定期清理过期备份文件并创建新备份<br/>
        /// 优化：兼容多种备份目录格式（yyyyMMdd、yyyy-MM-dd、yyyyMMdd_HHmmss、yyyy-MM-dd_HHmmss）<br/>
        /// 确保至少保留指定数量的备份，避免误删
        /// </summary>
        /// <param name="retentionDays">基准保留天数（默认3天）</param>
        /// <param name="minRetentionCount">最小保留数量（默认3个）</param>
        /// <returns>true：清理成功；false：失败</returns>
        private bool RegularClear(int retentionDays = 3, int minRetentionCount = 3)
        {
            try
            {
                // 更新进度（根据你的UI框架调整）
                UpdateStep(50, Project.Instance.SystemParm.Language == LanguageType.English ? "Program backup" : "程序备份", true);

                // 检查是否启用自动备份
                if (!Project.Instance.SystemParm.AutoBackupProgram)
                {
                    RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Blue, "自动备份已禁用，跳过备份操作。");
                    return true;
                }

                // 备份根目录路径
                string backupRoot = Path.Combine(Project.Instance.ParameterPath.DataPath, "Config", "Kim");
                // 防止在备份目录中运行程序导致死循环
                if (Application.StartupPath.StartsWith(backupRoot))
                {
                    Frm_MessageBox.Instance.MessageBoxShow("Do not run this backup program directly, please copy to another path and run again");
                    Process.GetCurrentProcess().Kill();
                }

                // 1. 创建带时间戳的备份目录（支持一天内多次备份，不覆盖）
                string timeDir = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupPath = Path.Combine(backupRoot, timeDir);
                if (Directory.Exists(backupPath))
                    Directory.Delete(backupPath, true);
                Directory.CreateDirectory(backupPath);
                // 复制程序目录下的所有文件到备份目录（根据你的复制逻辑调整）
                Frm_Main.Instance.CopyFiles(Application.StartupPath, backupPath);
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Green, $"已创建程序备份：{backupPath}");

                // 2. 获取所有有效的备份目录并按创建时间排序
                var allBackupDirs = Directory.GetDirectories(backupRoot)
                    .Select(path => new DirectoryInfo(path))
                    .Where(dir => TryParseBackupDate(dir.Name, out _)) // 过滤有效的备份目录
                    .OrderBy(dir => GetBackupDateTime(dir.Name)) // 按备份时间升序排列（最新的在最后）
                    .ToList();

                if (!allBackupDirs.Any())
                {
                    RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Green, "无备份目录，无需清理过期备份。");
                    return true;
                }

                // 3. 计算需要保留的备份目录：保留期内的所有备份 + 至少保留指定数量的备份
                DateTime now = DateTime.Now;
                var keepDirs = new HashSet<DirectoryInfo>();

                // 先保留保留期内的所有备份
                foreach (var dir in allBackupDirs)
                {
                    if (GetBackupDateTime(dir.Name) >= now.AddDays(-retentionDays))
                    {
                        keepDirs.Add(dir);
                    }
                }

                // 若保留期内的备份不足指定数量，则补充保留最近的备份
                if (keepDirs.Count < minRetentionCount)
                {
                    int needAdd = minRetentionCount - keepDirs.Count;
                    var addDirs = allBackupDirs
                        .Except(keepDirs)
                        .OrderByDescending(dir => GetBackupDateTime(dir.Name)) // 从最新的开始取
                        .Take(needAdd);
                    foreach (var dir in addDirs)
                    {
                        keepDirs.Add(dir);
                    }
                }

                // 4. 删除不需要保留的备份目录
                foreach (var dir in allBackupDirs)
                {
                    if (!keepDirs.Contains(dir))
                    {
                        DeleteDirectoryRecursively(dir);
                        RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Green,
                            $"已删除过期备份目录：{dir.Name}（超出保留天数/数量限制）");
                    }
                }

                UpdateStep(100, Project.Instance.SystemParm.Language == LanguageType.English ? "Backup completed" : "备份完成", true);
                return true;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex, "定期备份与清理操作失败。");
                UpdateStep(100, Project.Instance.SystemParm.Language == LanguageType.English ? "Backup failed" : "备份失败", false);
                return false;
            }
        }

        /// <summary>
        /// 辅助方法：解析备份目录名中的日期时间（兼容4种格式）<br/>
        /// 1. yyyyMMdd_HHmmss（如20251102_153045）<br/>
        /// 2. yyyy-MM-dd_HHmmss（如2024-05-01_153045）<br/>
        /// 3. yyyyMMdd（如20251102）<br/>
        /// 4. yyyy-MM-dd（如2024-05-01）
        /// </summary>
        /// <param name="dirName">备份目录名称</param>
        /// <param name="dateTime">解析出的日期时间</param>
        /// <returns>true：解析成功；false：解析失败</returns>
        private bool TryParseBackupDate(string dirName, out DateTime dateTime)
        {
            // 1. 尝试解析无分隔符带时间（yyyyMMdd_HHmmss）
            if (DateTime.TryParseExact(
                dirName,
                "yyyyMMdd_HHmmss",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out dateTime))
            {
                return true;
            }

            // 2. 尝试解析带分隔符带时间（yyyy-MM-dd_HHmmss）
            if (DateTime.TryParseExact(
                dirName,
                "yyyy-MM-dd_HHmmss",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out dateTime))
            {
                return true;
            }

            // 3. 尝试解析无分隔符仅日期（yyyyMMdd）
            if (DateTime.TryParseExact(
                dirName,
                "yyyyMMdd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out dateTime))
            {
                return true;
            }

            // 4. 尝试解析带分隔符仅日期（yyyy-MM-dd）
            if (DateTime.TryParseExact(
                dirName,
                "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out dateTime))
            {
                return true;
            }

            // 所有格式均解析失败
            dateTime = DateTime.MinValue;
            return false;
        }

        /// <summary>
        /// 辅助方法：获取备份目录的日期时间（用于排序和保留期判断）
        /// </summary>
        /// <param name="dirName">备份目录名称</param>
        /// <returns>解析出的日期时间（解析失败返回DateTime.MinValue）</returns>
        private DateTime GetBackupDateTime(string dirName)
        {
            if (TryParseBackupDate(dirName, out DateTime dateTime))
                return dateTime;
            // 无法解析的目录视为极早的时间（会被优先清理）
            return DateTime.MinValue;
        }

        #endregion

        #region 辅助方法（初始化流程控制与资源管理）
        /// <summary>
        /// 更新初始化进度并执行检查操作（封装进度更新+结果校验的逻辑）<br/>
        /// 流程：先更新为"进行中"状态→执行检查操作→根据结果更新最终状态
        /// </summary>
        /// <param name="percent">进度百分比（0-100）</param>
        /// <param name="message">当前步骤描述（如"检查配置文件"）</param>
        /// <param name="checkAction">检查/执行方法（返回true表示成功，false表示失败）</param>
        private void UpdateStepWithCheck(int percent, string message, Func<bool> checkAction)
        {
            UpdateStep(percent, message, true); // 先更新为"进行中"状态（临时标记为成功）
            bool success = checkAction.Invoke(); // 执行检查操作（如加载配置、初始化模块）
            UpdateStep(percent, message, success); // 根据结果更新最终状态（成功/失败）
        }

        /// <summary>
        /// 更新初始化进度（反馈给启动界面）<br/>
        /// 功能：
        /// 1. 步骤失败时，触发资源释放并退出程序
        /// 2. 步骤成功时，更新启动界面的进度条和描述文字
        /// 3. 支持多语言显示（根据系统语言设置切换中英文）
        /// </summary>
        /// <param name="percentValue">进度百分比（0-100）</param>
        /// <param name="stepMsg">步骤描述（中文）</param>
        /// <param name="succeed">步骤是否成功（true=成功，false=失败）</param>
        private void UpdateStep(int percentValue, string stepMsg, bool succeed)
        {
            try
            {
                // 步骤失败时，关闭资源并退出程序（初始化终止）
                if (!succeed)
                {
                    Close();
                    return;
                }

                // 多语言转换（根据系统设置显示中英文，提升国际化支持）
                string displayMsg = Project.Instance.SystemParm.Language == LanguageType.English
                    ? TranslateToEnglish(stepMsg)
                    : stepMsg;

                // 通知启动界面更新进度（Frm_Welcome是启动时的进度展示窗口）
                Frm_Welcome.Instance.UpdateMsg(percentValue, displayMsg);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex); // 记录进度更新失败的异常（不中断初始化流程）
            }
        }

        /// <summary>
        /// 关闭所有资源并退出程序（初始化失败时调用）<br/>
        /// 目的：确保程序退出时释放所有硬件/网络资源，避免资源泄漏<br/>
        /// 步骤：按资源依赖逆序释放（先断开外部设备，再关闭内部模块）
        /// </summary>
        private void Close()
        {
            try
            {
                // 相机由 JVS workspace 相机工具算子驱动，无 Cognex 硬件相机连接（VisionPro 已彻底移除）

                // 停止图像保存线程（避免线程持续运行导致的资源占用）
                //VisionLib.Instance.SaveImgThread_Stop();

                // 更新系统状态为"紧急停止"（通知其他模块程序异常退出）
                Project.Instance.GlobalVariables.IsProgramExiting = true;
                Controller.Instance.UpdateMachineRunStatus(MachineRunStatus.EmergencyStopped);

                // 关闭运动控制器（释放轴卡、电机等运动资源）
                var motionCards = MotionSystem.Instance.MotionCards;
                foreach (var card in motionCards)
                {
                    MotionSystem.Instance.MotionController.Close(card);
                }

                // 重置LED和蜂鸣器（关闭报警状态，避免设备持续报警）
                SysParm.Instance.BuzzerLed.SetBuzzerLedStatus(BuzzerLED.LedColor.Undefined);

                // 关闭所有Modbus连接（PLC、机器人等通讯资源）
                CloseModbusConnections();

                // 关闭所有TCP连接（服务器和客户端，释放网络资源）
                CloseTcpConnections();

                // 强制退出进程（确保所有资源释放，避免残留进程）
                Process.GetCurrentProcess().Kill();
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex); // 记录资源释放失败的异常（尽力释放后仍强制退出）
            }
        }

        /// <summary>
        /// 刷新系统参数（同步各模块的最新状态）<br/>
        /// 目的：确保初始化过程中各模块参数的一致性，避免因缓存导致的状态偏差<br/>
        /// 同步范围：IO、Modbus、视觉、TCPIP、运动、串口等核心模块
        /// </summary>
        /// <returns>true：所有模块刷新成功；false：至少一个模块刷新失败</returns>
        /// <summary>
        /// 刷新系统参数（同步各模块的最新状态）<br/>
        /// 目的：确保初始化过程中各模块参数的一致性，避免因缓存导致的状态偏差<br/>
        /// 同步范围：IO、Modbus、视觉、TCPIP、运动、串口等核心模块
        /// </summary>
        /// <returns>true：所有模块刷新成功；false：至少一个模块刷新失败</returns>
        private async Task<bool> RefreshParamAsync()
        {
            // 记录各模块刷新结果
            var refreshResults = new Dictionary<string, bool>();

            try
            {
                // 异步并行刷新所有模块，提高效率
                await Task.WhenAll(
                    RefreshModuleAsync("IO模块", () => IOSystem.Instance.RefreshDataAsync(), refreshResults),
                    RefreshModuleAsync("Modbus设备", () => ModbusSystem.Instance.RefreshDataAsync(), refreshResults),
                    RefreshModuleAsync("视觉系统", () => VisionSystem.Instance.RefreshDataAsync(), refreshResults),
                    RefreshModuleAsync("TCPIP通讯", () => TcpIpSystem.Instance.RefreshDataAsync(), refreshResults),
                    RefreshModuleAsync("运动系统", () => MotionSystem.Instance.RefreshDataAsync(), refreshResults),
                    RefreshModuleAsync("串口设备", () => SerialportSystem.Instance.RefreshDataAsync(), refreshResults)
                );

                // 检查是否所有模块都刷新成功
                return refreshResults.Values.All(success => success);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return false;
            }
        }

        /// <summary>
        /// 刷新单个模块数据并记录结果
        /// </summary>
        /// <param name="moduleName">模块名称</param>
        /// <param name="refreshAction">刷新操作</param>
        /// <param name="results">结果字典</param>
        private async Task RefreshModuleAsync(string moduleName, Func<Task> refreshAction, Dictionary<string, bool> results)
        {
            try
            {
                await refreshAction();
                results[moduleName] = true;
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Green, $"{moduleName}数据刷新成功");
            }
            catch (Exception ex)
            {
                results[moduleName] = false;
                RLog.SaveError(ex);
            }
        }
        /// <summary>
        /// 初始化配置目录（创建程序运行必需的目录结构）<br/>
        /// 确保程序启动时所需的配置、日志、数据存储目录已存在，避免文件操作失败<br/>
        /// 包含目录：配置目录（项目/运动/视觉）、日志目录（通讯/操作/错误）、数据存储目录（生产数据/图片）
        /// </summary>
        /// <returns>true：创建成功；false：失败</returns>
        private bool InitConfigDirectory()
        {
            try
            {
                // 主配置目录（项目配置、运动参数、视觉任务等核心配置）
                CreateDirectoryIfNotExists(Path.Combine(Application.StartupPath, "Config", "Project"));
                CreateDirectoryIfNotExists(Path.Combine(Application.StartupPath, "Config", "Project", "Motion"));
                CreateDirectoryIfNotExists(Path.Combine(Application.StartupPath, "Config", "Project", "Vision", "Job"));
                CreateDirectoryIfNotExists(Path.Combine(Application.StartupPath, "Config", "Resources"));

                // 日志目录（分类存储不同类型的日志，便于问题排查）
                string DataPath = Project.Instance.ParameterPath.DataPath;
                CreateDirectoryIfNotExists(Path.Combine(DataPath, "Config", "Log", "Comm"));       // 通讯日志（Modbus/TCP等）
                CreateDirectoryIfNotExists(Path.Combine(DataPath, "Config", "Log", "Operation"));    // 操作日志（用户操作记录）
                CreateDirectoryIfNotExists(Path.Combine(DataPath, "Config", "Log", "Error"));      // 错误日志（异常记录）
                CreateDirectoryIfNotExists(Path.Combine(DataPath, "Config", "Log", "Accord"));     // 协议日志（自定义通讯协议）

                // 数据存储目录（生产数据、统计信息、检测图片等业务数据）
                CreateDirectoryIfNotExists(Path.Combine(DataPath, "Config", "Data", "生产统计"));  // 生产数量、良率等统计
                CreateDirectoryIfNotExists(Path.Combine(DataPath, "Config", "Data", "生产数据"));  // 每个产品的详细检测数据
                CreateDirectoryIfNotExists(Path.Combine(DataPath, "Config", "Data", "CT统计"));    // 周期时间统计（生产效率分析）
                CreateDirectoryIfNotExists(ParameterPath.SaveImagePath);                           // 检测图片存储（用于追溯）

                return true;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return false;
            }
        }

        /// <summary>
        /// 初始化主窗体（设置界面状态、预加载页面）<br/>
        /// 确保界面组件在程序启动后可正常使用，避免用户操作时的UI异常<br/>
        /// 操作：最大化窗口、初始化状态栏信息、预加载功能页面（避免首次点击时的延迟）
        /// </summary>
        /// <returns>true：初始化成功；false：失败</returns>
        public bool InitializationForm()
        {
            try
            {
                // 在主线程更新UI（使用WFUtil确保跨线程操作安全，避免UI线程异常）
                WFUtil.UpdateMainThreadUIAsync(Frm_Main.Instance, () =>
                {
                    Frm_Main.Instance.WindowState = FormWindowState.Maximized; // 最大化主窗口（优化用户体验）
                    Frm_Main.Instance.tss_PermissionInfo.Text = "当前用户-未登录"; // 初始化权限信息（默认未登录）
                    Frm_Main.Instance.lbl_CurEngine.Text = $"当前方案：捷普科技视觉框架"; // 显示当前方案信息

                    // 预加载页面（初始化各功能页实例，避免首次访问时的加载延迟）
                    var pages = new object[]
                    {
                    ProcessMonitorPage.Instance,   // 流程监控页（实时显示生产状态）
                    PermissionPage.Instance,       // 权限管理页（用户/角色管理）
                    VisionPage.Instance,           // 视觉配置页（相机、算法参数设置）
                    ProductDataPage.Instance,      // 产品数据页（产品参数、检测标准）
                    ManualPage.Instance,           // 手动操作页（手动控制设备）
                    AlarmPage.Instance             // 报警信息页（显示历史/当前报警）
                    };
                });
                return true;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return false;
            }
        }

        /// <summary>
        /// 初始化软件参数（业务相关的配置参数）<br/>
        /// 可扩展添加具体参数初始化逻辑（如生产参数、工艺参数等）<br/>
        /// 当前为框架，实际项目中需根据业务需求填充内容
        /// </summary>
        /// <returns>true：初始化成功；false：失败</returns>
        private bool InitializeParameter()
        {
            try
            {
                // 此处可添加实际业务参数初始化逻辑（示例：加载生产计划、工艺参数等）
                return true;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return false;
            }
        }

        /// <summary>
        /// 初始化程序所需的后台线程（确保各模块异步运行）<br/>
        /// 启动支撑程序运行的核心线程（如视觉处理、IO监控、运动控制等）<br/>
        /// 线程启动后，各模块可独立处理实时任务，提升程序响应速度
        /// </summary>
        /// <returns>true：线程启动成功；false：失败</returns>
        public bool InitializeThreads()
        {
            try
            {
                SysParm.Instance.StartThreads(); // 调用系统参数实例的线程启动方法（封装线程启动逻辑）
                return true;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return false;
            }
        }

        /// <summary>
        /// 预初始化（创建核心系统单例，建立基础依赖）<br/>
        /// 按依赖顺序初始化关键系统，确保后续模块可正常引用（避免循环引用）<br/>
        /// 初始化顺序：项目实例→基础系统（视觉/IO/通讯）→权限系统→核心信息绑定
        /// </summary>
        private void PreInit()
        {
            // 初始化核心系统单例（按依赖顺序，避免循环引用导致的实例未就绪）
            _ = Project.Instance;           // 项目全局实例（最核心的单例，存储全局状态）
            _ = VisionSystem.Instance;      // 视觉系统实例（相机、视觉算法管理）
            _ = IOSystem.Instance;          // IO系统实例（数字量输入输出管理）
            _ = TcpIpSystem.Instance;       // TCPIP通讯系统实例（网络通讯管理）
            _ = ModbusSystem.Instance;      // Modbus通讯系统实例（Modbus设备管理）
            _ = MotionSystem.Instance;      // 运动系统实例（轴卡、电机控制）
            _ = SerialportSystem.Instance;  // 串口通讯系统实例（串口设备管理）
            //_ = SysParm.Instance;           // 系统参数实例（注释：按需启用，避免过早初始化）

            // 初始化权限系统（设置权限过期时间，增强系统安全性）
            PermissionSystem.Instance.AlloSeconds = PermissionExpireSeconds;

            // 初始化项目核心信息（绑定控制器和工位信息，建立业务基础）
            Project.Instance.Controller = Controller.Instance; // 绑定控制器（核心控制逻辑入口）
            Project.Instance.StationInfo = new JStationInfo(); // 初始化工位信息（存储工位状态）
        }


        #endregion

        #region 私有工具方法（辅助初始化过程）
        /// <summary>
        /// 若目录不存在则创建（确保初始化过程中目录可用）<br/>
        /// 封装目录创建逻辑，避免重复代码，确保文件操作有可用的目录
        /// </summary>
        /// <param name="path">目录路径</param>
        private void CreateDirectoryIfNotExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        /// <summary>
        /// 移除文件/目录的只读/隐藏属性（确保删除操作可执行）<br/>
        /// 某些系统或用户操作可能会设置文件为只读/隐藏，导致删除失败，此处统一处理
        /// </summary>
        /// <param name="attributes">文件/目录属性</param>
        /// <returns>移除只读/隐藏属性后的属性</returns>
        private FileAttributes RemoveReadOnlyAttribute(FileAttributes attributes)
        {
            return attributes & ~(FileAttributes.Archive | FileAttributes.ReadOnly | FileAttributes.Hidden);
        }

        /// <summary>
        /// 关闭所有Modbus连接（PLC和机器人）<br/>
        /// 确保程序退出时释放Modbus通讯资源，避免连接残留导致的设备占用
        /// </summary>
        private void CloseModbusConnections()
        {
            // 关闭PLC Modbus连接（遍历所有PLC实例，执行断开操作）
            foreach (var modbus in ModbusSystem.Instance.PlcModbusList)
            {
                modbus.Disconnect();
            }

            // 关闭Robot Modbus连接（遍历所有机器人实例，执行断开操作）
            foreach (var modbus in ModbusSystem.Instance.RobotModbusList)
            {
                modbus.Disconnect();
            }
        }

        /// <summary>
        /// 关闭所有TCP连接（服务器和客户端）<br/>
        /// 确保程序退出时释放网络资源，避免端口占用导致的下次启动失败
        /// </summary>
        private void CloseTcpConnections()
        {
            // 关闭所有TCP服务器（先关闭客户端连接，再关闭服务器，避免资源泄漏）
            foreach (var server in TcpIpSystem.Instance.TcpServers)
            {
                server.CloseAllClients(); // 先断开所有客户端
                server.StopServer(); // 再关闭服务器本身
            }

            // 关闭所有TCP客户端（断开与服务器的连接）
            foreach (var client in TcpIpSystem.Instance.TcpClients)
            {
                client.Close();
            }
        }

        /// <summary>
        /// 简单的中文到英文翻译（支持初始化进度的多语言显示）<br/>
        /// 提供初始化步骤描述的英文翻译，适配英文系统环境<br/>
        /// 可根据实际需求扩展翻译词典（添加更多关键词）
        /// </summary>
        /// <param name="chinese">中文描述</param>
        /// <returns>英文翻译（未找到时返回原中文）</returns>
        private string TranslateToEnglish(string chinese)
        {
            var translations = new Dictionary<string, string>
        {
            {"初始化", "Initialization"},
            {"检查各配置文件是否存在", "Check configuration files"},
            {"尝试加载配置路径文件", "Load configuration path"},
            {"尝试加载系统设置参数", "Load system parameters"},
            {"尝试加载视觉配置参数", "Load vision parameters"},
            {"尝试加载项目相关点表信息", "Loading point table information"},
            {"尝试运动控制模块加载", "Load motion control module"},
            {"尝试加载IO板卡", "Load IO board"},
            {"尝试加载Modbus通讯设备", "Load Modbus devices"},
            {"轴卡使能初始化", "Initialize axis servo"},
            {"尝试加载2D检测程序", "Load 2D inspection program"},
            {"定期备份清理备份程序，请耐心等待", "Regular backup and cleanup, please wait"},
            {"尝试初始化软件参数", "Initialize software parameters"},
            {"启动成功", "Startup successful"}
        };

            return translations.TryGetValue(chinese, out string english) ? english : chinese;
        }
        #endregion
    }
}
