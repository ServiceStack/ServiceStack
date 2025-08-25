using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests.Issues;

[TestFixtureOrmLite]
public class SaveAllIssue(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    class UserRole
    {
        public string Name { get; set; }
    }

    [Test]
    public void Can_use_SaveAll_to_save_one_column_table()
    {
        using var db = OpenDbConnection();
        db.CreateTableIfNotExists<UserRole>();

        db.SaveAll([
            new UserRole { Name = "Admin" },
            new UserRole { Name = "Reader" },
            new UserRole { Name = "Writer" }
        ]);

        var rows = db.Select<UserRole>();
        Assert.That(rows.Map(x => x.Name), Is.EquivalentTo(new[]{ "Admin", "Reader", "Writer"}));
    }
}