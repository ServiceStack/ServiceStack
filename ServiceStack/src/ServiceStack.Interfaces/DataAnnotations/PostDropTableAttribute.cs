using System;

namespace ServiceStack.DataAnnotations;

/// <summary>
/// Run Custom SQL immediately after RDBMS table is dropped
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class PostDropTableAttribute(string sql) : AttributeBase
{
    public string Sql { get; set; } = sql;
}