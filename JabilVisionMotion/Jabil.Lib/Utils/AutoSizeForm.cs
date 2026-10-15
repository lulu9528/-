using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Jabil.Lib.Utils
{
    /// <summary>
    /// 窗体控件自动缩放工具类
    /// 功能：记录窗体及控件的初始大小和位置，当窗体尺寸改变时，按比例调整所有控件的大小和位置
    /// 适用场景：需要支持窗体缩放的Windows Forms应用程序
    /// </summary>
    public class AutoSizeForm
    {
        /// <summary>
        /// 存储控件初始位置和大小的结构体
        /// </summary>
        public struct ControlRect
        {
            /// <summary>
            /// 控件左上角X坐标
            /// </summary>
            public int Left;
            /// <summary>
            /// 控件左上角Y坐标
            /// </summary>
            public int Top;
            /// <summary>
            /// 控件宽度
            /// </summary>
            public int Width;
            /// <summary>
            /// 控件高度
            /// </summary>
            public int Height;
        }

        /// <summary>
        /// 存储所有控件初始位置和大小的集合
        /// 索引0：窗体本身的初始尺寸；索引1及以后：各级子控件的初始信息
        /// </summary>
        private readonly List<ControlRect> _originalControlRects = new List<ControlRect>();

        /// <summary>
        /// 当前处理的控件索引（用于缩放时遍历控件集合）
        /// </summary>
        private int _currentControlIndex;

        /// <summary>
        /// 是否已完成初始尺寸记录（防止重复初始化）
        /// </summary>
        private bool _isInitialized;

        /// <summary>
        /// 记录窗体及所有子控件的初始大小和位置
        /// 注意：需在窗体加载完成后（如Load事件中）调用，确保所有控件已初始化
        /// </summary>
        /// <param name="mainForm">需要处理的主窗体</param>
        public void InitializeControlSizes(Control mainForm)
        {
            // 防止重复初始化
            if (_isInitialized) return;

            // 清空历史记录
            _originalControlRects.Clear();

            // 记录窗体本身的初始尺寸（索引0固定为窗体）
            var formRect = new ControlRect
            {
                Left = mainForm.Left,
                Top = mainForm.Top,
                Width = mainForm.Width,
                Height = mainForm.Height
            };
            _originalControlRects.Add(formRect);

            // 递归记录所有子控件的初始信息
            AddChildControls(mainForm);

            // 标记初始化完成
            _isInitialized = true;
        }

        /// <summary>
        /// 递归收集所有子控件的初始位置和大小
        /// </summary>
        /// <param name="parentControl">父控件容器（如Panel、GroupBox等）</param>
        private void AddChildControls(Control parentControl)
        {
            // 空引用检查：防止传入null控件
            if (parentControl == null || parentControl.Controls == null) return;

            foreach (Control childControl in parentControl.Controls)
            {
                // 跳过已释放的控件
                if (childControl.IsDisposed) continue;

                // 记录当前子控件的初始信息
                var controlRect = new ControlRect
                {
                    Left = childControl.Left,
                    Top = childControl.Top,
                    Width = childControl.Width,
                    Height = childControl.Height
                };
                _originalControlRects.Add(controlRect);

                // 递归处理嵌套控件（如Panel中的Button）
                if (childControl.Controls.Count > 0)
                {
                    AddChildControls(childControl);
                }
            }
        }

        /// <summary>
        /// 根据当前窗体尺寸，按比例调整所有控件的大小和位置
        /// 注意：需在窗体的Resize事件中调用
        /// </summary>
        /// <param name="mainForm">当前窗体实例</param>
        public void AutoScaleControls(Control mainForm)
        {
            // 未初始化则直接返回（避免异常）
            if (!_isInitialized || _originalControlRects.Count == 0) return;

            // 计算宽度和高度的缩放比例（当前尺寸 / 初始尺寸）
            float widthScale = (float)mainForm.Width / _originalControlRects[0].Width;
            float heightScale = (float)mainForm.Height / _originalControlRects[0].Height;

            // 重置控件索引（从1开始，因为索引0是窗体本身）
            _currentControlIndex = 1;

            // 递归缩放所有子控件
            ScaleChildControls(mainForm, widthScale, heightScale);
        }

        /// <summary>
        /// 递归缩放子控件的大小和位置
        /// </summary>
        /// <param name="parentControl">父控件容器</param>
        /// <param name="widthScale">宽度缩放比例</param>
        /// <param name="heightScale">高度缩放比例</param>
        private void ScaleChildControls(Control parentControl, float widthScale, float heightScale)
        {
            // 空引用检查
            if (parentControl == null || parentControl.Controls == null) return;

            foreach (Control childControl in parentControl.Controls)
            {
                // 跳过已释放的控件
                if (childControl.IsDisposed) continue;

                // 边界检查：确保索引不越界
                if (_currentControlIndex >= _originalControlRects.Count) break;

                // 获取初始位置和大小
                var originalRect = _originalControlRects[_currentControlIndex];

                // 按比例计算新的位置和大小
                childControl.Left = (int)(originalRect.Left * widthScale);
                childControl.Top = (int)(originalRect.Top * heightScale);
                childControl.Width = (int)(originalRect.Width * widthScale);
                childControl.Height = (int)(originalRect.Height * heightScale);

                // 处理特殊控件：DataGridView需要调整列宽
                if (childControl is DataGridView dataGridView)
                {
                    AdjustDataGridViewColumns(dataGridView);
                }

                // 索引递增，处理下一个控件
                _currentControlIndex++;

                // 递归处理嵌套控件
                if (childControl.Controls.Count > 0)
                {
                    ScaleChildControls(childControl, widthScale, heightScale);
                }
            }
        }

        /// <summary>
        /// 调整DataGridView的列宽，使其适应缩放后的表格宽度
        /// </summary>
        /// <param name="dgv">需要调整的DataGridView控件</param>
        private void AdjustDataGridViewColumns(DataGridView dgv)
        {
            // 保存当前光标状态，操作完成后恢复
            var originalCursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;

            try
            {
                // 先让列自适应内容
                int totalColumnWidth = 0;
                for (int i = 0; i < dgv.Columns.Count; i++)
                {
                    dgv.AutoResizeColumn(i, DataGridViewAutoSizeColumnMode.AllCells);
                    totalColumnWidth += dgv.Columns[i].Width;
                }

                // 如果总列宽超过表格宽度，则按显示的单元格自适应；否则填充整个表格
                dgv.AutoSizeColumnsMode = totalColumnWidth >= dgv.Width
                    ? DataGridViewAutoSizeColumnsMode.DisplayedCells
                    : DataGridViewAutoSizeColumnsMode.Fill;
            }
            finally
            {
                // 恢复光标状态
                Cursor.Current = originalCursor;
            }
        }
    }
}
