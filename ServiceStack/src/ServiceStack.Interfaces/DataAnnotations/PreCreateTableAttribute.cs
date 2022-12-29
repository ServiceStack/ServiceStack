using System;

namespace ServiceStack.DataAnnotations
{
    /// <summary>
    /// Run Custom SQL immediately before RDBMS table is created
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class PreCreateTableAttribute : AttributeBase
    {
        public string Sql { get; set; }

        public PreCreateTableAttribute(string sql)
        {
            Sql = sql;
        }
    }
}