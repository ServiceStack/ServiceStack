using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using ServiceStack.OrmLite.Tests.Shared;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests;

public class BulkInsertTests : OrmLiteTestBase
{
    [Test]
    public async Task Can_BulkInsert_CSV_Rockstars_MySql()
    {
        var dbFactory = new OrmLiteConnectionFactory(
            "Server=localhost;User Id=root;Password=p@55wOrd;Database=test;Pooling=true;MinPoolSize=0;MaxPoolSize=200;AllowLoadLocalInfile=true;SslMode=None;AllowPublicKeyRetrieval=true", 
            MySqlDialect.Provider);
        MySqlDialect.Instance.AllowLoadLocalInfile = true;
        
        using var db = await dbFactory.OpenDbConnectionAsync();
        
        db.DropAndCreateTable<Person>();
        
        var mysqlConn = (MySqlConnection)db.ToDbConnection();

        var tmpPath  = Path.GetTempFileName();
        using (var fs = File.OpenWrite(tmpPath))
        {
            CsvSerializer.SerializeToStream(Person.Rockstars, fs);
            fs.Close();
        }

        var bulkLoader = new MySqlBulkLoader(mysqlConn)
        {
            FileName = tmpPath,
            Local = true,
            TableName = nameof(Person),
            CharacterSet = "UTF8",
            NumberOfLinesToSkip = 1,
            FieldTerminator = ",",
            FieldQuotationCharacter = '"',
            FieldQuotationOptional = true,
            EscapeCharacter = '\\',
            LineTerminator = Environment.NewLine,
        };
        
        var dialect = db.Dialect();
        var modelDef = ModelDefinition<Person>.Definition;
        var columns = CsvSerializer.PropertiesFor<Person>()
            .Select(x => dialect.GetQuotedColumnName(modelDef.GetFieldDefinition(x.PropertyName)));
        bulkLoader.Columns.AddRange(columns);
        
        var rowCount = await bulkLoader.LoadAsync();
        File.Delete(tmpPath);

        var rows = db.Select<Person>();
        rows.PrintDump();
        Assert.That(rows.Count, Is.EqualTo(Person.Rockstars.Length));
    }

    [Test]
    public async Task Can_BulkInsert_CSV_Rockstars_Sqlite()
    {
        var dbFactory = CreateSqliteMemoryDbFactory();
        using var db = await dbFactory.OpenDbConnectionAsync();
        db.DropAndCreateTable<Person>();

        var dialect = db.Dialect();
        
        var sql = dialect.ToInsertRowsSql(Person.Rockstars);
        sql.Print();

        db.ExecuteSql(sql);
        
        var rows = db.Select<Person>();
        rows.PrintDump();
        Assert.That(rows.Count, Is.EqualTo(Person.Rockstars.Length));
    }
    
}
