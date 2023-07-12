using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using MySql.Data.MySqlClient;
using ServiceStack.OrmLite.MySql.Converters;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.MySql
{
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
	        
            var mysqlConn = (MySqlConnection)db.ToDbConnection();

            var tmpPath  = Path.GetTempFileName();
            using (var fs = File.OpenWrite(tmpPath))
            {
                CsvSerializer.SerializeToStream(objs, fs);
                fs.Close();
            }
	        
            var dialect = db.Dialect();
            var modelDef = ModelDefinition<T>.Definition;

            var bulkLoader = new MySqlBulkLoader(mysqlConn)
            {
                FileName = tmpPath,
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
        
            bulkLoader.Load();
            File.Delete(tmpPath);
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
}
