using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.OrmLite.Tests.Shared;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests;

public class BulkInsertSqliteTests : OrmLiteTestBase
{
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