using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicClassLibrary
{
    public class NPOIExcel
    {
        ///// <summary>
        ///// 改变xls或者xlsx单元格颜色
        ///// </summary>
        //public static void ChangeCellBackColor(IWorkbook workbook, string fileExt, byte red, byte green, byte blue, out ICellStyle style)
        //{
        //    style = workbook.CreateCellStyle();
        //    style.Alignment = HorizontalAlignment.Center;
        //    if (fileExt == ".xlsx")
        //    {
        //        //使用NPOI已经有的颜色创建（这里跟xls一样，不知道为什么可以通用）
        //        style.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Yellow.Index;
        //        style.FillPattern = FillPattern.SolidForeground;

        //        //使用没有的颜色，这里和xls不一样，不需要覆盖掉原来的色板
        //        style.FillForegroundColor = 0;
        //        style.FillPattern = FillPattern.SolidForeground;
        //        ((XSSFColor)style.FillForegroundColorColor).SetRgb(new byte[] { red, green, blue });
        //    }
        //    else if (fileExt == ".xls")
        //    {
        //        //使用NPOI已经有的颜色创建
        //        style.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Yellow.Index;
        //        style.FillPattern = FillPattern.SolidForeground;

        //        //没有的颜色，使用RGB值进行创建
        //        //这里要自定义一个颜色板覆盖掉原来的index，这里覆盖48号位置上的
        //        HSSFPalette palette = ((HSSFWorkbook)workbook).GetCustomPalette();
        //        palette.SetColorAtIndex(48, red, green, blue);
        //        style.FillForegroundColor = palette.FindColor(red, green, blue).Indexed;
        //    }

        //}
        ///// <summary>
        ///// 改变xls或者xlsx单元格字的颜色
        ///// </summary>
        //public static void ChangeCellTextColor(IWorkbook workbook, string fileExt, string str, short color, int startindex, int endindex, out IRichTextString textString)
        //{
        //    IFont fontRed = workbook.CreateFont();
        //    //fontRed.Color = HSSFColor.Red.Index;
        //    fontRed.Color = color;
        //    textString = new XSSFRichTextString(str);
        //    if (fileExt.Equals(".xls"))
        //    {
        //        textString = new HSSFRichTextString(str);
        //    }
        //    textString.ApplyFont(startindex, endindex, fontRed);//更改“第3列”颜色为红色
        //    //cell.SetCellValue(textString);
        //}
        ///// <summary>
        ///// Datable导出成Excel
        ///// </summary>
        ///// <param name="dt"></param>
        ///// <param name="file">导出路径(包括文件名与扩展名)</param>
        //public static void ArrayToExcel<T>(BindingList<T> dt, string file)
        //{
        //    IWorkbook workbook;
        //    string fileExt = Path.GetExtension(file).ToLower();
        //    if (fileExt == ".xlsx") { workbook = new XSSFWorkbook(); } else if (fileExt == ".xls") { workbook = new HSSFWorkbook(); } else { workbook = null; }
        //    if (workbook == null) { return; }
        //    ISheet sheet = workbook.CreateSheet("Sheet0");
        //    sheet.SetColumnWidth(0, 3600);
        //    sheet.SetColumnWidth(4, 3000);
        //    ICellStyle style = workbook.CreateCellStyle();//写入时获取最新值
        //    style.Alignment = HorizontalAlignment.Center;
        //    //表头
        //    IRow row = sheet.CreateRow(0);
        //    PropertyInfo[] ps = typeof(T).GetProperties();
        //    for (int i = 0; i < ps.Length; i++)
        //    {
        //        ICell cell = row.CreateCell(i);
        //        cell.CellStyle = style;
        //        cell.SetCellValue(ps[i].Name);
        //    }
        //    //数据
        //    for (int i = 0; i < dt.Count; i++)
        //    {
        //        IRow row1 = sheet.CreateRow(i + 1);
        //        for (int j = 0; j < ps.Length; j++)
        //        {
        //            ICell cell = row1.CreateCell(j);
        //            cell.CellStyle = style;
        //            if (ps[j].GetValue(dt[i]) == null)
        //            {
        //                continue;
        //            }
        //            string str = ps[j].GetValue(dt[i]).ToString();
        //            if (str.Contains("NG"))
        //            {
        //                ChangeCellBackColor(workbook, fileExt, 255, 0, 0, out ICellStyle style1);
        //                cell.CellStyle = style1;
        //                row1.GetCell(j - 1).CellStyle = style1;
        //                cell.SetCellValue(str);
        //                continue;
        //            }
        //            cell.SetCellValue(str);
        //        }
        //    }

        //    //转为字节数组
        //    MemoryStream stream = new MemoryStream();
        //    workbook.Write(stream);
        //    var buf = stream.ToArray();

        //    //保存为Excel文件
        //    using (FileStream fs = new FileStream(file, FileMode.Create, FileAccess.Write))
        //    {
        //        fs.Write(buf, 0, buf.Length);
        //        fs.Flush();
        //    }
        //}
        ///// <summary>
        ///// Datable导出成Excel
        ///// </summary>
        ///// <param name="dt"></param>
        ///// <param name="file">导出路径(包括文件名与扩展名)</param>
        //public static void TableToExcel(DataTable dt, string file)
        //{
        //    IWorkbook workbook;
        //    string fileExt = Path.GetExtension(file).ToLower();
        //    if (fileExt == ".xlsx") { workbook = new XSSFWorkbook(); } else if (fileExt == ".xls") { workbook = new HSSFWorkbook(); } else { workbook = null; }
        //    if (workbook == null) { return; }
        //    ISheet sheet = string.IsNullOrEmpty(dt.TableName) ? workbook.CreateSheet("Sheet1") : workbook.CreateSheet(dt.TableName);
        //    ICellStyle style = workbook.CreateCellStyle();//写入时获取最新值
        //    style.Alignment = HorizontalAlignment.Center;
        //    //表头
        //    IRow row = sheet.CreateRow(0);
        //    for (int i = 0; i < dt.Columns.Count; i++)
        //    {
        //        ICell cell = row.CreateCell(i);
        //        cell.CellStyle = style;
        //        cell.SetCellValue(dt.Columns[i].ColumnName);
        //    }

        //    //数据
        //    for (int i = 0; i < dt.Rows.Count; i++)
        //    {
        //        IRow row1 = sheet.CreateRow(i + 1);
        //        for (int j = 0; j < dt.Columns.Count; j++)
        //        {
        //            ICell cell = row1.CreateCell(j);
        //            cell.CellStyle = style;
        //            if (dt.Rows[i][j].ToString().Contains("NG"))
        //            {
        //                ChangeCellBackColor(workbook, fileExt, 255, 0, 0, out ICellStyle style1);
        //                cell.CellStyle = style1;
        //                ChangeCellTextColor(workbook, fileExt, dt.Rows[i][j].ToString(), HSSFColor.Black.Index, 0, 2, out IRichTextString str);
        //                cell.SetCellValue(str);
        //                continue;
        //            }
        //            cell.SetCellValue(dt.Rows[i][j].ToString());
        //        }
        //    }

        //    //转为字节数组
        //    MemoryStream stream = new MemoryStream();
        //    workbook.Write(stream);
        //    var buf = stream.ToArray();

        //    //保存为Excel文件
        //    using (FileStream fs = new FileStream(file, FileMode.Create, FileAccess.Write))
        //    {
        //        fs.Write(buf, 0, buf.Length);
        //        fs.Flush();
        //    }
        //}
        ///// <summary>
        ///// Excel导入成Datable
        ///// </summary>
        ///// <param name="file">导入路径(包含文件名与扩展名)</param>
        ///// <returns></returns>
        //public static DataTable ExcelToTable(string file)
        //{
        //    DataTable dt = new DataTable();
        //    IWorkbook workbook;
        //    string fileExt = Path.GetExtension(file).ToLower();
        //    using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
        //    {
        //        //XSSFWorkbook 适用XLSX格式，HSSFWorkbook 适用XLS格式
        //        if (fileExt == ".xlsx") { workbook = new XSSFWorkbook(fs); } else if (fileExt == ".xls") { workbook = new HSSFWorkbook(fs); } else { workbook = null; }
        //        if (workbook == null) { return null; }
        //        ISheet sheet = workbook.GetSheetAt(0);

        //        //表头
        //        IRow header = sheet.GetRow(sheet.FirstRowNum);
        //        List<int> columns = new List<int>();
        //        for (int i = 0; i < header.LastCellNum; i++)
        //        {
        //            object obj = GetValueType(header.GetCell(i));
        //            if (obj == null || obj.ToString() == string.Empty)
        //            {
        //                dt.Columns.Add(new DataColumn("Columns" + i.ToString()));
        //            }
        //            else
        //                dt.Columns.Add(new DataColumn(obj.ToString()));
        //            columns.Add(i);
        //        }
        //        //数据
        //        for (int i = sheet.FirstRowNum + 1; i <= sheet.LastRowNum; i++)
        //        {
        //            DataRow dr = dt.NewRow();
        //            bool hasValue = false;
        //            foreach (int j in columns)
        //            {
        //                dr[j] = GetValueType(sheet.GetRow(i).GetCell(j));
        //                if (dr[j] != null && dr[j].ToString() != string.Empty)
        //                {
        //                    hasValue = true;
        //                }
        //            }
        //            if (hasValue)
        //            {
        //                dt.Rows.Add(dr);
        //            }
        //        }
        //    }
        //    return dt;
        //}
        ///// <summary>
        ///// 获取单元格类型
        ///// </summary>
        ///// <param name="cell"></param>
        ///// <returns></returns>
        ////private static object GetValueType(ICell cell)
        ////{
        ////    if (cell == null)
        ////        return null;
        ////    switch (cell.CellType)
        ////    {
        ////        case CellType.Blank: //BLANK:
        ////            return null;
        ////        case CellType.Boolean: //BOOLEAN:
        ////            return cell.BooleanCellValue;
        ////        case CellType.Numeric: //NUMERIC:
        ////            return cell.NumericCellValue;
        ////        case CellType.String: //STRING:
        ////            return cell.StringCellValue;
        ////        case CellType.Error: //ERROR:
        ////            return cell.ErrorCellValue;
        ////        case CellType.Formula: //FORMULA:
        ////        default:
        ////            return "=" + cell.CellFormula;

        ////    }
        ////}
    }

}
