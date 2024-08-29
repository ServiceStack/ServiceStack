using System.Data;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.Async;

public class LetterFrequency
{
    [AutoIncrement]
    public int Id { get; set; }

    public string Letter { get; set; }
}

[TestFixtureOrmLiteDialects(Dialect.AnySqlServer)]
public class CustomSqlTestsAsync(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    private const string DropProcedureSql = @"
            IF OBJECT_ID('spSearchLetters') IS NOT NULL
                    DROP PROCEDURE spSearchLetters";

    private const string DropInsertProcedureSql = @"
            IF OBJECT_ID('spInsertLetter') IS NOT NULL
                    DROP PROCEDURE spInsertLetter";

    private const string CreateProcedureSql = @"
            CREATE PROCEDURE spSearchLetters 
            (
                @pLetter varchar(10),
                @pTotal int OUT
            )
            AS
            BEGIN
                SELECT @pTotal = COUNT(*) FROM LetterFrequency WHERE Letter = @pLetter
                SELECT * FROM LetterFrequency WHERE Letter = @pLetter
            END";

    private const string CreateInsertProcedureSql = @"
            CREATE PROCEDURE spInsertLetter
            (
                @pLetter varchar(10),
                @pTotal int OUT
            )
            AS
            BEGIN
                INSERT INTO LetterFrequency (Letter)
                VALUES (@pLetter)

                SELECT @pTotal = COUNT(*) FROM LetterFrequency WHERE Letter = @pLetter
            END";

    [Test]
    public async Task Can_execute_stored_procedure_using_SqlList_with_out_params()
    {
        using (var db = OpenDbConnection())
        {
            db.DropAndCreateTable<LetterFrequency>();

            var rows = "A,B,B,C,C,C,D,D,E".Split(',').Map(x => new LetterFrequency { Letter = x });
            await db.InsertAllAsync(rows);

            await db.ExecuteSqlAsync(DropProcedureSql);
            await db.ExecuteSqlAsync(CreateProcedureSql);

            IDbDataParameter pTotal = null;
            var results = await db.SqlListAsync<LetterFrequency>("spSearchLetters",
                cmd => {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.AddParam("pLetter", "C");
                    pTotal = cmd.AddParam("pTotal", direction: ParameterDirection.Output);
                });

            Assert.That(results.Count, Is.EqualTo(3));
            Assert.That(pTotal.Value, Is.EqualTo("3"));
        }
    }

    [Test]
    public async Task Can_execute_stored_procedure_using_SqlProc_with_out_params()
    {
        using (var db = OpenDbConnection())
        {
            db.DropAndCreateTable<LetterFrequency>();

            var rows = "A,B,B,C,C,C,D,D,E".Split(',').Map(x => new LetterFrequency { Letter = x });
            await db.InsertAllAsync(rows);

            await db.ExecuteSqlAsync(DropProcedureSql);
            await db.ExecuteSqlAsync(CreateProcedureSql);

            var cmd = db.SqlProc("spSearchLetters", new { pLetter = "C" });

            Assert.That(((OrmLiteCommand)cmd).IsDisposed, Is.False);

            var pTotal = cmd.AddParam("pTotal", direction: ParameterDirection.Output);
            var results = await cmd.ConvertToListAsync<LetterFrequency>();

            Assert.That(results.Count, Is.EqualTo(3));
            Assert.That(pTotal.Value, Is.EqualTo("3"));
        }
    }

    [Test]
    public async Task Can_execute_stored_procedure_using_SqlProc_with_out_params_NonQueryAsync()
    {
        using (var db = OpenDbConnection())
        {
            db.DropAndCreateTable<LetterFrequency>();

            var rows = "A,B,B,C,C,C,D,D,E".Split(',').Map(x => new LetterFrequency { Letter = x });
            await db.InsertAllAsync(rows);

            await db.ExecuteSqlAsync(DropInsertProcedureSql);
            await db.ExecuteSqlAsync(CreateInsertProcedureSql);

            var cmd = db.SqlProc("spInsertLetter", new { pLetter = "C" });

            Assert.That(((OrmLiteCommand)cmd).IsDisposed, Is.False);

            var pTotal = cmd.AddParam("pTotal", direction: ParameterDirection.Output);
            await cmd.ExecNonQueryAsync();

            Assert.That(pTotal.Value, Is.EqualTo("4"));
        }
    }

    [Test]
    [NUnit.Framework.Ignore("Requires out-of-band SP")]
    public async Task Can_execute_stored_procedure_returning_scalars()
    {
        using (var db = OpenDbConnection())
        {
            using (var cmd = db.SqlProc(
                       "GetUserIdsFromEmailAddresses", new { EmailAddresses = "as@if.com" }))
            {
                var userIds = await cmd.ConvertToListAsync<int>();

                Assert.That(userIds.Count, Is.GreaterThan(0));
            }
        }
    }
}