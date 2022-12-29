using System;

namespace ServiceStack.DataAnnotations;

public abstract class AlterColumnAttribute : AttributeBase {}

/// <summary>
/// Add Column during Db.Migrate, optional as adding columns are implied
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class AddColumnAttribute : AlterColumnAttribute
{
}

/// <summary>
/// Remove Column during Db.Migrate
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class RemoveColumnAttribute : AlterColumnAttribute
{
}

/// <summary>
/// Remove Column during Db.Migrate
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class RenameColumnAttribute : AlterColumnAttribute
{
    public string From { get; }
    public RenameColumnAttribute(string from) => From = from;
}
