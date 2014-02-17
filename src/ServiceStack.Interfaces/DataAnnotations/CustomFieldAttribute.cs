using System;

namespace ServiceStack.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property)]
    public class CustomFieldAttribute : AttributeBase
    {
        public string Sql { get; set; }

        public CustomFieldAttribute(string sql)
        {
            Sql = sql;
        }
    }
}