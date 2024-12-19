using System;

namespace ServiceStack.DataAnnotations;

/// <summary>
/// Use in FirebirdSql. indicates name of generator for columns of type AutoIncrement
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class SequenceAttribute(string name) : AttributeBase
{
    public string Name { get; set; } = name;
}