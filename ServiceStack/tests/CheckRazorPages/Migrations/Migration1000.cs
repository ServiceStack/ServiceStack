using System.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;

namespace MyApp.Migrations;

public class Migration1000 : MigrationBase
{
    public class Booking : AuditBase
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public RoomType RoomType { get; set; }
        public int RoomNumber { get; set; }
        public DateTime BookingStartDate { get; set; }
        public DateTime? BookingEndDate { get; set; }
        public decimal Cost { get; set; }
        public string? Notes { get; set; }
        public bool? Cancelled { get; set; }

        [References(typeof(Coupon))]
        public string? CouponId { get; set; }
    }

    public class Coupon
    {
        public string Id { get; set; } = default!;
        public string Description { get; set; } = default!;
        public int Discount { get; set; }
        public DateTime ExpiryDate { get; set; }
    }

    public enum RoomType
    {
        Queen,
        Double,
        Suite,
    }

    public override void Up()
    {
        Db.CreateTable<Coupon>();
        Db.CreateTable<Booking>();

        new[] { 5, 10, 15, 20, 25, 30, 40, 50, 60, 70, }.Each(percent => {
            Db.Insert(new Coupon
            {
                Id = $"BOOK{percent}",
                Description = $"{percent}% off",
                Discount = percent,
                ExpiryDate = DateTime.UtcNow.AddDays(30)
            });
        });

        CreateBooking(Db, "First Booking!", RoomType.Queen, 10, 100, "BOOK10", "employee@email.com");
        CreateBooking(Db, "Booking 2", RoomType.Double, 12, 120, "BOOK25", "manager@email.com");
        CreateBooking(Db, "Booking the 3rd", RoomType.Suite, 13, 130, null, "employee@email.com");
    }

    public void CreateBooking(IDbConnection? db,
        string name, RoomType type, int roomNo, decimal cost, string? couponId, string by) =>
        db.Insert(new Booking
        {
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
    }
}