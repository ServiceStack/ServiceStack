using System;

namespace ServiceStack.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PostCreateTableAttribute : AttributeBase
    {
        public string Sql { get; set; }

        public PostCreateTableAttribute(string sql)
        {
            Sql = sql;
        }
    }
}