using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BasicClassLibrary.HelperTool
{
    /// <summary>
    /// 系统登录用户
    /// </summary>
    public enum UserMode
    {
        /// <summary>
        /// 操作员
        /// </summary>
        Operator,
        /// <summary>
        /// 
        /// </summary>
        FAE,
        /// <summary>
        /// 调试员
        /// </summary>
        Adjustor,
        /// <summary>
        /// 工程师
        /// </summary>
        Engineer,
        /// <summary>
        /// 管理者
        /// </summary>
        Administrator
    }

    /// <summary>
    /// 安全权限控制类
    /// </summary>
    public class Security
    {
        private static UserMode m_userMode = UserMode.Operator;

        private static string Path = System.IO.Path.Combine(Application.StartupPath, "Security.ini");

        static UserMode CurrentUser
        {
            get { return m_userMode; }
        }

        public static int RemainDays
        {
            get
            {
                return ini.GetInt("PassWord", "RemainDays", 99999);
            }
            set
            {
                ini.WriteInt("PassWord", "RemainDays", value);
            }
        }
        static string FAE = "123";
        static string Adjustor = "456";
        static string Engineer = "789";
        static string Administrator = "000000";
        /// <summary>
        /// 定义模式变化事件
        /// </summary>
        public static event ModeChangedHandler ModeChangedEvent;

        static IniHelper ini;
        static Security()
        {
            if (!File.Exists(Path))
            {
                File.Create(Path);
            }
            ini = new IniHelper(Path);

            FAE = ini.GetString("PassWord", "FAE", "123");
            Adjustor = ini.GetString("PassWord", "Adjustor", "456");
            Engineer = ini.GetString("PassWord", "Engineer", "789");
            Administrator = ini.GetString("PassWord", "Administrator", "000000");
            RemainDays = ini.GetInt("PassWord", "RemainDays", 99999);
        }

        /// <summary>
        /// 系统权限切换到调试员
        /// </summary>
        /// <param name="strPassword"></param>
        /// <returns>切换成功或切换失败，成功则对整个系统触发权限变更事件</returns>
        public static bool ChangeAdjustorMode(string strPassword)
        {
            return ChangeUserMode(UserMode.Adjustor, strPassword);
        }

        /// <summary>
        /// 系统权限切换到管理员
        /// </summary>
        /// <param name="strPassword"></param>
        /// <returns>切换成功或切换失败，成功则对整个系统触发权限变更事件</returns>
        public static bool ChangeAdministratorMode(string strPassword)
        {
            return ChangeUserMode(UserMode.Administrator, strPassword);
        }

        /// <summary>
        /// 系统权限切换到工程师等级
        /// </summary>
        /// <param name="strPassword"></param>
        /// <returns>切换成功或切换失败，成功则对整个系统触发权限变更事件</returns>
        public static bool ChangeEngMode(string strPassword)
        {
            return ChangeUserMode(UserMode.Engineer, strPassword);
        }

        /// <summary>
        /// 系统权限切换到FAE等级
        /// </summary>
        /// <param name="strPassword"></param>
        /// <returns>切换成功或切换失败，成功则对整个系统触发权限变更事件</returns>
        public static bool ChangeFaeMode(string strPassword)
        {
            return ChangeUserMode(UserMode.FAE, strPassword);
        }

        /// <summary>
        /// 系统权限切换到OP等级
        /// </summary>
        public static bool ChangeOpMode()
        {
            return ChangeUserMode(UserMode.Operator, "");
        }

        /// <summary>
        /// 修改用户密码，只有管理员才有权限修改密码
        /// </summary>
        /// <param name="newMode">用户</param>
        /// <param name="strNewPassword">密码</param>
        /// <returns></returns>
        public static bool ChangePassword(UserMode newMode, string strNewPassword)
        {
            if (ChangePassword((int)newMode, strNewPassword))
            {
                LogHelper.Info("Modify the user " + newMode.ToString() + " password success !", null);
                return true;
            }
            LogHelper.Info("Modify the user " + newMode.ToString() + " password failed !", null);
            return true;
        }

        private static bool ChangePassword(int nUser, string szPassword)
        {
            bool bResult = false;

            if (m_userMode > UserMode.Engineer)
            {
                switch (nUser)
                {
                    case 0:
                        break;

                    case 1:
                        FAE = szPassword;
                        ini.WriteString("PassWord", "FAE", szPassword);
                        break;
                    case 2:
                        Adjustor = szPassword;
                        ini.WriteString("PassWord", "Adjustor", szPassword);
                        break;
                    case 3:
                        Engineer = szPassword;
                        ini.WriteString("PassWord", "Engineer", szPassword);
                        break;
                    case 4:
                        Administrator = szPassword;
                        ini.WriteString("PassWord", "Administrator", szPassword);
                        break;
                }
                bResult = true;
            }
            return bResult;
        }
        private static int ChangeUser(int nNewUser, string szPassword)
        {
            int mode = -1;
            if (m_userMode != (UserMode)nNewUser)
            {
                string currentPassword = "";

                switch (nNewUser)
                {
                    case 0:
                        break;

                    case 1:
                        currentPassword = FAE;
                        break;
                    case 2:
                        currentPassword = Adjustor;
                        break;
                    case 3:
                        currentPassword = Engineer;
                        break;
                    case 4:
                        currentPassword = Administrator;
                        break;
                }

                if (currentPassword == szPassword)
                {
                    m_userMode = (UserMode)nNewUser;
                    mode = nNewUser;
                }
            }
            return mode;
        }
        /// <summary>
        /// 切换用户模式
        /// </summary>
        /// <param name="newMode"></param>
        /// <param name="strPassword"></param>
        /// <returns></returns>
        public static bool ChangeUserMode(UserMode newMode, string strPassword)
        {
            if (GetUserMode() != newMode)
            {
                UserMode oldmode = GetUserMode();
                if (ChangeUser((int)newMode, strPassword) == (int)newMode)
                {
                    LogHelper.Info(oldmode.ToString() + " change security to " + newMode.ToString(), null);

                    if (ModeChangedEvent != null)
                    {
                        ModeChangedEvent();
                    }
                    return true;
                }
            }
            return false;
        }


        private static void CheckResource()
        {
            if (!File.Exists(Path))
            {
                File.Create(Path);

                ini.IniFile = Path;
            }
        }

        public static int GetRemainDays()
        {
            return RemainDays;
        }
        /// <summary>
        /// 获取当前模式
        /// </summary>
        /// <returns></returns>
        public static UserMode GetUserMode()
        {
            CheckResource();
            return m_userMode;
        }

        /// <summary>
        /// 调试员模式
        /// </summary>
        /// <returns></returns>
        public static bool IsAdjustorMode()
        {
            return m_userMode == UserMode.Adjustor;
        }

        /// <summary>
        /// 是否为管理员模式
        /// </summary>
        /// <returns></returns>
        public static bool IsAdminMode()
        {
            return m_userMode == UserMode.Administrator;
        }

        /// <summary>
        /// 是否为工程师模式
        /// </summary>
        /// <returns></returns>
        public static bool IsEngMode()
        {
            return m_userMode == UserMode.Engineer;
        }

        /// <summary>
        /// 是否为CPK模式
        /// </summary>
        /// <returns></returns>
        public static bool IsFaeMode()
        {
            return m_userMode == UserMode.FAE;
        }

        /// <summary>
        /// 是否为OP模式
        /// </summary>
        /// <returns></returns>
        public static bool IsOpMode()
        {
            return m_userMode == UserMode.Operator;
        }

        /// <summary>
        /// 是否注册
        /// </summary>
        private static bool IsValid;

        /// <summary>
        /// 定义一个模式变化委托函数
        /// </summary>
        public delegate void ModeChangedHandler();
    }
}
