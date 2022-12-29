#nullable enable
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.Migrations;

[Notes("Create initial Database")]
public class Migration1000 : MigrationBase
{
    public class Booking
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string? OldName { get; set; }
        public double ToDelete { get; set; }
    }

    public override void Up() => Db.CreateTable<Booking>();
    public override void Down() => Db.DropTable<Booking>();
}