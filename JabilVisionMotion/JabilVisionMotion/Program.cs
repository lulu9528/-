using Jabil.Lib;
using Jabil.Module.Log.Frm;
using Jabil.Module.Log.Lib;
using JabilVisionMotion.Lib;
using System;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Start
{
    /// <summary>
    /// 应用程序主入口类<br/>
    /// 负责：应用程序初始化、单实例运行控制、全局异常捕获与处理
    /// </summary>
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点<br/>
        /// 标记[STAThread]表示使用单线程单元模型，适用于Windows Forms应用
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 定义应用程序主流程的异常捕获
            try
            {
                // 1. 初始化应用程序基础设置
                InitializeApplicationSettings();

                // 2. 注册全局异常处理（捕获UI线程和非UI线程的未处理异常）
                RegisterGlobalExceptionHandlers();

                // 3. 检查应用程序是否已运行（单实例控制）
                if (CheckSingleInstance())
                {
                    // 初始化窗体日志记录
                    FormLoggerInitializer.Initialize();
                    // 4. 单实例检查通过，初始化并启动应用
                    InitializeAndRunApplication();
                    // 注册应用程序退出事件以清理资源
                    Application.ApplicationExit += (s, e) => FormLoggerInitializer.Shutdown();
                }
            }
            catch (Exception ex)
            {
                // 捕获主入口初始化阶段的致命异常
                string errorMsg = Project.Instance.SystemParm.Language == LanguageType.English
                    ? "Critical error during startup"
                    : "启动过程中发生致命错误（错误代码：00001）";

                // 显示错误信息并记录日志
                Frm_MessageBox.Instance.MessageBoxShow(errorMsg);
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 初始化应用程序基础设置<br/>
        /// 配置Windows Forms的视觉样式和文本渲染模式
        /// </summary>
        private static void InitializeApplicationSettings()
        {
            // 启用应用程序的可视化样式（使控件外观与系统主题一致）
            Application.EnableVisualStyles();

            // 设置控件文本渲染模式为兼容模式（避免不同.NET版本下的显示差异）
            Application.SetCompatibleTextRenderingDefault(false);
        }

        /// <summary>
        /// 注册全局异常处理程序<br/>
        /// 捕获UI线程和非UI线程中未处理的异常，避免程序意外崩溃
        /// </summary>
        private static void RegisterGlobalExceptionHandlers()
        {
            // 设置未处理异常的处理模式为"捕获异常"（而非交给系统默认处理）
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            // 注册UI线程未处理异常事件（如按钮点击事件中的异常）
            Application.ThreadException += Application_ThreadException;

            // 注册非UI线程未处理异常事件（如后台线程中的异常）
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        /// <summary>
        /// 检查应用程序是否已运行（单实例控制）<br/>
        /// 使用Mutex实现，确保同一时间只有一个应用实例运行
        /// </summary>
        /// <returns>true表示可以启动新实例，false表示已存在实例</returns>
        private static bool CheckSingleInstance()
        {
            // 使用using确保Mutex资源在使用后自动释放（避免资源泄漏）
            using (Mutex mutex = new Mutex(false, "ThisShouldOnlyRunOnce"))
            {
                try
                {
                    // 尝试在0毫秒内获取Mutex所有权：
                    // - 若成功（返回true）：表示无其他实例运行
                    // - 若失败（返回false）：表示已有实例运行
                    bool isNewInstance = mutex.WaitOne(0, false);

                    if (!isNewInstance)
                    {
                        // 已有实例运行，询问用户是否强制启动新实例
                        return AskUserForNewInstance();
                    }
                    return true;
                }
                catch (AbandonedMutexException ex)
                {
                    // 捕获"Mutex被遗弃"异常（上一个实例异常退出未释放Mutex）
                    RLog.SaveError(ex);
                    return true; // 视为可以启动新实例
                }
                catch (Exception ex)
                {
                    // 捕获其他Mutex操作异常（如权限问题）
                    RLog.SaveError(ex);
                    // 异常情况下允许启动，避免因Mutex问题导致程序无法运行
                    return true;
                }
            }
        }

        /// <summary>
        /// 询问用户是否在已有实例运行时启动新实例<br/>
        /// 根据用户选择返回是否允许启动
        /// </summary>
        /// <returns>用户选择"是"返回true，否则返回false</returns>
        private static bool AskUserForNewInstance()
        {
            // 根据当前语言设置生成提示信息
            string message = Project.Instance.SystemParm.Language == LanguageType.English
                ? "An instance is already running (or the previous instance did not close properly). Do you want to start another instance?"
                : "已运行一个实例（或旧实例未完全关闭），是否再开启一个实例？";

            string caption = Project.Instance.SystemParm.Language == LanguageType.English ? "Information" : "提示";

            // 显示询问对话框
            DialogResult result = MessageBox.Show(message, caption, MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Information);

            // 返回用户选择（Yes表示允许启动新实例）
            return result == DialogResult.Yes;
        }

        /// <summary>
        /// 初始化并启动应用程序核心逻辑
        /// </summary>
        private static void InitializeAndRunApplication()
        {
            try
            {
                // 初始化应用程序核心模块（VM为应用核心入口）
                VM.Init();

                // 启动应用程序消息循环（维持UI运行）
                Application.Run();
            }
            catch (Exception ex)
            {
                // 捕获应用初始化过程中的异常
                RLog.SaveError(ex);
                throw; // 抛出异常让上层处理（显示错误提示）
            }
        }

        /// <summary>
        /// UI线程未处理异常的处理函数<br/>
        /// 记录异常详情并保存到日志
        /// </summary>
        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            // 构建异常信息（使用StringBuilder提高字符串拼接效率）
            StringBuilder errorBuilder = new StringBuilder();
            errorBuilder.AppendLine($"Unhandled UI thread exception: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            errorBuilder.AppendLine($"Exception type: {e.Exception.GetType().FullName}");
            errorBuilder.AppendLine($"Message: {e.Exception.Message}");
            errorBuilder.AppendLine($"Stack trace: {e.Exception.StackTrace}");

            // 保存异常日志
            RLog.SaveError(e.Exception);
        }

        /// <summary>
        /// 非UI线程未处理异常的处理函数<br/>
        /// 记录异常详情并保存到日志（此类异常可能导致程序终止）
        /// </summary>
        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // 将异常对象转换为Exception类型
            Exception ex = e.ExceptionObject as Exception;
            if (ex == null)
            {
                RLog.SaveError(ex);
                return;
            }

            // 构建异常信息
            StringBuilder errorBuilder = new StringBuilder();
            errorBuilder.AppendLine($"Unhandled non-UI thread exception: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            errorBuilder.AppendLine($"Is terminating: {e.IsTerminating}"); // 标记是否导致程序终止
            errorBuilder.AppendLine($"Exception type: {ex.GetType().FullName}");
            errorBuilder.AppendLine($"Message: {ex.Message}");
            errorBuilder.AppendLine($"Stack trace: {ex.StackTrace}");

            // 保存异常日志
            RLog.SaveError(ex);
        }
    }
}

