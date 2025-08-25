using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests.Issues;

[TestFixtureOrmLite]
public class SelectWithBytesIssue(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    public class ModelWithBytes
    {
        public int Id { get; set; }
        public byte[] Bytes { get; set; }
    }

    [Test]
    public void Can_select_ModelWithBytes_using_anon_type()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<ModelWithBytes>();

        db.Insert(new ModelWithBytes
        {
            Id = 1,
            Bytes = 1.ToUtf8Bytes()
        });

        var result = db.Single<ModelWithBytes>(new
        {
            Bytes = 1.ToUtf8Bytes()
        });

        Assert.That(result, Is.Not.Null);
    }

}