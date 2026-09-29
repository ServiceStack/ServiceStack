using System;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Firebird;
using ServiceStack.OrmLite.Tests;

namespace ServiceStack.OrmLite.FirebirdTests;

// Regression test for string parameters being silently truncated to 128 characters.
//
// FirebirdStringConverter passes 128 to StringConverter as its StringLength: the default VARCHAR(128) used
// for CREATE TABLE. StringConverter.InitDbParam also uses StringLength as the Size of every string parameter
// that has none, and FirebirdClient truncates a parameter value to its Size without raising an error. So any
// SQL with anonymous parameters, e.g.
//
//     db.ExecuteSql("INSERT INTO T (Json) VALUES (@json)", new { json })
//
// stored only the first 128 characters of json, whatever the column was declared as (VARCHAR(2048), BLOB).
// Typed inserts were unaffected because the field's [StringLength] sizes the parameter.
[TestFixture]
public class FirebirdStringParameterTests : OrmLiteTestBase
{
    protected override string GetFileConnectionString() => FirebirdDb.V4Connection;
    protected override IOrmLiteDialectProvider GetDialectProvider() => Firebird4OrmLiteDialectProvider.Instance;

    public class LongStringModel
    {
        [AutoIncrement]
        public int Id { get; set; }

        [StringLength(1000)]
        public string Text { get; set; }

        [StringLength(StringLengthAttribute.MaxText)]
        public string Memo { get; set; }

        [StringLength(10)]
        public string Short { get; set; }
    }

    private static readonly string Long500 = new('x', 500);

    [Test]
    public void Anonymous_string_parameter_longer_than_128_chars_is_not_truncated()
    {
        using var db = new OrmLiteConnectionFactory(ConnectionString, Firebird4Dialect.Provider).OpenDbConnection();

        var length = db.SqlScalar<int>("SELECT CHAR_LENGTH(CAST(@s AS VARCHAR(1000))) FROM RDB$DATABASE", new { s = Long500 });

        Assert.That(length, Is.EqualTo(500));
    }

    [Test]
    public void Raw_insert_with_anonymous_parameters_stores_the_full_value_in_VARCHAR_and_BLOB()
    {
        using var db = new OrmLiteConnectionFactory(ConnectionString, Firebird4Dialect.Provider).OpenDbConnection();
        db.DropAndCreateTable<LongStringModel>();

        // The shape that lost data in production: raw SQL, anonymous object, long JSON-like payload.
        db.ExecuteSql("INSERT INTO LongStringModel (Text, Memo) VALUES (@text, @memo)".SqlFmt(db.GetDialectProvider()),
            new { text = Long500, memo = Long500 + Long500 });

        var row = db.Single<LongStringModel>(x => x.Id > 0);
        Assert.That(row.Text, Is.EqualTo(Long500));
        Assert.That(row.Memo, Is.EqualTo(Long500 + Long500));
    }

    [Test]
    public void Insert_returning_through_SqlScalar_stores_the_full_value()
    {
        using var db = new OrmLiteConnectionFactory(ConnectionString, Firebird4Dialect.Provider).OpenDbConnection();
        db.DropAndCreateTable<LongStringModel>();

        var id = db.SqlScalar<int>("INSERT INTO LongStringModel (Text, Memo) VALUES (@text, @memo) RETURNING Id".SqlFmt(db.GetDialectProvider()),
            new { text = Long500, memo = Long500 });

        var row = db.SingleById<LongStringModel>(id);
        Assert.That(row.Text, Is.EqualTo(Long500));
        Assert.That(row.Memo, Is.EqualTo(Long500));
    }

    [Test]
    public void SqlList_with_a_long_anonymous_parameter_finds_the_row()
    {
        using var db = new OrmLiteConnectionFactory(ConnectionString, Firebird4Dialect.Provider).OpenDbConnection();
        db.DropAndCreateTable<LongStringModel>();
        db.Insert(new LongStringModel { Text = Long500 });

        var rows = db.SqlList<LongStringModel>("SELECT * FROM LongStringModel WHERE Text = @text".SqlFmt(db.GetDialectProvider()),
            new { text = Long500 });

        Assert.That(rows, Has.Count.EqualTo(1));
    }

    [Test]
    public void Typed_expression_with_a_long_value_finds_the_row()
    {
        using var db = new OrmLiteConnectionFactory(ConnectionString, Firebird4Dialect.Provider).OpenDbConnection();
        db.DropAndCreateTable<LongStringModel>();
        db.Insert(new LongStringModel { Text = Long500 });

        // A truncated parameter compares against the first 128 chars only, so the row is not found.
        Assert.That(db.Select<LongStringModel>(x => x.Text == Long500), Has.Count.EqualTo(1));
    }

    [Test]
    public void Typed_insert_of_long_strings_still_round_trips()
    {
        using var db = new OrmLiteConnectionFactory(ConnectionString, Firebird4Dialect.Provider).OpenDbConnection();
        db.DropAndCreateTable<LongStringModel>();

        db.Insert(new LongStringModel { Text = Long500, Memo = Long500 });

        var row = db.Single<LongStringModel>(x => x.Id > 0);
        Assert.That(row.Text, Is.EqualTo(Long500));
        Assert.That(row.Memo, Is.EqualTo(Long500));
    }

    [Test]
    public void Null_and_short_anonymous_string_parameters_are_unchanged()
    {
        using var db = new OrmLiteConnectionFactory(ConnectionString, Firebird4Dialect.Provider).OpenDbConnection();
        db.DropAndCreateTable<LongStringModel>();

        db.ExecuteSql("INSERT INTO LongStringModel (Text, Short) VALUES (@text, @short)".SqlFmt(db.GetDialectProvider()),
            new { text = (string)null, @short = "abc" });

        var row = db.Single<LongStringModel>(x => x.Id > 0);
        Assert.That(row.Text, Is.Null);
        Assert.That(row.Short, Is.EqualTo("abc"));
    }

    [Test]
    public void Value_longer_than_its_column_fails_instead_of_being_silently_truncated()
    {
        using var db = new OrmLiteConnectionFactory(ConnectionString, Firebird4Dialect.Provider).OpenDbConnection();
        db.DropAndCreateTable<LongStringModel>();

        // Firebird itself rejects a value that does not fit the column ("string right truncation"),
        // which is the same behaviour as a typed insert. Previously the client cut the value first.
        Assert.Throws(Is.InstanceOf<Exception>(), () =>
            db.ExecuteSql("INSERT INTO LongStringModel (Short) VALUES (@short)".SqlFmt(db.GetDialectProvider()),
                new { @short = new string('y', 11) }));
    }

    [Test]
    public void Explicit_parameter_size_is_kept()
    {
        using var db = new OrmLiteConnectionFactory(ConnectionString, Firebird4Dialect.Provider).OpenDbConnection();
        using var cmd = db.CreateCommand();
        var p = cmd.CreateParameter();
        p.Size = 50;

        Firebird4Dialect.Provider.InitDbParam(p, typeof(string));

        Assert.That(p.Size, Is.EqualTo(50));
    }
}
