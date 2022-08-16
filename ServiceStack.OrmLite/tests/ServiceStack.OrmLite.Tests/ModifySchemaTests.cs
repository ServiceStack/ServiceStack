#nullable enable
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests;

public class ModifySchemaTests : OrmLiteTestBase
{
    [Alias("Booking")]
    public class BookingV1
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? ToRemove { get; set; }
    }

    [Alias("Booking")]
    public class BookingV2
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? ToCreate { get; set; }
    }

    public ModifySchemaTests() => OrmLiteUtils.PrintSql();
    private static HashSet<string> GetTableColumnNames(IDbConnection db) => db.GetTableColumns<BookingV1>().Select(x => x.ColumnName).ToSet();

    [Test]
    public void Can_create_column()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<BookingV1>();
        var toCreateName = db.GetNamingStrategy().GetColumnName(nameof(BookingV2.ToCreate));
        Assert.That(GetTableColumnNames(db), Does.Not.Contain(toCreateName));
        db.AddColumn<BookingV2>(x => x.ToCreate);
        Assert.That(GetTableColumnNames(db), Does.Contain(toCreateName));
    }

    [Test]
    public void Can_drop_column()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<BookingV1>();
        var toRemoveName = db.GetNamingStrategy().GetColumnName(nameof(BookingV1.ToRemove));
        Assert.That(GetTableColumnNames(db), Does.Contain(toRemoveName));
        db.DropColumn<BookingV1>(nameof(BookingV1.ToRemove));
        Assert.That(GetTableColumnNames(db), Does.Not.Contain(toRemoveName));
    }

    [Test]
    public void Can_rename_column()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<BookingV1>();
        var toRenameName = db.GetNamingStrategy().GetColumnName(nameof(BookingV1.ToRemove));
        Assert.That(GetTableColumnNames(db), Does.Contain(toRenameName));
        db.RenameColumn<BookingV2>(nameof(BookingV1.ToRemove), nameof(BookingV2.ToCreate));
        Assert.That(GetTableColumnNames(db), Does.Not.Contain(toRenameName));
        Assert.That(GetTableColumnNames(db), Does.Contain(db.GetNamingStrategy().GetColumnName(nameof(BookingV2.ToCreate))));
    }
}