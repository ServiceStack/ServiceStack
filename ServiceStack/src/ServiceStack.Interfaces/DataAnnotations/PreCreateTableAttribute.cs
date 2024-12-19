using System;

namespace ServiceStack.DataAnnotations;

/// <summary>
/// Run Custom SQL immediately before RDBMS table is created
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class PreCreateTableAttribute(string sql) : AttributeBase
{
    public string Sql { get; set; } = sql;
}