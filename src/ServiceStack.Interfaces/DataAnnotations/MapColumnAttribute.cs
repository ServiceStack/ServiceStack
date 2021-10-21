using System;

namespace ServiceStack.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class MapColumnAttribute : AttributeBase
    {
        public string Table { get; set; }
        public string Column { get; set; }

        public MapColumnAttribute(string table, string column)
        {
            Table = table ?? throw new ArgumentNullException(nameof(table));
            Column = column ?? throw new ArgumentNullException(nameof(column));
        }
    }
}