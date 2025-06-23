using System;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Kingbase.Tests.Cli;

// [Alias(nameof(TestTable))]
public class TestTable
{
    [AutoIncrement, PrimaryKey] public int Id { get; set; }
    public string Name { get; set; }
    [Default(OrmLiteVariables.SystemUtc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Default(OrmLiteVariables.SystemUtc)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [Default(OrmLiteVariables.False)]
    public bool IsActive { get; set; } = true;
}