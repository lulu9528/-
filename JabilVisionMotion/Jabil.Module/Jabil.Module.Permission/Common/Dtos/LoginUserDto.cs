using Jabil.Module.Permission.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Jabil.Module.Permission.Common
{
    /// <summary>
    /// 登录用户数据传输对象
    /// 继承自BindableBase，实现属性变更通知
    /// 封装登录所需的用户信息及登录命令逻辑
    /// </summary>
    public class LoginUserDto : BindableBase
    {
        /// <summary>
        /// 账户名
        /// 用于用户登录时输入的账号
        /// </summary>
        private string _account;

        /// <summary>
        /// 员工编号
        /// 可能用于RFID登录或员工身份标识
        /// </summary>
        private string _empNo;

        /// <summary>
        /// 登录完成回调委托
        /// 用于登录操作完成后通知调用方登录结果
        /// </summary>
        private Action<bool> _loginCompleted;

        /// <summary>
        /// 构造函数
        /// 初始化登录完成回调委托
        /// </summary>
        /// <param name="loginCompleted">登录完成后的回调方法，参数为登录成功与否</param>
        public LoginUserDto(Action<bool> loginCompleted = null)
        {
            this._loginCompleted = loginCompleted;
        }

        /// <summary>
        /// 账户名属性
        /// 支持属性变更通知，用于绑定到UI的账号输入框
        /// </summary>
        public string Account
        {
            get => _account;
            set
            {
                _account = value;
                RaisePropertyChanged(); // 通知UI属性值已变更
            }
        }

        /// <summary>
        /// 员工编号属性
        /// 支持属性变更通知，用于绑定到UI的员工编号输入框或RFID读取值
        /// </summary>
        public string EmpNo
        {
            get => _empNo;
            set
            {
                _empNo = value;
                RaisePropertyChanged(); // 通知UI属性值已变更
            }
        }

        /// <summary>
        /// 密码
        /// 用于用户登录时输入的密码
        /// </summary>
        private string _passwod;

        /// <summary>
        /// 密码属性
        /// 支持属性变更通知，用于绑定到UI的密码输入框
        /// </summary>
        public string PassWord
        {
            get => _passwod;
            set
            {
                _passwod = value;
                RaisePropertyChanged(); // 通知UI属性值已变更
            }
        }

        /// <summary>
        /// 登录模式
        /// 表示当前选择的登录方式（账户密码登录或RFID登录）
        /// </summary>
        private LoginMode _loginMode;

        /// <summary>
        /// 登录模式属性
        /// 支持属性变更通知，用于绑定到UI的登录方式选择控件
        /// </summary>
        public LoginMode LoginMode
        {
            get => _loginMode;
            set
            {
                _loginMode = value;
                RaisePropertyChanged(); // 通知UI属性值已变更
            }
        }

        /// <summary>
        /// 确认登录命令
        /// 实现ICommand接口，用于绑定到UI的登录按钮
        /// 执行登录验证逻辑
        /// </summary>
        public ICommand ConfirmCommand => new DelegateCommand<string>(Confirm);

        /// <summary>
        /// 确认登录方法
        /// 处理登录逻辑，根据登录模式进行不同的验证
        /// </summary>
        /// <param name="param">命令参数（未使用）</param>
        private async void Confirm(string param)
        {
            // 获取剪贴板文本（可能用于RFID登录场景）
            var txt = Clipboard.GetText();
            // 登录结果标识
            bool relt = false;

            // RFID登录模式处理
            if (LoginMode == LoginMode.RFID && txt.Equals(_empNo))
            {
                // 清空员工编号
                EmpNo = "";
                // RFID验证失败
                relt = false;
            }
            else
            {
                // 账户密码登录模式处理
                // 检查当前是否已有登录用户
                if (PermissionSystem.Instance.CurrentUser == null)
                {
                    // 调用权限系统进行登录验证，等待异步结果
                    relt = await PermissionSystem.Instance.Login(this);
                }
            }

            // 登录失败处理
            if (!relt)
            {
                // 在登录视图的消息提示控件中显示错误信息
                LoginView.Snackbar.MessageQueue.Enqueue("账户或密码错误,请重新输入");
            }

            // 触发登录完成回调，传递登录结果
            if (_loginCompleted != null)
            {
                _loginCompleted.Invoke(relt);
            }
        }
    }
}