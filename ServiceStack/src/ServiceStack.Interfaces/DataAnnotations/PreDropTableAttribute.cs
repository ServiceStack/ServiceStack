using System;

namespace ServiceStack.DataAnnotations;

/// <summary>
/// Run Custom SQL immediately before RDBMS table is dropped
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class PreDropTableAttribute : AttributeBase
{
    public string Sql { get; set; }

    public PreDropTableAttribute(string sql)
    {
        Sql = sql;
    }
}