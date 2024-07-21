using System;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.Models;

public class DefaultValues
{
    public int Id { get; set; }

    [Default(1)]
    public int DefaultInt { get; set; }

    public int DefaultIntNoDefault { get; set; }

    [Default(1)]
    public int? NDefaultInt { get; set; }

    [Default(1.1)]
    public double DefaultDouble { get; set; }

    [Default(1.1)]
    public double? NDefaultDouble { get; set; }

    [Default("'String'")]
    public string DefaultString { get; set; }

    [Default(OrmLiteVariables.SystemUtc)]
    public DateTime CreatedDateUtc { get; set; }

    [Default(OrmLiteVariables.SystemUtc)]
    public DateTime? NCreatedDateUtc { get; set; }

    [Default(OrmLiteVariables.SystemUtc)]
    public DateTime UpdatedDateUtc { get; set; }
}
    
public class DefaultValuesUpdate
{
    public int Id { get; set; }

    [Default(1)]
    public int DefaultInt { get; set; }

    public int DefaultIntNoDefault { get; set; }

    [Default(1)]
    public int? NDefaultInt { get; set; }

    [Default(1.1)]
    public double DefaultDouble { get; set; }

    [Default(1.1)]
    public double? NDefaultDouble { get; set; }

    [Default("'String'")]
    public string DefaultString { get; set; }

    [Default(OrmLiteVariables.SystemUtc)]
    public DateTime UpdatedDateUtc { get; set; }
}

public class ModelWithDefaults
{
    [AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; }

    [Default(1)]
    public int DefaultInt { get; set; }

    [Default("'String'")]
    public string DefaultString { get; set; }
}