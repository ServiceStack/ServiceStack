using System;

namespace ServiceStack.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PostDropTableAttribute : AttributeBase
    {
        public string Sql { get; set; }

        public PostDropTableAttribute(string sql)
        {
            Sql = sql;
        }
    }
}