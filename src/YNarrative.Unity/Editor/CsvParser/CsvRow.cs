namespace YNarrative.Editor
{
    internal readonly struct CsvRow
    {
        /// <summary>
        /// Excel 行号，表头是 1，第一条数据是 2
        /// </summary>
        public readonly int row;
        
        public readonly string[] fields;
        
        public CsvRow(int row, string[] fields)
        {
            this.row = row;
            this.fields = fields;
        }
        
        public override string ToString()
        {
            return $"Row: {row}, Fields: [{string.Join(", ", fields)}]";
        }
    }
}