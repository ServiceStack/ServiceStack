#nullable enable

using System.Collections.Generic;

namespace ServiceStack.OrmLite;

public enum BulkInsertMode
{
    Binary,
    Csv,
    Sql,
}

public class BulkInsertConfig
{
    public int BatchSize { get; set; } = 1000;
    public BulkInsertMode Mode { get; set; } = BulkInsertMode.Csv;
    public ICollection<string>? InsertFields { get; set; } = null;
}
