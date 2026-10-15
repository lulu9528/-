using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BasicClassLibrary.HelperTool
{
    /// <summary>
    /// UI扩展方法
    /// </summary>
    public static class ExtensionUIMethods
    {
        /// <summary>
        /// DataGridView双缓存
        /// </summary>
        /// <param name="dgv"></param>
        /// <param name="setting"></param>
        public static void DoubleBuffered(this DataGridView dgv, bool setting)
        {
            dgv.GetType().GetProperty("DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(dgv, setting, null);
        }

        /// <summary>
        /// ListView双缓存 
        /// </summary>
        /// <param name="lv"></param>
        /// <param name="setting"></param>
        public static void DoubleBuffered(this ListView lv, bool setting)
        {
            lv.GetType().GetProperty("DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(lv, setting, null);
        }
    }
}
