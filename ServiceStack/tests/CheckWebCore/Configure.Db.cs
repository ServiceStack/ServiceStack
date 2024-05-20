using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using System.Data;

//[assembly: HostingStartup(typeof(MyApp.ConfigureDb))]

namespace MyApp
{
    public class ConfigureDb : IConfigureAppHost, IConfigureServices // : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder) => builder
            .ConfigureServices(Configure)
            .ConfigureAppHost(Configure);

        public void Configure(IServiceCollection services)
        {
            var dbFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);
            // dbFactory.RegisterConnection("pgsql", new OrmLiteConnectionFactory(
            //     Environment.GetEnvironmentVariable("PGSQL_CONNECTION"),
            //     PostgreSqlDialect.Provider));
            services.AddSingleton<IDbConnectionFactory>(dbFactory);
        }

        public void Configure(IAppHost appHost)
        {
            // Create non-existing Table and add Seed Data Example
            using var db = appHost.Resolve<IDbConnectionFactory>().Open();
            if (db.CreateTableIfNotExists<Coupon>())
            {
                new[] { 5, 10, 15, 20, 25, 30, 40, 50, 60, 70,  }.Each(percent => {
                    db.Insert(new Coupon { Id = $"BOOK{percent}", Description = $"{percent}% off", Discount = 10, ExpiryDate = DateTime.UtcNow.AddDays(30) });
                });
            }
            if (db.CreateTableIfNotExists<Booking>())
            {
                db.CreateBooking("First Booking!",  RoomType.Queen,  10, 100, "BOOK10", "employee@email.com");
                db.CreateBooking("Booking 2",       RoomType.Double, 12, 120, "BOOK25", "manager@email.com");
                db.CreateBooking("Booking the 3rd", RoomType.Suite,  13, 130, null,     "employee@email.com");
            }
            
            // using var dbPgsql = appHost.Resolve<IDbConnectionFactory>().Open("pgsql");
            // if (dbPgsql.CreateTableIfNotExists<Table1>())
            // {
            //     dbPgsql.Insert(new Table1 { Name = "Table1" });
            // }
            // if (dbPgsql.CreateTableIfNotExists<Table2>())
            // {
            //     dbPgsql.Insert(new Table2 { Name = "Table2" });
            // }
        }
    }

    public static class ConfigureDbUtils
    {
        static int bookingId = 0;

        public static void CreateBooking(this IDbConnection Db, string name, RoomType type, int roomNo, decimal cost, string? couponId, string by) =>
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
    }
}