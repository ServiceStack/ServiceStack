using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;

namespace MyApp.Migrations;

public class Migration1000 : MigrationBase
{
    public class Booking : AuditBase
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public RoomType RoomType { get; set; }
        public int RoomNumber { get; set; }
        [IntlDateTime(DateStyle.Long)]
        public DateTime BookingStartDate { get; set; }
        [IntlRelativeTime]
        public DateTime? BookingEndDate { get; set; }
        //[IntlNumber(Currency = NumberCurrency.USD)]
        [Format(FormatMethods.Currency, Options = "{ currency:modelValue.notes||'GBP' }")]
        public decimal Cost { get; set; }

        [Ref(Model = nameof(Coupon), RefId = nameof(Coupon.Id), RefLabel = nameof(Coupon.Description))]
        [References(typeof(Coupon))]
        public string? CouponId { get; set; }

        [Reference]
        public Coupon Discount { get; set; }
        [Format(FormatMethods.Hidden)]
        public string? Notes { get; set; }
        public bool? Cancelled { get; set; }
    
        [References(typeof(Address))]
        public long? PermanentAddressId { get; set; }

        [Reference]
        public Address? PermanentAddress { get; set; }

        [References(typeof(Address))]
        public long? PostalAddressId { get; set; }

        [Reference]
        public Address? PostalAddress { get; set; }    
    }
    
    public enum RoomType
    {
        Queen,
        Double,
        Suite,
    }
    
    public class Coupon
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public int Discount { get; set; }
        public DateTime ExpiryDate { get; set; }
    }

    public class Address
    {
        [AutoIncrement]
        [PrimaryKey]
        public long Id { get; set; }
        public string? AddressText { get; set; }
    }
    

    public override void Up()
    {
        Db.CreateTable<Address>();
        Db.CreateTable<Coupon>();
        Db.CreateTable<Booking>();

        new[] { 5, 10, 15, 20, 25, 30, 40, 50, 60, 70,  }.Each(percent => {
            Db.Insert(new Coupon { Id = $"BOOK{percent}", Description = $"{percent}% off", Discount = percent, ExpiryDate = DateTime.UtcNow.AddDays(30) });
        });

        CreateBooking("First Booking!",  RoomType.Queen,  10, 100, "BOOK10", "employee@email.com");
        CreateBooking("Booking 2",       RoomType.Double, 12, 120, "BOOK25", "manager@email.com");
        CreateBooking("Booking the 3rd", RoomType.Suite,  13, 130, null,     "employee@email.com");
    }
    
    public void CreateBooking(string name, RoomType type, int roomNo, decimal cost, string? couponId, string by) =>
        Db.Insert(new Booking {
            Name = name,
            RoomType = type,
            RoomNumber = roomNo,
            Cost = cost,
            BookingStartDate = DateTime.UtcNow.AddDays(roomNo),
            BookingEndDate = DateTime.UtcNow.AddDays(roomNo + 7),
            CouponId = couponId,
            CreatedBy = by,
            CreatedDate = DateTime.UtcNow,
            ModifiedBy = by,
            ModifiedDate = DateTime.UtcNow,
        });

    public override void Down()
    {
        Db.DropTable<Booking>();
        Db.DropTable<Coupon>();
        Db.DropTable<Address>();
    }
}
