using Cognex.VisionPro.FGGigE;
using Cognex.VisionPro.FGGigE.Implementation.Internal;
using Gma.UserActivityMonitor;
using Jabil.Lib;
using Jabil.Lib.Parameter;
using Jabil.Lib.Utils;
using Jabil.Module.Log.Frm;
using Jabil.Module.Log.Lib;
using Jabil.Module.Modbus;
using Jabil.Module.Modbus.Lib;
using Jabil.Module.Motion;
using Jabil.Module.Motion.Frm;
using Jabil.Module.Permission;
using Jabil.Module.Permission.Forms;
using Jabil.Module.Permission.View;
using Jabil.Module.ProductData.Lib;
using Jabil.Module.TCPIP;
using Jabil.Module.TCPIP.Frm;
using Jabil.Module.TCPIP.Lib;
using Jabil.Module.Setting.Lib.JVS;
using Jabil.Module.Vision.Frm;
using JabilVisionMotion.Edit;
using JabilVisionMotion.Edit.Process;
using JabilVisionMotion.Edit.Process.IOControlProcess;
using JabilVisionMotion.Edit.Process.ModbusControlProcess;
using JabilVisionMotion.Frm.Dialog;
using JabilVisionMotion.Frm.UserPage;
using JabilVisionMotion.Lib;
using Microsoft.Win32;
using Sunny.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;



namespace JabilVisionMotion.Frm
{
    /// <summary>
    /// 应用程序主窗体类<br/>
    /// 继承自UIForm2，作为设备控制软件的核心界面，负责统筹视图管理、系统监控、设备交互及用户操作响应<br/>
    /// 采用单例模式确保全局唯一实例，通过模块化设计整合初始化、资源管理、性能监控等功能
    /// </summary>
    public partial class Frm_Main : UIForm2
    {
        #region 单例模式（线程安全）
        /// <summary>
        /// 线程安全的单例实例<br/>
        /// 使用Lazy<T>实现延迟初始化，确保多线程环境下仅创建一个实例，避免并发冲突
        /// </summary>
        private static readonly Lazy<Frm_Main> _instance = new Lazy<Frm_Main>(() => new Frm_Main(), LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// 全局访问点，获取主窗体单例实例
        /// </summary>
        public static Frm_Main Instance => _instance.Value;

        /// <summary>
        /// 私有构造函数（单例模式核心）<br/>
        /// 防止外部直接实例化，在内部完成初始化准备（资源加载、控件样式设置、钩子注册等）
        /// </summary>
        private Frm_Main()
        {
            try
            {
                EnsureApplicationResources(); // 确保WPF资源加载完成
                InitializeComponent();       // 初始化Windows窗体控件

                // 启用双缓冲和优化重绘，减少UI闪烁，提升渲染性能
                SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
#if !DEBUG
                HookManager.MouseMove+=HookManagee_MouseMove;
#endif

                _operationLogger = new ControlOperationLogger();

            }
            catch (Exception ex)
            {
                RLog.SaveError(ex); // 记录初始化异常日志
            }
        }
        #endregion

        #region 常量定义（替代魔法值）
        /// <summary>Windows系统开机自启动项在注册表中的路径</summary>
        private const string AutoStartRegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        /// <summary>应用程序名称（用于注册表项命名和日志标识）</summary>
        private const string ApplicationName = "应用名称";
        /// <summary>默认监控的硬盘盘符（用于空间检测）</summary>
        private const string DiskName = "D";
        /// <summary>截图保存目录格式（按日期分目录，{0}为日期占位符）</summary>
        private const string ScreenshotDirectoryFormat = "Config\\Log\\ScreenShot\\{0}";
        /// <summary>截图文件命名格式（含时间戳，{0}为DateTime占位符）</summary>
        private const string ScreenshotFileNameFormat = "Jabil_{0:yyyy-MM-dd-HH'h'mm'm'ss's'}.jpg";
        #endregion

        #region 字段（修正命名规范）
        /// <summary>
        /// 虚拟键盘进程对象<br/>
        /// 用于管理系统自带虚拟键盘（osk.exe）的启动与关闭
        /// </summary>
        public Process KeyboardProcess { get; private set; }

        /// <summary>
        /// 窗体锁定状态标识<br/>
        /// true表示窗体已锁定（需密码解锁），false表示正常可操作状态
        /// </summary>
        public static bool IsLocked = false;

        /// <summary>工位控制器接口实例（依赖注入，实际指向Controller单例，用于设备控制）</summary>
        private IStationController _stationController;
        /// <summary>上一次CPU采样UTC时间（用于时间差法计算CPU使用率，不依赖PDH性能计数器）</summary>
        private DateTime _lastCpuSampleUtc;
        /// <summary>上一次CPU累计运行时间（用于时间差法计算CPU使用率）</summary>
        private TimeSpan _lastCpuTotalTime;
        /// <summary>窗体自适应大小工具（用于调整控件尺寸适配不同分辨率）</summary>
        private AutoSizeForm _autoSizeForm = new AutoSizeForm();
        /// <summary>
        /// 实例化报警窗体
        /// </summary>
        public Frm_AlarmWindow frm_AlarmWindow = new Frm_AlarmWindow();

        private ControlOperationLogger _operationLogger;

        #endregion

        #region 初始化与资源管理
        /// <summary>
        /// 确保应用程序WPF资源加载<br/>
        /// 初始化WPF应用实例并加载权限相关资源字典，避免WPF控件资源引用异常
        /// </summary>
        private void EnsureApplicationResources()
        {
            try
            {
                // 若WPF应用实例未初始化，则创建并加载权限资源
                if (System.Windows.Application.Current == null)
                {
                    var app = new System.Windows.Application();
                    app.ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown; // 手动控制关闭时机
                    app.Resources.MergedDictionaries.Add(
                        System.Windows.Application.LoadComponent(new Uri("Jabil.Module.Permission;component/Permission.xaml", UriKind.Relative)) as System.Windows.ResourceDictionary);
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex); // 记录资源加载异常
            }
        }

        /// <summary>
        /// 集中订阅事件（便于后期统一取消，减少内存泄漏风险）<br/>
        /// 包括菜单视图切换、日志刷新、属性变更及登录用户变更等事件
        /// </summary>
        private void SubscribeEvents()
        {
            MenuProxy.OnOpenView += MenuProxy_OnOpenView; // 菜单视图切换事件
            RLog.OperationLogRefreshed += Rlog_DataChanged; // 操作日志刷新事件
            MenuProxy.Instance.PropertyChanged += Instance_PropertyChanged; // 菜单属性变更事件
            PermissionSystem.Instance.OnLoginUserChanged += Instance_OnLoginUserChanged; // 登录用户变更事件
        }

        /// <summary>
        /// 取消所有事件订阅（窗体关闭时调用，防止因委托引用导致的内存泄漏）
        /// </summary>
        private void UnsubscribeEvents()
        {
            MenuProxy.OnOpenView -= MenuProxy_OnOpenView;
            RLog.OperationLogRefreshed -= Rlog_DataChanged;
            MenuProxy.Instance.PropertyChanged -= Instance_PropertyChanged;
            PermissionSystem.Instance.OnLoginUserChanged -= Instance_OnLoginUserChanged;
            HookManager.MouseMove -= HookManager_MouseMove; // 移除鼠标移动钩子
        }
        #endregion

        #region 核心功能方法
        /// <summary>
        /// 初始化语言设置<br/>
        /// 根据系统参数配置界面语言（当前仅实现英文场景下的权限信息显示）
        /// </summary>
        private void InitLanguage()
        {
            try
            {
                if (Project.Instance.SystemParm.Language == LanguageType.English)
                {
                    tss_PermissionInfo.Text = "Current login: not logged in, default is minimum permission";
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 保存所有系统配置<br/>
        /// 统一保存参数路径、系统参数及视觉参数等核心配置，确保退出前配置生效
        /// </summary>
        public void SaveAllConfigurations()
        {
            try
            {
                // 保存参数路径配置
                Project.Instance.SaveSystemConfig(Project.Instance.ParameterPath, ParameterPath.ParameterPathPath);
                // 保存系统参数配置
                Project.Instance.SaveSystemConfig(Project.Instance.SystemParm, ParameterPath.SystemParmPath);
                // 保存视觉参数配置
                Project.Instance.SaveSystemConfig(Project.Instance.visionParm, ParameterPath.VisionParmPath);
                Application.DoEvents(); // 处理未完成的UI事件，确保保存操作同步完成
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 设置应用程序开机自启动<br/>
        /// 通过修改Windows注册表的启动项，实现应用开机自动运行或关闭该功能
        /// </summary>
        /// <param name="isAuto">是否启用开机自启动（true：启用；false：禁用）</param>
        public void SetAutoStart(bool isAuto)
        {
            try
            {
                // 打开注册表的开机启动项节点（需管理员权限）
                using (var localKey = Registry.LocalMachine)
                using (var runKey = localKey.CreateSubKey(AutoStartRegistryPath))
                {
                    if (isAuto)
                    {
                        // 启用自启动：添加应用程序路径到注册表
                        runKey.SetValue(ApplicationName, Application.ExecutablePath);
                        ShowAutoStartMessage(true);
                    }
                    else
                    {
                        // 禁用自启动：从注册表删除应用程序项（删除不存在的项时不抛异常）
                        runKey.DeleteValue(ApplicationName, false);
                        ShowAutoStartMessage(false);
                    }
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                // 提示用户操作失败原因（通常是缺少管理员权限）
                Frm_MessageBox.Instance.MessageBoxShow("\r\n开机程序自启动设置失败，请以管理员权限运行此程序后重新尝试");
            }
        }

        /// <summary>
        /// 显示自启动设置结果消息<br/>
        /// 向用户反馈开机自启动设置成功或失败，并自动关闭消息框
        /// </summary>
        /// <param name="success">设置是否成功（true：成功；false：失败）</param>
        private void ShowAutoStartMessage(bool success)
        {
            var message = success
                ? "\r\n开机程序自启动成功"
                : "\r\n关闭开机程序自启动成功";

            Frm_MessageBox.Instance.MessageBoxShow(message);
            Thread.Sleep(3000); // 显示3秒后自动关闭
            Frm_MessageBox.Instance.Close();
        }

        /// <summary>
        /// 拷贝目录下的所有文件到目标目录（含子目录递归处理）<br/>
        /// 支持路径前缀替换，用于配置文件备份、日志迁移等场景
        /// </summary>
        /// <param name="sourceFolder">源目录路径</param>
        /// <param name="targetFolder">目标目录路径</param>
        /// <param name="removePrefix">需要从源路径中移除的前缀（用于调整目标路径结构，默认与源目录相同）</param>
        public void CopyFiles(string sourceFolder, string targetFolder, string removePrefix = "")
        {
            try
            {
                if (string.IsNullOrEmpty(removePrefix))
                    removePrefix = sourceFolder; // 默认为源目录路径

                Directory.CreateDirectory(targetFolder); // 确保目标目录存在

                var directory = new DirectoryInfo(sourceFolder);

                // 拷贝所有文件（排除系统自动生成的缩略图缓存文件Thumbs.db）
                foreach (var file in directory.GetFiles().Where(f => f.Name != "Thumbs.db"))
                {
                    // 构建目标路径（移除前缀后拼接，避免路径格式问题）
                    var tempPath = Path.Combine(targetFolder, file.FullName.Replace(removePrefix, "").TrimStart(Path.DirectorySeparatorChar));
                    Directory.CreateDirectory(Path.GetDirectoryName(tempPath)); // 确保目标子目录存在
                    File.Delete(tempPath); // 若目标文件已存在则先删除
                    file.CopyTo(tempPath, true); // 覆盖拷贝文件
                }

                // 递归处理所有子目录
                foreach (var subDir in directory.GetDirectories())
                {
                    CopyFiles(subDir.FullName, targetFolder, removePrefix);
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 切换窗体视图（提取重复逻辑，统一视图切换流程）<br/>
        /// 根据目标视图模式清理当前视图，加载新视图并调整UI布局
        /// </summary>
        /// <param name="FormPage">目标视图模式（如主页、生产统计、流程监控等）</param>
        private void SwitchForm(FormPage FormPage)
        {
            try
            {
                if (SysParm.Instance.CurrentFormMode == FormPage)
                    return; // 若已是目标视图则无需处理

                SysParm.Instance.CurrentFormMode = FormPage; // 更新当前视图模式
                var targetForm = GetFormByMode(FormPage); // 获取目标视图对应的窗体实例
                if (targetForm == null) return;

                // 统一的视图切换逻辑
                uiPanel_window.Visible = false; // 隐藏当前视图容器
                uiPanel_window.Controls.Clear(); // 清理容器内所有控件
                uiPanel_window.Dock = DockStyle.Fill; // 容器填充父窗体

                // 配置目标窗体属性（使其可嵌入容器）
                targetForm.TopLevel = false; // 非顶级窗体（可作为子控件嵌入）
                targetForm.Dock = DockStyle.Fill; // 填充容器
                targetForm.AutoScaleMode = AutoScaleMode.None; // 禁用自动缩放（避免布局错乱）
                targetForm.Show(); // 显示目标窗体

                // 将目标窗体和工具栏添加到容器
                uiPanel_window.Controls.Add(targetForm);
                toolStrip_工具栏.Visible = true;
                toolStrip_工具栏.Dock = DockStyle.Top; // 工具栏置顶
                uiPanel_window.Controls.Add(toolStrip_工具栏);

                uiPanel_window.Visible = true; // 显示新视图
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 根据FormPage枚举获取对应的窗体实例<br/>
        /// 采用switch表达式映射视图模式与窗体，简化视图切换逻辑
        /// </summary>
        /// <param name="mode">视图模式枚举</param>
        /// <returns>目标窗体实例（未匹配时返回null）</returns>
        private Form GetFormByMode(FormPage mode)
        {
            return mode switch
            {
                FormPage.MainPage => MainPage.Instance,           // 主页
                FormPage.ProductDataPage => ProductDataPage.Instance, // 生产统计页
                FormPage.ProcessMonitorPage => ProcessMonitorPage.Instance, // 流程监控页
                FormPage.VisionPage => VisionPage.Instance,       // 视觉配置页
                FormPage.ManualPage => ManualPage.Instance,       // 手动操作页
                FormPage.SettingPage => SettingPage.Instance,  // 系统设置页
                FormPage.AlarmPage => AlarmPage.Instance,         // 报警记录页
                FormPage.PermissionPage => PermissionPage.Instance, // 权限管理页
                FormPage.HelpPage => HelpPage.Instance,           // 帮助文档页
                _ => null
            };
        }

        /// <summary>
        /// 更新权限显示<br/>
        /// 根据当前登录用户的权限动态控制工具栏按钮的启用状态（示例逻辑，可扩展）
        /// </summary>
        private void UpdatePermission()
        {
            try
            {
                // 示例：根据权限控制按钮可用性
                // toolStripLabel_安全门抑制.Enabled = PermissionSystem.CheckPermission("主页/安全门抑制", out _);
                // toolStripLabel_蜂鸣器抑制.Enabled = PermissionSystem.CheckPermission("主页/蜂鸣器抑制", out _);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }
        #endregion

        #region 系统关键性能显示
        /// <summary>
        /// 系统性能监控相关功能
        /// 负责CPU、内存、磁盘空间等关键指标的采集与显示
        /// </summary>

        // 常量定义 - 集中管理配置参数
        private const string MONITORED_DISK = "D";          // 监控的磁盘
        private const int REFRESH_INTERVAL_MS = 3000;       // 刷新间隔(毫秒)——内存优化：原1000ms降为3000ms（泄漏速率随频率等比例下降）
        private const double DISK_FULL_THRESHOLD = 90.0;    // 磁盘空间不足阈值(%)

        /// <summary>
        /// 获取指定磁盘的可用空间(GB)和可用百分比
        /// </summary>
        /// <param name="diskName">磁盘名称(如"D")</param>
        /// <returns>返回数组[可用空间(GB), 可用百分比]</returns>
        private double[] GetDiskAvailableVolume(string diskName)
        {
            try
            {
                var driveInfo = new DriveInfo(diskName);

                // 计算可用空间(GB)和可用百分比
                double availableGB = driveInfo.AvailableFreeSpace / Math.Pow(1024, 3);
                double availablePercent = (double)driveInfo.AvailableFreeSpace / driveInfo.TotalSize;

                return new[] { availableGB, availablePercent };
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new[] { 0.0, 0.0 }; // 异常时返回默认值
            }
        }

        /// <summary>
        /// 程序启动时间
        /// </summary>
        public DateTime RunStartTime;

        /// <summary>
        /// 启动CPU、内存等性能监控（长时间运行场景保持开启）
        /// CPU/内存采集基于 Process 属性与时间差计算，不依赖 PerformanceCounter(PDH) 通道，
        /// 避免性能计数器组合环境下的内存持续累积
        /// </summary>
        public async void StartPerformanceMonitoring()
        {
            var currentProcess = Process.GetCurrentProcess();

            if (currentProcess == null) return;

            try
            {
                await Task.Run(async () =>
                {
                    while (true)
                    {
                        // 检查退出条件
                        if (ShouldStopMonitoring())
                            break;

                        // 加载中不更新数据
                        if (Initialization.IsLoading)
                        {
                            await Task.Delay(REFRESH_INTERVAL_MS);
                            continue;
                        }

                        // 采集并更新性能数据
                        await UpdatePerformanceDataAsync(currentProcess);

                        await Task.Delay(REFRESH_INTERVAL_MS);
                    }
                });
            }
            catch (Exception ex)
            {
                UpdateUIOnError();
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 检查是否应该停止监控
        /// </summary>
        private bool ShouldStopMonitoring()
        {
            return Project.Instance?.GlobalVariables?.IsProgramExiting ?? false;
        }

        /// <summary>
        /// 采集并更新性能数据到UI
        /// </summary>
        private async Task UpdatePerformanceDataAsync(Process currentProcess)
        {
            // 采集性能数据
            var (cpuUsage, memoryUsageMB, diskData) = CollectPerformanceMetrics(currentProcess);

            // 在UI线程更新显示
            await UpdateUIDisplayAsync(
                cpuUsage,
                memoryUsageMB,
                diskData,
                currentProcess);
        }

        /// <summary>
        /// 采集性能指标数据
        /// CPU使用率：基于 Process.TotalProcessorTime 相邻两次采样的差值计算（不依赖PDH性能计数器）
        /// 内存使用量：直接读取 Process.WorkingSet64
        /// </summary>
        private (double cpuUsage, double memoryUsageMB, double[] diskData) CollectPerformanceMetrics(Process currentProcess)
        {
            // CPU使用率(%)：采样间隔内累计CPU时间 / 间隔时长 / 核心数
            double cpuUsage = 0;
            DateTime nowUtc = DateTime.UtcNow;
            TimeSpan currentTotal = currentProcess.TotalProcessorTime;
            if (_lastCpuSampleUtc != default && nowUtc > _lastCpuSampleUtc)
            {
                double elapsedSeconds = (nowUtc - _lastCpuSampleUtc).TotalSeconds;
                double cpuDeltaSeconds = (currentTotal - _lastCpuTotalTime).TotalSeconds;
                if (elapsedSeconds > 0 && cpuDeltaSeconds >= 0)
                {
                    cpuUsage = cpuDeltaSeconds / elapsedSeconds / Environment.ProcessorCount * 100;
                }
            }
            _lastCpuSampleUtc = nowUtc;
            _lastCpuTotalTime = currentTotal;

            // 内存使用量(MB)
            double memoryUsageMB = currentProcess.WorkingSet64 / 1024.0 / 1024.0;

            // 获取磁盘数据
            double[] diskData = GetDiskAvailableVolume(MONITORED_DISK);

            return (cpuUsage, memoryUsageMB, diskData);
        }

        /// <summary>
        /// 在UI线程更新显示内容
        /// </summary>
        private Task UpdateUIDisplayAsync(
            double cpuUsage,
            double memoryUsageMB,
            double[] diskData,
            Process currentProcess)
        {
            return Task.Run(() =>
            {
                Invoke(new Action(() =>
                {
                    // 更新机器运行状态与稼动率
                    UpdateMachineStatusUtilization();

                    // 更新运行时间和当前时间
                    UpdateTimeDisplay();

                    // 更新CPU和内存显示
                    UpdateCpuMemoryDisplay(cpuUsage, memoryUsageMB);

                    // 更新磁盘空间显示
                    UpdateDiskSpaceDisplay(diskData);

                    // 更新当前用户信息
                    UpdateCurrentUserDisplay();
                }));
            });
        }

        /// <summary>
        /// 更新机器运行状态与稼动率
        /// </summary>
        private void UpdateMachineStatusUtilization()
        {
            switch (MenuProxy.Instance.MachineRunStatus)
            {
                case MachineRunStatus.Running:
                    EquipmentUtilizationManager.Instance.RecordStatus(EquipmentStatus.Running);
                    break;
                case MachineRunStatus.Alarming:
                    if (Controller.Instance.StationInfo.CachedMachineRunStatus == MachineRunStatus.Running)
                    {
                        EquipmentUtilizationManager.Instance.RecordStatus(EquipmentStatus.Abnormal);
                    }
                    break;
                case MachineRunStatus.EmergencyStopped:
                    EquipmentUtilizationManager.Instance.RecordStatus(EquipmentStatus.Abnormal);
                    break;
                    // 其他状态无需处理
            }
        }

        /// <summary>
        /// 更新运行时间和当前时间显示
        /// </summary>
        private void UpdateTimeDisplay()
        {
            TimeSpan runTime = DateTime.Now - RunStartTime;
            ts_RunTime.Text = $"运行: {runTime.TotalHours:F2}H";

            tss_CurTime.Text = $"{DateTime.Now:yyyy/MM/dd HH:mm:ss} {GetWeekName()}";
        }

        /// <summary>
        /// 更新CPU和内存显示
        /// </summary>
        private void UpdateCpuMemoryDisplay(double cpuUsage, double memoryUsageMB)
        {
            // 保存CPU使用率用于异常监控
            Project.Instance.GlobalVariables.CpuAbnormalMonitor = cpuUsage;

            Run_CPU.Text = $"CPU(%): {cpuUsage:F2}";
            Run_Memory.Text = $"内存: {memoryUsageMB:F2} MB";
        }

        /// <summary>
        /// 更新磁盘空间显示
        /// </summary>
        private void UpdateDiskSpaceDisplay(double[] diskData)
        {
            double availableGB = diskData[0];
            double availablePercent = diskData[1];

            // 更新磁盘进度条
            toolStripProgressBar1.Value = 100 - (int)(availablePercent * 100);
            toolStripProgressBar1.ToolTipText = $"{MONITORED_DISK}盘可用空间：{availableGB:f0} G";

            // 磁盘空间不足提示
            bool isDiskFull = toolStripProgressBar1.Value > DISK_FULL_THRESHOLD;
            toolStripStatusLabel1.Visible = isDiskFull;

            if (isDiskFull)
            {
                toolStripStatusLabel1.Text = "可用空间不足，请及时清理图片";
                toolStripStatusLabel1.ForeColor = Color.Red;
            }
        }

        /// <summary>
        /// 更新当前用户信息显示
        /// </summary>
        private void UpdateCurrentUserDisplay()
        {
            var currentUser = PermissionSystem.Instance.CurrentUser;
            string userName = currentUser != null ? currentUser.UserName : "未登录";

            // 多语言支持(保持原有逻辑)
            Frm_Main.Instance.tss_PermissionInfo.Text =
                $"{(Project.Instance.SystemParm.Language == LanguageType.English ? "当前用户：" : "当前用户：")}{userName}";
        }

        /// <summary>
        /// 发生错误时更新UI显示
        /// </summary>
        private void UpdateUIOnError()
        {
            Invoke(new Action(() =>
            {
                Run_CPU.Text = "0%";
                Run_Memory.Text = "0 KB";
            }));
        }

        /// <summary>
        /// 获取当前星期的中文名称
        /// </summary>
        private static string GetWeekName()
        {
            return DateTime.Now.DayOfWeek switch
            {
                DayOfWeek.Monday => "星期一",
                DayOfWeek.Tuesday => "星期二",
                DayOfWeek.Wednesday => "星期三",
                DayOfWeek.Thursday => "星期四",
                DayOfWeek.Friday => "星期五",
                DayOfWeek.Saturday => "星期六",
                DayOfWeek.Sunday => "星期日",
                _ => ""
            };
        }
        #endregion

        #region 事件处理
        /// <summary>
        /// 窗体加载事件（主窗体初始化入口）<br/>
        /// 完成语言初始化、系统资源准备、事件订阅、设备状态显示及初始视图加载
        /// </summary>
        private void Frm_Main_Load(object sender, EventArgs e)
        {
            try
            {
                Instance.Visible = false; // 初始隐藏，避免加载过程中界面闪烁
                InitLanguage(); // 初始化语言设置
                Initialization.Instance.InitializationForm(); // 初始化其他窗体
                Initialization.Instance.InitializeThreads(); // 初始化后台工作线程

                // 初始化菜单和样式
                elementHost1.Child = new MenuView(); // 加载WPF菜单视图
                uiStyleManager1.Style = Style; // 应用界面样式
                Project.Instance.StationInfo.RunningSpeedRatio = 100; // 初始化运行速度为100%
                Controller.Instance.UpdateMachineRunStatus(MachineRunStatus.None); // 初始设备状态设为None
                _stationController = Controller.Instance; // 绑定工位控制器

                SubscribeEvents(); // 订阅事件
                InitExternalDeviceStatus();
                //InitExternalDeviceStatus(); // 初始化外部设备状态显示（TCPIP/Modbus设备）
                MenuProxy.Instance.OperateType = OperateMode.Online; // 默认设置为联机模式
                                                                     // 软件首次打开时设置LED状态
                SysParm.Instance.BuzzerLed.SetBuzzerLedStatus(BuzzerLED.LedColor.SoftwareFirstOpen);
                SwitchForm(FormPage.MainPage); // 切换到主页视图
                _autoSizeForm.AutoScaleControls(this); // 初始化窗体自适应大小
                _operationLogger.RegisterFormControls(this);

                Instance.Visible = true; // 加载完成后显示窗体
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 初始化外部设备状态显示<br/>
        /// 在工具栏添加TCPIP和Modbus设备的状态菜单（初始为红色，代表未连接）
        /// </summary>
        private void InitExternalDeviceStatus()
        {
            // 初始化TCPIP设备状态菜单
            if (TcpIpSystem.Instance.TcpIpCommunications?.Any() ?? false)
            {
                foreach (var device in TcpIpSystem.Instance.TcpIpCommunications)
                {
                    tssButton_TCPIP_State.DropDownItems.Add(new ToolStripMenuItem
                    {
                        Name = device.设备名称,
                        Size = new Size(192, 22),
                        Text = device.设备名称,
                        ForeColor = Color.Red // 初始默认未连接（红色）
                    });
                }
            }

            // 初始化Modbus设备状态菜单
            if (ModbusSystem.Instance.ModbusCommunications?.Any() ?? false)
            {
                foreach (var device in ModbusSystem.Instance.ModbusCommunications)
                {
                    tssButton_Modbus_State.DropDownItems.Add(new ToolStripMenuItem
                    {
                        Name = device.设备名称,
                        Size = new Size(192, 22),
                        Text = device.设备名称,
                        ForeColor = Color.Red // 初始默认未连接（红色）
                    });
                }
            }
        }

        /// <summary>
        /// 窗体显示事件<br/>
        /// 窗体完全显示后设置透明度，并根据配置自动锁定窗体
        /// </summary>
        private void Frm_Main_Shown(object sender, EventArgs e)
        {
            try
            {
                Opacity = 1; // 完全显示（避免初始半透明状态）
                             // 若配置为启动后自动锁定，则显示锁定窗体
                if (Project.Instance.SystemParm.AutoLockAfterStart)
                {
                    using (var lockForm = new Frm_Lock())
                    {
                        lockForm.ShowDialog(); // 模态显示锁定窗体
                    }
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 鼠标移动钩子事件处理<br/>
        /// 用户移动鼠标时更新操作状态（用于自动锁定逻辑，检测用户活动）
        /// </summary>
        private void HookManager_MouseMove(object sender, MouseEventArgs e)
        {
            PermissionSystem.Instance.IsOperating = true; // 标记用户正在操作
        }

        /// <summary>
        /// 窗体关闭事件<br/>
        /// 显示关闭确认对话框，确认后保存配置并清理资源
        /// </summary>
        private void Frm_Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                // 显示关闭确认对话框
                Frm_ConfirmBox.Instance.lbl_Message.Text = JabilUILocalize.FormClosing_lbl_info;
                Frm_ConfirmBox.Instance.ShowDialog();

                if (Frm_ConfirmBox.Instance.Result != ConfirmBoxResult.Yes)
                {
                    e.Cancel = true; // 取消关闭操作
                    return;
                }

                // 若配置为退出时保存，则保存所有系统配置
                if (Project.Instance.SystemParm.SaveOnExit)
                    SaveAllConfigurations();

                // 清理应用程序资源
                CleanupResources();
                // 保存当前窗体尺寸（下次启动时恢复）
                Project.Instance.SystemParm.MainFormWidth = Size.Width;
                Project.Instance.SystemParm.MainFormHeight = Size.Height;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 清理应用程序资源（窗体关闭前执行）<br/>
        /// 释放设备连接、停止线程、关闭外部进程等，确保程序完全退出
        /// </summary>
        private void CleanupResources()
        {
            Project.Instance.GlobalVariables.IsProgramExiting = true; // 设置程序退出标识
            Controller.Instance.UpdateMachineRunStatus(MachineRunStatus.EmergencyStopped); // 更新设备状态为急停
            // 断开所有相机连接
            CogFrameGrabberGigEs cameras = new CogFrameGrabberGigEs();
            foreach (CogFrameGrabberGigE icf in cameras)
            {
                icf.Disconnect(true);
            }

            // 停止图像保存线程
            //VisionLib.Instance.SaveImgThread_Stop();

            // 关闭运动控制器
            var motionCards = MotionSystem.Instance.MotionCards;
            foreach (var card in motionCards)
            {
                MotionSystem.Instance.MotionController.Close(card);
            }

            // 重置LED和蜂鸣器状态
            SysParm.Instance.BuzzerLed.SetBuzzerLedStatus(BuzzerLED.LedColor.Undefined);

            // 关闭所有Modbus连接
            CloseModbusConnections();

            // 关闭所有TCP连接
            CloseTcpConnections();

            // 关闭 JVS 引擎（释放 ToolBlock 运行器、LBAS 相机与 Python 引擎）
            try
            {
                JvsVisionBridge.Instance.Shutdown();
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }

            // 关闭虚拟键盘进程（若存在）
            if (KeyboardProcess != null && !KeyboardProcess.HasExited)
                KeyboardProcess.Kill();
        }

        /// <summary>
        /// 关闭所有Modbus连接<br/>
        /// 断开PLC和机器人的Modbus通信连接
        /// </summary>
        private void CloseModbusConnections()
        {
            foreach (var plc in ModbusSystem.Instance.PlcModbusList)
                plc.Disconnect();

            foreach (var robot in ModbusSystem.Instance.RobotModbusList)
                robot.Disconnect();
        }

        /// <summary>
        /// 关闭所有TCP连接<br/>
        /// 断开TCP服务器和客户端的网络连接
        /// </summary>
        private void CloseTcpConnections()
        {
            foreach (var server in TcpIpSystem.Instance.TcpServers)
            {
                server.CloseAllClients(); // 关闭所有客户端连接
                server.StopServer(); // 关闭服务器
            }

            foreach (var client in TcpIpSystem.Instance.TcpClients)
            {
                client.Close(); // 关闭客户端
            }
        }

        /// <summary>
        /// 窗体关闭完成事件<br/>
        /// 取消事件订阅，显示关闭提示，最终终止进程
        /// </summary>
        private void Frm_Main_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                UnsubscribeEvents(); // 取消所有事件订阅
                Opacity = 0; // 设为透明（视觉上隐藏）
                using (var closeTip = new Frm_CloseTip())
                {
                    closeTip.Show(); // 显示关闭提示窗体
                }
                ShowInTaskbar = false; // 从任务栏隐藏
                RLog.SaveLog(LogType.Operation, Jabil.Module.Log.Lib.LogLevel.Info, Color.Black, JabilUILocalize.FormClosed_lbl_info);
                Process.GetCurrentProcess().Kill(); // 强制终止进程（确保完全退出）
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 菜单视图切换事件处理<br/>
        /// 响应菜单点击，切换对应视图或执行设备操作（启动/暂停/停止等）
        /// </summary>
        private void MenuProxy_OnOpenView(object sender, OpenViewEventArgs e)
        {
            try
            {
                switch (e.ViewName)
                {
                    case "主页":
                        SwitchForm(FormPage.MainPage);
                        break;
                    case "生产统计":
                        SwitchForm(FormPage.ProductDataPage);
                        break;
                    case "流程监控":
                        SwitchForm(FormPage.ProcessMonitorPage);
                        break;
                    case "视觉":
                        SwitchForm(FormPage.VisionPage);
                        break;
                    case "手动":
                        SwitchForm(FormPage.ManualPage);
                        break;
                    case "设置":
                        SwitchForm(FormPage.SettingPage);
                        break;
                    case "报警":
                        SwitchForm(FormPage.AlarmPage);
                        break;
                    case "权限":
                        SwitchForm(FormPage.PermissionPage);
                        break;
                    case "帮助":
                        SwitchForm(FormPage.HelpPage);
                        break;
                    case "运行":
                        _stationController.StartStation(); // 启动设备
                        break;
                    case "暂停":
                        _stationController.Pause(); // 暂停设备
                        break;
                    case "继续":
                        if (Controller.Instance.IsSafetyDoorStatusMonitored == false)
                            return; // 安全门没锁定不允许恢复设备运行

                        _stationController.Resume(); // 恢复运行
                        break;
                    case "停止":
                        _stationController.Stop(); // 停止设备
                        break;
                    case "初始化":
                        _stationController.InitStation(); // 初始化工位
                        break;
                    case "复位":
                        _stationController.Reset(); // 复位设备
                        break;
                    case "退出":
                        HandleExit(); // 处理退出逻辑
                        break;
                }
                RLog.SaveLog(LogType.Operation, Jabil.Module.Log.Lib.LogLevel.Info, Color.Black, $"{e.ViewName}被打开！");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 操作日志刷新事件处理<br/>
        /// 在UI上显示最新操作日志内容
        /// </summary>
        /// <param name="ItemValue">日志内容</param>
        private void Rlog_DataChanged(string ItemValue)
        {
            try
            {
                // 在主线程更新日志显示（避免跨线程异常）
                WFUtil.UpdateMainThreadUIAsync(Frm_Main.Instance, () =>
                {
                    Frm_Main.Instance.toolStripLabel_LOG显示.Text = ItemValue;
                    Frm_Main.Instance.toolStripLabel_LOG显示.ForeColor = Color.White;
                });
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 处理退出逻辑<br/>
        /// 仅允许在手动模式下退出，否则提示用户切换模式
        /// </summary>
        private void HandleExit()
        {
            if (MenuProxy.Instance.MachineRunModel != MachineMode.Manual)
            {
                RLog.SaveLog(LogType.Operation, Jabil.Module.Log.Lib.LogLevel.Info, Color.Orange, "请先将工位运行模式切换至手动，再退出软件！！！！");
                return;
            }
            Close(); // 关闭窗体
        }

        /// <summary>
        /// "锁定"工具条按钮点击事件<br/>
        /// 锁定窗体并显示密码解锁界面
        /// </summary>
        private void toolStripLabel_锁定_Click(object sender, EventArgs e)
        {
            try
            {
                IsLocked = true; // 标记窗体为锁定状态
                using (var lockForm = new Frm_Lock { TopMost = true }) // 锁定窗体置顶
                {
                    lockForm.ShowDialog(); // 模态显示锁定窗体
                }
            }
            catch (Exception ex)
            {
                toolStripLabel_锁定.Enabled = true; // 异常时恢复按钮可用
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// "虚拟键盘"工具条按钮点击事件<br/>
        /// 启动系统自带虚拟键盘（osk.exe）
        /// </summary>
        private void toolStripLabel_虚拟键盘_Click(object sender, EventArgs e)
        {
            //Controller.Instance.StationInfo.Epson1下相机请求位置号 = 1;
            //new VisionProcess("Vision|定位|定位相机|右下定位|5", "隐藏").Run();
           
            //new ModbusRobotCommunicationProcess("ModbusRobot|输出|Robot|Epson1流水线呼叫组装|1|3", "隐藏").Run();
            //new VisionProcess("Vision|定位|定位相机|左下定位|5", "隐藏").Run();
            //new VisionProcess("Vision|扫码|扫码相机|扫码检测1|5", "隐藏").Run();
            //new VisionProcess("Vision|扫码|扫码相机|扫码检测2|5", "隐藏").Run();
            //new ModbusRobotCommunicationProcess("ModbusRobot|输出|Robot|左下相机拍照NG|1|3", "隐藏").Run();
            //new ModbusRobotCommunicationProcess("ModbusRobot|输出|Robot|左下相机拍照OK|1|3", "隐藏").Run();
            try
            {
                toolStripLabel_虚拟键盘.Enabled = false; // 防止重复点击
                KeyboardProcess = Process.Start("osk.exe"); // 启动虚拟键盘
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
            finally
            {
                toolStripLabel_虚拟键盘.Enabled = true; // 恢复按钮可用
            }
        }




        // 添加私有变量用于保存原有控件状态
        private List<System.Windows.Forms.Control> _previousControlsInWindow = new List<System.Windows.Forms.Control>();

        /// <summary>
        /// "日志显示"工具条按钮点击事件
        /// </summary>
        private void toolStripLabel_日志显示_Click(object sender, EventArgs e)
        {
            try
            {
                toolStripLabel_日志显示.Enabled = false;
                uiPanel_window.Visible = false; // 隐藏面板避免闪烁

                // 检查日志视图是否已在容器中
                bool isLogVisible = uiPanel_window.Controls.Contains(Frm_OperateView.Instance);

                if (isLogVisible)
                {
                    // 状态1：日志已显示，需要隐藏日志并恢复原有控件
                    uiPanel_window.Controls.Remove(Frm_OperateView.Instance);

                    // 恢复之前保存的控件
                    foreach (var control in _previousControlsInWindow)
                    {
                        if (!uiPanel_window.Controls.Contains(control))
                        {
                            // 恢复控件时保持原有停靠属性
                            uiPanel_window.Controls.Add(control);
                            control.BringToFront(); // 确保恢复的控件在正确层级
                        }
                    }

                    // 清空保存的控件列表
                    _previousControlsInWindow.Clear();
                }
                else
                {
                    // 状态2：日志未显示，需要保存当前控件并显示日志
                    // 保存除工具栏和日志外的所有控件（工具栏需要保留）
                    _previousControlsInWindow.Clear();
                    foreach (System.Windows.Forms.Control ctrl in uiPanel_window.Controls)
                    {
                        // 排除工具栏和日志视图本身
                        if (ctrl != toolStrip_工具栏 && ctrl != Frm_OperateView.Instance)
                        {
                            _previousControlsInWindow.Add(ctrl);
                        }
                    }


                    uiPanel_window.Visible = false;
                    uiPanel_window.Controls.Clear();
                    uiPanel_window.Dock = DockStyle.Fill;

                    Frm_OperateView.Instance.TopLevel = false;
                    Frm_OperateView.Instance.Dock = DockStyle.Fill;
                    Frm_OperateView.Instance.FormBorderStyle = FormBorderStyle.None;
                    Frm_OperateView.Instance.Show();

                    uiPanel_window.Controls.Add(Frm_OperateView.Instance);
                    toolStrip_工具栏.Visible = true;
                    toolStrip_工具栏.Dock = DockStyle.Top;
                    uiPanel_window.Controls.Add(toolStrip_工具栏);
                }

                // 强制刷新布局
                uiPanel_window.PerformLayout();
                uiPanel_window.Visible = true;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
            finally
            {
                toolStripLabel_日志显示.Enabled = true;
            }
        }

        /// <summary>
        /// "蜂鸣器抑制"工具条按钮点击事件<br/>
        /// 切换蜂鸣器抑制状态（启用/禁用蜂鸣器报警声）
        /// </summary>
        private void toolStripLabel_蜂鸣器抑制_Click(object sender, EventArgs e)
        {
            var isSuppressed = Controller.Instance.StationInfo._屏蔽蜂鸣器;
            Controller.Instance.StationInfo._屏蔽蜂鸣器 = !isSuppressed; // 切换状态

            // 更新按钮显示文本和颜色（反映当前状态）
            toolStripLabel_蜂鸣器抑制.Text = isSuppressed ? "蜂鸣器" : "蜂鸣器被抑制";
            toolStripLabel_蜂鸣器抑制.ForeColor = isSuppressed ? Color.DarkGray : Color.Red;
        }

        /// <summary>
        /// "STO复位"工具条按钮点击事件<br/>
        /// 发送安全继电器复位信号（脉冲信号：先0后1，确保复位生效）
        /// </summary>
        private void toolStripLabel_STO复位_Click(object sender, EventArgs e)
        {
            if (Controller.Instance.IsMachineInternalStoMonitored)
            {
                new IOProcess("IO|输出|安全继电器复位|0|5", "隐藏").Run();
                Thread.Sleep(100); // 延迟100ms确保信号切换
                new IOProcess("IO|输出|安全继电器复位|1|5", "隐藏").Run();
            }
        }

        /// <summary>
        /// TCPIP状态按钮双击事件<br/>
        /// 手动重连所有TCPIP设备（需用户确认，避免误操作）
        /// </summary>
        private async void tssButton_TCPIP_State_ButtonDoubleClick(object sender, EventArgs e)
        {
            if (!await ConfirmAction("请确认是否手动执行TCPIP通讯重连操作！！！"))
                return;

            try
            {
                tssButton_TCPIP_State.Enabled = false; // 禁用按钮防止重复操作
                TCPIPPage.Instance.Close(); // 关闭TCPIP配置页

                // 重连所有未连接的TCP客户端设备
                foreach (var device in TcpIpSystem.Instance.TcpIpCommunications)
                {
                    if (device.设备类型 == "TCPClient")
                    {
                        foreach (var client in TcpIpSystem.Instance.TcpClients.Where(c => !c.IsConnected))
                        {
                            client.Connect(); // 执行重连
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
            finally
            {
                tssButton_TCPIP_State.Enabled = true; // 恢复按钮可用
            }
        }

        /// <summary>
        /// Modbus状态按钮双击事件<br/>
        /// 手动重连所有Modbus设备（机器人和PLC，需用户确认）
        /// </summary>
        private async void tssButton_Modbus_State_ButtonDoubleClick(object sender, EventArgs e)
        {
            if (!await ConfirmAction("请确认是否手动执行Modbus通讯重连操作！！！"))
                return;

            try
            {
                tssButton_Modbus_State.Enabled = false; // 禁用按钮防止重复操作
                ModbusPage.Instance.Close(); // 关闭Modbus配置页

                // 按设备类型重连
                foreach (var device in ModbusSystem.Instance.ModbusCommunications)
                {
                    if (device.设备类型 == "Robot")
                    {
                        await ReconnectRobots(device); // 重连机器人
                    }
                    else if (device.设备类型 == "PLC" && device.设备名称.Contains("H5u"))
                    {
                        await ReconnectPlcs(device); // 重连H5u类型PLC
                    }
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
            finally
            {
                tssButton_Modbus_State.Enabled = true; // 恢复按钮可用
            }
        }

        /// <summary>
        /// 显示确认对话框（异步）<br/>
        /// 在后台线程执行，避免阻塞UI，返回用户是否确认操作
        /// </summary>
        /// <param name="message">确认消息内容</param>
        /// <returns>用户是否确认（true：是；false：否）</returns>
        private async Task<bool> ConfirmAction(string message)
        {
            return await Task.Run(() =>
            {
                WFUtil.UpdateMainThreadUIAsync(this, () =>
                {
                    Frm_ConfirmBox.Instance.lbl_Message.Text = message;
                    Frm_ConfirmBox.Instance.Visible = false;
                    Frm_ConfirmBox.Instance.TopMost = true;
                    Frm_ConfirmBox.Instance.ShowDialog();
                });
                return Frm_ConfirmBox.Instance.Result == ConfirmBoxResult.Yes;
            });
        }

        /// <summary>
        /// 重连机器人（异步）<br/>
        /// 断开并重新连接指定类型的机器人（Epson/Fanuc/Efort）
        /// </summary>
        /// <param name="device">机器人设备信息</param>
        private Task ReconnectRobots(dynamic device)
        {
            return Task.Run(() =>
            {
                foreach (var robot in ModbusSystem.Instance.RobotModbusList
                    .Where(r => r.DeviceName == device.设备名称 && !r.IsConnected))
                {
                    robot.Disconnect(); // 先断开连接
                                        // 根据机器人类型执行重连
                    if (device.设备名称.Contains("Epson"))
                        robot.Connect(Robot_Modbus.RobotType.Epson, device.设备IP, device.设备Port);
                    else if (device.设备名称.Contains("Fanuc"))
                        robot.Connect(Robot_Modbus.RobotType.Fanuc, device.设备IP, device.设备Port);
                    else if (device.设备名称.Contains("Efort"))
                        robot.Connect(Robot_Modbus.RobotType.Efort, device.设备IP, device.设备Port);
                }
            });
        }

        /// <summary>
        /// 重连PLC（异步）<br/>
        /// 断开并重新连接H5u类型PLC
        /// </summary>
        /// <param name="device">PLC设备信息</param>
        private Task ReconnectPlcs(dynamic device)
        {
            return Task.Run(() =>
            {
                foreach (var plc in ModbusSystem.Instance.PlcModbusList
                    .Where(p => p.DeviceName == device.设备名称 && !p.IsConnected))
                {
                    plc.Disconnect(); // 先断开连接
                    plc.Connect(PLC_Modbus.PLCType.H5u, device.设备IP, device.设备Port); // 重连
                }
            });
        }

        /// <summary>
        /// 登录用户变更事件处理<br/>
        /// 用户切换后更新权限相关UI显示
        /// </summary>
        private void Instance_OnLoginUserChanged(object sender, EventArgs e)
        {
            try
            {
                UpdatePermission();
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 菜单属性变更事件处理<br/>
        /// 响应操作类型变更（预留逻辑，当前未启用）
        /// </summary>
        private void Instance_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
                if (e.PropertyName == "OperateType")
                {
                    // 示例：同步操作类型到工位信息（当前注释，按需启用）
                    // Project.Instance.StationInfo._OperateType = MenuProxy.Instance.OperateType;
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 日志显示标签鼠标按下事件<br/>
        /// 清空当前显示的日志内容
        /// </summary>
        private void toolStripLabel_LOG显示_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                toolStripLabel_LOG显示.Enabled = false;
                toolStripLabel_LOG显示.Text = ""; // 清空文本
                toolStripLabel_LOG显示.ForeColor = Color.Transparent; // 透明化（隐藏）
            }
            catch (Exception ex)
            {
                toolStripLabel_LOG显示.Enabled = true;
                RLog.SaveError(ex);
            }
            finally
            {
                toolStripLabel_LOG显示.Enabled = true;
            }
        }

        /// <summary>
        /// "下料安全门打开"工具条按钮点击事件<br/>
        /// 预留：下料安全门状态控制（当前无实现）
        /// </summary>
    //    private void toolStripLabel_下料安全门打开_Click(object sender, EventArgs e)
    //    {
    //        var process = new IOProcess(
    //Controller.Instance.IsSafetyDoorStatusMonitored
    //                ? "IO|输出|后安全门上锁|0|5"
    //                : "IO|输出|后安全门上锁|1|5",
    //            "隐藏");
    //        process.Run();
    //    }

        /// <summary>
        /// "机台安全门打开"工具条按钮点击事件<br/>
        /// 预留：机台安全门锁定状态控制（当前注释，按需启用）
        /// </summary>
        private void toolStripLabel_机台安全门打开_Click(object sender, EventArgs e)
        {
            if (MenuProxy.Instance.MachineRunModel == MachineMode.Auto)
            {
                RLog.SaveLog(LogType.Operation, Jabil.Module.Log.Lib.LogLevel.Info, Color.Orange,
                    "自动模式下不允许开门，请切换到手动后重试！");
                return;
            }

            var process = new IOProcess(
    Controller.Instance.IsSafetyDoorStatusMonitored
        ? "IO|输出|前安全门上锁|0|5"
        : "IO|输出|前安全门上锁|1|5",
    "隐藏");
            process.Run();

    //        process = new IOProcess(
    //Controller.Instance.IsSafetyDoorStatusMonitored
    //    ? "IO|输出|后安全门上锁|0|5"
    //    : "IO|输出|后安全门上锁|1|5",
    //"隐藏");
    //        process.Run();
        }
        /// <summary>
        /// "上料安全门打开"工具条按钮点击事件<br/>
        /// 预留：上料安全门锁定状态控制（当前注释，按需启用）
        /// </summary>
    //    private void toolStripLabel_上料安全门打开_Click(object sender, EventArgs e)
    //    {
    //        var process = new IOProcess(
    //Controller.Instance.IsSafetyDoorStatusMonitored
    //    ? "IO|输出|前安全门上锁|0|5"
    //    : "IO|输出|前安全门上锁|1|5",
    //"隐藏");
    //        process.Run();

    //    }


        /// <summary>
        /// "截屏"工具条按钮点击事件<br/>
        /// 捕获当前屏幕并保存到指定目录（按日期分目录）
        /// </summary>
        private async void toolStripLabel_截屏_Click(object sender, EventArgs e)
        {
            try
            {
                toolStripLabel_截屏.Enabled = false;
                await Task.Delay(100); // 延迟100ms确保UI状态稳定

                // 构建截图保存目录（按日期分目录）
                var dateDir = DateTime.Now.ToString("yyyy-MM-dd");
                var screenshotDir = Path.Combine(Project.Instance.ParameterPath.DataPath,
                    string.Format(ScreenshotDirectoryFormat, dateDir));
                Directory.CreateDirectory(screenshotDir); // 确保目录存在

                // 构建截图文件名（含时间戳）
                var fileName = Path.Combine(screenshotDir, string.Format(ScreenshotFileNameFormat, DateTime.Now));
                CaptureScreenshot(fileName); // 捕获并保存截图
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
            finally
            {
                toolStripLabel_截屏.Enabled = true;
            }
        }

        /// <summary>
        /// 捕获屏幕截图<br/>
        /// 截取全屏图像并保存为JPG文件
        /// </summary>
        /// <param name="fileName">保存路径</param>
        private void CaptureScreenshot(string fileName)
        {
            var bounds = Screen.GetBounds(Point.Empty); // 获取屏幕边界（全屏）
            using (var bitmap = new Bitmap(bounds.Width, bounds.Height)) // 创建与屏幕同尺寸的位图
            using (var g = Graphics.FromImage(bitmap)) // 从位图创建绘图对象
            {
                g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size); // 拷贝屏幕图像到位图
                bitmap.Save(fileName); // 保存为文件
            }
        }
        #endregion
    }
}
