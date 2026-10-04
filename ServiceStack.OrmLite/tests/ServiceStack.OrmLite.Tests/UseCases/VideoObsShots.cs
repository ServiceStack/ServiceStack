using System.IO;
using System.Text;
using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests.UseCases;

// TEMPORARY: generates the output of the observability video's code screenshots
[TestFixtureOrmLite]
public class VideoObsShots(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    const string OutDir = "/tmp/claude-1000/-home-mythz-src-ServiceStack-ServiceStack-ServiceStack-OrmLite/b94012d0-814a-437c-8995-9c1913062bd3/scratchpad/obs";

    [Test]
    public void Shots()
    {
        using var db = OpenDbConnection();
        Bookstore.SeedMany(db, 5000);
        var sb = new StringBuilder();
        var q = db.From<Book>().Where(x => x.Author == "J.R.R. Tolkien").OrderBy(x => x.Year);
        sb.AppendLine("SQL: " + q.ToSelectStatement());
        sb.AppendLine("----- before");
        sb.AppendLine(db.Explain(q));
        var t = DialectProvider.GetQuotedTableName(ModelDefinition<Book>.Definition);
        var c = DialectProvider.GetQuotedColumnName(ModelDefinition<Book>.Definition.GetFieldDefinition("Author"));
        var create = $"CREATE INDEX idx_book_author ON {t} ({c})";
        db.ExecuteSql(create);
        if (Dialect.AnyPostgreSql.HasFlag(Dialect)) db.ExecuteSql("ANALYZE " + t);
        sb.AppendLine("----- " + create);
        sb.AppendLine(db.Explain(q));
        sb.AppendLine("----- analyze");
        try { sb.AppendLine(db.Explain(q, analyze: true)); }
        catch (System.Exception e) { sb.AppendLine(e.GetType().Name + ": " + e.Message); }
        sb.AppendLine("----- byid");
        sb.AppendLine(db.Explain(db.From<Book>().Where(x => x.Id == 1)));
        File.WriteAllText(Path.Combine(OutDir, $"obs-{DialectProvider.GetType().Name}.txt"), sb.ToString());
        db.DropAndCreateTable<Book>();
    }
}
