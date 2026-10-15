using Jabil.Module.Permission.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Jabil.Module.Permission.View
{
    /// <summary>
    /// AddRoleView.xaml 的交互逻辑
    /// </summary>
    public partial class AddUserView : UserControl
    {
        public AddUserView()
        {
            InitializeComponent();
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.DataContext is UserDto user)
            {
                var selected = (TabItem)this.tabControl.SelectedItem;
                if (selected.Header.ToString()=="密码用户")
                {
                    user.LoginMode = LoginMode.Account;
                }
                else if (selected.Header.ToString() == "刷卡用户")
                {
                    user.LoginMode = LoginMode.RFID;
                }
            }
        }
    }
}
