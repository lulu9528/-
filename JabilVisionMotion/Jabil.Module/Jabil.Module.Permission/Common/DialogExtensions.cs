using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Module.Permission.Common
{
    /// <summary>
    /// 对话框扩展类
    /// 提供对话框相关的扩展方法，简化常见对话框的调用流程
    /// 主要用于MVVM模式中，通过服务接口显示各类对话框
    /// </summary>
    public static class DialogExtensions
    {
        /// <summary>
        /// 显示询问对话框
        /// 用于向用户展示询问信息并获取用户选择（通常是确认/取消）
        /// </summary>
        /// <param name="dialogHost">对话框主机服务实例</param>
        /// <param name="title">对话框标题</param>
        /// <param name="content">询问内容文本</param>
        /// <param name="dialogHostName">对话框主机名称（唯一标识，默认为"Root"）</param>
        /// <returns>返回对话框结果，包含用户的选择信息</returns>
        //public static async Task<IDialogResult> Question(this IDialogHostService dialogHost,
        //    string title, string content, string dialogHostName = "Root"
        //    )
        //{
        //    // 创建对话框参数集合
        //    DialogParameters param = new DialogParameters();
        //    // 添加标题参数
        //    param.Add("Title", title);
        //    // 添加内容参数
        //    param.Add("Content", content);
        //    // 添加对话框主机名称参数
        //    param.Add("dialogHostName", dialogHostName);
        //    // 显示对话框并等待结果
        //    // "MsgView"为对话框视图的名称，用于定位对应的视图
        //    var dialogResult = await dialogHost.ShowDialog("MsgView", param, dialogHostName);
        //    // 返回用户操作结果
        //    return dialogResult;
        //}
    }
}