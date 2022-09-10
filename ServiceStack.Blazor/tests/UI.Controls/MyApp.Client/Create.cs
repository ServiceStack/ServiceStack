using MyApp.ServiceModel;
using ServiceStack;
using System.Xml.Linq;

namespace MyApp.Client;

public static class Create
{
    static int bookingId = 0;
    public static Booking Booking(string name, RoomType type, int roomNo, decimal cost, string by) =>
        new Booking {
            Id = ++bookingId,
            Name = name,
            RoomType = type,
            RoomNumber = roomNo,
            Cost = cost,
            BookingStartDate = DateTime.UtcNow.AddDays(bookingId),
            BookingEndDate = DateTime.UtcNow.AddDays(bookingId + 7),
        }.WithAudit(by);
    
    public static T WithAudit<T>(this T row, string by, DateTime? date = null) where T : AuditBase
    {
        var useDate = date ?? DateTime.Now;
        row.CreatedBy = by;
        row.CreatedDate = useDate;
        row.ModifiedBy = by;
        row.ModifiedDate = useDate;
        return row;
    }
}
