using System;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Firebird;
using ServiceStack.OrmLite.Tests;

namespace ServiceStack.OrmLite.FirebirdTests;

// Regression tests for the Firebird type converters. Each of these mapped to the wrong Firebird column type or
// bound/round-tripped incorrectly (fails on CreateTable + insert/select against a real DB with the stock client):
//   double   -> FLOAT (32-bit, precision loss)          => DOUBLE PRECISION
//   bool     -> RemoveConverter<bool> / INTEGER (bind)  => BOOLEAN (FB3+)
//   Guid     -> CHAR(16) OCTETS, asymmetric byte order  => round-trip must match
//   DateTime -> LOCALTIME (not a valid column type)     => TIMESTAMP
//   DateOnly -> DATETIME  (not a Firebird type)         => DATE      (net6+)
//   TimeOnly -> BIGINT ticks (not a TIME column)        => TIME      (net6+)
// DateOnly/TimeOnly are net6+, so those parts are guarded (matching the converters).
[TestFixture]
public class FirebirdTypeConverterTests : OrmLiteTestBase
{
    protected override string GetFileConnectionString() => FirebirdDb.V4Connection;
    protected override IOrmLiteDialectProvider GetDialectProvider() => Firebird4OrmLiteDialectProvider.Instance;

    public class TypeConverterModel
    {
        [AutoIncrement]
        public int Id { get; set; }
        public bool BoolValue { get; set; }
        public double DoubleValue { get; set; }
        public Guid GuidValue { get; set; }
        public DateTime DateTimeValue { get; set; }
#if NET6_0_OR_GREATER
        public DateOnly DateOnlyValue { get; set; }
        public TimeOnly TimeOnlyValue { get; set; }
#endif
    }

    [Test]
    public void Can_create_table_and_roundtrip_all_types()
    {
        using var db = new OrmLiteConnectionFactory(ConnectionString, Firebird4Dialect.Provider).OpenDbConnection();
        db.DropAndCreateTable<TypeConverterModel>();

        var row = new TypeConverterModel
        {
            BoolValue = true,
            DoubleValue = 3.14159265358979d,               // needs 64-bit DOUBLE PRECISION
            GuidValue = Guid.NewGuid(),
            DateTimeValue = new DateTime(2026, 7, 3, 21, 25, 44),
#if NET6_0_OR_GREATER
            DateOnlyValue = new DateOnly(2026, 7, 3),
            TimeOnlyValue = new TimeOnly(21, 25, 44),
#endif
        };

        var id = db.Insert(row, selectIdentity: true);
        var loaded = db.SingleById<TypeConverterModel>(id);

        Assert.That(loaded.BoolValue, Is.EqualTo(row.BoolValue));
        Assert.That(loaded.DoubleValue, Is.EqualTo(row.DoubleValue));   // no precision loss
        Assert.That(loaded.GuidValue, Is.EqualTo(row.GuidValue));       // symmetric byte order
        Assert.That(loaded.DateTimeValue, Is.EqualTo(row.DateTimeValue));
#if NET6_0_OR_GREATER
        Assert.That(loaded.DateOnlyValue, Is.EqualTo(row.DateOnlyValue));
        Assert.That(loaded.TimeOnlyValue, Is.EqualTo(row.TimeOnlyValue));
#endif
    }

    [Test]
    public void Roundtrips_false_bool_and_empty_guid()
    {
        using var db = new OrmLiteConnectionFactory(ConnectionString, Firebird4Dialect.Provider).OpenDbConnection();
        db.DropAndCreateTable<TypeConverterModel>();

        var row = new TypeConverterModel
        {
            BoolValue = false,
            DoubleValue = -1.0d / 3.0d,
            GuidValue = Guid.Empty,
            DateTimeValue = new DateTime(2000, 1, 1, 0, 0, 0),
        };
        var id = db.Insert(row, selectIdentity: true);
        var loaded = db.SingleById<TypeConverterModel>(id);

        Assert.That(loaded.BoolValue, Is.False);
        Assert.That(loaded.DoubleValue, Is.EqualTo(row.DoubleValue));
        Assert.That(loaded.GuidValue, Is.EqualTo(Guid.Empty));
    }

    [Test]
    public void Generates_correct_Firebird_column_types()
    {
        var ddl = Firebird4Dialect.Provider.ToCreateTableStatement(typeof(TypeConverterModel));

        // Assert the full "<column> <type> NOT NULL" fragment (not a naked type substring): a column whose NAME
        // contains a type keyword (e.g. "DATETIMEVALUE") would otherwise false-match, and "DATE" is a prefix of
        // "DATETIME" so a DateOnly->DATETIME regression must be ruled out by the trailing " NOT NULL".
        Assert.That(ddl, Does.Contain("BOOLVALUE BOOLEAN NOT NULL"));            // bool     (not INTEGER)
        Assert.That(ddl, Does.Contain("DOUBLEVALUE DOUBLE PRECISION NOT NULL")); // double   (not FLOAT)
        Assert.That(ddl, Does.Contain("DATETIMEVALUE TIMESTAMP NOT NULL"));      // DateTime (not LOCALTIME)
#if NET6_0_OR_GREATER
        Assert.That(ddl, Does.Contain("DATEONLYVALUE DATE NOT NULL"));           // DateOnly (not DATETIME)
        Assert.That(ddl, Does.Contain("TIMEONLYVALUE TIME NOT NULL"));           // TimeOnly (not BIGINT)
#endif
        // the invalid / precision-losing column types the buggy converters emitted must NOT appear:
        Assert.That(ddl, Does.Not.Contain("LOCALTIME"));     // DateTime used LOCALTIME (not a real FB column type)
        Assert.That(ddl, Does.Not.Contain("FLOAT"));         // double used single-precision FLOAT
#if NET6_0_OR_GREATER
        Assert.That(ddl, Does.Not.Contain("BIGINT"));        // TimeOnly used BIGINT ticks
#endif
    }

    // Firebird5Dialect inherits every converter from Firebird4; smoke-test that it resolves + round-trips.
    [Test]
    public void Firebird5Dialect_inherits_type_converters()
    {
        using var db = new OrmLiteConnectionFactory(ConnectionString, Firebird5Dialect.Provider).OpenDbConnection();
        db.DropAndCreateTable<TypeConverterModel>();

        var row = new TypeConverterModel
        {
            BoolValue = true,
            DoubleValue = 2.718281828459045d,
            GuidValue = Guid.NewGuid(),
            DateTimeValue = new DateTime(2026, 7, 3, 8, 15, 30),
#if NET6_0_OR_GREATER
            DateOnlyValue = new DateOnly(2026, 7, 3),
            TimeOnlyValue = new TimeOnly(8, 15, 30),   // FB5 must inherit the TIME converter (the original bug)
#endif
        };
        var id = db.Insert(row, selectIdentity: true);
        var loaded = db.SingleById<TypeConverterModel>(id);

        Assert.That(loaded.BoolValue, Is.True);
        Assert.That(loaded.DoubleValue, Is.EqualTo(row.DoubleValue));
        Assert.That(loaded.GuidValue, Is.EqualTo(row.GuidValue));
        Assert.That(loaded.DateTimeValue, Is.EqualTo(row.DateTimeValue));
#if NET6_0_OR_GREATER
        Assert.That(loaded.DateOnlyValue, Is.EqualTo(row.DateOnlyValue));
        Assert.That(loaded.TimeOnlyValue, Is.EqualTo(row.TimeOnlyValue));
#endif
    }
}
