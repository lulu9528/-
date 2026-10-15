using System;
using System.Threading;
using System.Windows.Forms;

namespace Jabil.Module.AutoDeleteImage
{
    static class Program
    {
        // 定义一个唯一的标识符，用于Mutex
        private const string UniqueMutexId = "Jabil.Module.AutoDeleteImage.SingleInstanceMutex";

        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 创建互斥体并检查是否已有实例在运行
            using (Mutex mutex = new Mutex(true, UniqueMutexId, out bool isNewInstance))
            {
                if (isNewInstance)
                {
                    // 如果是新实例，正常启动应用程序
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new AutoDeleteImageForm());
                }
                else
                {
                    // 如果已有实例在运行，提示用户并退出当前实例
                    MessageBox.Show("程序已在运行中，不能重复启动！",
                                  "提示",
                                  MessageBoxButtons.OK,
                                  MessageBoxIcon.Information);

                    // 可选：如果需要，可以激活已运行的窗口
                    // 这需要使用Win32 API来查找并激活窗口
                }
            }
        }
    }
}
