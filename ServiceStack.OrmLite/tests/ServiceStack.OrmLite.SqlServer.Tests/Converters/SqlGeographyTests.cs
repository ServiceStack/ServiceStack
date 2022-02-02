using System;
using System.Linq;
using Microsoft.SqlServer.Types;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.SqlServerTests.Converters
{
    [TestFixture]
    public class SqlGeographyTests : SqlServer2012ConvertersOrmLiteTestBase
    {
        public string ColumnDefinition { get; set; }

        [OneTimeSetUp]
        public new void TestFixtureSetUp()
        {
            base.TestFixtureSetUp();

            var converter = new SqlServer.Converters.SqlServerGeographyTypeConverter();
            ColumnDefinition = converter.ColumnDefinition;
        }

        [Test]
        public void Can_insert_and_retrieve_SqlGeography()
        {
            Db.DropAndCreateTable<GeographyTable>();

            // Statue of Liberty
            var geo = SqlGeography.Point(40.6898329, -74.0452177, 4326);

            Db.Insert(new GeographyTable {Id = 1, Location = geo, NullLocation = SqlGeography.Null});

            var result = Db.SingleById<GeographyTable>(1);

            Assert.IsTrue(geo.STEquals(result.Location).Value);

            // Converter always resolves to null even when Null property inserted into database
            Assert.AreEqual(null, result.NullLocation);

            result.PrintDump();
        }

        [Test]
        public void Can_convert_to_and_from_SqlGeography_and_string_with_anon_typed_insert()
        {
            Db.DropAndCreateTable<GeographyTable>();

            var dialect = Db.GetDialectProvider();
            var tableName = dialect.GetQuotedTableName(ModelDefinition<GeographyTable>.Definition);

            var stringValue = "POINT(2 6)";
            var location = SqlGeography.Parse(stringValue);
            stringValue = location.ToString(); // to fix any whitespace issues

            var sql = $"INSERT {tableName} (Location, StringLocation) VALUES (@Location, @StringLocation);";
            Db.ExecuteSql(sql, new { Location = location, StringLocation = stringValue });

            var result = Db.Select<FlippedGeographyTable>().FirstOrDefault();
            Assert.AreEqual(stringValue, result.StringLocation);
            Assert.IsTrue(location.STEquals(result.Location).Value);
        }

        [Test]
        public void Can_convert_to_and_from_SqlGeography_and_string_with_strong_typed_insert()
        {
            Db.DropAndCreateTable<GeographyTable>();

            var stringValue = "POINT(38.028495788574205 55.895460650576936)";
            var location = SqlGeography.STGeomFromText(new System.Data.SqlTypes.SqlChars(stringValue), 4326);
            stringValue = location.ToString(); // to fix any whitespace issues

            Db.Insert(new GeographyTable() { Location = location, StringLocation = stringValue, NullLocation = SqlGeography.Null });

            var result = Db.Select<FlippedGeographyTable>().FirstOrDefault();
            Assert.AreEqual(stringValue, result.StringLocation);
            Assert.IsTrue(location.STEquals(result.Location).Value);
        }

        [Test]
        public void Can_convert_SqlGeography_to_quoted_string()
        {
            var converter = new SqlServer.Converters.SqlServerGeographyTypeConverter();

            string stringValue = null;
            var location = SqlGeography.Parse(stringValue); // NULL
            var str = converter.ToQuotedString(typeof(SqlGeography), location);

            Assert.AreEqual($"CAST(null AS {ColumnDefinition})", str);

            stringValue = "POINT(0 0)";
            location = SqlGeography.Parse(stringValue);
            stringValue = location.ToString(); // to fix any whitespace issues

            str = converter.ToQuotedString(typeof(SqlGeography), location);

            Assert.AreEqual($"CAST('{stringValue}' AS {ColumnDefinition})", str);
        }



        [Test]
        public void Can_insert_and_update_SqlGeography()
        {
            Db.DropAndCreateTable<ModelWithSqlGeography>();

            var wkt = "POINT(38.028495788574205 55.895460650576936)";
            var geo = SqlGeography.STGeomFromText(new System.Data.SqlTypes.SqlChars(wkt), 4326);

            var obj = new ModelWithSqlGeography { Name = "Test", Created = DateTime.UtcNow, Geo = geo };

            var id = (int)Db.Insert(obj, selectIdentity: true);
            obj.ID = id;

            try
            {
                // Update of POCO with SqlGeography proprety should work
                obj.Name = "Test - modified";
                obj.Edited = DateTime.UtcNow;
                Db.Update(obj);                    
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
            finally
            {
                // GetLastSql shouldn't return null after exception
                var lastSql = Db.GetLastSql();
                Assert.IsNotNull(lastSql);
            }                
        }
    }

    public class GeographyTable
    {
        [AutoIncrement]
        public long Id { get; set; }

        public SqlGeography Location { get; set; }

        public string StringLocation { get; set; }

        public SqlGeography NullLocation { get; set; }
    }

    [Alias("GeographyTable")]
    public class FlippedGeographyTable
    {
        [AutoIncrement]
        public long Id { get; set; }

        [Alias("StringLocation")]
        public SqlGeography Location { get; set; }

        [Alias("Location")]
        public string StringLocation { get; set; }

        public SqlGeography NullLocation { get; set; }
    }

    public class ModelWithSqlGeography
    {
        [AutoIncrement]
        public int ID { get; set; }
        [StringLength(255)]
        public string Name { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Edited { get; set; }
        public SqlGeography Geo { get; set; }
    }
}
