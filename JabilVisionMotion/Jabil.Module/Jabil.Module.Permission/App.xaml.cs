using Jabil.Module.Permission.Common;
using Jabil.Module.Permission.Entity;
using Jabil.Module.Permission.Service;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Jabil.Module.Permission
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {

        public  App()
        {
            _ = Permission.PermissionSystem.Instance;
            //Permission.PermissionSystem.Instance.DefaultDisableStatistics = true;
            //var aa= SugarSQLHelper.Connect().Queryable("VisionTool", "");
            //var user= SugarSQLHelper.Connect().Queryable<UserInfo>().ToList();
            ////var users = SugarSQLHelper.Connect().Queryable<UserEntity>().ToList();
            //UserService userService = new UserService();
            //var AA=  userService.GetAllAsync();


            //var BB = AA.Result;
           // var aa = PermissionSystem.Instance;



        }
    }
}
