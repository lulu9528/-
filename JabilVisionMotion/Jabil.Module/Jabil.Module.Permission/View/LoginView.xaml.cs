using Jabil.Module.Permission.Common;
using Jabil.Module.Permission.Service;
using Jabil.Module.Permission.ViewModel;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Jabil.Module.Permission.View
{
    /// <summary>
    /// LoginView.xaml 的交互逻辑
    /// </summary>
    public partial class LoginView : UserControl
    {
        public static Snackbar Snackbar = new Snackbar();
        private bool _isListening = false;
        private CardReadService cardReadService;
        public LoginView()
        {
            InitializeComponent();
            this.card.IsVisibleChanged += LoginView_IsVisibleChanged;
            Snackbar = this.LoginSnakeBar;
            cardReadService = new CardReadService();
            

        }

        private async void LoginView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue  is true)
            {
                if (_isListening) return;
                else
                {
                    _isListening = true;
                    await ListenForCardSwipes();
                }
            }
            else
            {
                _isListening = false;
                System.Threading.Thread.Sleep(100);
            }
        }

        private async Task ListenForCardSwipes()
        {
            while (_isListening)
            {
                var cardID = this.cardInput.Password;
                if (!string.IsNullOrEmpty(cardID))
                {
                    if (this.DataContext is LoginUserDto user)
                    {
                        user.EmpNo = cardID;
                        
                        user.ConfirmCommand.Execute("");
                        cardReadService.SetId(string.Empty);
                    }
                }
                this.cardInput.Password = string.Empty;
                if (!Permission.PermissionSystem.Instance.IsOperating)
                {

                    this.cardInput.Focus();

                }
                await Task.Delay(1500);
            }

        }

        private async void TabItem_Loaded(object sender, RoutedEventArgs e)
        {
            _isListening = true;
            this.cardInput.Password = string.Empty;
            this.cardInput.IsHitTestVisible = true;
            this.cardInput.Focus();

           // await ListenForCardSwipes();
        }

        private void TabItem_Unloaded(object sender, RoutedEventArgs e)
        {
            this.cardInput.Password = string.Empty;
            _isListening = false;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            var sss = this.cardInput.Password;
            cardReadService.SetId(sss);
            this.cardInput.Focus();
        }

        private void cardInput_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            e.CancelCommand();e.Handled = true;
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.DataContext is LoginUserDto user)
            {
                var selected = (TabItem)this.tabControl.SelectedItem;
                if (selected.Header.ToString() == "账号密码登录")
                {
                    user.LoginMode = LoginMode.Account;
                }
                else if (selected.Header.ToString() == "刷卡登录")
                {
                    user.LoginMode = LoginMode.RFID;
                }
            }
        }
    }
}
