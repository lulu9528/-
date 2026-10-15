using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BasicClassLibrary.RexHelps
{
    /// <summary>
    /// 剪贴板操作
    /// </summary>
    public class RCopyPaste
    {
        /// <summary>
        /// 拷贝数据到粘贴板中
        /// </summary>
        /// <param name="input"></param>
        public static void CopyToClipboard(object input)
        {
            try
            {
                Clipboard.SetDataObject(input);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        /// <summary>
        /// 获取粘贴板数据
        /// </summary>
        /// <returns></returns>
        public static string GetDataFromClipboard_string()
        {
            try
            {
                IDataObject iData = Clipboard.GetDataObject();
                string dst = "";
                if (iData.GetDataPresent(DataFormats.Text))
                {
                    dst = (string)iData.GetData(DataFormats.Text);
                }
                return dst;
            }
            catch (Exception ex)
            {
                throw ex;
                return null;
            }
        }
        /// <summary>
        /// 清空粘贴板数据
        /// </summary>
        public static void ClearClipboard()
        {
            try
            {
                Clipboard.Clear();
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
    }
}
