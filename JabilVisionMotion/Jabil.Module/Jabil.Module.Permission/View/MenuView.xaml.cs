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
    /// MenuView.xaml 的交互逻辑
    /// </summary>
    public partial class MenuView : UserControl
    {
        public static Snackbar Snackbar = new Snackbar();
        public MenuView()
        {
            InitializeComponent();
            Task.Factory.StartNew(() => Thread.Sleep(2500)).ContinueWith(t =>
            {

                MenuSnackbar.MessageQueue?.Enqueue("Welcome to Jabil Vision System");
            }, TaskScheduler.FromCurrentSynchronizationContext());
            //this.DataContext = new MainViewModel(MainSnackbar.MessageQueue);
            MenuProxy.Instance.MessageQueue = MenuSnackbar.MessageQueue;
        }
    }
}
