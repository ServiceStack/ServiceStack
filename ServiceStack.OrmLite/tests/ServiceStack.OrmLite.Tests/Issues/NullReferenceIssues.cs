using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests.Issues;

[TestFixtureOrmLite]
public class NullReferenceIssues(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    public class Foo
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Key { get; set; }

        public int Int { get; set; }
    }

    [Test]
    [IgnoreDialect(Dialect.Sqlite, "Not supported")]
    public void Can_AlterColumn()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Foo>();

        db.AlterColumn(typeof(Foo), new FieldDefinition
        {
            Name = nameof(Foo.Name),
            FieldType = typeof(string),
            IsNullable = true,
            DefaultValue = null
        });

        db.AlterColumn(typeof(Foo), new FieldDefinition
        {
            Name = nameof(Foo.Int),
            FieldType = typeof(int),
            IsNullable = true,
            DefaultValue = null
        });

        db.AddColumn(typeof(Foo), new FieldDefinition
        {
            Name = "Bool",
            FieldType = typeof(bool),
        });
    }
}