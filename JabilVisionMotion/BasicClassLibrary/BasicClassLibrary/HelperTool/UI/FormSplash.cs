using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BasicClassLibrary.HelperTool.UI
{
    public partial class FormSplash : Form
    {
        private static FormSplash m_instance;
        private static object m_lock = new object();
        public FormSplash()
        {
            InitializeComponent();
        }

        private void FormSplash_Load(object sender, EventArgs e)
        {
            base.TopMost = true;
            base.FormBorderStyle = FormBorderStyle.None;
            int width = SystemInformation.WorkingArea.Width;
            int height = SystemInformation.WorkingArea.Height;
            base.Location = new Point((width - base.Width) / 2, (height - base.Height) / 2);
            this.label_Msg.Text = "";
            this.label_Percent.Text = "0%";
        }
        /// <summary>
        /// 显示信息
        /// </summary>
        /// <param name="strMsg"></param>
        /// <param name="nPercent"></param>
        public void ShowMsg(string strMsg, int nPercent)
        {
            if (base.InvokeRequired)
            {
                base.BeginInvoke((MethodInvoker)delegate
                {
                    this.label_Msg.Text = strMsg;
                    this.label_Percent.Text = string.Format("{0}%", nPercent);
                    this.progressBar1.Value = nPercent;
                    if (nPercent == 100)
                    {
                        Thread.Sleep(0x3e8);
                        this.Close();
                        m_instance = null;
                    }
                });
            }
            else
            {
                this.label_Msg.Text = strMsg;
                this.label_Percent.Text = string.Format("{0}%", nPercent);
                this.progressBar1.Value = nPercent;
                if (nPercent == 100)
                {
                    Thread.Sleep(0x3e8);
                    base.Close();
                    m_instance = null;
                }
            }
        }

        /// <summary>
        /// 更新UI
        /// </summary>
        /// <param name="strMsg"></param>
        /// <param name="nPercent"></param>
        public static void UpdateUI(string strMsg, int nPercent)
        {
            if (m_instance == null)
            {
                Task.Run((() => { FormSplash.Instance.ShowDialog(); }));
                Thread.Sleep(0x3e8);
            }
            if (m_instance != null)
            {
                Instance.ShowMsg(strMsg, nPercent);
            }
        }

        /// <summary>
        /// 单例
        /// </summary>
        public static FormSplash Instance
        {
            get
            {
                if (m_instance == null)
                {
                    object @lock = m_lock;
                    lock (@lock)
                    {
                        if (m_instance == null)
                        {
                            m_instance = new FormSplash();
                        }
                    }
                }
                return m_instance;
            }
        }
    }
}
