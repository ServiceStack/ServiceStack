#nullable enable

using System.Data;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.SqlServerTests.Issues;

class Migration1000 : MigrationBase
{
    [Schema("MySchema")]
    public class BookingInASchema
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string? OldName { get; set; }
        public double ToDelete { get; set; }
    }

    public override void Up()
    {
        Db.DropTable<BookingInASchema>();
        Db.CreateSchema("MySchema");
        Db.CreateTable<BookingInASchema>();
    }

    public override void Down()
    {
        Db.DropTable<BookingInASchema>();
    }
}
class Migration1001 : MigrationBase
{
    [Schema("MySchema")]
    public class BookingInASchema
    {
        [RenameColumn(nameof(Migration1000.BookingInASchema.OldName))]
        public string? Name { get; set; }
        
        public RoomType RoomType { get; set; }
        
        [RemoveColumn]
        public double ToDelete { get; set; }
    }
    public enum RoomType {} // Values not necessary (Enum's saved as string by default)

    public override void Up() => Db.Migrate<BookingInASchema>();

    public override void Down() => Db.Revert<BookingInASchema>();
}

public class MigrationsWithSchemas : OrmLiteTestBase
{
    [Test]
    public void Can_run_migrations()
    {
        var dbFactory = CreateDbFactory();
        Migrator.Up(dbFactory, [typeof(Migration1000), typeof(Migration1001)]);
        using var db = dbFactory.OpenDbConnection();
        var value = db.Scalar<string>("SELECT Name FROM MySchema.BookingInASchema");
        Assert.That(value, Is.Null);
    }
}
