using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using ServiceStack.OrmLite.Tests.Shared;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests;

[TestFixtureOrmLiteDialects(Dialect.AnyMySql)]
public class BulkInsertMySqlTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public async Task Can_BulkInsert_CSV_Rockstars_MySql()
    {
        // "Server=localhost;User Id=root;Password=p@55wOrd;Database=test;Pooling=true;MinPoolSize=0;MaxPoolSize=200;AllowLoadLocalInfile=true;SslMode=None;AllowPublicKeyRetrieval=true"
        var dbFactory = new OrmLiteConnectionFactory(MySqlDb.DefaultConnection, MySqlDialect.Provider);
        MySqlDialect.Instance.AllowLoadLocalInfile = true;
        
        using var db = await dbFactory.OpenDbConnectionAsync();
        
        db.DropAndCreateTable<Person>();
        
        var mysqlConn = (MySqlConnection)db.ToDbConnection();

        var tmpPath  = Path.GetTempFileName();
        await using (var fs = File.OpenWrite(tmpPath))
        {
            await CsvSerializer.SerializeToStreamAsync(Person.Rockstars, fs);
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

        try
        {
            var rowCount = await bulkLoader.LoadAsync();
            File.Delete(tmpPath);

            var rows = db.Select<Person>();
            rows.PrintDump();
            Assert.That(rows.Count, Is.EqualTo(Person.Rockstars.Length));
        }
        catch (Exception e)
        {
            if (!IgnoreException(e))
                throw;
        }
    }
}
