#nullable enable
using System;
using System.Data;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.Migrations;

public class Booking
{
    [AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; }
    public RoomType RoomType { get; set; }
    public int RoomNumber { get; set; }
    public DateTime BookingStartDate { get; set; }
    public DateTime? BookingEndDate { get; set; }
    public decimal Cost { get; set; }
    public string? Notes { get; set; }
    public bool? Cancelled { get; set; }
}

public enum RoomType
{
    Single,
    Double,
    Queen,
    Twin,
    Suite,
}

[Notes("Create initial Database")]
public class Migration1000 : MigrationBase
{
    public class Booking
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string? OldName { get; set; }
        public double ToDelete { get; set; }
    }

    public override void Up()
    {
        Db.CreateTable<Booking>();
    }
}

[Notes("Update Bookings Columns")]
public class Migration1001 : MigrationBase
{
    public class Booking
    {
        [RenameColumn(nameof(Migration1000.Booking.OldName))]
        public string? Name { get; set; }
        
        public RoomType RoomType { get; set; }
        
        [RemoveColumn]
        public double ToDelete { get; set; }
    }

    public override void Up()
    {
        Db.Migrate<Booking>();
    }
}


public class MigrateBookings : OrmLiteTestBase
{
    // public MigrateBookings() => OrmLiteUtils.PrintSql();

    IDbConnection Create()
    {
        var db = DbFactory.Open();
        Migrator.Clean(db);
        db.DropAndCreateTable<Migration1000.Booking>();
        return db;
    }

    [Test]
    public void Runs_all_migrations()
    {
        using var db = Create();

        var migrator = new Migrator(DbFactory, typeof(Migration1000).Assembly);
        Assert.That(migrator.Run(), Is.EquivalentTo(new[]{ typeof(Migration1000), typeof(Migration1001) }));
    }

    [Test]
    public void Runs_only_remaining_migrations()
    {
        using var db = Create();

        db.Insert(new Migration {
            Name = nameof(Migration1000), 
            CreatedDate = DateTime.UtcNow, 
            CompletedDate = DateTime.UtcNow
        });

        var migrator = new Migrator(DbFactory, typeof(Migration1000).Assembly);
        Assert.That(migrator.Run(), Is.EquivalentTo(new[]{ typeof(Migration1001) }));
    }

    [Test]
    public void Runs_no_migrations_if_last_migration_has_been_run()
    {
        using var db = Create();

        db.Insert(new Migration {
            Name = nameof(Migration1001), 
            CreatedDate = DateTime.UtcNow, 
            CompletedDate = DateTime.UtcNow
        });

        var migrator = new Migrator(DbFactory, typeof(Migration1000).Assembly);
        Assert.That(migrator.Run(), Is.Empty);
    }

    [Test]
    public void Runs_no_migrations_if_last_migration_has_not_completed_within_timeout()
    {
        using var db = Create();

        db.Insert(new Migration {
            Name = nameof(Migration1001), 
            CreatedDate = DateTime.UtcNow, 
        });

        var migrator = new Migrator(DbFactory, typeof(Migration1000).Assembly);
        Assert.That(migrator.Run(), Is.Empty);
    }

    [Test]
    public void Runs_migration_if_has_not_completed_after_timeout()
    {
        using var db = Create();

        db.Insert(new Migration {
            Name = nameof(Migration1001), 
            CreatedDate = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(11)),
        });

        var migrator = new Migrator(DbFactory, typeof(Migration1000).Assembly) {
            Timeout = TimeSpan.FromMinutes(10)
        };
        Assert.That(migrator.Run(), Is.EquivalentTo(new[]{ typeof(Migration1001) }));
    }
}