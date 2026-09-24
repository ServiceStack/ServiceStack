using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using ServiceStack.OrmLite.MySql.Converters;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.MySql;

public class MySqlConnectorDialectProvider : MySqlDialectProviderBase<MySqlConnectorDialectProvider>
{
    public static MySqlConnectorDialectProvider Instance = new();

    private const string TextColumnDefinition = "TEXT";

    public MySqlConnectorDialectProvider()
    {
        base.RegisterConverter<DateTime>(new MySqlConnectorDateTimeConverter());
    }

    public override IDbConnection CreateConnection(string connectionString, Dictionary<string, string> options)
    {
        return new MySqlConnection(connectionString);
    }

    public override IDbDataParameter CreateParam()
    {
        return new MySqlParameter();
    }

    public override void BulkInsert<T>(IDbConnection db, IEnumerable<T> objs, BulkInsertConfig config = null)
    {
        config ??= new();
        if (config.Mode == BulkInsertMode.Sql)
        {
            base.BulkInsert(db, objs, config);
            return;
        }

        using var ms = MemoryStreamFactory.GetStream();
        CreateBulkLoader(db, objs, ms).Load();
    }
    
    public override async Task BulkInsertAsync<T>(IDbConnection db, IEnumerable<T> objs, BulkInsertConfig config = null, CancellationToken token=default)
    {
        config ??= new();
        if (config.Mode == BulkInsertMode.Sql)
        {
            await base.BulkInsertAsync(db, objs, config, token).ConfigAwait();
            return;
        }

        using var ms = MemoryStreamFactory.GetStream();
        await CreateBulkLoader(db, objs, ms).LoadAsync(token).ConfigAwait();
    }

    private static MySqlBulkLoader CreateBulkLoader<T>(IDbConnection db, IEnumerable<T> objs, Stream ms)
    {
        var mysqlConn = (MySqlConnection)db.ToDbConnection();

        CsvSerializer.SerializeToStream(objs, ms);
        ms.Position = 0;
	        
        var dialect = db.Dialect();
        var modelDef = ModelDefinition<T>.Definition;

        var bulkLoader = new MySqlBulkLoader(mysqlConn)
        {
            SourceStream = ms,
            Local = true,
            TableName = dialect.GetQuotedTableName(modelDef),
            CharacterSet = "UTF8",
            NumberOfLinesToSkip = 1,
            FieldTerminator = ",",
            FieldQuotationCharacter = '"',
            FieldQuotationOptional = true,
            EscapeCharacter = '\\',
            LineTerminator = Environment.NewLine,
        };
        
        var columns = CsvSerializer.PropertiesFor<T>()
            .Select(x => dialect.GetQuotedColumnName(modelDef.GetFieldDefinition(x.PropertyName)));
        bulkLoader.Columns.AddRange(columns);
        return bulkLoader;
    }
}
