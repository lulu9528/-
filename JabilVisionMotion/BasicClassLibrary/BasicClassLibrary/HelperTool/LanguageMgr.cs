using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BasicClassLibrary.HelperTool
{
    /// <summary>
    /// 语言管理类
    /// </summary>
    public class LanguageMgr : SingletonTemplate<LanguageMgr>
    {

        /// <summary>
        /// 语言改变事件
        /// </summary>
        public event LanguageChangeHandler LanguageChangeEvent;

        /// <summary>
        /// 构造函数
        /// </summary>
        public LanguageMgr()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "language", "zh-CN");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "language", "en-US");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            this.Language = "zh-CN";
            this.LanguageID = 0;
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(this.Language, false);
        }

        /// <summary>
        /// 改变语言方法
        /// </summary>
        /// <param name="strLanName">语言名称，例如en-US zh-CN</param>
        /// <param name="bChange">是改变还是保存</param>
        public void ChangeLanguage(string strLanName, bool bChange = true)
        {
            this.LanguageID = (strLanName == "zh-CN") ? 0 : 1;
            this.Language = strLanName;
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(strLanName);
            if (this.LanguageChangeEvent != null)
            {
                this.LanguageChangeEvent(strLanName, bChange);
            }
        }

        /// <summary>
        /// 修改控件文本显示
        /// </summary>
        /// <param name="strFormName"></param>
        /// <param name="status"></param>
        /// <param name="ini"></param>
        public void ChangeUIText(string strFormName, StatusStrip status, IniHelper ini)
        {
            foreach (ToolStripStatusLabel label in status.Items)
            {
                if (ini.IsKeyExist(strFormName, label.Name))
                {
                    string text = ini.GetString(strFormName, label.Name, label.Text);
                    if (string.IsNullOrEmpty(text))
                    {
                        text = label.Text;
                    }
                    label.Text = text;
                }
            }
        }

        /// <summary>
        /// 修改控件文本显示
        /// </summary>
        /// <param name="strFormName"></param>
        /// <param name="menu"></param>
        /// <param name="ini"></param>
        public void ChangeUIText(string strFormName, ToolStrip menu, IniHelper ini)
        {
            foreach (ToolStripMenuItem item in menu.Items)
            {
                if (ini.IsKeyExist(strFormName, item.Name))
                {
                    string text = ini.GetString(strFormName, item.Name, item.Text);
                    if (string.IsNullOrEmpty(text))
                    {
                        text = item.Text;
                    }
                    item.Text = text;
                }
                this.ChangeUIText(strFormName, item, ini);
            }
        }

        /// <summary>
        /// 修改控件文本显示
        /// </summary>
        /// <param name="strFormName"></param>
        /// <param name="menuItem"></param>
        /// <param name="ini"></param>
        public void ChangeUIText(string strFormName, ToolStripMenuItem menuItem, IniHelper ini)
        {
            foreach (ToolStripMenuItem item in menuItem.DropDownItems)
            {
                if (ini.IsKeyExist(strFormName, item.Name))
                {
                    string text = ini.GetString(strFormName, item.Name, item.Text);
                    if (string.IsNullOrEmpty(text))
                    {
                        text = item.Text;
                    }
                    item.Text = text;
                }
                this.ChangeUIText(strFormName, item, ini);
            }
        }

        /// <summary>
        /// 修改控件文本显示
        /// </summary>
        /// <param name="strFormName">Form名称</param>
        /// <param name="ctrl">控件</param>
        /// <param name="ini">ini文件</param>
        /// <param name="strParent">父节点</param>
        public void ChangeUIText(string strFormName, Control ctrl, IniHelper ini, string strParent = "")
        {
            foreach (Control control in ctrl.Controls)
            {
                string name = control.Name;
                if (!string.IsNullOrEmpty(strParent))
                {
                    name = strParent + "." + name;
                }
                if (control is GroupBox)
                {
                    if (ini.IsKeyExist(strFormName, name))
                    {
                        string text = ini.GetString(strFormName, name, control.Text);
                        if (string.IsNullOrEmpty(text))
                        {
                            text = control.Text;
                        }
                        control.Text = text;
                    }
                    this.ChangeUIText(strFormName, control, ini, strParent);
                }
                else if (control is TabPage)
                {
                    name = control.Parent.Name + "." + control.Name;
                    if (!string.IsNullOrEmpty(strParent))
                    {
                        name = strParent + "." + name;
                    }
                    if (ini.IsKeyExist(strFormName, name))
                    {
                        string str3 = ini.GetString(strFormName, name, control.Text);
                        if (string.IsNullOrEmpty(str3))
                        {
                            str3 = control.Text;
                        }
                        control.Text = str3;
                    }
                    this.ChangeUIText(strFormName, control, ini, strParent);
                }
                else if ((control is Panel) || (control is TabControl))
                {
                    this.ChangeUIText(strFormName, control, ini, strParent);
                }
                else if ((((control is Label) || (control is Button)) || (control is CheckBox)) || (control is RadioButton))
                {
                    if (ini.IsKeyExist(strFormName, name))
                    {
                        string str4 = ini.GetString(strFormName, name, control.Text);
                        if (string.IsNullOrEmpty(str4))
                        {
                            str4 = control.Text;
                        }
                        control.Text = str4;
                    }
                }
                else if (control is DataGridView)
                {
                    DataGridView view = (DataGridView)control;
                    foreach (DataGridViewColumn column in view.Columns)
                    {
                        string strKey = name + "." + column.Name;
                        string headerText = ini.GetString(strFormName, strKey, column.HeaderText);
                        if (string.IsNullOrEmpty(headerText))
                        {
                            headerText = column.HeaderText;
                        }
                        column.HeaderText = headerText;
                    }
                }
            }
        }

        /// <summary>
        /// 获取中英文字符串
        /// </summary>
        /// <param name="strNameSpace">命名空间</param>
        /// <param name="strClassName">类名</param>
        /// <param name="strKey">关键字名称</param>
        /// <param name="strDefault">默认字符串</param>
        /// <returns></returns>
        public string GetString(string strNameSpace, string strClassName, string strKey, string strDefault)
        {
            string str = new IniHelper { IniFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "language", this.Language, strNameSpace + ".ini") }.GetString(strClassName, strKey, strDefault);
            if (string.IsNullOrEmpty(str))
            {
                str = strDefault;
            }
            return str;
        }

        /// <summary>
        /// 保存UI文本
        /// </summary>
        /// <param name="strFormName"></param>
        /// <param name="status"></param>
        /// <param name="ini"></param>
        public void SaveUIText(string strFormName, StatusStrip status, IniHelper ini)
        {
            foreach (ToolStripStatusLabel label in status.Items)
            {
                ini.WriteString(strFormName, label.Name, label.Text);
            }
        }

        /// <summary>
        /// 保存UI文本
        /// </summary>
        /// <param name="strFormName"></param>
        /// <param name="menu"></param>
        /// <param name="ini"></param>
        public void SaveUIText(string strFormName, ToolStrip menu, IniHelper ini)
        {
            foreach (ToolStripMenuItem item in menu.Items)
            {
                ini.WriteString(strFormName, item.Name, item.Text);
                this.SaveUIText(strFormName, item, ini);
            }
        }

        /// <summary>
        /// 保存UI文本
        /// </summary>
        /// <param name="strFormName"></param>
        /// <param name="menuItem"></param>
        /// <param name="ini"></param>
        public void SaveUIText(string strFormName, ToolStripMenuItem menuItem, IniHelper ini)
        {
            foreach (ToolStripMenuItem item in menuItem.DropDownItems)
            {
                ini.WriteString(strFormName, item.Name, item.Text);
                this.SaveUIText(strFormName, item, ini);
            }
        }

        /// <summary>
        /// 保存UI文本
        /// </summary>
        /// <param name="strFormName"></param>
        /// <param name="ctrl"></param>
        /// <param name="ini"></param>
        /// <param name="strParent"></param>
        public void SaveUIText(string strFormName, Control ctrl, IniHelper ini, string strParent = "")
        {
            foreach (Control control in ctrl.Controls)
            {
                string name = control.Name;
                if (!string.IsNullOrEmpty(strParent))
                {
                    name = strParent + "." + name;
                }
                if (control is GroupBox)
                {
                    ini.WriteString(strFormName, name, control.Text);
                    this.SaveUIText(strFormName, control, ini, strParent);
                }
                else if (control is TabPage)
                {
                    name = control.Parent.Name + "." + control.Name;
                    if (!string.IsNullOrEmpty(strParent))
                    {
                        name = strParent + "." + name;
                    }
                    ini.WriteString(strFormName, name, control.Text);
                    this.SaveUIText(strFormName, control, ini, strParent);
                }
                else if ((control is Panel) || (control is TabControl))
                {
                    this.SaveUIText(strFormName, control, ini, strParent);
                }
                else if ((((control is Label) || (control is Button)) || (control is CheckBox)) || (control is RadioButton))
                {
                    ini.WriteString(strFormName, name, control.Text);
                }
                else if (control is DataGridView)
                {
                    DataGridView view = (DataGridView)control;
                    foreach (DataGridViewColumn column in view.Columns)
                    {
                        string strKey = name + "." + column.Name;
                        ini.WriteString(strFormName, strKey, column.HeaderText);
                    }
                }
            }
        }

        /// <summary>
        /// 保存UI文本
        /// </summary>
        /// <param name="strNameSpace"></param>
        /// <param name="strClassName"></param>
        /// <param name="strKey"></param>
        /// <param name="strValue"></param>
        public void WriteString(string strNameSpace, string strClassName, string strKey, string strValue)
        {
            new IniHelper { IniFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "language", this.Language, strNameSpace + ".ini") }.WriteString(strClassName, strKey, strValue);
        }

        /// <summary>
        /// 语言 zh-CN - 中文  en-US - 英文
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// 语言代码：0 - 中文  1 - 英文
        /// </summary>
        public int LanguageID { get; set; }

        /// <summary>
        /// 语言改变委托
        /// </summary>
        /// <param name="strLanguage">语言：en-US、zh-CN</param>
        /// <param name="bChange">是改变还是保存</param>
        public delegate void LanguageChangeHandler(string strLanguage, bool bChange = true);
    }
}
