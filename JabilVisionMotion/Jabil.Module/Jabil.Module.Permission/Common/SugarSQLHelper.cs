using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Module.Permission.Common
{
    /// <summary>
    /// SqlSugar数据库操作辅助类
    /// 基于SqlSugar ORM框架封装的单例工具类，用于简化SQLite数据库操作
    /// 提供全局唯一的数据库连接客户端，统一管理数据库连接配置
    /// </summary>
    public class SugarSQLHelper
    {
        /// <summary>
        /// 数据库连接字符串
        /// 指向程序运行目录下的SQLite数据库文件SysConfig.db
        /// </summary>
        private static string connString = "Data Source=" +
                                         AppDomain.CurrentDomain.BaseDirectory +
                                         "\\Config\\Project\\Profile\\SQL\\SysConfig.db";

        /// <summary>
        /// 单例实例
        /// 存储当前类的唯一实例，确保全局使用同一个数据库操作对象
        /// </summary>
        private static SugarSQLHelper _instance;

        /// <summary>
        /// 单例访问属性
        /// 提供全局访问点，使用懒加载方式创建实例（首次访问时初始化）
        /// </summary>
        public static SugarSQLHelper Instance => _instance ?? new SugarSQLHelper();

        /// <summary>
        /// SqlSugar数据库客户端
        /// 封装了SQLite数据库的操作接口，配置为自动关闭连接模式
        /// </summary>
        public SqlSugarClient DB => new SqlSugarClient(new ConnectionConfig()
        {
            ConnectionString = connString,      // 数据库连接字符串
            DbType = DbType.Sqlite,             // 数据库类型为SQLite
            IsAutoCloseConnection = true,       // 自动关闭连接，无需手动释放
            InitKeyType = InitKeyType.Attribute // 通过实体类属性特性初始化主键信息
        });

        /// <summary>
        /// 私有构造函数
        /// 防止外部直接实例化，确保单例模式的有效性
        /// </summary>
        private SugarSQLHelper() { }
    }
}