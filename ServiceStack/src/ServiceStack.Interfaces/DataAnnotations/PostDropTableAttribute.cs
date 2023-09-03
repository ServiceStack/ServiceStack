using System;

namespace ServiceStack.DataAnnotations;

/// <summary>
/// Run Custom SQL immediately after RDBMS table is dropped
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class PostDropTableAttribute : AttributeBase
{
    public string Sql { get; set; }

    public PostDropTableAttribute(string sql)
    {
        Sql = sql;
    }
}