using Jabil.Module.Log.Lib;
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

namespace Jabil.Module.IO.Frm
{
    public partial class Frm_IOConfig : UIPage
    {
        public Frm_IOConfig()
        {
            InitializeComponent();
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        }

        /// <summary>
        /// 窗体对象实例
        /// </summary>
        private static Frm_IOConfig _instance;
        public static Frm_IOConfig Instance
        {
            get
            {
                if (_instance == null || _instance.IsDisposed)
                    _instance = new Frm_IOConfig();
                return _instance;
            }
        }

        private void FrmIOConfig_Load(object sender, EventArgs e)
        {
            try
            {
                UpdatePermission();
                //PermissionSystem.Instance.OnLoginUserChanged += OnLoginUserChanged;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }

        }
        private void OnLoginUserChanged(object sender, EventArgs e)
        {
            UpdatePermission();
        }
        private void UpdatePermission()
        {
            //this.toolStripButton_IOControl.Visible = PermissionSystem.CheckPermission("手动/IO/监控", out _) ? true : false;
        }

    }
}
