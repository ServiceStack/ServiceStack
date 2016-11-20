using System;

namespace ServiceStack.DataAnnotations
{
    // SQL 2014: https://msdn.microsoft.com/en-us/library/dn553122(v=sql.120).aspx
    // SQL 2016: https://msdn.microsoft.com/en-us/library/dn553122(v=sql.130).aspx
    public class SqlServerMemoryOptimizedAttribute : AttributeBase
    {
        public SqlServerMemoryOptimizedAttribute() { }

        public SqlServerMemoryOptimizedAttribute(SqlServerDurability durability) { Durability = durability; }

        public SqlServerDurability? Durability { get; set; }
    }

    public enum SqlServerDurability
    {
        SchemaOnly, // (non-durable table) recreated upon server restart, data is lost, no transaction logging and checkpoints
        SchemaAndData  // (durable table) data persists upon server restart
    }
}