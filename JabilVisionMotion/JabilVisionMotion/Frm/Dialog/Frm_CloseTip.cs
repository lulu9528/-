using Sunny.UI;
using System;
using System.Threading;
using System.Windows.Forms;

namespace JabilVisionMotion.Frm.Dialog
{
    /// <summary>
    /// 关闭提示窗口，用于在程序关闭时显示确认信息
    /// </summary>
    public partial class Frm_CloseTip : UIForm
    {
        #region 单例模式（线程安全）
        /// <summary>
        /// 窗体唯一实例（线程安全的延迟初始化）
        /// </summary>
        private static readonly Lazy<Frm_CloseTip> _instance = new Lazy<Frm_CloseTip>(
            () => new Frm_CloseTip(),
            LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// 获取窗体唯一实例
        /// </summary>
        public static Frm_CloseTip Instance => _instance.Value;

        /// <summary>
        /// 私有构造函数防止外部实例化
        /// </summary>
        public Frm_CloseTip()
        {
            InitializeComponent();
            InitializeFormStyles();
        }
        #endregion

        #region 初始化方法
        /// <summary>
        /// 初始化窗体样式和属性
        /// </summary>
        private void InitializeFormStyles()
        {
            // 启用双缓冲减少闪烁
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint, true);

            // 确保窗口置顶
            TopMost = true;

            // 可根据需要添加其他初始化设置
            // 例如：设置默认标题、尺寸或位置
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 显示关闭提示对话框
        /// </summary>
        /// <returns>如果用户确认关闭则返回true，否则返回false</returns>
        public bool ShowCloseConfirmation()
        {
            // 根据实际需求设置提示信息
            // 假设存在名为lbl_Message的标签控件
            if (lbl_Message != null)
            {
                lbl_Message.Text = "确定要关闭程序吗？未保存的数据可能会丢失。";
            }

            // 显示对话框并返回结果
            return ShowDialog() == DialogResult.Yes;
        }
        #endregion

        #region 事件处理
        /// <summary>
        /// 确认按钮点击事件（示例）
        /// </summary>
        private void btn_Confirm_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Yes;
            Close();
        }

        /// <summary>
        /// 取消按钮点击事件（示例）
        /// </summary>
        private void btn_Cancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.No;
            Close();
        }
        #endregion

    }
}
