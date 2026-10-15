using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace Jabil.Module.AutoDeleteImage
{
    /// <summary>
    /// 主窗体类，包含应用程序的所有UI交互和核心功能实现
    /// </summary>
    public partial class AutoDeleteImageForm : Form
    {
        #region 配置和状态变量定义

        /// <summary>
        /// 应用程序设置对象，用于存储和管理所有用户配置
        /// </summary>
        private Settings _settings;

        /// <summary>
        /// 监控状态标志，指示当前是否正在进行自动监控
        /// </summary>
        private bool _isMonitoring = false;

        /// <summary>
        /// 自动监控定时器，用于定期触发文件检查和删除操作
        /// </summary>
        private System.Windows.Forms.Timer _monitoringTimer;

        /// <summary>
        /// 日志条目列表，用于存储所有操作日志
        /// </summary>
        private List<string> _logEntries = new List<string>();

        /// <summary>
        /// 取消令牌源，用于在需要时取消后台任务
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// 注册表路径，用于设置开机自动启动（当前用户）
        /// HKEY_CURRENT_USER下的项不需要管理员权限
        /// </summary>
        private const string AutoStartRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

        /// <summary>
        /// 应用程序在注册表中的名称，用于标识开机启动项
        /// </summary>
        private const string AppName = "ImageAutoDeleter";

        /// <summary>
        /// 系统托盘图标
        /// </summary>
        private NotifyIcon _trayIcon;

        /// <summary>
        /// 热键注册所需的API常量
        /// </summary>
        private const int WM_HOTKEY = 0x0312;
        private const int MOD_CONTROL = 0x0002;
        private const int HOTKEY_ID = 1;

        #endregion

        #region 热键注册所需的DLL导入

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        #endregion

        /// <summary>
        /// 主窗体构造函数，初始化应用程序
        /// </summary>
        public AutoDeleteImageForm()
        {
            // 初始化Windows窗体控件（由设计器自动生成的代码）
            InitializeComponent();

            // 初始化系统托盘图标
            InitializeTrayIcon();

            // 初始化应用程序设置和状态变量
            InitializeSettings();

            // 初始化自动监控定时器
            InitializeMonitoringTimer();

            // 从本地加载保存的用户设置
            LoadSettings();

            // 根据当前状态更新UI控件的可用状态
            UpdateUIState();

            // 检查并设置开机启动相关的UI状态
            CheckAutoStartStatus();

            // 注册热键 (Ctrl+H)
            RegisterHotKey(Handle, HOTKEY_ID, MOD_CONTROL, (int)'H');
        }

        #region 初始化方法

        /// <summary>
        /// 初始化系统托盘图标
        /// </summary>
        private void InitializeTrayIcon()
        {
            _trayIcon = new NotifyIcon();
            _trayIcon.Icon = Icon; // 使用窗体图标
            _trayIcon.Text = "图片自动删除器";
            _trayIcon.Visible = true;

            // 创建右键菜单
            var contextMenu = new ContextMenuStrip();

            // 显示窗口菜单项
            var showItem = new ToolStripMenuItem("显示窗口");
            showItem.Click += (s, e) => ShowWindow();
            contextMenu.Items.Add(showItem);

            // 退出程序菜单项
            var exitItem = new ToolStripMenuItem("退出");
            exitItem.Click += (s, e) => ExitApplication();
            contextMenu.Items.Add(exitItem);

            _trayIcon.ContextMenuStrip = contextMenu;

            // 双击托盘图标显示窗口
            _trayIcon.DoubleClick += (s, e) => ShowWindow();
        }

        /// <summary>
        /// 初始化应用程序设置和取消令牌
        /// </summary>
        private void InitializeSettings()
        {
            // 创建新的设置对象，使用默认值
            _settings = new Settings();

            // 初始化取消令牌源，用于控制后台任务的取消
            _cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// 初始化自动监控定时器，设置默认间隔并绑定事件处理方法
        /// </summary>
        private void InitializeMonitoringTimer()
        {
            // 创建定时器对象
            _monitoringTimer = new System.Windows.Forms.Timer();

            // 设置默认检查间隔为60000毫秒（1分钟）
            _monitoringTimer.Interval = 60000;

            // 绑定定时器触发事件到处理方法
            _monitoringTimer.Tick += MonitoringTimer_Tick;
        }

        /// <summary>
        /// 从本地存储加载保存的用户设置
        /// </summary>
        private void LoadSettings()
        {
            // 调用设置管理器加载保存的设置
            var savedSettings = SettingsManager.LoadSettings();

            // 如果存在保存的设置，则使用这些设置
            if (savedSettings != null)
            {
                _settings = savedSettings;

                // 根据加载的设置更新UI显示
                UpdateSettingsUI();
            }
        }

        /// <summary>
        /// 根据当前设置更新UI控件的显示值
        /// </summary>
        private void UpdateSettingsUI()
        {
            // 更新OK图片保留天数数值框
            numericOKDays.Value = _settings.OkImageRetentionDays;

            // 更新NG图片保留天数数值框
            numericNGDays.Value = _settings.NgImageRetentionDays;

            // 更新监控文件夹路径文本框
            txtFolderPath.Text = _settings.MonitoredFolder;

            // 更新OK图片关键字文本框
            txtOKKeyword.Text = _settings.OkKeyword;

            // 更新NG图片关键字文本框
            txtNGKeyword.Text = _settings.NgKeyword;

            // 更新"包含子文件夹"复选框状态
            chkIncludeSubfolders.Checked = _settings.IncludeSubfolders;

            // 更新"删除前确认"复选框状态
            chkConfirmDeletion.Checked = _settings.ConfirmBeforeDeletion;

            // 更新检查间隔数值框
            numericCheckInterval.Value = _settings.CheckIntervalMinutes;

            // 根据检查间隔更新定时器的实际间隔（转换为毫秒）
            _monitoringTimer.Interval = (int)numericCheckInterval.Value * 60000;

            // 更新"开机自动启动"复选框状态
            chkAutoStart.Checked = IsAutoStartEnabled();

            // 更新"开机自动监控"复选框状态
            chkAutoStartMonitoring.Checked = _settings.AutoStartMonitoring;

            // 更新"开机自动隐藏"复选框状态
            chkAutoHideOnStart.Checked = _settings.AutoHideOnStartup;
        }

        /// <summary>
        /// 根据当前监控状态更新UI控件的可用状态
        /// </summary>
        private void UpdateUIState()
        {
            // 开始监控按钮：当未在监控时可用
            btnStartMonitoring.Enabled = !_isMonitoring;

            // 停止监控按钮：当正在监控时可用
            btnStopMonitoring.Enabled = _isMonitoring;

            // 手动删除按钮：当未在监控时可用
            btnManualDelete.Enabled = !_isMonitoring;

            // 设置分组框：当未在监控时可编辑
            groupSettings.Enabled = !_isMonitoring;

            // 开机启动复选框始终可操作
            chkAutoStart.Enabled = true;

            // 开机自动监控复选框：仅当开机启动启用时可用
            chkAutoStartMonitoring.Enabled = chkAutoStart.Checked;

            // 开机自动隐藏复选框：仅当开机启动启用时可用
            chkAutoHideOnStart.Enabled = chkAutoStart.Checked;
        }

        #endregion

        #region 开机自动启动功能

        /// <summary>
        /// 检查应用程序是否已设置为开机自动启动
        /// </summary>
        /// <returns>如果已启用开机启动则返回true，否则返回false</returns>
        private bool IsAutoStartEnabled()
        {
            // 打开注册表项（只读模式）
            // Registry.CurrentUser表示仅对当前用户生效
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(AutoStartRegistryPath, false))
            {
                // 检查注册表项中是否存在当前应用程序的启动项
                // 如果存在且值不为null，则表示已启用开机启动
                return key?.GetValue(AppName) != null;
            }
        }

        /// <summary>
        /// 启用或禁用应用程序的开机自动启动功能
        /// </summary>
        /// <param name="enable">true表示启用，false表示禁用</param>
        private void SetAutoStart(bool enable)
        {
            // 打开注册表项（可写模式）
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(AutoStartRegistryPath, true))
            {
                // 如果无法打开注册表项（通常是权限问题）
                if (key == null)
                {
                    // 记录错误日志
                    AddLogEntry("无法访问注册表项，开机启动设置失败");
                    return;
                }

                if (enable)
                {
                    // 获取当前应用程序的完整路径
                    string appPath = Application.ExecutablePath;

                    // 将路径用引号括起来，以处理路径中包含空格的情况
                    string quotedPath = $"\"{appPath}\"";

                    // 在注册表中设置启动项
                    key.SetValue(AppName, quotedPath);

                    // 记录日志
                    AddLogEntry("已启用开机自动启动");
                }
                else
                {
                    // 从注册表中删除启动项
                    // 第二个参数false表示：如果值不存在也不抛出异常
                    key.DeleteValue(AppName, false);

                    // 记录日志
                    AddLogEntry("已禁用开机自动启动");
                }
            }
        }

        /// <summary>
        /// 检查并设置开机启动相关的UI状态
        /// （此方法主要用于初始化时同步UI和实际状态）
        /// </summary>
        private void CheckAutoStartStatus()
        {
            // 方法体为空，实际功能已在UpdateSettingsUI中实现
            // 保留此方法是为了代码结构完整性
        }

        #endregion

        #region 窗口显示/隐藏和老板键功能

        /// <summary>
        /// 显示窗口
        /// </summary>
        private void ShowWindow()
        {
            Visible = true;
            WindowState = FormWindowState.Normal;
            ShowInTaskbar = true;
            AddLogEntry("窗口已显示");
        }

        /// <summary>
        /// 隐藏窗口到系统托盘
        /// </summary>
        private void HideWindow()
        {
            Visible = false;
            ShowInTaskbar = false;
            AddLogEntry("窗口已隐藏到系统托盘（按Ctrl+H显示）");
        }

        /// <summary>
        /// 切换窗口显示/隐藏状态
        /// </summary>
        private void ToggleWindowVisibility()
        {
            if (Visible)
                HideWindow();
            else
                ShowWindow();
        }

        /// <summary>
        /// 处理热键消息
        /// </summary>
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                // 热键被按下，切换窗口显示状态
                ToggleWindowVisibility();
            }
        }

        /// <summary>
        /// 退出应用程序
        /// </summary>
        private void ExitApplication()
        {
            _trayIcon.Visible = false;
            UnregisterHotKey(Handle, HOTKEY_ID);
            Application.Exit();
        }

        #endregion

        #region 事件处理方法

        /// <summary>
        /// "浏览文件夹"按钮点击事件处理
        /// 打开文件夹选择对话框，让用户选择监控文件夹
        /// </summary>
        private void btnBrowseFolder_Click(object sender, EventArgs e)
        {
            // 使用using语句确保对话框资源会被正确释放
            using (var folderDialog = new FolderBrowserDialog())
            {
                // 设置对话框的初始路径为当前已设置的路径
                folderDialog.SelectedPath = txtFolderPath.Text;

                // 显示文件夹选择对话框，如果用户点击了"确定"
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    // 更新文本框显示选中的路径
                    txtFolderPath.Text = folderDialog.SelectedPath;

                    // 更新设置中的监控文件夹路径
                    _settings.MonitoredFolder = folderDialog.SelectedPath;

                    // 立即保存更新后的设置
                    SettingsManager.SaveSettings(_settings);
                }
            }
        }

        /// <summary>
        /// "保存设置"按钮点击事件处理
        /// 将当前UI上的设置保存到配置对象并持久化
        /// </summary>
        private void btnSaveSettings_Click(object sender, EventArgs e)
        {
            // 从UI控件更新设置对象的值

            // 保存OK图片保留天数
            _settings.OkImageRetentionDays = (int)numericOKDays.Value;

            // 保存NG图片保留天数
            _settings.NgImageRetentionDays = (int)numericNGDays.Value;

            // 保存监控文件夹路径
            _settings.MonitoredFolder = txtFolderPath.Text;

            // 保存OK图片关键字
            _settings.OkKeyword = txtOKKeyword.Text;

            // 保存NG图片关键字
            _settings.NgKeyword = txtNGKeyword.Text;

            // 保存是否包含子文件夹的设置
            _settings.IncludeSubfolders = chkIncludeSubfolders.Checked;

            // 保存是否需要删除前确认的设置
            _settings.ConfirmBeforeDeletion = chkConfirmDeletion.Checked;

            // 保存检查间隔时间（分钟）
            _settings.CheckIntervalMinutes = (int)numericCheckInterval.Value;

            // 保存开机自动监控设置
            _settings.AutoStartMonitoring = chkAutoStartMonitoring.Checked;

            // 保存开机自动隐藏设置
            _settings.AutoHideOnStartup = chkAutoHideOnStart.Checked;

            // 根据新的检查间隔更新定时器间隔（转换为毫秒）
            _monitoringTimer.Interval = (int)numericCheckInterval.Value * 60000;

            // 将设置持久化到本地存储
            SettingsManager.SaveSettings(_settings);

            // 记录设置保存日志
            AddLogEntry("设置已保存");
        }

        /// <summary>
        /// "开始自动监控"按钮点击事件处理
        /// 启动自动监控功能，定期检查并删除过期文件
        /// </summary>
        private void btnStartMonitoring_Click(object sender, EventArgs e)
        {
            // 验证监控文件夹是否有效
            if (string.IsNullOrEmpty(_settings.MonitoredFolder) || !Directory.Exists(_settings.MonitoredFolder))
            {
                // 如果文件夹无效，显示错误提示
                MessageBox.Show("请选择有效的监控文件夹", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 更新监控状态为正在监控
            _isMonitoring = true;

            // 启动定时器，开始定期检查
            _monitoringTimer.Start();

            // 记录日志
            AddLogEntry("开始自动监控...");

            // 根据新状态更新UI
            UpdateUIState();
        }

        /// <summary>
        /// "停止自动监控"按钮点击事件处理
        /// 停止自动监控功能
        /// </summary>
        private void btnStopMonitoring_Click(object sender, EventArgs e)
        {
            // 更新监控状态为停止监控
            _isMonitoring = false;

            // 停止定时器
            _monitoringTimer.Stop();

            // 记录日志
            AddLogEntry("已停止自动监控");

            // 根据新状态更新UI
            UpdateUIState();
        }

        /// <summary>
        /// "手动删除"按钮点击事件处理
        /// 立即执行一次文件检查和删除操作
        /// </summary>
        private void btnManualDelete_Click(object sender, EventArgs e)
        {
            // 验证监控文件夹是否有效
            if (string.IsNullOrEmpty(_settings.MonitoredFolder) || !Directory.Exists(_settings.MonitoredFolder))
            {
                // 如果文件夹无效，显示错误提示
                MessageBox.Show("请选择有效的文件夹", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 执行文件删除操作
            PerformDeletion();
        }

        /// <summary>
        /// 监控定时器触发事件处理
        /// 当定时器到达设定间隔时触发，执行文件检查和删除
        /// </summary>
        private void MonitoringTimer_Tick(object sender, EventArgs e)
        {
            // 执行文件删除操作
            PerformDeletion();
        }

        /// <summary>
        /// "清空日志"按钮点击事件处理
        /// 清除当前所有日志记录
        /// </summary>
        private void btnClearLog_Click(object sender, EventArgs e)
        {
            // 清空日志列表
            _logEntries.Clear();

            // 更新日志显示
            UpdateLogDisplay();

            // 记录日志清空操作
            AddLogEntry("日志已清空");
        }

        /// <summary>
        /// 窗体关闭事件处理
        /// 在窗体关闭前执行必要的清理操作
        /// </summary>
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 如果用户不是选择退出，而是点击关闭按钮，则隐藏窗口
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true; // 取消关闭操作
                HideWindow();    // 隐藏窗口到系统托盘
                return;
            }

            // 停止定时器（如果存在）
            _monitoringTimer?.Stop();

            // 取消所有后台任务（如果存在）
            _cancellationTokenSource?.Cancel();

            // 保存当前设置
            SettingsManager.SaveSettings(_settings);

            // 注销热键
            UnregisterHotKey(Handle, HOTKEY_ID);

            // 隐藏托盘图标
            _trayIcon.Visible = false;
        }

        /// <summary>
        /// "开机自动启动"复选框状态变化事件处理
        /// 启用或禁用开机自动启动功能
        /// </summary>
        private void chkAutoStart_CheckedChanged(object sender, EventArgs e)
        {
            // 根据复选框状态设置开机启动
            SetAutoStart(chkAutoStart.Checked);

            // 根据开机启动状态更新相关复选框的可用性
            chkAutoStartMonitoring.Enabled = chkAutoStart.Checked;
            chkAutoHideOnStart.Enabled = chkAutoStart.Checked;

            // 如果启用了开机启动，默认勾选相关选项
            if (chkAutoStart.Checked && !chkAutoStartMonitoring.Checked)
            {
                chkAutoStartMonitoring.Checked = true;
            }

            // 保存设置
            _settings.AutoStartMonitoring = chkAutoStartMonitoring.Checked;
            _settings.AutoHideOnStartup = chkAutoHideOnStart.Checked;
            SettingsManager.SaveSettings(_settings);
        }

        /// <summary>
        /// "开机自动监控"复选框状态变化事件处理
        /// 更新开机后是否自动开始监控的设置
        /// </summary>
        private void chkAutoStartMonitoring_CheckedChanged(object sender, EventArgs e)
        {
            // 更新设置中的自动监控标志
            _settings.AutoStartMonitoring = chkAutoStartMonitoring.Checked;

            // 保存设置
            SettingsManager.SaveSettings(_settings);

            // 记录日志
            AddLogEntry($"开机自动监控已{(chkAutoStartMonitoring.Checked ? "启用" : "禁用")}");
        }

        /// <summary>
        /// "开机自动隐藏"复选框状态变化事件处理
        /// 更新开机后是否自动隐藏窗口的设置
        /// </summary>
        private void chkAutoHideOnStart_CheckedChanged(object sender, EventArgs e)
        {
            // 更新设置中的自动隐藏标志
            _settings.AutoHideOnStartup = chkAutoHideOnStart.Checked;

            // 保存设置
            SettingsManager.SaveSettings(_settings);

            // 记录日志
            AddLogEntry($"开机自动隐藏已{(chkAutoHideOnStart.Checked ? "启用" : "禁用")}");
        }

        #endregion

        #region 核心功能实现

        /// <summary>
        /// 执行文件检查和删除操作的核心方法
        /// 此方法会在后台线程执行，避免阻塞UI
        /// </summary>
        private void PerformDeletion()
        {
            // 验证监控文件夹是否有效
            if (string.IsNullOrEmpty(_settings.MonitoredFolder) || !Directory.Exists(_settings.MonitoredFolder))
            {
                AddLogEntry("监控文件夹不存在或无效");
                return;
            }

            // 记录开始检查日志
            AddLogEntry("开始检查文件...");

            // 在后台线程执行文件处理，避免UI卡顿
            // 使用Task.Run将工作放入线程池
            Task.Run(() =>
            {
                try
                {
                    // 用于记录已删除的文件列表
                    var deletedFiles = new List<string>();

                    // 获取所有符合条件的图片文件
                    var imageFiles = GetImageFiles(_settings.MonitoredFolder);

                    // 遍历每个图片文件进行处理
                    foreach (var file in imageFiles)
                    {
                        // 检查是否已请求取消操作（如程序关闭）
                        if (_cancellationTokenSource.Token.IsCancellationRequested)
                            break;

                        // 确定当前文件的保留天数（根据OK/NG类型）
                        var retentionDays = GetRetentionDaysForFile(file);

                        // 如果返回-1，表示文件不匹配任何类型，跳过处理
                        if (retentionDays == -1)
                            continue;

                        // 计算文件的存在时间（当前时间 - 文件最后修改时间）
                        var fileAge = DateTime.Now - File.GetLastWriteTime(file);

                        // 检查文件是否已过期（存在时间 > 保留天数）
                        if (fileAge.TotalDays > retentionDays)
                        {
                            // 是否需要删除确认
                            bool deleteFile = true;
                            if (_settings.ConfirmBeforeDeletion)
                            {
                                // 在UI线程显示确认对话框（因为MessageBox属于UI操作）
                                // 使用Invoke确保在主线程执行
                                Invoke(new Action(() =>
                                {
                                    deleteFile = ShowDeleteConfirmation(file, retentionDays, fileAge);
                                }));
                            }

                            // 如果确认删除
                            if (deleteFile)
                            {
                                try
                                {
                                    // 删除文件
                                    File.Delete(file);

                                    // 记录已删除的文件
                                    deletedFiles.Add(file);

                                    // 记录删除日志
                                    AddLogEntry($"已删除文件: {Path.GetFileName(file)}");
                                }
                                catch (Exception ex)
                                {
                                    // 记录删除失败日志
                                    AddLogEntry($"删除文件失败: {Path.GetFileName(file)} - {ex.Message}");
                                }
                            }
                        }
                    }

                    // 检查完成后记录结果
                    if (deletedFiles.Count == 0)
                    {
                        AddLogEntry("没有符合删除条件的文件");
                    }
                    else
                    {
                        AddLogEntry($"完成检查，共删除 {deletedFiles.Count} 个文件");
                    }
                }
                catch (Exception ex)
                {
                    // 记录操作过程中的异常
                    AddLogEntry($"执行删除操作时出错: {ex.Message}");
                }
            }, _cancellationTokenSource.Token); // 传入取消令牌，允许取消任务
        }

        /// <summary>
        /// 获取指定文件夹中所有图片文件
        /// </summary>
        /// <param name="folderPath">要搜索的文件夹路径</param>
        /// <returns>图片文件路径列表</returns>
        private List<string> GetImageFiles(string folderPath)
        {
            // 定义需要处理的图片文件扩展名
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff" };

            // 根据设置决定是否搜索子文件夹
            SearchOption searchOption = _settings.IncludeSubfolders
                ? SearchOption.AllDirectories  // 搜索所有子目录
                : SearchOption.TopDirectoryOnly;  // 仅搜索当前目录

            try
            {
                // 枚举文件夹中的所有文件，筛选出图片文件
                return Directory.EnumerateFiles(folderPath, "*.*", searchOption)
                    // 检查文件扩展名是否在图片扩展名列表中（不区分大小写）
                    .Where(file => imageExtensions.Contains(Path.GetExtension(file).ToLower()))
                    // 转换为列表并返回
                    .ToList();
            }
            catch (Exception ex)
            {
                // 记录获取文件列表时的错误（如权限不足）
                AddLogEntry($"获取文件列表时出错: {ex.Message}");
                // 返回空列表
                return new List<string>();
            }
        }

        /// <summary>
        /// 确定指定文件的保留天数（根据文件名匹配的关键字）
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>保留天数，-1表示不匹配任何类型</returns>
        private int GetRetentionDaysForFile(string filePath)
        {
            // 获取文件名（不含路径）并转换为小写，用于不区分大小写的匹配
            string fileName = Path.GetFileName(filePath).ToLower();

            // 检查是否匹配OK图片（支持多关键字，逗号分隔）
            if (!string.IsNullOrEmpty(_settings.OkKeyword))
            {
                // 将关键字字符串按逗号分割为数组，忽略空项
                var okKeywords = _settings.OkKeyword.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                // 检查文件名是否包含任何一个OK关键字（忽略大小写和前后空格）
                if (okKeywords.Any(keyword =>
                    fileName.Contains(keyword.Trim().ToLower())))  // Trim()处理关键字前后可能的空格
                {
                    // 返回OK图片的保留天数
                    return _settings.OkImageRetentionDays;
                }
            }

            // 检查是否匹配NG图片（支持多关键字，逗号分隔）
            if (!string.IsNullOrEmpty(_settings.NgKeyword))
            {
                // 将关键字字符串按逗号分割为数组，忽略空项
                var ngKeywords = _settings.NgKeyword.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                // 检查文件名是否包含任何一个NG关键字（忽略大小写和前后空格）
                if (ngKeywords.Any(keyword =>
                    fileName.Contains(keyword.Trim().ToLower())))  // Trim()处理关键字前后可能的空格
                {
                    // 返回NG图片的保留天数
                    return _settings.NgImageRetentionDays;
                }
            }

            // 不匹配任何类型，返回-1表示跳过此文件
            return -1;
        }

        /// <summary>
        /// 显示删除确认对话框，询问用户是否删除过期文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="retentionDays">保留天数</param>
        /// <param name="fileAge">文件存在时间</param>
        /// <returns>如果用户确认删除则返回true，否则返回false</returns>
        private bool ShowDeleteConfirmation(string filePath, int retentionDays, TimeSpan fileAge)
        {
            // 如果窗口当前是隐藏的，显示窗口以便用户能看到确认对话框
            if (!Visible)
            {
                ShowWindow();
            }

            // 显示确认对话框，包含文件信息和保留策略
            var result = MessageBox.Show(
                $"是否删除以下过期文件？\n{filePath}\n\n保留时间: {retentionDays} 天\n文件年龄: {fileAge.TotalDays:F1} 天",
                "确认删除",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            // 返回用户的选择（Yes表示删除）
            return result == DialogResult.Yes;
        }

        #endregion

        #region 日志功能实现

        /// <summary>
        /// 添加一条日志记录
        /// 确保在UI线程执行，避免跨线程操作异常
        /// </summary>
        /// <param name="message">日志消息内容</param>
        private void AddLogEntry(string message)
        {
            // 格式化日志条目，包含时间戳
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";

            // 检查当前线程是否为UI线程
            if (txtLog.InvokeRequired)
            {
                // 如果不是UI线程，通过Invoke在UI线程执行此方法
                txtLog.Invoke(new Action(() => AddLogEntry(message)));
                return;
            }

            // 将日志条目添加到列表
            _logEntries.Add(logEntry);

            // 限制日志数量，防止内存占用过大（最多保留1000条）
            if (_logEntries.Count > 1000)
            {
                // 移除最旧的日志条目
                _logEntries.RemoveRange(0, _logEntries.Count - 1000);
            }

            // 更新日志显示
            UpdateLogDisplay();
        }

        /// <summary>
        /// 更新日志文本框的显示内容
        /// </summary>
        private void UpdateLogDisplay()
        {
            // 将日志列表转换为字符串数组，设置为文本框的内容
            txtLog.Lines = _logEntries.ToArray();

            // 自动滚动到最新日志
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.ScrollToCaret();
        }

        #endregion

        /// <summary>
        /// 窗体加载完成事件处理
        /// 执行开机自动监控的检查和启动
        /// </summary>
        private void AutoDeleteImageForm_Load(object sender, EventArgs e)
        {
            // 如果启用了开机自动启动，并且设置了开机自动监控
            if (IsAutoStartEnabled() && _settings.AutoStartMonitoring)
            {
                // 自动触发"开始监控"按钮的点击事件，启动监控
                btnStartMonitoring.PerformClick();
            }

        }

        private void AutoDeleteImageForm_Shown(object sender, EventArgs e)
        {            
            // 如果启用了开机自动隐藏，且是通过开机启动的
            if (IsAutoStartEnabled() && _settings.AutoHideOnStartup)
            {
                // 隐藏窗口到系统托盘
                HideWindow();
            }

        }
    }
}
