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

    public static List<Player> Players
    {
        get
        {
            var by = "player@email.com";
            var ret = new List<Player>
            {
                new Player
                {
                    Id = 1,
                    FirstName = "North",
                    LastName = "West",
                    Email = "north@west.com",
                    PhoneNumbers = new List<Phone>
                    {
                        new() { Kind = PhoneKind.Mobile, Number = "123-555-5555" },
                        new() { Kind = PhoneKind.Home,   Number = "555-555-5555", Ext = "123" },
                    },
                    Profile = new Profile
                    {
                        Username = "north",
                        Role = PlayerRole.Leader,
                        Region = PlayerRegion.Australasia,
                        HighScore = 100,
                        GamesPlayed = 10,
                        CoverUrl = "files/cover.docx",
                    }.WithAudit(by),
                },
                new Player
                {
                    Id = 2,
                    FirstName = "South",
                    LastName = "East",
                    Email = "south@east.com",
                    PhoneNumbers = new List<Phone>
                    {
                        new() { Kind = PhoneKind.Mobile, Number = "456-666-6666" },
                        new() { Kind = PhoneKind.Work,   Number = "666-666-6666", Ext = "456" },
                    },
                    Profile = new Profile
                    {
                        Username = "south",
                        Role = PlayerRole.Player,
                        Region = PlayerRegion.Americas,
                        HighScore = 50,
                        GamesPlayed = 20,
                        CoverUrl = "files/profile.jpg",
                    }.WithAudit(by),
                }
            };

            return ret;
        }
    }

    public static Track Track(string name, string artist, string album, int year) =>
        new Track
        {
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
        new Booking
        {
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
