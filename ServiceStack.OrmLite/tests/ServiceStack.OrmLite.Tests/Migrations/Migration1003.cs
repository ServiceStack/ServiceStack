namespace ServiceStack.OrmLite.Tests.Migrations;

[Notes("Update Bookings Columns")]
public class Migration1003 : MigrationBase
{
    class Booking
    {
        public bool IsRepeatCustomer { get; set; }
    }

    public override void Up() => Db.Migrate<Booking>();

    public override void Down() => Db.Revert<Booking>();
}