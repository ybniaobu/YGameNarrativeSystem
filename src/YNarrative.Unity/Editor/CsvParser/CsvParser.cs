using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace YNarrative.Editor
{
    internal sealed class CsvParserException : Exception
    {
        public CsvParserException(string message) : base(message) { }
    }
    
    /// <summary>
    /// CSV 文本解析器
    /// </summary>
    /// <remarks>
    /// <para>CSV 每条记录通常占一行，字段之间用逗号分隔。字段可以被双引号括起来，以允许字段中包含<b>逗号、换行符或双引号本身</b>。
    /// 参考 RFC 4180：https://www.rfc-editor.org/info/rfc4180/</para>
    ///
    /// <para>解析过程会为每个字段新建 string（名字里的 String 的由来），每行还会新建一个 string 数组，分配量大，并会产生较多 GC。
    /// 只适合编辑器里的工具（导表、烘焙）这种一次性调用，不要用在运行时的高频路径上。</para>
    /// 
    /// <para>该类位于 Editor 程序集，运行时不可用。即便将来需要在运行时读 CSV，也应该改成按字节解析或使用烘焙后的二进制/JSON。</para>
    /// </remarks>
    internal static class CsvParser
    {
        /// <summary>
        /// 未加引号的字段（unquoted field）遇到这些字符就停：逗号、回车、换行、引号。
        /// </summary>
        private static readonly char[] FieldStops = { ',', '\r', '\n', '"' };

        /// <summary>
        /// 解析 CSV 文件，返回 StringCsvTable。
        /// </summary>
        /// <remarks>
        /// 只支持 UTF-8，Excel 在中文环境另存 CSV 若存为 GBK，读进来中文会变成乱码
        /// </remarks>
        /// <exception cref="CsvParserException">文件不存在</exception>
        public static CsvTable ParseTableFromFile(string absolutePath)
        {
            if (!File.Exists(absolutePath)) throw new CsvParserException($"文件不存在：{absolutePath}");
            return ParseTable(File.ReadAllText(absolutePath, Encoding.UTF8)); // ReadAllText 自动去除 BOM
        }
        
        /// <summary>
        /// 解析 CSV 文件，返回 StringCsvRow 列表（即不把首行当表头）。
        /// </summary>
        /// <remarks>
        /// 只支持 UTF-8，Excel 在中文环境另存 CSV 若存为 GBK，读进来中文会变成乱码
        /// </remarks>
        /// <exception cref="CsvParserException">文件不存在</exception>
        public static List<CsvRow> ParseRowsFromFile(string absolutePath)
        {
            if (!File.Exists(absolutePath)) throw new CsvParserException($"文件不存在：{absolutePath}");
            return ParseRows(File.ReadAllText(absolutePath, Encoding.UTF8)); // ReadAllText 自动去除 BOM
        }

        /// <summary>
        /// 使用状态机解析 CSV 文本，返回 StringCsvTable。
        /// </summary>
        /// <remarks>
        /// 需要考虑的情况：字段可能会被引号括起来，被引号包裹的字段（quoted field）内可能包含换行符（\r、\n）、逗号、引号（"" 转义）。
        /// </remarks>
        public static CsvTable ParseTable(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return CsvTable.Empty;

            List<CsvRow> rows = new List<CsvRow>();
            List<string> fields = new List<string>();

            int row = 1; // Excel 行号
            int i = text[0] == '\uFEFF' ? 1 : 0; // 字符索引，去除 UTF-8 BOM

            while (i < text.Length)
            {
                ReadRow(text, ref i, fields, row);
                if (!IsBlank(fields)) rows.Add(new CsvRow(row, fields.ToArray())); // 空行不计入
                row++;
            }

            return CsvTable.FromRows(rows);
        }

        /// <summary>
        /// 解析 CSV 文本，返回 StringCsvRow 列表（即不把首行当表头）。
        /// </summary>
        public static List<CsvRow> ParseRows(string text)
        {
            List<CsvRow> rows = new List<CsvRow>();
            if (string.IsNullOrWhiteSpace(text)) return rows;
            
            List<string> fields = new List<string>();
            int row = 1;
            int i = text[0] == '\uFEFF' ? 1 : 0;
            
            while (i < text.Length)
            {
                ReadRow(text, ref i, fields, row);
                if (!IsBlank(fields)) rows.Add(new CsvRow(row, fields.ToArray())); // 空行不计入
                row++;
            }

            return rows;
        }

        /// <summary>
        /// 读一行记录，结果放进 fields。i 前进到记录之后。
        /// </summary>
        private static void ReadRow(string text, ref int i, List<string> fields, int row)
        {
            fields.Clear();

            while (true)
            {
                string value = i < text.Length && text[i] == '"'
                    ? ReadQuotedField(text, ref i, row)
                    : ReadUnquotedField(text, ref i, row);

                fields.Add(value);

                if (i >= text.Length) return; // EOF，记录结束

                char c = text[i];

                if (c == ',')
                {
                    i++;
                    if (i >= text.Length)
                    {
                        fields.Add(string.Empty); // 末尾逗号 -> 最后一个空字段
                        return;
                    }
                    continue;
                }

                // 换行 \r、\n、\r\n -> 记录结束
                if (c == '\r') i++;
                if (i < text.Length && text[i] == '\n') i++;
                return;
            }
        }

        /// <summary>
        /// 未加引号的字段（unquoted field）：一次跳到下一个分隔符。
        /// </summary>
        private static string ReadUnquotedField(string text, ref int i, int row)
        {
            int stop = text.IndexOfAny(FieldStops, i);
            if (stop < 0) stop = text.Length; // 没有分隔符就读到末尾

            if (stop < text.Length && text[stop] == '"')
                throw new CsvParserException($"第 {row} 行：未加引号的字段里出现了引号");

            string value = text.Substring(i, stop - i); // 取字段值（不包括分隔符）
            i = stop; // 前进到分隔符
            return value;
        }

        /// <summary>
        /// 被引号包裹的字段（quoted field）：一次跳到下一个引号，处理 "" 转义。换行符视作普通字符，无需处理。
        /// </summary>
        private static string ReadQuotedField(string text, ref int i, int row)
        {
            i++; // 跳过开头的双引号
            
            StringBuilder sb = new StringBuilder();

            while (true)
            {
                int q = text.IndexOf('"', i);
                
                if (q < 0) throw new CsvParserException($"第 {row} 行：引号没有闭合");
                if (q > i) sb.Append(text, i, q - i); // 取字段值（不包括收尾双引号）

                if (q + 1 < text.Length && text[q + 1] == '"') // 判断是否是 "" 转义
                {
                    sb.Append('"'); // 两个双引号 "" -> 一个双引号
                    i = q + 2; // 跳过转义双引号，到下一个字符
                    continue;
                }

                i = q + 1; // 跳过收尾双引号，前进到分隔符
                break;
            }

            // 收尾引号后面只能是逗号、换行或 EOF
            if (i < text.Length && text[i] != ',' && text[i] != '\r' && text[i] != '\n')
                throw new CsvParserException($"第 {row} 行：引号后面出现了非法字符 '{text[i]}'");

            return sb.ToString();
        }
        
        /// <summary>
        /// 判断一行是否是空行（所有字段都是空白字符串或没有字段）。
        /// </summary>
        private static bool IsBlank(List<string> fields)
        {
            foreach (string f in fields)
                if (!string.IsNullOrWhiteSpace(f)) return false;
            return true;
        }
    }
}