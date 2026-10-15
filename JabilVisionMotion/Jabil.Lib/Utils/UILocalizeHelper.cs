using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Lib
{
    public static class UILocalizeHelper
    {
        public static void SetEN()
        {
            JabilUILocalize.InfoTitle = "Info";
            JabilUILocalize.SuccessTitle = "Success";
            JabilUILocalize.WarningTitle = "Warning";
            JabilUILocalize.ErrorTitle = "Error";
            JabilUILocalize.AskTitle = "Query";
            JabilUILocalize.InputTitle = "Input";
            JabilUILocalize.CloseAll = "Close all";
            JabilUILocalize.OK = "OK";
            JabilUILocalize.Cancel = "Cancel";
            JabilUILocalize.GridNoData = "[ No data ]";
            JabilUILocalize.GridDataLoading = "Data loading ......";
            JabilUILocalize.GridDataSourceException = "The data source must be DataTable or List";
        }

        public static void SetCH()
        {
            JabilUILocalize.InfoTitle = "提示";
            JabilUILocalize.SuccessTitle = "正确";
            JabilUILocalize.WarningTitle = "警告";
            JabilUILocalize.ErrorTitle = "错误";
            JabilUILocalize.AskTitle = "提示";
            JabilUILocalize.InputTitle = "输入";
            JabilUILocalize.CloseAll = "全部关闭";
            JabilUILocalize.OK = "确定";
            JabilUILocalize.Cancel = "取消";
            JabilUILocalize.GridNoData = "[ 无数据 ]";
            JabilUILocalize.GridDataLoading = "数据加载中 ......";
            JabilUILocalize.GridDataSourceException = "数据源必须为DataTable或者List";
        }
    }

    public static class JabilUILocalize
    {
        public static string InfoTitle = "提示";
        public static string SuccessTitle = "正确";
        public static string WarningTitle = "警告";
        public static string ErrorTitle = "错误";
        public static string AskTitle = "提示";
        public static string InputTitle = "输入";
        public static string CloseAll = "全部关闭";
        public static string OK = "确定";
        public static string Cancel = "取消";
        public static string GridNoData = "[ 无数据 ]";
        public static string GridDataLoading = "数据加载中 ......";
        public static string GridDataSourceException = "数据源必须为DataTable或者List";



        public static string FormClosing_lbl_info = "\r\n退出前是否需要保存项目？";
        public static string FormClosed_lbl_info = "执行程序关闭\r\n";

    }
}
