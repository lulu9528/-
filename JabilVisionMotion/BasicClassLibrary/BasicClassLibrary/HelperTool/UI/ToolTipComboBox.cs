
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace BasicClassLibrary.HelperTool.UI
{

    /// <summary>
    /// 提示下拉框
    /// </summary>
    public class ToolTipComboBox : ComboBox
    {
        private bool m_bDropDown = false;
        private bool m_bToolTipVisable = false;
        private List<string> m_listTips = new List<string>();
        private ToolTip m_toolTip = new ToolTip();

        /// <summary>
        /// 构造函数
        /// </summary>
        public ToolTipComboBox()
        {
            base.DrawMode = DrawMode.OwnerDrawFixed;
        }

        /// <summary>
        /// 增加单个Item
        /// </summary>
        /// <param name="item"></param>
        /// <param name="tip"></param>
        public void AddItem(object item, string tip)
        {
            base.Items.Add(item);
            this.m_listTips.Add(tip);
        }

        /// <summary>
        /// 增加多个项
        /// </summary>
        /// <param name="items"></param>
        /// <param name="tips"></param>
        public void AddItemRange(object[] items, string[] tips)
        {
            base.Items.AddRange(items);
            this.m_listTips.AddRange(tips);
        }

        /// <summary>
        /// 增加提示信息
        /// </summary>
        /// <param name="tip"></param>
        public void AddTip(string tip)
        {
            this.m_listTips.Add(tip);
        }

        /// <summary>
        /// 增加多个提示信息
        /// </summary>
        /// <param name="tips"></param>
        public void AddTipRange(string[] tips)
        {
            this.m_listTips.AddRange(tips);
        }

        /// <summary>
        /// 清空
        /// </summary>
        public void Clear()
        {
            base.Items.Clear();
            this.m_listTips.Clear();
        }

        /// <summary>
        /// 重写OnDraw
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index >= 0)
            {
                string itemText = base.GetItemText(base.Items[e.Index]);
                e.DrawBackground();
                if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
                {
                    TextRenderer.DrawText(e.Graphics, itemText, e.Font, e.Bounds.Location, SystemColors.Window);
                    string text = itemText;
                    if (this.m_listTips.Count > e.Index)
                    {
                        text = this.m_listTips[e.Index];
                    }
                    if (this.m_bDropDown)
                    {
                        if (this.ToolTipVisiable)
                        {
                            this.ShowToolTip(text, e.Bounds.X + e.Bounds.Width, e.Bounds.Y + e.Bounds.Height);
                        }
                    }
                    else
                    {
                        this.m_toolTip.SetToolTip(this, text);
                    }
                }
                else
                {
                    this.ResetToolTip();
                    TextRenderer.DrawText(e.Graphics, itemText, e.Font, e.Bounds.Location, this.ForeColor);
                }
                e.DrawFocusRectangle();
            }
            base.OnDrawItem(e);
        }

        /// <summary>
        /// 重写下拉事件
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDropDown(EventArgs e)
        {
            this.m_bDropDown = true;
            base.OnDropDown(e);
        }

        /// <summary>
        /// 重写OnDraw
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDropDownClosed(EventArgs e)
        {
            base.OnDropDownClosed(e);
            this.ResetToolTip();
            this.m_bDropDown = false;
        }

        /// <summary>
        /// 重写鼠标离开事件
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            this.ResetToolTip();
        }

        /// <summary>
        /// 重写SelectIndex改变事件
        /// </summary>
        /// <param name="e"></param>
        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            if (this.SelectedIndex >= 0)
            {
                string caption = base.Items[this.SelectedIndex].ToString();
                if (this.m_listTips.Count > this.SelectedIndex)
                {
                    caption = this.m_listTips[this.SelectedIndex];
                }
                this.m_toolTip.SetToolTip(this, caption);
            }
            base.OnSelectedIndexChanged(e);
        }

        private void ResetToolTip()
        {
            if (this.m_bToolTipVisable)
            {
                this.m_toolTip.Hide(this);
                this.m_bToolTipVisable = false;
            }
        }

        private void ShowToolTip(string text, int x, int y)
        {
            this.m_toolTip.Show(text, this, x, y);
            this.m_bToolTipVisable = true;
        }

        /// <summary>
        /// 提示信息
        /// </summary>
        public List<string> Tips
        {
            get
            {
                return this.m_listTips;
            }
        }

        /// <summary>
        /// 是否显示ToolTip
        /// </summary>
        public bool ToolTipVisiable { get; set; }
    }
}

