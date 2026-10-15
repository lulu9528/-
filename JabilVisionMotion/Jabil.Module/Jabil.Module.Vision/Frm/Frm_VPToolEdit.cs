using Jabil.Module.Vision.Common.Dtos;
using Sunny.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Jabil.Module.Vision.Frm
{
    /// <summary>
    /// 视觉工具编辑窗体
    /// 功能：用于添加和修改视觉检测工具的配置信息
    /// 设计模式：线程安全的单例模式
    /// </summary>
    public partial class Frm_VpToolEdit : UIForm
    {
        #region 常量定义
        /// <summary>
        /// 工具名称最大长度限制
        /// </summary>
        private const int MaxToolNameLength = 50;

        /// <summary>
        /// 工具类型选项
        /// </summary>
        private static readonly List<string> _toolTypeOptions = new List<string>
        {
            "CogAcqFifoTool",
            "CogCalibNPointToNPointTool",
            "CogCalibCheckerboardTool",
            "CogToolBlockTool"
        };

        /// <summary>
        /// 工具名称输入提示文本
        /// </summary>
        private const string ToolNameWatermark = "请输入检测工具名称";
        #endregion

        #region 公共属性
        /// <summary>
        /// 选中的工具类型
        /// </summary>
        public string ToolType { get; private set; }

        /// <summary>
        /// 输入的工具名称
        /// </summary>
        public string ToolName { get; private set; }
        #endregion

        #region 单例模式实现
        /// <summary>
        /// 窗体单例实例
        /// </summary>
        private static Frm_VpToolEdit _instance;

        /// <summary>
        /// 线程同步锁
        /// 确保多线程环境下实例创建的安全性
        /// </summary>
        private static readonly object _syncLock = new object();

        /// <summary>
        /// 单例实例访问属性
        /// 采用双重检查锁定模式确保线程安全
        /// </summary>
        public static Frm_VpToolEdit Instance
        {
            get
            {
                if (_instance == null || _instance.IsDisposed)
                {
                    lock (_syncLock)
                    {
                        if (_instance == null || _instance.IsDisposed)
                        {
                            // 确保在UI线程创建控件实例
                            if (Application.OpenForms.Count > 0)
                            {
                                _instance = Application.OpenForms[0]
                                    .Invoke(new Func<Frm_VpToolEdit>(() => new Frm_VpToolEdit())) as Frm_VpToolEdit;
                            }
                            else
                            {
                                _instance = new Frm_VpToolEdit();
                            }
                        }
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region 构造函数与初始化
        /// <summary>
        /// 私有构造函数
        /// 防止外部直接实例化，确保单例模式有效性
        /// </summary>
        private Frm_VpToolEdit()
        {
            try
            {
                InitializeComponent();
                InitializeFormSettings();
                InitializeControls();
                RegisterEvents();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// 初始化窗体基本设置
        /// </summary>
        private void InitializeFormSettings()
        {
            Text = "添加检测工具";
            MaximizeBox = false;
            MinimizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            KeyPreview = true; // 确保窗体可以捕获按键事件
        }

        /// <summary>
        /// 初始化控件
        /// </summary>
        private void InitializeControls()
        {
            InitializeToolTypeComboBox();
            InitializeToolNameTextBox();
        }

        /// <summary>
        /// 初始化工具类型下拉框
        /// </summary>
        private void InitializeToolTypeComboBox()
        {
            if (uiComboBoxToolType.Items.Count == 0)
            {
                uiComboBoxToolType.Items.AddRange(_toolTypeOptions.ToArray());
            }

            if (uiComboBoxToolType.Items.Count > 0)
                uiComboBoxToolType.SelectedIndex = 0;
        }

        /// <summary>
        /// 初始化工具名称文本框
        /// </summary>
        private void InitializeToolNameTextBox()
        {
            txtBoxToolName.Text = ToolNameWatermark;
            txtBoxToolName.ForeColor = SystemColors.GrayText;
            txtBoxToolName.MaxLength = MaxToolNameLength; // 设置最大长度限制
        }

        /// <summary>
        /// 注册事件处理方法
        /// </summary>
        private void RegisterEvents()
        {
            txtBoxToolName.GotFocus += TxtBoxToolName_GotFocus;
            txtBoxToolName.LostFocus += TxtBoxToolName_LostFocus;
            btnAdd.Click += BtnAdd_Click;
            btnCancel.Click += BtnCancel_Click;
            KeyDown += FrmVpToolEdit_KeyDown;
        }
        #endregion

        #region 事件处理方法
        /// <summary>
        /// 工具名称文本框获得焦点事件
        /// </summary>
        private void TxtBoxToolName_GotFocus(object sender, EventArgs e)
        {
            if (txtBoxToolName.Text == ToolNameWatermark &&
                txtBoxToolName.ForeColor == SystemColors.GrayText)
            {
                txtBoxToolName.Text = string.Empty;
                txtBoxToolName.ForeColor = SystemColors.WindowText;
            }
        }

        /// <summary>
        /// 工具名称文本框失去焦点事件
        /// </summary>
        private void TxtBoxToolName_LostFocus(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBoxToolName.Text))
            {
                txtBoxToolName.Text = ToolNameWatermark;
                txtBoxToolName.ForeColor = SystemColors.GrayText;
            }
        }

        /// <summary>
        /// 添加按钮点击事件
        /// </summary>
        private void BtnAdd_Click(object sender, EventArgs e)
        {
            if (ValidateInput())
            {
                ToolName = txtBoxToolName.Text.Trim();
                ToolType = uiComboBoxToolType.Text;
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        private void BtnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        /// <summary>
        /// 窗体按键事件
        /// 支持Enter确认和Escape取消快捷操作
        /// </summary>
        private void FrmVpToolEdit_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                BtnAdd_Click(sender, e);
                e.SuppressKeyPress = true; // 防止系统提示音
            }
            else if (e.KeyCode == Keys.Escape)
            {
                BtnCancel_Click(sender, e);
            }
        }
        #endregion

        #region 输入验证
        /// <summary>
        /// 验证用户输入是否有效
        /// </summary>
        /// <returns>验证通过返回true，否则返回false</returns>
        private bool ValidateInput()
        {
            string toolName = GetSanitizedToolName();

            if (!IsValidToolName(toolName))
            {
                ShowWarning($"请输入有效的检测工具名称（1-{MaxToolNameLength}个字符）");
                txtBoxToolName.Focus();
                return false;
            }

            if (!IsValidToolType(uiComboBoxToolType.Text))
            {
                ShowWarning("请选择有效的检测工具类型");
                uiComboBoxToolType.Focus();
                return false;
            }

            return true;
        }

        /// <summary>
        /// 获取经过处理的工具名称
        /// </summary>
        private string GetSanitizedToolName()
        {
            if (txtBoxToolName.ForeColor == SystemColors.GrayText)
                return string.Empty;

            return txtBoxToolName.Text.Trim();
        }

        /// <summary>
        /// 验证工具名称是否有效
        /// </summary>
        private bool IsValidToolName(string toolName)
        {
            return !string.IsNullOrWhiteSpace(toolName) && toolName.Length <= MaxToolNameLength;
        }

        /// <summary>
        /// 验证工具类型是否有效
        /// </summary>
        private bool IsValidToolType(string toolType)
        {
            return !string.IsNullOrWhiteSpace(toolType) && _toolTypeOptions.Contains(toolType);
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 刷新窗体数据
        /// </summary>
        /// <param name="dto">工具DTO对象，为空则重置表单</param>
        public void RefreshData(VisionToolDto dto = null)
        {
            // 确保在UI线程执行刷新操作
            if (InvokeRequired)
            {
                Invoke(new Action<VisionToolDto>(RefreshData), dto);
                return;
            }

            if (dto != null)
            {
                txtBoxToolName.Text = dto.检测工具名称 ?? string.Empty;
                txtBoxToolName.ForeColor = SystemColors.WindowText;
                uiComboBoxToolType.Text = dto.检测工具类型 ?? string.Empty;
            }
            else
            {
                txtBoxToolName.Text = ToolNameWatermark;
                txtBoxToolName.ForeColor = SystemColors.GrayText;
                if (uiComboBoxToolType.Items.Count > 0)
                    uiComboBoxToolType.SelectedIndex = 0;
            }
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 显示警告信息
        /// </summary>
        private void ShowWarning(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("警告信息不能为空", nameof(message));

            UIMessageBox.ShowWarning(message);
        }
        #endregion
    }
}
