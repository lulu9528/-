using Jabil.Module.Vision.Common.Dtos;
using Sunny.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Jabil.Module.Vision.Frm
{
    /// <summary>
    /// 检测点位编辑窗体
    /// 功能：用于添加新的视觉检测点位或修改已有检测点位的详细参数信息
    /// 设计模式：单例模式，确保应用中只有一个该窗体的实例
    /// </summary>
    public partial class Frm_InspectionEdit : UIForm
    {
        #region 常量定义
        // 输入验证提示信息 - 集中管理用户输入错误时的提示文本
        private const string InvalidIntIndexMsg = "检测点位索引必须为非负整数";  // 索引验证失败提示
        private const string InvalidExposureMsg = "曝光值必须为非负数字";       // 曝光值验证失败提示
        private const string InvalidContrastMsg = "对比度值必须为非负数字";     // 对比度验证失败提示
        private const string EmptyNameMsg = "请输入检测点位名称";               // 名称为空提示
        private const string RefreshFailedMsg = "刷新数据失败：{0}";            // 数据刷新失败提示（带参数）

        // 工具类型常量 - 定义系统支持的视觉工具类型，用于工具分类和下拉框数据加载
        private const string ToolBlockType = "CogToolBlockTool";                 // 检测工具类型
        private const string AcqFifoType = "CogAcqFifoTool";                     // 取像工具类型
        private const string CalibNPointType = "CogCalibNPointToNPointTool";     // N点标定工具类型
        private const string CalibCheckerboardType = "CogCalibCheckerboardTool"; // 棋盘格标定工具类型
        #endregion

        #region 字段与属性
        /// <summary>
        /// 检测点位数据传输对象
        /// 作用：存储用户编辑后的检测点位信息，供调用方获取结果
        /// </summary>
        public VisionInspectionDto InspectionDto { get; private set; }

        /// <summary>
        /// 窗体单例实例
        /// 作用：保存当前窗体的唯一实例引用
        /// </summary>
        private static Frm_InspectionEdit _instance;

        /// <summary>
        /// 线程同步锁
        /// 作用：确保多线程环境下创建单例实例的线程安全性
        /// </summary>
        private static readonly object _syncLock = new object();

        /// <summary>
        /// 窗体单例访问属性
        /// 实现：双重检查锁定模式（double-checked locking），高效且线程安全地获取唯一实例
        /// </summary>
        public static Frm_InspectionEdit Instance
        {
            get
            {
                // 第一次检查：未加锁，提高性能
                if (_instance == null || _instance.IsDisposed)
                {
                    // 加锁确保只有一个线程进入实例创建逻辑
                    lock (_syncLock)
                    {
                        // 第二次检查：防止多线程同时通过第一次检查后重复创建实例
                        if (_instance == null || _instance.IsDisposed)
                        {
                            _instance = new Frm_InspectionEdit();
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
        /// 设计目的：防止外部直接创建实例，确保只能通过Instance属性获取单例
        /// </summary>
        private Frm_InspectionEdit()
        {
            // 初始化窗体控件（设计器自动生成的代码）
            InitializeComponent();
            // 初始化控件样式和属性
            InitializeControls();
            // 绑定事件处理方法
            BindEvents();
        }

        /// <summary>
        /// 初始化控件属性
        /// 作用：统一设置窗体上所有控件的样式、尺寸等属性，保证UI一致性
        /// </summary>
        private void InitializeControls()
        {
            // 统一设置文本框样式和输入限制
            SetTextBoxStyle(txtInspectionName, 50);       // 检测点位名称：最大长度50
            SetTextBoxStyle(txtInspectionType, 50);       // 检测点位类型：最大长度50
            SetTextBoxStyle(txtInspectionDefinition, 50); // 检测点位定义：最大长度50
            SetTextBoxStyle(txtInspectionIndex, 10);      // 检测点位索引：最大长度10
            SetTextBoxStyle(txtProductColor, 50);         // 产品颜色定义：最大长度50
            SetTextBoxStyle(txtExposure, 20);             // 曝光值：最大长度20
            SetTextBoxStyle(txtContrast, 20);             // 对比度：最大长度20

            // 统一设置下拉框样式
            SetComboBoxStyle(cbxAcqName);
            SetComboBoxStyle(cbxToolBlockName);
            SetComboBoxStyle(cbxCalibName);

            // 统一设置按钮样式
            btnSave.Text = "保存";       // 保存按钮文本
            btnCancel.Text = "取消";     // 取消按钮文本
            btnSave.Size = btnCancel.Size = new Size(120, 35); // 统一按钮大小
        }

        /// <summary>
        /// 统一设置下拉框样式
        /// </summary>
        private void SetComboBoxStyle(UIComboBox comboBox)
        {
            comboBox.Font = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
            comboBox.Size = new Size(250, 36);
            comboBox.Margin = new Padding(5);
        }

        /// <summary>
        /// 绑定事件处理方法
        /// 作用：集中管理窗体事件绑定，便于维护事件与处理方法的关联关系
        /// </summary>
        private void BindEvents()
        {
            btnCancel.Click += BtnCancel_Click;  // 取消按钮点击事件
            btnSave.Click += BtnSave_Click;      // 保存按钮点击事件
        }

        /// <summary>
        /// 统一设置文本框样式
        /// 作用：封装文本框的通用样式设置，避免重复代码，便于统一修改
        /// </summary>
        /// <param name="textBox">需要设置样式的文本框控件</param>
        /// <param name="maxLength">文本框允许的最大输入长度</param>
        private void SetTextBoxStyle(TextBox textBox, int maxLength)
        {
            textBox.Font = new Font("宋体", 16.2F, FontStyle.Regular, GraphicsUnit.Point, 134); // 设置字体
            textBox.MaxLength = maxLength;         // 设置最大输入长度
            textBox.Multiline = true;              // 允许多行（此处用于调整高度）
            textBox.Size = new Size(250, 36);      // 设置固定尺寸
            textBox.Margin = new Padding(5);       // 设置统一边距
        }
        #endregion

        #region 事件处理
        /// <summary>
        /// 取消按钮点击事件处理
        /// 功能：设置对话框结果为取消，关闭窗体
        /// </summary>
        private void BtnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;  // 设置对话框结果为取消
            Close();                             // 关闭窗体
        }

        /// <summary>
        /// 保存按钮点击事件处理
        /// 功能：验证用户输入，处理数据并保存到DTO对象，关闭窗体
        /// </summary>
        private void BtnSave_Click(object sender, EventArgs e)
        {
            // 验证输入有效性，若验证失败则直接返回
            if (!ValidateInput())
                return;

            try
            {
                // 解析输入值（已通过验证，可安全转换）
                int inspectionIndex = int.Parse(txtInspectionIndex.Text.Trim());       // 检测点位索引
                double exposure = double.Parse(txtExposure.Text.Trim());               // 曝光值
                double contrast = double.Parse(txtContrast.Text.Trim());               // 对比度

                // 生成绑定代码（格式：V+3位数字，如V001）
                // 安全获取当前检测点位数量，若为null则使用0
                int count = VisionSystem.Instance?.ListVisionInspection?.Count ?? 0;
                string bindCode = $"V{(count + 1).ToString().PadLeft(3, '0')}";

                // 构建DTO对象，存储用户输入的所有信息
                InspectionDto = new VisionInspectionDto
                {
                    检测点位名称 = txtInspectionName.Text.Trim(),
                    检测点位类型 = txtInspectionType.Text.Trim(),
                    检测点位定义 = txtInspectionDefinition.Text.Trim(),
                    检测点位索引 = inspectionIndex,
                    绑定取像工具名称 = cbxAcqName.Text.Trim(),
                    绑定检测工具名称 = cbxToolBlockName.Text.Trim(),
                    绑定标定工具名称 = cbxCalibName.Text.Trim(),
                    产品颜色定义 = txtProductColor.Text.Trim(),
                    取像工具曝光值 = exposure,
                    取像工具对比度值 = contrast,
                    绑定代码 = bindCode
                };

                // 设置对话框结果为确定，关闭窗体
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                // 显示保存失败的错误信息
                UIMessageBox.ShowError2($"保存失败：{ex.Message}");
            }
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 刷新窗体数据
        /// 功能：加载工具列表到下拉框，根据传入的DTO对象初始化控件值（新增/编辑模式）
        /// </summary>
        /// <param name="dto">待编辑的检测点位数据，为null时表示新增模式</param>
        public void RefreshData(VisionInspectionDto dto = null)
        {
            try
            {
                // 清空所有控件当前值
                ClearControlValues();
                // 初始化工具下拉框选项
                InitToolDropDowns();

                // 如果传入了DTO对象，说明是编辑模式，加载数据到控件
                if (dto != null)
                {
                    LoadDtoData(dto);
                }
                else
                {
                    // 新增模式，设置默认索引值
                    txtInspectionIndex.Text = GetDefaultIndex();
                }
            }
            catch (Exception ex)
            {
                // 显示数据刷新失败的错误信息
                UIMessageBox.ShowError2(string.Format(RefreshFailedMsg, ex.Message));
            }
        }
        #endregion

        #region 私有辅助方法
        /// <summary>
        /// 验证用户输入的有效性
        /// 功能：检查所有必填项和数据格式是否符合要求
        /// </summary>
        /// <returns>验证通过返回true，否则返回false</returns>
        private bool ValidateInput()
        {
            // 验证检测点位名称（不能为空）
            if (string.IsNullOrWhiteSpace(txtInspectionName.Text.Trim()))
            {
                ShowValidationError(txtInspectionName, EmptyNameMsg);
                return false;
            }

            // 验证检测点位索引（必须是非负整数）
            if (!int.TryParse(txtInspectionIndex.Text.Trim(), out int index) || index < 0)
            {
                ShowValidationError(txtInspectionIndex, InvalidIntIndexMsg);
                return false;
            }

            // 验证曝光值（必须是非负数）
            if (!double.TryParse(txtExposure.Text.Trim(), out double exposure) || exposure < 0)
            {
                ShowValidationError(txtExposure, InvalidExposureMsg);
                return false;
            }

            // 验证对比度值（必须是非负数）
            if (!double.TryParse(txtContrast.Text.Trim(), out double contrast) || contrast < 0)
            {
                ShowValidationError(txtContrast, InvalidContrastMsg);
                return false;
            }

            // 所有验证通过
            return true;
        }

        /// <summary>
        /// 显示验证错误信息并聚焦到对应控件
        /// 功能：统一处理验证失败的交互逻辑，提升用户体验
        /// </summary>
        /// <param name="control">验证失败的控件</param>
        /// <param name="message">要显示的错误信息</param>
        private void ShowValidationError(Control control, string message)
        {
            UIMessageBox.ShowError2(message);  // 显示错误提示框
            control.Focus();                   // 将焦点设置到错误控件
            if (control is TextBox textBox)
                textBox.SelectAll();           // 如果是文本框，选中所有内容便于修改
        }

        /// <summary>
        /// 清空所有控件值
        /// 功能：重置窗体上所有输入控件的值，为加载新数据做准备
        /// </summary>
        private void ClearControlValues()
        {
            txtInspectionName.Text = string.Empty;
            txtInspectionType.Text = string.Empty;
            txtInspectionDefinition.Text = string.Empty;
            txtInspectionIndex.Text = string.Empty;
            cbxAcqName.Text = string.Empty;
            cbxToolBlockName.Text = string.Empty;
            cbxCalibName.Text = string.Empty;
            txtProductColor.Text = string.Empty;
            txtExposure.Text = string.Empty;
            txtContrast.Text = string.Empty;
        }

        /// <summary>
        /// 获取默认的检测点位索引
        /// 功能：新增检测点位时，根据现有数量自动生成默认索引（当前数量+1）
        /// </summary>
        /// <returns>默认索引的字符串表示</returns>
        private string GetDefaultIndex()
        {
            // 安全获取当前检测点位数量，若为null则返回"0"
            return VisionSystem.Instance?.ListVisionInspection?.Count.ToString() ?? "0";
        }

        /// <summary>
        /// 初始化工具下拉框选项
        /// 功能：从系统中加载所有可用工具，按类型分类添加到对应的下拉框中（去重处理）
        /// </summary>
        private void InitToolDropDowns()
        {
            // 清空现有选项，避免数据残留
            cbxToolBlockName.Items.Clear();
            cbxAcqName.Items.Clear();
            cbxCalibName.Items.Clear();

            // 安全获取工具列表，若为null则直接返回
            var visionTools = VisionSystem.Instance?.ListVisionTool;
            if (visionTools == null) return;

            // 遍历所有工具，按类型添加到对应的下拉框
            foreach (var tool in visionTools)
            {
                // 跳过空对象或工具名称为空的项
                if (tool == null || string.IsNullOrWhiteSpace(tool.检测工具名称))
                    continue;

                // 根据工具类型添加到对应下拉框
                switch (tool.检测工具类型)
                {
                    case ToolBlockType:
                        AddUniqueItem(cbxToolBlockName, tool.检测工具名称);
                        break;
                    case AcqFifoType:
                        AddUniqueItem(cbxAcqName, tool.检测工具名称);
                        break;
                    case CalibNPointType:
                        AddUniqueItem(cbxCalibName, tool.检测工具名称);
                        break;
                    case CalibCheckerboardType:
                        // 棋盘格标定工具暂不添加到下拉框
                        break;
                }
            }
        }

        /// <summary>
        /// 向下拉框添加不重复的项
        /// 功能：确保下拉框中没有重复选项，保持数据唯一性
        /// </summary>
        /// <param name="comboBox">目标下拉框控件</param>
        /// <param name="itemText">要添加的选项文本</param>
        private void AddUniqueItem(UIComboBox comboBox, string itemText)
        {
            // 只有当下拉框中不包含该选项时才添加
            if (!comboBox.Items.Contains(itemText))
            {
                comboBox.Items.Add(itemText);
            }
        }

        /// <summary>
        /// 将DTO数据加载到控件
        /// 功能：编辑模式时，将现有检测点位的数据填充到对应的输入控件中
        /// </summary>
        /// <param name="dto">包含检测点位数据的DTO对象</param>
        private void LoadDtoData(VisionInspectionDto dto)
        {
            // 为每个控件赋值，使用null合并运算符处理可能的null值
            txtInspectionName.Text = dto.检测点位名称 ?? string.Empty;
            txtInspectionType.Text = dto.检测点位类型 ?? string.Empty;
            txtInspectionDefinition.Text = dto.检测点位定义 ?? string.Empty;
            txtInspectionIndex.Text = dto.检测点位索引.ToString();
            cbxAcqName.Text = dto.绑定取像工具名称 ?? string.Empty;
            cbxToolBlockName.Text = dto.绑定检测工具名称 ?? string.Empty;
            cbxCalibName.Text = dto.绑定标定工具名称 ?? string.Empty;
            txtProductColor.Text = dto.产品颜色定义 ?? string.Empty;
            txtExposure.Text = dto.取像工具曝光值.ToString();
            txtContrast.Text = dto.取像工具对比度值.ToString();
        }
        #endregion
    }


}
