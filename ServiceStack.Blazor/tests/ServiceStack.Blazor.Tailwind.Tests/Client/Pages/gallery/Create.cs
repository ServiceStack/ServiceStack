using MyApp.ServiceModel;
using ServiceStack;
using System;
using System.Collections.Generic;

namespace MyApp.Client;

public class Track
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Artist { get; set; }
    public string Album { get; set; }
    public int Year { get; set; }

    public static List<Track> Results { get; set; } = new() {
        Create.Track("Everythings Ruined", "Faith No More", "Angel Dust", 1992),
        Create.Track("Lightning Crashes", "Live", "Throwing Copper", 1994),
        Create.Track("Heart-Shaped Box", "Nirvana", "In Utero", 1993),
        Create.Track("Alive", "Pearl Jam", "Ten", 1991),
    };
}

public static class Create
{
    static int trackId = 0;

    public static Track Track(string name, string artist, string album, int year) =>
        new Track {
            Id = ++trackId,
            Name = name,
            Artist = artist,
            Album = album,
            Year = year,
        };

    static Dictionary<string, Coupon> Coupons = new()
    {
        ["BOOK10"] = new Coupon { Id = "BOOK10", Discount = 10, Description = "10% Discount", ExpiryDate = DateTime.UtcNow.AddDays(30) },
        ["BOOK25"] = new Coupon { Id = "BOOK25", Discount = 25, Description = "25% Discount", ExpiryDate = DateTime.UtcNow.AddDays(30) },
        ["BOOK50"] = new Coupon { Id = "BOOK50", Discount = 50, Description = "50% Discount", ExpiryDate = DateTime.UtcNow.AddDays(30) },
    };


    static int bookingId = 0;
    public static Booking Booking(string name, RoomType type, int roomNo, decimal cost, string by, string? couponId = null) =>
        new Booking {
            Id = ++bookingId,
            Name = name,
            RoomType = type,
            RoomNumber = roomNo,
            Cost = cost,
            BookingStartDate = DateTime.UtcNow.AddDays(bookingId),
            BookingEndDate = DateTime.UtcNow.AddDays(bookingId + 7),
            CouponId = couponId,
            Discount = couponId != null ? Coupons[couponId] : null,
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
