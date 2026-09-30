using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests.Support;

/// <summary>
/// Readers are read with a single GetValues() call per row, when a provider's GetValues() throws OrmLite falls back
/// to reading each field, which should only be detected once per reader instead of throwing and logging on every row
/// </summary>
[TestFixtureOrmLite]
public class ReaderGetValuesFallbackTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    public class Item
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    /// <summary>
    /// A reader over a DataTable whose GetValues() throws, like some ADO.NET providers do for certain column types
    /// </summary>
    class ThrowingGetValuesReader(DataTable table) : DbDataReader
    {
        readonly DataTableReader reader = table.CreateDataReader();
        public int GetValuesCalls { get; private set; }

        public override int GetValues(object[] values)
        {
            GetValuesCalls++;
            throw new NotSupportedException("GetValues() isn't supported");
        }

        public override int Depth => reader.Depth;
        public override int FieldCount => reader.FieldCount;
        public override bool HasRows => reader.HasRows;
        public override bool IsClosed => reader.IsClosed;
        public override int RecordsAffected => reader.RecordsAffected;
        public override object this[int ordinal] => reader[ordinal];
        public override object this[string name] => reader[name];
        public override bool GetBoolean(int ordinal) => reader.GetBoolean(ordinal);
        public override byte GetByte(int ordinal) => reader.GetByte(ordinal);
        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length) =>
            reader.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);
        public override char GetChar(int ordinal) => reader.GetChar(ordinal);
        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length) =>
            reader.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);
        public override string GetDataTypeName(int ordinal) => reader.GetDataTypeName(ordinal);
        public override DateTime GetDateTime(int ordinal) => reader.GetDateTime(ordinal);
        public override decimal GetDecimal(int ordinal) => reader.GetDecimal(ordinal);
        public override double GetDouble(int ordinal) => reader.GetDouble(ordinal);
        public override IEnumerator GetEnumerator() => reader.GetEnumerator();
        public override Type GetFieldType(int ordinal) => reader.GetFieldType(ordinal);
        public override float GetFloat(int ordinal) => reader.GetFloat(ordinal);
        public override Guid GetGuid(int ordinal) => reader.GetGuid(ordinal);
        public override short GetInt16(int ordinal) => reader.GetInt16(ordinal);
        public override int GetInt32(int ordinal) => reader.GetInt32(ordinal);
        public override long GetInt64(int ordinal) => reader.GetInt64(ordinal);
        public override string GetName(int ordinal) => reader.GetName(ordinal);
        public override int GetOrdinal(string name) => reader.GetOrdinal(name);
        public override string GetString(int ordinal) => reader.GetString(ordinal);
        public override object GetValue(int ordinal) => reader.GetValue(ordinal);
        public override bool IsDBNull(int ordinal) => reader.IsDBNull(ordinal);
        public override bool NextResult() => reader.NextResult();
        public override bool Read() => reader.Read();
    }

    // SQLite's provider disables GetValues() for every provider in the process, enable it for these tests
    bool hold;
    [SetUp] public void SetUp() => (hold, OrmLiteConfig.DeoptimizeReader) = (OrmLiteConfig.DeoptimizeReader, false);
    [TearDown] public void TearDown() => OrmLiteConfig.DeoptimizeReader = hold;

    static DataTable CreateItems(int count)
    {
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Name", typeof(string));
        for (var i = 1; i <= count; i++)
            table.Rows.Add(i, "Item " + i);
        return table;
    }

    [Test]
    public void Falls_back_to_reading_each_field_once_per_reader()
    {
        using var reader = new ThrowingGetValuesReader(CreateItems(100));

        var items = reader.ConvertToList<Item>(DialectProvider);

        Assert.That(items.Count, Is.EqualTo(100));
        Assert.That(items[0].Id, Is.EqualTo(1));
        Assert.That(items[99].Name, Is.EqualTo("Item 100"));
        Assert.That(reader.GetValuesCalls, Is.EqualTo(1), "GetValues() should only be tried once per reader");
    }

    [Test]
    public void Falls_back_for_value_tuples()
    {
        using var reader = new ThrowingGetValuesReader(CreateItems(10));

        var items = reader.ConvertToList<(int Id, string Name)>(DialectProvider);

        Assert.That(items.Count, Is.EqualTo(10));
        Assert.That(items[9], Is.EqualTo((10, "Item 10")));
        Assert.That(reader.GetValuesCalls, Is.EqualTo(1));
    }
}
