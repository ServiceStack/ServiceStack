using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Model;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Issues;

public class DbPoco : IHasId<string>
{
    [Alias("Id_primary")]
    public string Id { get; set; }

    public string Other_Id { get; set; }
}

public class DTOPoco
{
    public string _Id { get; set; }
    public string Other_Id { get; set; }
}

[TestFixtureOrmLite]
public class SelectIntoTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public void Dont_guess_column_in_mismatched_Into_model()
    {
        OrmLiteConfig.DisableColumnGuessFallback = true;

        try
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<DbPoco>();

            db.Insert(new DbPoco { Id = "1", Other_Id = "OTHER" });

            var row = db.Select<DTOPoco>(db.From<DbPoco>()).First();

            row.PrintDump();

            Assert.That(row._Id, Is.Null);
            Assert.That(row.Other_Id, Is.EqualTo("OTHER"));
        }
        finally
        {
            OrmLiteConfig.DisableColumnGuessFallback = false;
        }
    }
}