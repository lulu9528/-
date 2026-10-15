using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;  // 包含DataGridView等Windows窗体控件

namespace BasicClassLibrary
{
    /// <summary>
    /// DataGridView 工具类，提供一系列操作DataGridView控件的实用方法，包括：
    /// - 获取列、行的所有值
    /// - 获取当前选中行的数据
    /// - 删除选中行
    /// - 访问指定单元格的值等
    /// </summary>
    public class DataGridViewUtils
    {
        #region 获取某列的所有值

        /// <summary>
        /// 获取DataGridView中指定列的所有非空行的值（排除新行）
        /// </summary>
        /// <param name="dgv">目标DataGridView控件</param>
        /// <param name="columnIndex">列索引（从0开始）</param>
        /// <returns>包含该列所有单元格值的字符串列表；若发生异常，返回空列表</returns>
        public static List<string> GetDataGridViewColumnValue(DataGridView dgv, int columnIndex)
        {
            List<string> ls = new List<string>();  // 存储列值的列表
            string cellValue;  // 临时存储单元格值

            try
            {
                // 遍历所有行（dgv.Rows.Count - 0 实际等同于dgv.Rows.Count，可能是笔误）
                for (int i = 0; i < dgv.Rows.Count; i++)
                {
                    // 跳过新行（DataGridView默认最后一行是可新增的空行，IsNewRow为true）
                    if (!dgv.Rows[i].IsNewRow)
                    {
                        // 获取当前行指定列的单元格值，并转换为字符串
                        cellValue = dgv.Rows[i].Cells[columnIndex].Value.ToString();
                        ls.Add(cellValue);  // 添加到列表
                    }
                }
            }
            catch (System.Exception ex)
            {
                // 捕获异常（如单元格值为null时调用ToString()会报错）
                // 可在此处添加日志记录：ex.Message
                return ls;  // 异常时返回已收集的值（可能为空）
            }

            return ls;
        }

        #endregion

        #region 获取某行的所有值

        /// <summary>
        /// 获取DataGridView中指定行的所有列的值
        /// </summary>
        /// <param name="dgv">目标DataGridView控件</param>
        /// <param name="rowIndex">行索引（从0开始）</param>
        /// <returns>包含该行所有单元格值的字符串列表；若单元格值为null，调用ToString()会抛出异常</returns>
        public static List<string> GetDataGridViewRowValue(DataGridView dgv, int rowIndex)
        {
            List<string> ls = new List<string>();  // 存储行值的列表

            // 遍历该行的所有列
            for (int i = 0; i < dgv.ColumnCount; i++)
            {
                // 获取当前列的单元格值并转换为字符串（注意：若值为null会抛出异常）
                string cellValue = dgv.Rows[rowIndex].Cells[i].Value.ToString();
                ls.Add(cellValue);  // 添加到列表
            }

            return ls;
        }

        #endregion

        #region 获取当前选中行的所有值

        /// <summary>
        /// 获取DataGridView中当前选中行的所有列的值（仅获取第一行选中行）
        /// </summary>
        /// <param name="dgv">目标DataGridView控件</param>
        /// <returns>包含选中行所有单元格值的字符串列表；若无选中行或发生异常，返回null</returns>
        public static List<string> GetDataGridViewCurrentValues(DataGridView dgv)
        {
            try
            {
                List<string> list = new List<string>();

                // 检查当前是否有选中行（CurrentRow为null表示无选中行）
                if (dgv.CurrentRow == null)
                    return null;

                // 获取当前选中行的列数
                int columnCount = dgv.CurrentRow.Cells.Count;
                if (columnCount == 0)  // 无列时返回null
                    return null;

                string cellValue = null;  // 临时存储单元格值

                // 遍历当前选中行的所有列
                for (int i = 0; i < columnCount; i++)
                {
                    // 仅处理值不为null的单元格
                    if (dgv.CurrentRow.Cells[i].Value != null)
                    {
                        cellValue = dgv.CurrentRow.Cells[i].Value.ToString();
                        list.Add(cellValue);
                    }
                }

                return list;
            }
            catch (Exception ex)
            {
                // 捕获异常（如索引越界、值为null等）
                // 可在此处添加日志记录：ex.Message
                return null;
            }
        }

        #endregion

        #region 获取当前选中行的指定列值（重载方法）

        /// <summary>
        /// （重载）获取当前选中行中指定列索引的单元格值
        /// </summary>
        /// <param name="dgv">目标DataGridView控件</param>
        /// <param name="columnIndex">列索引（从0开始）</param>
        /// <returns>单元格的字符串值；若无选中行或控件为null，返回空字符串</returns>
        public static string GetDataGridViewCurrentRowCellValue(DataGridView dgv, int columnIndex)
        {
            string cellValue = "";  // 默认返回空字符串

            // 检查控件和当前行是否有效
            if (dgv != null && dgv.CurrentRow != null)
            {
                // 获取指定列的值（注意：若值为null，调用ToString()会抛出异常）
                cellValue = dgv.CurrentRow.Cells[columnIndex].Value.ToString();
            }

            return cellValue;
        }

        /// <summary>
        /// （重载）获取当前选中行中指定列名的单元格值
        /// </summary>
        /// <param name="dgv">目标DataGridView控件</param>
        /// <param name="columnName">列的名称（与DataGridViewColumn.Name对应）</param>
        /// <returns>单元格的字符串值；若无选中行、控件为null或列名不存在，返回空字符串</returns>
        public static string GetDataGridViewCurrentRowCellValue(DataGridView dgv, string columnName)
        {
            string cellValue = "";  // 默认返回空字符串

            // 检查控件和当前行是否有效
            if (dgv != null && dgv.CurrentRow != null)
            {
                // 通过列名获取单元格值（注意：若列名不存在或值为null，会抛出异常）
                cellValue = dgv.CurrentRow.Cells[columnName].Value.ToString();
            }

            return cellValue;
        }

        #endregion

        #region 刷新DataGridView数据（已注释）

        /// <summary>
        /// （已注释）将数据库中的数据刷新到DataGridView（原计划使用SQLiteDataAdapter）
        /// </summary>
        /// <param name="dgv">目标DataGridView控件</param>
        /// <param name="dt">输出参数，存储刷新后的数据表</param>
        /// <param name="sda">SQLite数据适配器，用于填充数据表</param>
        /// <returns>刷新成功返回true</returns>
        //public static bool RefreshDGV(DataGridView dgv, out DataTable dt, SQLiteDataAdapter sda)
        //{
        //    dt = new DataTable();
        //    sda.Fill(dt);  // 用适配器填充数据表
        //    dgv.DataSource = dt;  // 将数据表绑定到DataGridView
        //    return true;
        //}

        #endregion

        #region 删除选中的行

        /// <summary>
        /// 删除DataGridView中所有选中的行，并重新编号第一列（假设第一列为序号列）
        /// </summary>
        /// <param name="dgv">目标DataGridView控件</param>
        public static void DeleteSelectedRows(DataGridView dgv)
        {
            try
            {
                // 获取选中行的数量
                int selectedRowCount = dgv.SelectedRows.Count;
                if (selectedRowCount < 1)  // 无选中行时直接返回
                {
                    return;
                }

                // 遍历所有行，删除选中的行
                // 注意：从后往前删可避免索引错乱，此处从前往后删可能有问题（删除后行索引会变化）
                for (int i = 0; i < dgv.Rows.Count - 1; i++)
                {
                    if (dgv.Rows[i].Selected)  // 若当前行被选中
                    {
                        dgv.Rows.RemoveAt(i);  // 删除该行
                        i--;  // 修正索引（删除后下一行索引变为当前i）
                    }
                }

                // 重新编号第一列（假设第一列是序号列，值为行号+1）
                for (int i = 0; i < dgv.Rows.Count - 1; i++)
                {
                    dgv.Rows[i].Cells[0].Value = i + 1;
                }
            }
            catch (Exception ex)
            {
                // 捕获异常（如行已被删除、控件未初始化等）
                // 可在此处添加日志记录：ex.Message
            }
        }

        #endregion

        #region 获取指定行列的单元格值

        /// <summary>
        /// 获取DataGridView中指定行和列的单元格值
        /// </summary>
        /// <param name="dgv">目标DataGridView控件</param>
        /// <param name="rowIndex">行索引（从0开始）</param>
        /// <param name="columnIndex">列索引（从0开始）</param>
        /// <returns>单元格的字符串值；若值为null或索引无效，会抛出异常</returns>
        public static string GetDataGridViewCellValue(DataGridView dgv, int rowIndex, int columnIndex)
        {
            // 直接获取指定单元格的值并转换为字符串（注意：若值为null会抛出异常）
            string cellValue = dgv.Rows[rowIndex].Cells[columnIndex].Value.ToString();
            return cellValue;
        }

        #endregion
    }
}