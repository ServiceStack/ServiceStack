using System;

namespace ServiceStack.DataAnnotations
{
    // https://msdn.microsoft.com/en-us/library/dn553122.aspx
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
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