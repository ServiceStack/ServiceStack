using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using ServiceStack.OrmLite.MySql.Converters;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.MySql;

public class MySqlDialectProvider : MySqlDialectProviderBase<MySqlDialectProvider>
{
    public static MySqlDialectProvider Instance = new();

    private const string TextColumnDefinition = "TEXT";

    public override IDbConnection CreateConnection(string connectionString, Dictionary<string, string> options)
    {
        return new MySqlConnection(connectionString);
    }

    public MySqlDialectProvider()
    {
        RegisterConverter<DateTime>(new MySqlDateTimeConverter());
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

        using var fs = CreateTempFileStream();
        CreateBulkLoader(db, objs, fs).Load(fs);
    }
    
    public override async Task BulkInsertAsync<T>(IDbConnection db, IEnumerable<T> objs, BulkInsertConfig config = null, CancellationToken token=default)
    {
        config ??= new();
        if (config.Mode == BulkInsertMode.Sql)
        {
            await base.BulkInsertAsync(db, objs, config, token).ConfigAwait();
            return;
        }

        using var fs = CreateTempFileStream();
        await CreateBulkLoader(db, objs, fs).LoadAsync(fs, token).ConfigAwait();
    }

    private static FileStream CreateTempFileStream() => new(Path.GetTempFileName(), 
        FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose);

    private static MySqlBulkLoader CreateBulkLoader<T>(IDbConnection db, IEnumerable<T> objs, Stream stream)
    {
        var mysqlConn = (MySqlConnection)db.ToDbConnection();

        CsvSerializer.SerializeToStream(objs, stream);
        stream.Position = 0;
	        
        var dialect = db.Dialect();
        var modelDef = ModelDefinition<T>.Definition;

        var bulkLoader = new MySqlBulkLoader(mysqlConn)
        {
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
    
public class MySql55DialectProvider : MySqlDialectProviderBase<MySqlDialectProvider>
{
    public static MySql55DialectProvider Instance = new();

    private const string TextColumnDefinition = "TEXT";

    public override IDbConnection CreateConnection(string connectionString, Dictionary<string, string> options)
    {
        return new MySqlConnection(connectionString);
    }

    public MySql55DialectProvider()
    {
        RegisterConverter<DateTime>(new MySql55DateTimeConverter());
        RegisterConverter<string>(new MySql55StringConverter());
        RegisterConverter<char[]>(new MySql55CharArrayConverter());
    }

    public override IDbDataParameter CreateParam()
    {
        return new MySqlParameter();
    }
}