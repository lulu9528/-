using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using Sunny.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Jabil.Lib.Parameter;
using Jabil.Lib;
using Jabil.Module.Setting.Lib.JVS;

namespace Jabil.Module.Setting.Frm
{
    public partial class Frm_System : UIForm
    {
        public Frm_System()
        {
            InitializeComponent();
        }
        /// <summary>
        /// 窗体对象实例
        /// </summary>
        private static Frm_System _instance;
        public static Frm_System Instance
        {
            get
            {
                if (_instance == null || _instance.IsDisposed)
                    _instance = new Frm_System();
                return _instance;
            }
        }




        private void uiButton3_Click(object sender, EventArgs e)
        {

        }
        //UILineSeries series1;
        private void Frm_Data_Load(object sender, EventArgs e)
        {
            try
            {


            }
            catch (Exception ex)
            {
                Log.Lib.RLog.SaveError(ex);
            }
        }

        private void uiSymbolButton2_Click(object sender, EventArgs e)
        {
            uiTabControl1.Enabled = true;
        }

        /// <summary>
        /// 打开 JVS 设置页（映射配置编辑/保存）
        /// </summary>
        private void uiButton_JvsSetting_Click(object sender, EventArgs e)
        {
            try
            {
                using (var frm = new Frm_JvsSetting())
                {
                    frm.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                Log.Lib.RLog.SaveError(ex);
                MessageBox.Show(this, "打开 JVS 设置页失败：" + ex.Message,
                    "JVS 设置", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }










    }



}
