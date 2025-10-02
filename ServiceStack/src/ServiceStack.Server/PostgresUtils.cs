using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ServiceStack.OrmLite;

namespace ServiceStack;

public static class PostgresUtils
{
    public static string CreatePartitionTableSql(IOrmLiteDialectProvider dialect, Type modelType, string dateField)
    {
        var modelDef = modelType.GetModelMetadata();
        var createTableSql = dialect.ToCreateTableStatement(modelType);
        var rawSql = createTableSql
            .Replace("CREATE TABLE ", "CREATE TABLE IF NOT EXISTS ")
            .Replace(" PRIMARY KEY", "").LastLeftPart(')').Trim();
        var idField = dialect.GetQuotedColumnName(modelDef.PrimaryKey);
        var createdFieldDef = modelDef.GetFieldDefinition(dateField)
                              ?? throw new Exception($"Field {dateField} not found on {modelType.Name}");
        var createdField = dialect.GetQuotedColumnName(createdFieldDef);
        var newSql = rawSql +
                     $"""
                      ,
                        PRIMARY KEY ({idField},{createdField})
                      ) PARTITION BY RANGE ({createdField});
                      """;
        return newSql;
    }

    public static string CreatePartitionSql(IOrmLiteDialectProvider dialect, Type modelType, DateTime createdDate)
    {
        // Normalize to first day of month to avoid overlapping partitions
        var monthStart = new DateTime(createdDate.Year, createdDate.Month, 1);
        var partitionName = GetMonthTableName(dialect, modelType, createdDate);
        var quotedPartitionName = dialect.QuoteTable(partitionName);
        var quotedTableName = dialect.GetQuotedTableName(modelType);

        var sql = $"""
                   CREATE TABLE IF NOT EXISTS {quotedPartitionName}
                   PARTITION OF {quotedTableName}
                   FOR VALUES FROM ('{monthStart:yyyy-MM-dd} 00:00:00') TO ('{monthStart.AddMonths(1):yyyy-MM-dd} 00:00:00');
                   """;
        return sql;
    }

    public static Func<IOrmLiteDialectProvider, Type, DateTime, string> GetMonthTableName { get; set; } = DefaultGetMonthTableName;

    public static string DefaultGetMonthTableName(IOrmLiteDialectProvider dialect, Type modelType, DateTime createdDate)
    {
        var suffix = createdDate.ToString("_yyyy_MM");
        var tableName = dialect.GetTableName(modelType);
        return tableName.EndsWith(suffix) 
            ? tableName 
            : tableName + suffix;
    }
    
    public static List<DateTime> GetTableMonths(IOrmLiteDialectProvider dialect, IDbConnection db, Type modelType)
    {
        var quotedTable = dialect.GetQuotedTableName(modelType);
        var partitionNames = db.SqlColumn<string>(
            $"SELECT relid::text\nFROM pg_partition_tree('{quotedTable}'::regclass)\nWHERE parentrelid IS NOT NULL");
        //["RequestLog_2025_10"]
        var monthDbs = partitionNames
            .Where(x => x.Contains('_'))
            .Select(x =>
                DateTime.TryParse(x.StripDbQuotes().RightPart('_').Replace('_', '-') + "-01", out var date) ? date : (DateTime?)null)
            .Where(x => x != null)
            .Select(x => x!.Value)
            .OrderByDescending(x => x)
            .ToList();
        return monthDbs;
    }
    
    
    // strftime('%Y-%m-%d %H:%M:%S', 'now')
    public static Dictionary<string, string> DateFormatMap = new() {
        {"%Y", "YYYY"},
        {"%m", "MM"},
        {"%d", "DD"},
        {"%H", "HH24"},
        {"%M", "MI"},
        {"%S", "SS"},
    };
    
    public static string SqlDateFormat(string quotedColumn, string format)
    {
        var fmt = format.Contains('\'')
            ? format.Replace("'", "")
            : format;
        foreach (var entry in DateFormatMap)
        {
            fmt = fmt.Replace(entry.Key, entry.Value);
        }
        return $"TO_CHAR({quotedColumn}, '{fmt}')";
    }

    public static void DropAllPartitions(IOrmLiteDialectProvider dialect, IDbConnection db, Type modelType)
    {
        var quotedTable = dialect.GetQuotedTableName(modelType);
        var partitionNames = db.SqlColumn<string>(
            $"SELECT relid::regclass::text FROM pg_partition_tree('{quotedTable}'::regclass) WHERE parentrelid IS NOT NULL");

        foreach (var partitionName in partitionNames)
        {
            db.ExecuteSql($"DROP TABLE IF EXISTS {partitionName} CASCADE;");
        }
    }

}