using System;

namespace ServiceStack.DataAnnotations;

/// <summary>
/// Populate property with Custom SELECT expression, e.g. [CustomSelect("Width * Height")]
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class CustomSelectAttribute(string sql) : AttributeBase
{
    public string Sql { get; set; } = sql;
}

/// <summary>
/// Populate INSERT parameter with Custom SQL expression, e.g. [CustomInsert("crypt({0}, gen_salt('bf'))")]
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class CustomInsertAttribute(string sql) : AttributeBase
{
    public string Sql { get; set; } = sql;
}

/// <summary>
/// Populate UPDATE parameter with Custom SQL expression, e.g. [CustomUpdate("crypt({0}, gen_salt('bf'))")]
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class CustomUpdateAttribute(string sql) : AttributeBase
{
    public string Sql { get; set; } = sql;
}