using System.Data;
using MyApp.ServiceModel;
using ServiceStack.OrmLite;

namespace MyApp;

public static class ConfigureDbBookings
{
    public static void SeedBookings(this IDbConnection db)
    {
        if (db.CreateTableIfNotExists<Booking>())
        {
            db.CreateBooking("First Booking!", RoomType.Queen, 10, 100, "employee@email.com");
            db.CreateBooking("Booking 2", RoomType.Double, 12, 120, "manager@email.com");
            db.CreateBooking("Booking the 3rd", RoomType.Suite, 13, 130, "employee@email.com");
        }
    }
        
    static int bookingId = 0;
    public static void CreateBooking(this IDbConnection db, string name, RoomType type, int roomNo, decimal cost, string by) =>
        db.Insert(new Booking
        {
            Id = ++bookingId,
            Name = name,
            RoomType = type,
            RoomNumber = roomNo,
            Cost = cost,
            BookingStartDate = DateTime.UtcNow.AddDays(bookingId),
            BookingEndDate = DateTime.UtcNow.AddDays(bookingId + 7),
        }.WithAudit(by));
}