using System;

namespace ServiceStack.DataAnnotations
{
    /// <summary>
    /// Run Custom SQL immediately after RDBMS table is created
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class PostCreateTableAttribute : AttributeBase
    {
        public string Sql { get; set; }

        public PostCreateTableAttribute(string sql)
        {
            Sql = sql;
        }
    }
}