using System;

namespace ServiceStack.DataAnnotations;

/// <summary>
/// Create a system-versioned (temporal) table, where the RDBMS keeps every previous version of each row, so the
/// table can be queried as it was at any time with q.AsOf(time). Supported by SQL Server 2016+ and MariaDB 10.3+.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class SystemVersionedAttribute : AttributeBase
{
    /// <summary>
    /// The name of the table SQL Server keeps previous versions in, which is the table's name with History by default
    /// </summary>
    public string HistoryTable { get; set; }
}

/// <summary>
/// The time a version of a row of a [SystemVersioned] table is from, which is set by the RDBMS
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class RowStartAttribute : AttributeBase;

/// <summary>
/// The time a version of a row of a [SystemVersioned] table was replaced, which is set by the RDBMS. Current rows
/// have the latest time the RDBMS can store.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class RowEndAttribute : AttributeBase;
