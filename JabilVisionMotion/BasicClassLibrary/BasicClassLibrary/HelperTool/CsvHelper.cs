using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BasicClassLibrary.HelperTool
{
    /// <summary>
    /// 读写CSV文件的类
    /// </summary>
    public class CsvHelper
    {
        private Encoding encoding;
        private string fileName;
        private bool m_bInsertQuota;
        private ArrayList rowAL;

        /// <summary>
        /// 默认以系统参数路径，当前日期生成文件名
        /// </summary>
        public CsvHelper()
        {
            this.m_bInsertQuota = false;
            this.rowAL = new ArrayList();
            this.fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", DateTime.Now.ToString("yyyyMMdd") + ".csv");
            this.encoding = Encoding.Default;
        }

        /// <summary>
        /// 以系统参数路径，文件名称及当前日期组合生成文件名
        /// </summary>
        /// <param name="fileName">文件名,包括文件路径</param>
        public CsvHelper(string fileName)
        {
            this.m_bInsertQuota = false;
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            this.rowAL = new ArrayList();
            this.fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", fileName + "_" + DateTime.Now.ToString("yyyyMMdd") + ".csv");
            this.encoding = Encoding.Default;
        }

        /// <summary>
        /// 指定文件路径和文件名
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="fileName">文件名,包括文件路径</param>
        public CsvHelper(string filePath, string fileName)
        {
            this.m_bInsertQuota = false;
            this.rowAL = new ArrayList();
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }
            this.fileName = Path.Combine(filePath, fileName);
            this.encoding = Encoding.Default;
        }

        /// <summary>
        /// 指定文件名和编码方式
        /// </summary>
        /// <param name="fileName">文件名,包括文件路径</param>
        /// <param name="encoding">文件编码</param>
        public CsvHelper(string fileName, Encoding encoding)
        {
            this.m_bInsertQuota = false;
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            this.rowAL = new ArrayList();
            this.fileName = Path.Combine(path, fileName + "_" + DateTime.Now.ToString("yyyyMMdd") + ".csv");
            this.encoding = encoding;
        }

        /// <summary>
        /// 添加表数据到CSV文件中
        /// </summary>
        /// <param name="dataDT">表数据</param>
        /// <param name="beginCol">从第几列开始,beginCol = 1代表第一列</param>
        public void AddData(DataTable dataDT, int beginCol)
        {
            if (dataDT == null)
            {
                throw new Exception("需要添加的表数据为空");
            }
            int count = this.rowAL.Count;
            for (int i = 0; i < dataDT.Rows.Count; i++)
            {
                for (int j = 0; j < dataDT.Columns.Count; j++)
                {
                    this[(count + i) + 1, beginCol + j] = dataDT.Rows[i][j].ToString();
                }
            }
        }

        /// <summary>
        /// 加入新的数据行
        /// </summary>
        /// <param name="newDataLine">新的数据行</param>
        private void AddNewDataLine(string newDataLine)
        {
            Debug.WriteLine("NewLine:" + newDataLine);
            ArrayList list = new ArrayList();
            char[] separator = new char[] { ',' };
            string[] strArray = newDataLine.Split(separator);
            bool flag = false;
            string fileCellData = "";
            for (int i = 0; i < strArray.Length; i++)
            {
                if (flag)
                {
                    fileCellData = fileCellData + "," + strArray[i];
                    if (this.IfOddEndQuota(strArray[i]))
                    {
                        list.Add(this.GetHandleData(fileCellData));
                        flag = false;
                    }
                }
                else if (this.IfOddStartQuota(strArray[i]))
                {
                    if ((this.IfOddEndQuota(strArray[i]) && (strArray[i].Length > 2)) && !this.IfOddQuota(strArray[i]))
                    {
                        list.Add(this.GetHandleData(strArray[i]));
                        flag = false;
                    }
                    else
                    {
                        flag = true;
                        fileCellData = strArray[i];
                    }
                }
                else
                {
                    list.Add(this.GetHandleData(strArray[i]));
                }
            }
            if (flag)
            {
                throw new Exception("数据格式有问题");
            }
            this.rowAL.Add(list);
        }

        /// <summary>
        /// 检查列数是否是有效的
        /// </summary>
        /// <param name="col"></param>
        private void CheckColValid(int col)
        {
            if (col < 0)
            {
                throw new Exception("列数不能小于0");
            }
            if (col > this.ColCount)
            {
                throw new Exception("没有当前列的数据");
            }
        }

        /// <summary>
        /// 检查检查最大列数是否是有效的
        /// </summary>
        /// <param name="maxCol"></param>
        private void CheckMaxColValid(int maxCol)
        {
            if ((maxCol <= 0) && (maxCol != -1))
            {
                throw new Exception("列数不能等于0或小于-1");
            }
            if (maxCol > this.ColCount)
            {
                throw new Exception("没有当前列的数据");
            }
        }

        /// <summary>
        /// 检查最大行数是否是有效的
        /// </summary>
        /// <param name="maxRow"></param>
        private void CheckMaxRowValid(int maxRow)
        {
            if ((maxRow <= 0) && (maxRow != -1))
            {
                throw new Exception("行数不能等于0或小于-1");
            }
            if (maxRow > this.RowCount)
            {
                throw new Exception("没有当前行的数据");
            }
        }

        /// <summary>
        /// 检查行数是否是有效的
        /// </summary>
        /// <param name="row"></param>
        private void CheckRowValid(int row)
        {
            if (row < 0)
            {
                throw new Exception("行数不能小于0");
            }
            if (row > this.RowCount)
            {
                throw new Exception("没有当前行的数据");
            }
        }

        /// <summary>
        /// 字符串转换成CSV中的格子
        /// 双引号转换成两个双引号，然后首尾各加一个双引号
        /// 这样就不需要考虑逗号及换行的问题
        /// </summary>
        /// <param name="cell">格子内容</param>
        /// <returns></returns>
        private string ConvertToSaveCell(string cell)
        {
            cell = cell.Replace("\"", "\"\"");
            return ("\"" + cell + "\"");
        }

        /// <summary>
        /// 转换成保存行
        /// </summary>
        /// <param name="colAL">一行</param>
        /// <returns></returns>
        private string ConvertToSaveLine(ArrayList colAL)
        {
            string str = "";
            for (int i = 0; i < colAL.Count; i++)
            {
                if (this.m_bInsertQuota)
                {
                    str = str + this.ConvertToSaveCell(colAL[i].ToString());
                }
                else
                {
                    str = str + colAL[i].ToString();
                }
                if (i < (colAL.Count - 1))
                {
                    str = str + ",";
                }
            }
            return str;
        }

        /// <summary>
        /// 获取两个连续引号变成单个引号的数据行
        /// </summary>
        /// <param name="fileDataLine">文件数据行</param>
        /// <returns></returns>
        private string GetDeleteQuotaDataLine(string fileDataLine)
        {
            return fileDataLine.Replace("\"\"", "\"");
        }

        /// <summary>
        /// 去掉格子的首尾引号，把双引号变成单引号
        /// </summary>
        /// <param name="fileCellData"></param>
        /// <returns></returns>
        private string GetHandleData(string fileCellData)
        {
            if (fileCellData == "")
            {
                return "";
            }
            if (this.IfOddStartQuota(fileCellData))
            {
                if (!this.IfOddEndQuota(fileCellData))
                {
                    throw new Exception("数据引号无法匹配" + fileCellData);
                }
                return fileCellData.Substring(1, fileCellData.Length - 2).Replace("\"\"", "\"");
            }
            if ((fileCellData.Length > 2) && (fileCellData[0] == '"'))
            {
                fileCellData = fileCellData.Substring(1, fileCellData.Length - 2).Replace("\"\"", "\"");
            }
            return fileCellData;
        }

        /// <summary>
        /// 判断是否以奇数个引号结尾
        /// </summary>
        /// <param name="dataCell"></param>
        /// <returns></returns>
        private bool IfOddEndQuota(string dataCell)
        {
            int num = 0;
            for (int i = dataCell.Length - 1; i >= 0; i--)
            {
                if (dataCell[i] == '"')
                {
                    num++;
                }
                else
                {
                    break;
                }
            }
            bool flag = false;
            if ((num % 2) == 1)
            {
                flag = true;
            }
            return flag;
        }

        /// <summary>
        /// 判断字符串是否包含奇数个引号
        /// </summary>
        /// <param name="dataLine">数据行</param>
        /// <returns>为奇数时，返回为真；否则返回为假</returns>
        private bool IfOddQuota(string dataLine)
        {
            int num = 0;
            for (int i = 0; i < dataLine.Length; i++)
            {
                if (dataLine[i] == '"')
                {
                    num++;
                }
            }
            bool flag = false;
            if ((num % 2) == 1)
            {
                flag = true;
            }
            return flag;
        }

        /// <summary>
        /// 判断是否以奇数个引号开始
        /// </summary>
        /// <param name="dataCell"></param>
        /// <returns></returns>
        private bool IfOddStartQuota(string dataCell)
        {
            int num = 0;
            for (int i = 0; i < dataCell.Length; i++)
            {
                if (dataCell[i] == '"')
                {
                    num++;
                }
                else
                {
                    break;
                }
            }
            bool flag = false;
            if ((num % 2) == 1)
            {
                flag = true;
            }
            return flag;
        }

        /// <summary>
        /// 载入CSV文件
        /// </summary>
        public void LoadCsvFile()
        {
            if (ReferenceEquals(this.fileName, null))
            {
                throw new Exception("请指定要载入的CSV文件名");
            }
            if (!File.Exists(this.fileName))
            {
                throw new Exception("指定的CSV文件不存在");
            }
            if (ReferenceEquals(this.encoding, null))
            {
                this.encoding = Encoding.Default;
            }
            string dataLine = "";
            using (FileStream stream = new FileStream(this.fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (StreamReader reader = new StreamReader(stream, this.encoding))
                {
                    while (true)
                    {
                        string objA = reader.ReadLine();
                        if (objA == null)
                        {
                            break;
                        }
                        dataLine = (dataLine != "") ? (dataLine + "/r/n" + objA) : objA;
                        if (!this.IfOddQuota(dataLine))
                        {
                            this.AddNewDataLine(dataLine);
                            dataLine = "";
                        }
                    }
                }
            }
            if (dataLine.Length > 0)
            {
                throw new Exception("CSV文件的格式有错误");
            }
        }


        /// <summary>
        /// 保存数据,如果当前硬盘中已经存在文件名一样的文件，将会覆盖
        /// </summary>
        public void Save()
        {
            if (this.fileName == null)
            {
                throw new Exception("缺少文件名");
            }
            if (this.encoding == null)
            {
                this.encoding = Encoding.Default;
            }
            try
            {
                using (FileStream stream = new FileStream(this.fileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                {
                    using (StreamWriter writer = new StreamWriter(stream, this.encoding))
                    {
                        for (int i = 0; i < this.rowAL.Count; i++)
                        {
                            writer.WriteLine(this.ConvertToSaveLine((ArrayList)this.rowAL[i]));
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString(), "文件写入失败");
            }
        }

        /// <summary>
        /// 保存数据,如果当前硬盘中已经存在文件名一样的文件，将会覆盖
        /// </summary>
        /// <param name="fileName">文件名,包括文件路径</param>
        public void Save(string fileName)
        {
            this.fileName = fileName;
            this.Save();
        }

        /// <summary>
        /// 保存数据,如果当前硬盘中已经存在文件名一样的文件，将会覆盖
        /// </summary>
        /// <param name="fileName">文件名,包括文件路径</param>
        /// <param name="encoding">文件编码</param>
        public void Save(string fileName, Encoding encoding)
        {
            this.fileName = fileName;
            this.encoding = encoding;
            this.Save();
        }

        /// <summary>
        /// 是否添加引号
        /// </summary>
        public bool BInsertQuota
        {
            get
            {
                return this.m_bInsertQuota;
            }
            set
            {
                this.m_bInsertQuota = value;
            }
        }

        /// <summary>
        /// 获取列数
        /// </summary>
        public int ColCount
        {
            get
            {
                int num = 0;
                for (int i = 0; i < this.rowAL.Count; i++)
                {
                    ArrayList list = (ArrayList)this.rowAL[i];
                    num = (num > list.Count) ? num : list.Count;
                }
                return num;
            }
        }

        /// <summary>
        /// 文件编码
        /// </summary>
        public Encoding FileEncoding
        {
            set
            {
                this.encoding = value;
            }
        }

        /// <summary>
        /// 文件名,包括文件路径
        /// </summary>
        public string FileName
        {
            get
            {
                return this.fileName;
            }
            set
            {
                this.fileName = value;
            }
        }

        /// <summary>
        /// 根据最小行，最大行，最小列，最大列，来生成一个DataTable类型的数据
        /// 行等于1代表第一行
        /// 列等于1代表第一列
        /// maxrow: -1代表最大行
        /// maxcol: -1代表最大列
        /// </summary>
        public DataTable this[int minRow, int maxRow, int minCol, int maxCol]
        {
            get
            {
                this.CheckRowValid(minRow);
                this.CheckMaxRowValid(maxRow);
                this.CheckColValid(minCol);
                this.CheckMaxColValid(maxCol);
                if (maxRow == -1)
                {
                    maxRow = this.RowCount;
                }
                if (maxCol == -1)
                {
                    maxCol = this.ColCount;
                }
                if (maxRow < minRow)
                {
                    throw new Exception("最大行数不能小于最小行数");
                }
                if (maxCol < minCol)
                {
                    throw new Exception("最大列数不能小于最小列数");
                }
                DataTable table = new DataTable();
                int num = minCol;
                while (num <= maxCol)
                {
                    table.Columns.Add(num.ToString());
                    num++;
                }
                for (int i = minRow; i <= maxRow; i++)
                {
                    DataRow row = table.NewRow();
                    num = 0;
                    for (int j = minCol; j <= maxCol; j++)
                    {
                        row[num] = this[i, j];
                        num++;
                    }
                    table.Rows.Add(row);
                }
                return table;
            }
        }

        /// <summary>
        /// 获取某行某列的数据
        /// row:行,row = 1代表第一行
        /// col:列,col = 1代表第一列  
        /// </summary>
        public string this[int row, int col]
        {
            get
            {
                this.CheckRowValid(row);
                this.CheckColValid(col);
                ArrayList list = (ArrayList)this.rowAL[row];
                if (list.Count <= col)
                {
                    return "";
                }
                return list[col].ToString();
            }
            set
            {
                if (row < 0)
                {
                    throw new Exception("行数不能小于0");
                }
                if (row >= this.rowAL.Count)
                {
                    for (int i = this.rowAL.Count; i <= row; i++)
                    {
                        this.rowAL.Add(new ArrayList());
                    }
                }
                if (col < 0)
                {
                    throw new Exception("列数不能小于0");
                }
                ArrayList list2 = (ArrayList)this.rowAL[row];
                if (col >= list2.Count)
                {
                    for (int j = list2.Count; j <= col; j++)
                    {
                        list2.Add("");
                    }
                }
                this.rowAL[row] = list2;
                ArrayList list = (ArrayList)this.rowAL[row];
                list[col] = value;
                this.rowAL[row] = list;
            }
        }

        /// <summary>
        /// 获取行数
        /// </summary>
        public int RowCount
        {
            get
            {
                return this.rowAL.Count;
            }
        }

        /// <summary>
        /// 获取行
        /// </summary>
        public ArrayList Rows
        {
            get
            {
                return this.rowAL;
            }
        }
    }
}
