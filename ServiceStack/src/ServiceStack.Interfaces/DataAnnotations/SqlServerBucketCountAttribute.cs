using System;

namespace ServiceStack.DataAnnotations
{
    // https://msdn.microsoft.com/en-us/library/dn494956.aspx
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SqlServerBucketCountAttribute : AttributeBase
    {
        public SqlServerBucketCountAttribute(int count) { Count = count; }

        public int Count { get; set; } 
    }
}