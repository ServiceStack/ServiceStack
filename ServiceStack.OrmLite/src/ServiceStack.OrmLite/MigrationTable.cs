#nullable enable
using System;
using System.Collections.Generic;

namespace ServiceStack.OrmLite;

/// <summary>
/// A table that migrations create or change, from the copy of its model a migration declares, and the App's model
/// of the same table, which is the latest version of the table
/// </summary>
public class MigrationTable
{
    /// <summary>
    /// The name of the table, its [Alias] or class name
    /// </summary>
    public string Table { get; set; } = "";

    /// <summary>
    /// The schema of the table, from its [Schema]
    /// </summary>
    public string? Schema { get; set; }

    /// <summary>
    /// The database of the migrations, from their [NamedConnection], null for the App's main database
    /// </summary>
    public string? NamedConnection { get; set; }

    /// <summary>
    /// The copies of the model declared in migrations, in the order the migrations run
    /// </summary>
    public List<Type> Copies { get; set; } = [];

    /// <summary>
    /// The App's model of the table, null when the App doesn't have one, e.g. when the table was dropped, or when
    /// more than one App model has the name of the table
    /// </summary>
    public Type? ModelType { get; set; }

    /// <summary>
    /// The App's models with the name of the table, which has more than one when ModelType can't be chosen
    /// </summary>
    public List<Type> Candidates { get; set; } = [];

    public override string ToString() => Schema != null ? $"{Schema}.{Table}" : Table;
}
