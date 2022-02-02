using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Tests;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.PostgreSQL.Tests
{
    public class ModelWithJsonType
    {
        public int Id { get; set; }

        [PgSqlJson]
        public ComplexType ComplexTypeJson { get; set; }

        [PgSqlJsonB]
        public ComplexType ComplexTypeJsonb { get; set; }
    }

    public class ComplexType
    {
        public int Id { get; set; }
        public SubType SubType { get; set; }
    }

    public class SubType
    {
        public string Name { get; set; }
    }

    public class PgsqlData
    {
        [PrimaryKey]
        public Guid Id { get; set; }

        public int[] Ints { get; set; }
        public long[] Longs { get; set; }
        public float[] Floats { get; set; }
        public double[] Doubles { get; set; }
        public decimal[] Decimals { get; set; }
        public string[] Strings { get; set; }
        public DateTime[] DateTimes { get; set; }
        public DateTimeOffset[] DateTimeOffsets { get; set; }
        
        public List<int> ListInts { get; set; }
        public List<long> ListLongs { get; set; }
        public List<float> ListFloats { get; set; }
        public List<double> ListDoubles { get; set; }
        public List<decimal> ListDecimals { get; set; }
        public List<string> ListStrings { get; set; }
        public List<DateTime> ListDateTimes { get; set; }
        public List<DateTimeOffset> ListDateTimeOffsets { get; set; }
    }

    public class PgsqlDataAnnotated
    {
        [PrimaryKey]
        public Guid Id { get; set; }

        [PgSqlIntArray]
        public int[] Ints { get; set; }
        [PgSqlBigIntArray]
        public long[] Longs { get; set; }
        [PgSqlFloatArray]
        public float[] Floats { get; set; }
        [PgSqlDoubleArray]
        public double[] Doubles { get; set; }
        [PgSqlDecimalArray]
        public decimal[] Decimals { get; set; }
        [PgSqlTextArray]
        public string[] Strings { get; set; }

        [PgSqlTimestamp]
        public DateTime[] DateTimes { get; set; }
        
        [PgSqlTimestampTz]
        public DateTimeOffset[] DateTimeOffsets { get; set; }

        [PgSqlIntArray]
        public List<int> ListInts { get; set; }
        [PgSqlBigIntArray]
        public List<long> ListLongs { get; set; }
        [PgSqlFloatArray]
        public List<float> ListFloats { get; set; }
        [PgSqlDoubleArray]
        public List<double> ListDoubles { get; set; }
        [PgSqlDecimalArray]
        public List<decimal> ListDecimals { get; set; }
        [PgSqlTextArray]
        public List<string> ListStrings { get; set; }
        [PgSqlTimestamp]
        public List<DateTime> ListDateTimes { get; set; }
        [PgSqlTimestampTz]
        public List<DateTimeOffset> ListDateTimeOffsets { get; set; }
    }

    [TestFixtureOrmLiteDialects(Dialect.AnyPostgreSql)]
    public class PostgreSqlJsonTests : OrmLiteProvidersTestBase
    {
        public PostgreSqlJsonTests(DialectContext context) : base(context) {}

        [Test]
        public void Can_save_complex_types_as_JSON()
        {
            
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<ModelWithJsonType>();

                db.GetLastSql().Print();

                var row = new ModelWithJsonType
                {
                    Id = 1,
                    ComplexTypeJson = new ComplexType
                    {
                        Id = 2, SubType = new SubType { Name = "SubType2" }
                    },
                    ComplexTypeJsonb = new ComplexType
                    {
                        Id = 3, SubType = new SubType { Name = "SubType3" }
                    },
                };

                db.Insert(row);

                var result = db.Single<ModelWithJsonType>(
                    "complex_type_json->'SubType'->>'Name' = 'SubType2'");

                db.GetLastSql().Print();

                Assert.That(result.Id, Is.EqualTo(1));
                Assert.That(result.ComplexTypeJson.Id, Is.EqualTo(2));
                Assert.That(result.ComplexTypeJson.SubType.Name, Is.EqualTo("SubType2"));

                var results = db.Select<ModelWithJsonType>(
                    "complex_type_jsonb->'SubType'->>'Name' = 'SubType3'");

                Assert.That(results[0].ComplexTypeJsonb.Id, Is.EqualTo(3));
                Assert.That(results[0].ComplexTypeJsonb.SubType.Name, Is.EqualTo("SubType3"));
            }
        }

        [Test]
        public void Does_save_PgSqlData()
        {
            OrmLiteUtils.PrintSql();
            
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<PgsqlData>();
                long UnixEpoch = 621355968000000000L;
                var dateTimes = new DateTime[] {
                    new DateTime(UnixEpoch, DateTimeKind.Utc),
                    new DateTime(2001, 01, 01, 1, 1, 1, 1, DateTimeKind.Utc),
                };
                var dateTimeOffsets = dateTimes.Select(x => new DateTimeOffset(x, TimeSpan.Zero)).ToArray();

                var data = new PgsqlData
                {
                    Id = Guid.NewGuid(),
                    Ints = new[] { 2, 4, 1 },
                    Longs = new long[] { 2, 4, 1 },
                    Floats = new float[] { 2, 4, 1 },
                    Doubles = new double[] { 2, 4, 1 },
                    Strings = new[] { "test string 1", "test string 2" },
                    Decimals = new decimal[] { 2, 4, 1 },
                    DateTimes = dateTimes,
                    DateTimeOffsets = dateTimeOffsets,
                    
                    ListInts = new[] { 2, 4, 1 }.ToList(),
                    ListLongs = new long[] { 2, 4, 1 }.ToList(),
                    ListFloats = new float[] { 2, 4, 1 }.ToList(),
                    ListDoubles = new double[] { 2, 4, 1 }.ToList(),
                    ListStrings = new[] { "test string 1", "test string 2" }.ToList(),
                    ListDecimals = new decimal[] { 2, 4, 1 }.ToList(),
                    ListDateTimes = dateTimes.ToList(),
                    ListDateTimeOffsets = dateTimeOffsets.ToList(),
                };

                db.Save(data);

                var row = db.Select<PgsqlData>()[0];
                Assert.That(row.Ints.EquivalentTo(data.Ints));
                Assert.That(row.Longs.EquivalentTo(data.Longs));
                Assert.That(row.Floats.EquivalentTo(data.Floats));
                Assert.That(row.Doubles.EquivalentTo(data.Doubles));
                Assert.That(row.Decimals.EquivalentTo(data.Decimals));
                Assert.That(row.ListInts.EquivalentTo(data.ListInts));
                Assert.That(row.ListLongs.EquivalentTo(data.ListLongs));
                Assert.That(row.ListFloats.EquivalentTo(data.ListFloats));
                Assert.That(row.ListDoubles.EquivalentTo(data.ListDoubles));
                Assert.That(row.ListDecimals.EquivalentTo(data.ListDecimals));
                Assert.That(row.Strings.EquivalentTo(data.Strings));
                Assert.That(row.ListStrings.EquivalentTo(data.ListStrings));
            }

            OrmLiteUtils.UnPrintSql();
        }
        [Test]
        public void Does_save_PgSqlDataAnnotated()
        {
            OrmLiteUtils.PrintSql();
            
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<PgsqlDataAnnotated>();
                long UnixEpoch = 621355968000000000L;
                var dateTimes = new DateTime[] {
                    new DateTime(UnixEpoch, DateTimeKind.Utc),
                    new DateTime(2001, 01, 01, 1, 1, 1, 1, DateTimeKind.Utc),
                };
                var dateTimeOffsets = dateTimes.Select(x => new DateTimeOffset(x, TimeSpan.Zero)).ToArray();

                var data = new PgsqlDataAnnotated
                {
                    Id = Guid.NewGuid(),
                    Ints = new[] { 2, 4, 1 },
                    Longs = new long[] { 2, 4, 1 },
                    Floats = new float[] { 2, 4, 1 },
                    Doubles = new double[] { 2, 4, 1 },
                    Strings = new[] { "test string 1", "test string 2" },
                    Decimals = new decimal[] { 2, 4, 1 },
                    DateTimes = dateTimes,
                    DateTimeOffsets = dateTimeOffsets,
                    
                    ListInts = new[] { 2, 4, 1 }.ToList(),
                    ListLongs = new long[] { 2, 4, 1 }.ToList(),
                    ListFloats = new float[] { 2, 4, 1 }.ToList(),
                    ListDoubles = new double[] { 2, 4, 1 }.ToList(),
                    ListStrings = new[] { "test string 1", "test string 2" }.ToList(),
                    ListDecimals = new decimal[] { 2, 4, 1 }.ToList(),
                    ListDateTimes = dateTimes.ToList(),
                    ListDateTimeOffsets = dateTimeOffsets.ToList(),
                };

                db.Save(data);

                var row = db.Select<PgsqlDataAnnotated>()[0];
                Assert.That(row.Ints.EquivalentTo(data.Ints));
                Assert.That(row.Longs.EquivalentTo(data.Longs));
                Assert.That(row.Floats.EquivalentTo(data.Floats));
                Assert.That(row.Doubles.EquivalentTo(data.Doubles));
                Assert.That(row.Decimals.EquivalentTo(data.Decimals));
                Assert.That(row.ListInts.EquivalentTo(data.ListInts));
                Assert.That(row.ListLongs.EquivalentTo(data.ListLongs));
                Assert.That(row.ListFloats.EquivalentTo(data.ListFloats));
                Assert.That(row.ListDoubles.EquivalentTo(data.ListDoubles));
                Assert.That(row.ListDecimals.EquivalentTo(data.ListDecimals));
                Assert.That(row.Strings.EquivalentTo(data.Strings));
                Assert.That(row.ListStrings.EquivalentTo(data.ListStrings));
            }

            OrmLiteUtils.UnPrintSql();
        }
    }
}