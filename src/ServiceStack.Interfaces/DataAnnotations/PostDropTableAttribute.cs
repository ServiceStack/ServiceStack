using System;

namespace ServiceStack.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class PostDropTableAttribute : AttributeBase
    {
        public string Sql { get; set; }

        public PostDropTableAttribute(string sql)
        {
            Sql = sql;
        }
    }
}