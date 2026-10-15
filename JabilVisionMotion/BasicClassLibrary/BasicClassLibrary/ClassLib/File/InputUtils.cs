using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BasicClassLibrary
{
    public class InputUtils
    {
        //定义只能输入数字
        public static void OnlyNumber(object sender, KeyPressEventArgs e)
        {
            TextBox txt = sender as TextBox;
            //判断按键是不是要输入的类型
            if (((int)e.KeyChar < 48 || (int)e.KeyChar > 57) && (int)e.KeyChar != 8 && (int)e.KeyChar != 46)
                e.Handled = true;

            //小数点的处理
            if ((int)e.KeyChar == 46)
            {
                if (txt.Text.Length <= 0)
                    e.Handled = true;   //小数点不能在第一位
            }
        }
    }
}
