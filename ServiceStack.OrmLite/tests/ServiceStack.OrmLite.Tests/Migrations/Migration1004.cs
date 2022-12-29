using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.Migrations;

public class Migration1004 : MigrationBase
{
    class Booking
    {
        public bool IsLocked { get; set; }
    }
    
    public override void Up()
    {
        Db.AddColumn<Booking>(booking => booking.IsLocked);
    }
}