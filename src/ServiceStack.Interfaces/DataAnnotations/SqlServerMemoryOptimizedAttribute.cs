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

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SqlServerBucketCountAttribute : AttributeBase
    {
        public SqlServerBucketCountAttribute(int bucketCount)
        {
            BucketCount = bucketCount;
        }

        public int BucketCount { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SqlServerCollateAttribute : AttributeBase
    {
        public SqlServerCollateAttribute(string collation)
        {
            Collation = collation;
        }

        public string Collation { get; set; }
    }
}