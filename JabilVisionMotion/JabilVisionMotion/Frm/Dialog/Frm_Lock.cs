using BasicClassLibrary;
using BasicClassLibrary.RexHelps;
using Jabil.Module.Log.Lib;
using JabilVisionMotion.Properties;
using Sunny.UI;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JabilVisionMotion.Frm.Dialog
{
    /// <summary>
    /// 程序锁定窗体，用于限制未授权访问
    /// </summary>
    public partial class Frm_Lock : UIForm  // 统一继承自项目基类UIForm
    {
        #region 单例模式（线程安全）
        /// <summary>
        /// 窗体唯一实例
        /// </summary>
        private static readonly Lazy<Frm_Lock> _instance = new Lazy<Frm_Lock>(
            () => new Frm_Lock(),
            LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// 获取窗体实例
        /// </summary>
        public static Frm_Lock Instance => _instance.Value;

        /// <summary>
        /// 私有构造函数防止外部实例化
        /// </summary>
        public Frm_Lock()
        {
            InitializeComponent();
            InitializeSettings();
        }
        #endregion

        #region 字段
        // 窗体拖动相关字段（改为实例变量，避免静态变量冲突）
        private bool _isDragging;
        private int _dragStartX;
        private int _dragStartY;

        // 自动移动相关字段（规范化命名）
        private bool _isXIncreasing = true;  // X方向是否递增
        private bool _isYIncreasing = true;  // Y方向是否递增
        private bool _isMoving = true;       // 是否自动移动
        private int _idleTicks = 0;          // 空闲计时（ ticks）
        private const int IdleThreshold = 333; // 空闲阈值（约5秒：333 * 15ms）
        private bool _stopUpdating = false;  // 是否停止更新
        private System.Windows.Forms.Timer _moveTimer; // 替代线程循环的定时器
        #endregion

        #region 初始化
        /// <summary>
        /// 初始化窗体设置
        /// </summary>
        private void InitializeSettings()
        {
            // 启用双缓冲减少闪烁
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint, true);

            // 初始化移动定时器（替代线程循环，更安全高效）
            _moveTimer = new System.Windows.Forms.Timer
            {
                Interval = 15 // 15ms刷新一次，与原逻辑保持一致
            };
            _moveTimer.Tick += MoveTimer_Tick;

            // 绑定事件（集中管理）
            BindEvents();
        }

        /// <summary>
        /// 绑定所有事件
        /// </summary>
        private void BindEvents()
        {
            // 拖动相关事件
            MouseDown += Frm_Lock_MouseDown;
            MouseUp += Frm_Lock_MouseUp;
            MouseMove += Frm_Lock_MouseMove;

            // 其他事件
            Load += Frm_Lock_Load;
            Click += Frm_Lock_Click;
            Shown += Frm_Lock_Shown;
            FormClosing += Frm_Lock_FormClosing;
            tbx_password.TextChanged += Tbx_Password_TextChanged;
            tbx_password.Click += Tbx_Password_Click;
            btn_unlock.Click += Btn_Unlock_Click;
        }
        #endregion

        #region 窗体拖动逻辑
        /// <summary>
        /// 鼠标按下事件（开始拖动）
        /// </summary>
        private void Frm_Lock_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                _isDragging = true;
                _dragStartX = e.Location.X;
                _dragStartY = e.Location.Y;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 鼠标释放事件（结束拖动）
        /// </summary>
        private void Frm_Lock_MouseUp(object sender, MouseEventArgs e)
        {
            try
            {
                _isDragging = false;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 鼠标移动事件（处理拖动）
        /// </summary>
        private void Frm_Lock_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (_isDragging)
                {
                    Left += e.Location.X - _dragStartX;
                    Top += e.Location.Y - _dragStartY;
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }
        #endregion

        #region 自动移动逻辑
        /// <summary>
        /// 移动定时器Tick事件（替代原线程循环）
        /// </summary>
        private void MoveTimer_Tick(object sender, EventArgs e)
        {
            if (_stopUpdating)
            {
                _moveTimer.Stop();
                return;
            }

            try
            {
                // 确保密码框获得焦点
                if (!tbx_password.Focused)
                {
                    tbx_password.Focus();
                }

                // 处理自动移动逻辑
                if (!_isMoving)
                {
                    _idleTicks++;
                    if (_idleTicks >= IdleThreshold)
                    {
                        // 空闲时间到，恢复移动
                        _isMoving = true;
                        _idleTicks = 0;
                        Opacity = 0.4;
                    }
                    return;
                }

                // 计算新位置（边界检测）
                var mainForm = Frm_Main.Instance;
                var newX = Location.X;
                var newY = Location.Y;

                // X方向移动
                newX += _isXIncreasing ? 1 : -1;
                // 边界检查（主窗体范围内）
                if (newX >= mainForm.Location.X + mainForm.Width - Width - 10)
                    _isXIncreasing = false;
                if (newX <= mainForm.Location.X + 10)
                    _isXIncreasing = true;

                // Y方向移动
                newY += _isYIncreasing ? 1 : -1;
                // 边界检查
                if (newY >= mainForm.Location.Y + mainForm.Height - Height - 10)
                    _isYIncreasing = false;
                if (newY <= mainForm.Location.Y)
                    _isYIncreasing = true;

                // 更新位置
                Location = new Point(newX, newY);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }
        #endregion

        #region 事件处理
        /// <summary>
        /// 窗体加载事件
        /// </summary>
        private void Frm_Lock_Load(object sender, EventArgs e)
        {
            try
            {
                TopMost = true;
                Opacity = 0.4; // 初始半透明
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 窗体显示事件（启动定时器）
        /// </summary>
        private void Frm_Lock_Shown(object sender, EventArgs e)
        {
            _moveTimer.Start();
        }

        /// <summary>
        /// 窗体点击事件（切换移动状态）
        /// </summary>
        private void Frm_Lock_Click(object sender, EventArgs e)
        {
            ToggleMoveState();
        }

        /// <summary>
        /// 密码框文本变化事件
        /// </summary>
        private void Tbx_Password_TextChanged(object sender, EventArgs e)
        {
            ActivateInputMode();
        }

        /// <summary>
        /// 密码框点击事件
        /// </summary>
        private void Tbx_Password_Click(object sender, EventArgs e)
        {
            ActivateInputMode();
        }

        /// <summary>
        /// 解锁按钮点击事件
        /// </summary>
        private void Btn_Unlock_Click(object sender, EventArgs e)
        {
            try
            {
                // 密码应从配置获取，此处保留原逻辑
                const string correctPassword = "1";

                if (tbx_password.Text.Trim() == correctPassword)
                {
                    // 解锁成功
                    Frm_Main.IsLocked = false;
                    Frm_Main.Instance.toolStripLabel_锁定.Image = Resources.未锁定;
                    Close();
                }
                else
                {
                    // 密码错误处理
                    tbx_password.Clear();
                    ShowErrorMsg("密码错误！");
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 窗体关闭事件
        /// </summary>
        private void Frm_Lock_FormClosing(object sender, FormClosingEventArgs e)
        {
            _stopUpdating = true;
            _moveTimer?.Dispose(); // 释放定时器资源
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 切换移动状态
        /// </summary>
        private void ToggleMoveState()
        {
            try
            {
                _isMoving = !_isMoving;
                _idleTicks = 0; // 重置空闲计时

                if (!_isMoving)
                {
                    // 进入输入模式
                    Opacity = 1;
                    tbx_password.Focus();
                }
                else
                {
                    // 恢复移动模式
                    Opacity = 0.4;
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 激活输入模式（停止移动并聚焦）
        /// </summary>
        private void ActivateInputMode()
        {
            if (_isMoving)
            {
                _isMoving = false;
                _idleTicks = 0;
                Opacity = 1;
                tbx_password.Focus();
            }
        }

        /// <summary>
        /// 显示错误信息
        /// </summary>
        private void ShowErrorMsg(string message)
        {
            label1.Text = message;
            label1.ForeColor = Color.Red;

            // 使用定时器恢复默认文本，避免Thread.Sleep阻塞UI
            var tempTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            tempTimer.Tick += (s, e) =>
            {
                label1.Text = "锁定中......";
                label1.ForeColor = Color.Black;
                tempTimer.Stop();
                tempTimer.Dispose();
            };
            tempTimer.Start();
        }
        #endregion
    }
}
