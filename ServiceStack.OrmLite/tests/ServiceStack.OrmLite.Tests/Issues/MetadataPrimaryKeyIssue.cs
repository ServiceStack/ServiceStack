using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.OrmLite.Tests.Expression;

namespace ServiceStack.OrmLite.Tests.Issues;

[TestFixtureOrmLite]
public class MetadataPrimaryKeyIssue(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public void Should_generate_select_statement_multi_threaded()
    {
        typeof(LetterFrequency).GetModelMetadata();

        Task<string> task1 = Task.Run(() => SelectStatement());
        Task<string> task2 = Task.Run(() => SelectStatement());
        Task.WaitAll(task1, task2);

        Assert.AreEqual(task1.Result, task2.Result);
    }

    private string SelectStatement()
    {
        var pk = typeof(LetterFrequency).GetModelMetadata().PrimaryKey;
        using var db = OpenDbConnection();
        return db.From<LetterFrequency>().OrderByFields(pk).ToSelectStatement();
    }
}