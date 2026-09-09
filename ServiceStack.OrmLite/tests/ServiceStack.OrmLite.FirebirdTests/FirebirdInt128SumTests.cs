using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Firebird;
using ServiceStack.OrmLite.Tests;

namespace ServiceStack.OrmLite.FirebirdTests;

// Regression test for reading an aggregate off Firebird 4+.
//
// Firebird 4 introduced INT128 and widens SUM() of a BIGINT to it. The stock FirebirdClient correctly
// surfaces INT128 as System.Numerics.BigInteger, but BigInteger does not implement IConvertible, so
// ConvertNumber's Convert.ToInt64(value) threw
//
//     InvalidCastException: Unable to cast object of type 'System.Numerics.BigInteger'
//                           to type 'System.IConvertible'
//
// rather than converting it.
//
// WHICH EXPRESSIONS WIDEN, measured on Firebird 5.0.4 - the distinction matters, because a test built on
// the wrong one passes with or without the fix:
//     SUM(SMALLINT)              -> Int64        (unaffected)
//     SUM(INTEGER)               -> Int64        (unaffected)
//     SUM(BIGINT)                -> BigInteger   <-- reproduces
//     SUM(OCTET_LENGTH(blob))    -> BigInteger   <-- reproduces (OCTET_LENGTH of a BLOB is BIGINT)
//     SUM(NUMERIC(18,0))         -> Decimal      (unaffected)
//     COUNT(*)                   -> Int64        (unaffected)
//
// That last row is what made this easy to miss in the wild: counting worked, summing a BIGINT did not, and
// only at runtime in whichever query happened to hit it first.
[TestFixture]
public class FirebirdInt128SumTests : OrmLiteTestBase
{
    protected override string GetFileConnectionString() => FirebirdDb.V4Connection;
    protected override IOrmLiteDialectProvider GetDialectProvider() => Firebird4OrmLiteDialectProvider.Instance;

    public class Int128SumModel
    {
        [AutoIncrement]
        public int Id { get; set; }

        // BIGINT, so Firebird 4+ widens SUM() over it to INT128. An int column would NOT reproduce this.
        public long LongValue { get; set; }
    }

    [Test]
    public void Can_read_SUM_of_BIGINT_column_as_long()
    {
        using var db = new OrmLiteConnectionFactory(ConnectionString, Firebird4Dialect.Provider).OpenDbConnection();
        db.DropAndCreateTable<Int128SumModel>();

        db.Insert(new Int128SumModel { LongValue = 10 });
        db.Insert(new Int128SumModel { LongValue = 20 });
        db.Insert(new Int128SumModel { LongValue = 12 });

        // The call that threw. No CAST in the SQL on purpose: casting to BIGINT server-side is the
        // workaround this fix exists to make unnecessary.
        var total = db.Scalar<long>("SELECT SUM(LongValue) FROM Int128SumModel".SqlFmt(db.GetDialectProvider()));

        Assert.That(total, Is.EqualTo(42L));
    }

    [Test]
    public void Can_read_SUM_of_BIGINT_column_as_other_numeric_types()
    {
        using var db = new OrmLiteConnectionFactory(ConnectionString, Firebird4Dialect.Provider).OpenDbConnection();
        db.DropAndCreateTable<Int128SumModel>();

        db.Insert(new Int128SumModel { LongValue = 7 });
        db.Insert(new Int128SumModel { LongValue = 35 });

        var sql = "SELECT SUM(LongValue) FROM Int128SumModel".SqlFmt(db.GetDialectProvider());

        // Every target a caller may ask for goes through the same conversion, so each must handle INT128.
        Assert.That(db.Scalar<int>(sql), Is.EqualTo(42));
        Assert.That(db.Scalar<long>(sql), Is.EqualTo(42L));
        Assert.That(db.Scalar<decimal>(sql), Is.EqualTo(42m));
        Assert.That(db.Scalar<double>(sql), Is.EqualTo(42d));
    }

    [Test]
    public void SUM_over_no_rows_reads_as_default()
    {
        using var db = new OrmLiteConnectionFactory(ConnectionString, Firebird4Dialect.Provider).OpenDbConnection();
        db.DropAndCreateTable<Int128SumModel>();

        // SUM() over no rows is NULL, not zero - it must not be mistaken for a conversion failure.
        var total = db.Scalar<long>("SELECT SUM(LongValue) FROM Int128SumModel".SqlFmt(db.GetDialectProvider()));

        Assert.That(total, Is.EqualTo(0L));
    }

    // The shape that actually failed in production: summing OCTET_LENGTH over a BLOB column. OCTET_LENGTH
    // returns BIGINT, so SUM() of it widens to INT128 even though nothing in the schema is declared INT128.
    public class Int128BlobModel
    {
        [AutoIncrement]
        public int Id { get; set; }
        public byte[] Data { get; set; }
    }

    [Test]
    public void Can_read_SUM_of_OCTET_LENGTH_over_a_BLOB_as_long()
    {
        using var db = new OrmLiteConnectionFactory(ConnectionString, Firebird4Dialect.Provider).OpenDbConnection();
        db.DropAndCreateTable<Int128BlobModel>();

        db.Insert(new Int128BlobModel { Data = new byte[100] });
        db.Insert(new Int128BlobModel { Data = new byte[150] });

        var total = db.Scalar<long>(
            "SELECT SUM(OCTET_LENGTH(Data)) FROM Int128BlobModel".SqlFmt(db.GetDialectProvider()));

        Assert.That(total, Is.EqualTo(250L));
    }
}
