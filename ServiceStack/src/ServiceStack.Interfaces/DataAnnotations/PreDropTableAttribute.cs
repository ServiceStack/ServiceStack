using System;

namespace ServiceStack.DataAnnotations;

/// <summary>
/// Run Custom SQL immediately before RDBMS table is dropped
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class PreDropTableAttribute(string sql) : AttributeBase
{
    public string Sql { get; set; } = sql;
}