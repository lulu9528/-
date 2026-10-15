using Jabil.Module.Log.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JabilVisionMotion.Lib
{
    public static class FormLoggerInitializer
    {
        private static ControlOperationLogger _logger;
        private static System.Windows.Forms.Timer _formMonitorTimer;
        private static HashSet<Form> _loggedForms = new HashSet<Form>();

        public static void Initialize()
        {
            _logger = new ControlOperationLogger();

            // 使用定时器定期检查新创建的窗体
            _formMonitorTimer = new System.Windows.Forms.Timer
            {
                Interval = 500 // 每500毫秒检查一次
            };
            _formMonitorTimer.Tick += FormMonitorTimer_Tick;
            _formMonitorTimer.Start();
        }

        private static void FormMonitorTimer_Tick(object sender, EventArgs e)
        {
            // 遍历所有打开的窗体
            foreach (Form form in Application.OpenForms)
            {
                // 检查是否已经注册过日志（使用Tag属性标记）
                if (!_loggedForms.Contains(form))
                {
                    // 注册当前窗体的控件
                    _logger.RegisterFormControls(form);

                    // 标记为已注册
                    _loggedForms.Add(form);

                    // 监听窗体关闭事件，从已注册列表中移除
                    form.FormClosed += (s, args) =>
                    {
                        _loggedForms.Remove(form);
                    };
                }
            }
        }

        // 应用程序退出时清理资源
        public static void Shutdown()
        {
            if (_formMonitorTimer != null)
            {
                _formMonitorTimer.Stop();
                _formMonitorTimer.Dispose();
            }
            _loggedForms.Clear();
        }
    }
}
