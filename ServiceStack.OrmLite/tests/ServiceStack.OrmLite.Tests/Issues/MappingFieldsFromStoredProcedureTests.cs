using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Issues;

[TestFixtureOrmLite]
public class MappingFieldsFromStoredProcedureTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    public class ProUser
    {
        public string Id { get; set; }
        public string Password { get; set; }
        public int UserType { get; set; }
    }

    public class Order
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string ProUserId { get; set; }
        public string Details { get; set; }
        public int ItemId { get; set; }
    }

    public class Address
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string ProUserId { get; set; }
        public string StreetName { get; set; }
        public int? ZipCode { get; set; }
    }

    public class DomainUserDto
    {
        public string Id { get; set; }
        public string StreetName { get; set; }
        public string ZipCode { get; set; }
        public string Details { get; set; }
    }

    [NUnit.Framework.Ignore("Requires existing tables / SP at https://gist.github.com/mythz/6f336094f8dd07c22d91"), Test]
    public void Can_select_from_Custom_stored_procedure()
    {
        using var db = OpenDbConnection();
        db.DeleteAll<Address>();
        db.DeleteAll<Order>();
        db.DeleteAll<ProUser>();

        var user = new ProUser
        {
            Id = "A",
            Password = "p@55w03d",
            UserType = 1,
        };

        db.Insert(user);

        var order = new Order
        {
            ProUserId = user.Id,
            Details = "Details",
            ItemId = 1,
        };

        db.Insert(order);

        var address = new Address
        {
            ProUserId = user.Id,
            StreetName = "1 Street",
            ZipCode = 90210
        };

        db.Insert(address);

        var rows = db.SqlList<DomainUserDto>(
            "EXEC TestJoinSupportForORMLite @Id", new { user.Id });

        rows.PrintDump();
    } 
}