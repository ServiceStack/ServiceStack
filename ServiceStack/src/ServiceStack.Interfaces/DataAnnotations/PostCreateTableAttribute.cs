using System;

namespace ServiceStack.DataAnnotations;

/// <summary>
/// Run Custom SQL immediately after RDBMS table is created
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class PostCreateTableAttribute(string sql) : AttributeBase
{
    public string Sql { get; set; } = sql;
}