using Cognex.VisionPro;
using Jabil.Lib;
using Jabil.Module.Log.Lib;
using JabilVisionMotion.Frm;
using JabilVisionMotion.Frm.Dialog;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JabilVisionMotion.Lib
{
    /// <summary>
    /// 框架对外调用入口类，提供程序初始化和启动的静态方法
    /// 作用：封装框架内部初始化逻辑，为外部程序提供简单的启动入口
    /// </summary>
    public static class VM
    {
        /// <summary>
        /// 初始化并启动框架（程序入口点）
        /// 流程：显示欢迎窗体→后台执行初始化→初始化完成后显示主窗体
        /// </summary>
        /// <param name="showWelcomeWindow">是否显示启动欢迎窗体（默认true）</param>
        public static void Init(bool showWelcomeWindow = true)
        {
            // 确保在单线程公寓模式下运行（UI程序必需）
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                throw new InvalidOperationException("框架必须在单线程公寓(STA)模式下运行，请标记主线程为[STAThread]");
            }

            // 初始化主窗体实例（提前创建以避免跨线程问题）
            var mainForm = Frm_Main.Instance;

            // 初始化信号量（用于等待初始化完成）
            using (var initCompletedEvent = new ManualResetEvent(false))
            {
                Exception initException = null; // 用于捕获初始化线程中的异常

                // 显示欢迎窗体（如果需要）
                Frm_Welcome welcomeForm = null;
                if (showWelcomeWindow)
                {
                    welcomeForm = Frm_Welcome.Instance;
                    welcomeForm.Show();
                    Application.DoEvents(); // 刷新UI，确保欢迎窗体显示
                }

                // 启动后台初始化线程
                var initThread = new Thread(() =>
                {
                    try
                    {
                        // 执行框架初始化（包含配置加载、模块初始化等）
                        Initialization.Instance.InitAsync();
                    }
                    catch (Exception ex)
                    {
                        initException = ex; // 捕获初始化异常
                    }
                    finally
                    {
                        initCompletedEvent.Set(); // 无论成功失败，标记初始化完成
                    }
                })
                {
                    IsBackground = true, // 设为后台线程，随主线程退出
                    Name = "FrameworkInitThread" // 命名线程，便于调试
                };
                initThread.Start();

                while (Initialization.IsLoading == true)
                {
                    Thread.Sleep(1);
                    Application.DoEvents();
                } //当该程序正在加载的状态为true时，主窗体显示
                // 等待初始化完成（替代原轮询逻辑，降低CPU占用）
                initCompletedEvent.WaitOne();
                // 关闭欢迎窗体
                welcomeForm?.Close();
                Application.DoEvents();

                // 处理初始化异常
                if (initException != null)
                {
                    MessageBox.Show(
                        $"框架初始化失败：{initException.Message}\n详情请查看错误日志",
                        "初始化错误",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    RLog.SaveError(initException); // 记录异常到日志
                    return;
                }

                // 显示主窗体（模态方式，确保主线程消息循环）
                mainForm.ShowDialog();
            }
        }
    }
}

