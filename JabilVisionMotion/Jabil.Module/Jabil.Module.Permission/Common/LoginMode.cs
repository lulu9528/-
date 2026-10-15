using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Module.Permission
{
    /// <summary>
    /// 登录模式枚举
    /// 定义系统支持的登录方式，用于区分不同的身份验证流程
    /// </summary>
    public enum LoginMode
    {
        /// <summary>
        /// 账户密码登录
        /// 通过用户名（或账号）和密码进行身份验证
        /// 适用于常规的账号密码登录场景
        /// </summary>
        Account,

        /// <summary>
        /// RFID卡登录
        /// 通过读取RFID卡中的标识信息进行身份验证
        /// 适用于需要物理卡片的快捷登录场景
        /// </summary>
        RFID
    }
}