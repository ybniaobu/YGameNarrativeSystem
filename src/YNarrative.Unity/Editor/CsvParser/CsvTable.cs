using System;
using System.Collections.Generic;

namespace YNarrative.Editor
{
    internal sealed class CsvTable
    {
        public static readonly CsvTable Empty =
            new CsvTable(Array.Empty<string>(), Array.Empty<CsvRow>());
        
        private readonly string[] _header; // 表头
        private readonly IReadOnlyList<CsvRow> _rows;
        
        public CsvRow this[int index] => _rows[index];
        public int Count => _rows.Count;

        private CsvTable(string[] header, IReadOnlyList<CsvRow> rows)
        {
            _header = header;
            _rows = rows;
        }

        /// <summary>
        /// 从 StringCsvRow 列表创建 StringCsvTable。第一行为表头，表头列名不允许重复。
        /// </summary>
        public static CsvTable FromRows(List<CsvRow> rows)
        {
            if (rows.Count == 0) return Empty;

            string[] header = rows[0].fields; // 第一行为表头

            for (int i = 0; i < header.Length; i++)
            {
                header[i] = header[i].Trim(); // 去除前后空格
                string name = header[i];
                if (Array.IndexOf(header, name, 0, i) >= 0)
                    throw new CsvParserException($"表头有重复的列名：{name}");
            }

            rows.RemoveAt(0); // 移除表头
            return new CsvTable(header, rows);
        }
        
        public bool TryGetColumn(string name, out int index)
        {
            index = Array.IndexOf(_header, name); 
            return index >= 0;
        }
        
        private int GetColumnIndex(string name)
        {
            if (TryGetColumn(name, out int index)) return index;
            throw new CsvParserException($"缺少列：{name}");
        }

        public string Get(CsvRow row, int columnIndex) => row.fields[columnIndex];
        public string Get(CsvRow row, string columnName) => Get(row, GetColumnIndex(columnName));
    }
}