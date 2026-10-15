using Jabil.Module.Permission.ViewModel;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
    /// MainView.xaml 的交互逻辑
    /// </summary>
    public partial class MainView : UserControl
    {

        public static Snackbar Snackbar = new Snackbar();
        public MainView()
        {
            InitializeComponent();
            Task.Factory.StartNew(() => Thread.Sleep(2500)).ContinueWith(t =>
            {

                //MainSnackbar.MessageQueue?.Enqueue("Welcome to Jabil Vision System");
            }, TaskScheduler.FromCurrentSynchronizationContext());
            this.DataContext = new MainViewModel(MainSnackbar.MessageQueue);
            Snackbar = MainSnackbar;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Snackbar.MessageQueue.Enqueue("进入角色管理系统...");
        }


    }
}
