using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lint.Debugging
{
    /// <summary>
    ///     Represents a Debug table.
    /// </summary>
    internal sealed class DebugTable
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DebugTable" /> class with the specified headers.
        /// </summary>
        /// <param name="headers">The headers.</param>
        public DebugTable(params string[] headers)
        {
            if (headers == null)
            {
                throw new ArgumentNullException("headers");
            }

            if (!headers.Any())
            {
                throw new ArgumentException("Headers cannot be empty.", "headers");
            }

            ColumnHeaders = headers;
        }

        /// <summary>
        ///     Gets the table's headers.
        /// </summary>
        private IList<string> ColumnHeaders { get; set; }

        /// <summary>
        ///     Gets the table's entities.
        /// </summary>
        private IList<string[]> Entities { get {return _Entities;} }
		private IList<string[]> _Entities = new List<string[]>();
        
        /// <summary>
        ///     Adds a new row with the specified values.
        /// </summary>
        /// <param name="values">The values.</param>
        public void AddRow(params object[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            if (values.Length > ColumnHeaders.Count)
            {
                throw new ArgumentException("Too much data.", "values");
            }

            Entities.Add(values.Select(v => v.ToString()).ToArray());
        }

        /// <summary>
        ///     Gets the table output.
        /// </summary>
        public string GetOutput()
        {
            var tableBuilder = new StringBuilder();
            tableBuilder.AppendLine(GetRowSeparatorString('='));

            var columnLengths = GetColumnLengths().ToArray();
            for (var i = 0; i < ColumnHeaders.Count; ++i)
            {
                var header = ColumnHeaders[i];
                tableBuilder.Append("" + header + "" + new string(' ', columnLengths[i] - header.Length) + " |");
            }

            tableBuilder.AppendLine("\n" + GetRowSeparatorString('=') + "");
            foreach (var entity in Entities)
            {
                for (var i = 0; i < entity.Length; ++i)
                {
                    var value = entity[i];
                    tableBuilder.Append("" + value + "" + new string(' ', columnLengths[i] - value.Length) + " |");
                }

                tableBuilder.AppendLine("\n" + GetRowSeparatorString() + "");
            }

            return tableBuilder.ToString();
        }

        /// <summary>
        ///     Calculates column lengths based on the longest element in each column.
        /// </summary>
        /// <returns>The lengths.</returns>
        private IEnumerable<int> GetColumnLengths()
        {
            return ColumnHeaders.Select((c, ix) =>
                Entities.Select(e => e[ix]).Union(new[] {ColumnHeaders[ix]}).Max(s => s.Length));
        }

        private string GetRowSeparatorString(char separator = '-') { return 
        		new string(separator, GetColumnLengths().Sum() + ColumnHeaders.Count * 2); }
    }
}