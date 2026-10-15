using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Module.Permission.Common
{
    /// <summary>
    /// OleDb数据库操作辅助类
    /// 封装了与OleDb数据源（主要是Access数据库）的交互方法
    /// 提供增删改查等常用数据库操作的简化接口
    /// </summary>
    public class OleDbHelper
    {
        // 旧版Excel连接字符串（适用于Excel 2003及以前版本）
        // private static string connString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source={0};Extended Properties=Excel 8.0";

        /// <summary>
        /// 数据库连接字符串
        /// 使用ACE OLEDB提供程序，支持Access 2007及以上版本和Excel 2007及以上版本
        /// 连接到程序运行目录下的SQL_ALL.accdb数据库文件
        /// </summary>
        private static string connString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" +
                                         AppDomain.CurrentDomain.BaseDirectory + "\\Config\\Project\\Profile\\SQL\\SQL_ALL.accdb";

        /// <summary>
        /// 执行增、删、改操作（INSERT/UPDATE/DELETE）
        /// </summary>
        /// <param name="sql">要执行的SQL命令</param>
        /// <returns>受影响的行数</returns>
        public static int Update(string sql)
        {
            // 创建数据库连接对象
            OleDbConnection conn = new OleDbConnection(connString);
            // 创建命令对象，关联SQL命令和连接
            OleDbCommand cmd = new OleDbCommand(sql, conn);
            try
            {
                // 打开数据库连接
                conn.Open();
                // 执行命令并获取受影响的行数
                int result = cmd.ExecuteNonQuery();
                return result;
            }
            catch (Exception ex)
            {
                // 抛出异常，由调用者处理
                throw ex;
            }
            finally
            {
                // 确保连接关闭，释放资源
                conn.Close();
            }
        }

        /// <summary>
        /// 执行查询并返回单一结果（如COUNT、SUM等聚合查询）
        /// </summary>
        /// <param name="sql">要执行的SQL查询命令</param>
        /// <returns>查询结果中的第一行第一列的值，若查询无结果则返回null</returns>
        public static object GetSingleResult(string sql)
        {
            OleDbConnection conn = new OleDbConnection(connString);
            OleDbCommand cmd = new OleDbCommand(sql, conn);
            try
            {
                conn.Open();
                // 执行查询并返回单一结果
                object result = cmd.ExecuteScalar();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                conn.Close();
            }
        }

        /// <summary>
        /// 执行查询并返回数据阅读器（OleDbDataReader）
        /// 适用于需要逐行读取大量数据的场景
        /// </summary>
        /// <param name="sql">要执行的SQL查询命令</param>
        /// <returns>OleDbDataReader对象，用于读取查询结果</returns>
        public static OleDbDataReader GetReader(string sql)
        {
            OleDbConnection conn = new OleDbConnection(connString);
            OleDbCommand cmd = new OleDbCommand(sql, conn);
            try
            {
                conn.Open();
                // 执行查询，指定CommandBehavior.CloseConnection以确保阅读器关闭时自动关闭连接
                OleDbDataReader objReader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                return objReader;
            }
            catch (Exception ex)
            {
                // 发生异常时手动关闭连接
                conn.Close();
                throw ex;
            }
        }

        /// <summary>
        /// 执行查询并将结果填充到数据集中（DataSet）
        /// 适用于需要在内存中处理完整结果集的场景
        /// </summary>
        /// <param name="sql">要执行的SQL查询命令</param>
        /// <returns>包含查询结果的DataSet对象</returns>
        public static DataSet GetDataSet(string sql)
        {
            OleDbConnection conn = new OleDbConnection(connString);
            OleDbCommand cmd = new OleDbCommand(sql, conn);
            // 创建数据适配器，用于填充数据集
            OleDbDataAdapter da = new OleDbDataAdapter(cmd);
            // 创建内存数据集
            DataSet ds = new DataSet();
            try
            {
                conn.Open();
                // 使用数据适配器填充数据集
                da.Fill(ds);
                return ds;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                conn.Close();
            }
        }

        /// <summary>
        /// 执行查询并将结果填充到数据集中（DataSet），支持指定数据源路径
        /// 主要用于操作Excel文件或其他外部OleDb数据源
        /// </summary>
        /// <param name="sql">要执行的SQL查询命令</param>
        /// <param name="path">数据源文件路径（如Excel文件路径）</param>
        /// <returns>包含查询结果的DataSet对象</returns>
        public static DataSet GetDataSet(string sql, string path)
        {
            // 使用指定路径格式化连接字符串
            OleDbConnection conn = new OleDbConnection(string.Format(connString, path));
            OleDbCommand cmd = new OleDbCommand(sql, conn);
            OleDbDataAdapter da = new OleDbDataAdapter(cmd);
            DataSet ds = new DataSet();
            try
            {
                conn.Open();
                da.Fill(ds);
                return ds;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                conn.Close();
            }
        }
    }
}