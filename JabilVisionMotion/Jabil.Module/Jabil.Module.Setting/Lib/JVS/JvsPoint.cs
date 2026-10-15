using System;
using System.Collections.Generic;
using System.Globalization;

namespace Jabil.Module.Setting.Lib.JVS
{
    /// <summary>JVS 检测结果点（X, Y, R 角度）</summary>
    public class JvsPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double R { get; set; }

        public override string ToString() => $"{X:F3},{Y:F3},{R:F3}";
    }

    /// <summary>JVS 单次执行结果（进程内模式）</summary>
    public class JvsRunResult
    {
        /// <summary>run_status.success</summary>
        public bool Success { get; set; }
        /// <summary>JVS 内工具处理耗时（ms）</summary>
        public double ProcessingMs { get; set; }
        /// <summary>JVS 总耗时（ms）</summary>
        public double TotalMs { get; set; }
        /// <summary>解析后的检测点数组（0~N 个）</summary>
        public List<JvsPoint> Points { get; set; } = new List<JvsPoint>();
        /// <summary>输出 pin 原始字符串（data）</summary>
        public string RawData { get; set; }
        /// <summary>错误描述（Success=false 时有效）</summary>
        public string Error { get; set; }
    }

    /// <summary>
    /// JVS 结果解析器：把输出 pin 字符串解析为点数组。
    /// 真实方案"筛选分拣程序.jvws"输出 pin = data（str），格式："X,Y,R;X,Y,R;…"；
    /// 分号分点、逗号分 X/Y/R（角度），点数动态（2~5）。
    /// 分隔符/字段顺序可通过 <see cref="JvsWorkspaceMapping"/> 配置。
    /// </summary>
    public static class JvsDataParser
    {
        /// <summary>
        /// 解析输出 pin 字符串为点数组。
        /// </summary>
        /// <param name="rawData">输出 pin 原始值（字符串或可 ToString 的对象）。</param>
        /// <param name="mapping">映射配置（分隔符/字段顺序）。</param>
        /// <returns>解析出的点数组；为空字符串/null/格式不符时返回空列表。</returns>
        public static List<JvsPoint> Parse(object rawData, JvsWorkspaceMapping mapping)
        {
            var points = new List<JvsPoint>();
            if (rawData == null)
            {
                return points;
            }

            string text = rawData is string ? (string)rawData : Convert.ToString(rawData, CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(text))
            {
                return points;
            }

            string[] fieldNames = (mapping.FieldOrder ?? "X,Y,R").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            string[] pointParts = text.Split(new[] { mapping.PointSeparator ?? ";" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string pointPart in pointParts)
            {
                string[] fields = pointPart.Split(new[] { mapping.FieldSeparator ?? "," }, StringSplitOptions.RemoveEmptyEntries);
                if (fields.Length < fieldNames.Length)
                {
                    continue; // 字段不足，丢弃该点
                }
                var p = new JvsPoint();
                bool ok = true;
                for (int i = 0; i < fieldNames.Length; i++)
                {
                    double v;
                    if (!double.TryParse(fields[i], NumberStyles.Float, CultureInfo.InvariantCulture, out v))
                    {
                        ok = false;
                        break;
                    }
                    switch (fieldNames[i].Trim().ToUpperInvariant())
                    {
                        case "X": p.X = v; break;
                        case "Y": p.Y = v; break;
                        case "R": p.R = v; break;
                    }
                }
                if (ok)
                {
                    points.Add(p);
                }
            }
            return points;
        }
    }
}
