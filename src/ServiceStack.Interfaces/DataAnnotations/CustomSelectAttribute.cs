using System;

namespace ServiceStack.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property)]
    public class CustomSelectAttribute : AttributeBase
    {
        public string Sql { get; set; }

        public CustomSelectAttribute(string sql)
        {
            Sql = sql;
        }
    }
}