using Jabil.Lib;
using Jabil.Module.Log.Frm;
using Jabil.Module.Log.Lib;
using Jabil.Module.Motion.Frm;
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

namespace Jabil.Module.Motion
{
    public partial class MotionPage : UIPage
    {
        public MotionPage()
        {
            InitializeComponent();
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        }

        /// <summary>
        /// 窗体对象实例
        /// </summary>
        private static MotionPage _instance;
        public static MotionPage Instance
        {
            get
            {
                if (_instance == null || _instance.IsDisposed)
                    _instance = new MotionPage();
                return _instance;
            }
        }


        #region  Fields
        /// <summary>
        /// 枚举窗体
        /// </summary>
        private enum FormMode
        {
            None,
            Frm_MotionControl,
        }
        private FormMode curFormMode = FormMode.None;

        #endregion
        #region  Methods
        /// <summary>
        /// 窗体切换
        /// </summary>
        /// <param name="formMode"></param>
        private void SwitchFrom(FormMode formMode)
        {
            try
            {
                WFUtil.UpdateMainThreadUIAsync(this, () =>
                {
                    switch (formMode)
                    {
                        case FormMode.Frm_MotionControl:
                            if (curFormMode != FormMode.Frm_MotionControl)
                            {
                                curFormMode = FormMode.Frm_MotionControl;
                                uiPanel_window.Visible = false;
                                uiPanel_window.Controls.Clear();
                                uiPanel_window.Dock = DockStyle.Fill;

                                Frm_MotionControl.Instance.TopLevel = false;
                                Frm_MotionControl.Instance.Dock = DockStyle.Fill;
                                Frm_MotionControl.Instance.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
                                Frm_MotionControl.Instance.Show();
                                uiPanel_window.Controls.Add(Frm_MotionControl.Instance);
                                uiPanel_window.Visible = true;
                            }
                            break;
                    }
                });
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        #endregion
        #region  Control Event Handler
        private void MotionPage_Load(object sender, EventArgs e)
        {
            try
            {
                WFUtil.UpdateMainThreadUIAsync(this, () =>
                {
                    Instance.Enabled = false;
                    SwitchFrom(FormMode.Frm_MotionControl);
                    Instance.Enabled = true;
                });
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }
        private void toolStripButton_MotionControl_Click(object sender, EventArgs e)
        {
            try
            {
                WFUtil.UpdateMainThreadUIAsync(this, () =>
                {
                    toolStripButton_MotionControl.Enabled = false;
                    SwitchFrom(FormMode.Frm_MotionControl);
                    toolStripButton_MotionControl.Enabled = true;
                });
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }
        #endregion
    }
}
