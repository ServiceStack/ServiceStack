#nullable enable
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.Migrations;

[Notes("Update Bookings Columns")]
public class Migration1001 : MigrationBase
{
    public class Booking
    {
        [RenameColumn(nameof(Migration1000.Booking.OldName))]
        public string? Name { get; set; }
        
        public RoomType RoomType { get; set; }
        
        [RemoveColumn]
        public double ToDelete { get; set; }
    }
    public enum RoomType
    {
        Single,
        Double,
        Queen,
        Twin,
        Suite,
    }

    public override void Up()
    {
        Db.Migrate<Booking>();
    }
}