using System;
using System.Linq;
using Microsoft.SqlServer.Types;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.SqlServerTests.Converters
{
    [TestFixture]
    public class SqlGeometryTests : SqlServer2012ConvertersOrmLiteTestBase
    {
        public string ColumnDefinition { get; set; }

        [OneTimeSetUp]
        public new void TestFixtureSetUp()
        {
            base.TestFixtureSetUp();

            var converter = new SqlServer.Converters.SqlServerGeometryTypeConverter();
            ColumnDefinition = converter.ColumnDefinition;
        }

        [Test]
        public void Can_insert_and_retrieve_SqlGeometry()
        {
            Db.DropAndCreateTable<GeometryTable>();

            // A simple line from (0,0) to (4,4)  Length = SQRT(2 * 4^2)
            var wkt = new System.Data.SqlTypes.SqlChars("LINESTRING(0 0, 4 4)".ToCharArray());
            var shape = SqlGeometry.STLineFromText(wkt, 0);

            Db.Insert(new GeometryTable { Id = 1, Shape = shape});

            var result = Db.SingleById<GeometryTable>(1).Shape;

            var lengths = Db.Column<double>("SELECT Shape.STLength() AS Length FROM GeometryTable");

            Assert.AreEqual((double) result.STLength(), lengths.First());

            Assert.AreEqual(shape.STStartPoint().STX, result.STStartPoint().STX);
            Assert.AreEqual(shape.STStartPoint().STY, result.STStartPoint().STY);

            Assert.AreEqual(shape.STEndPoint().STX, result.STEndPoint().STX);
            Assert.AreEqual(shape.STEndPoint().STY, result.STEndPoint().STY);

            Assert.AreEqual(2, (int) result.STNumPoints());

            result.PrintDump();
        }

        [Test]
        public void Can_convert_to_and_from_SqlGeometry_and_string_with_anon_typed_insert()
        {
            Db.DropAndCreateTable<GeometryTable>();

            var dialect = Db.GetDialectProvider();
            var tableName = dialect.GetQuotedTableName(ModelDefinition<GeometryTable>.Definition);

            var stringValue = "POINT(2 6)";
            var shape = SqlGeometry.Parse(stringValue);
            stringValue = shape.ToString(); // to fix any whitespace issues

            var sql = $"INSERT {tableName} (Shape, StringShape) VALUES (@Shape, @StringShape);";
            Db.ExecuteSql(sql, new { Shape = shape, StringShape = stringValue });

            var result = Db.Select<FlippedGeometryTable>().FirstOrDefault();
            Assert.AreEqual(stringValue, result.StringShape);
            Assert.IsTrue(shape.STEquals(result.Shape).Value);
        }

        [Test]
        public void Can_convert_to_and_from_SqlGeometry_and_string_with_strong_typed_insert()
        {
            Db.DropAndCreateTable<GeometryTable>();

            var stringValue = "LINESTRING(0 0, 4 4)";
            var shape = SqlGeometry.Parse(stringValue);
            stringValue = shape.ToString(); // to fix any whitespace issues

            Db.Insert(new GeometryTable() { Shape = shape, StringShape = stringValue });

            var result = Db.Select<FlippedGeometryTable>().FirstOrDefault();
            Assert.AreEqual(stringValue, result.StringShape);
            Assert.IsTrue(shape.STEquals(result.Shape).Value);
        }

        [Test]
        public void Can_convert_SqlGeometry_to_quoted_string()
        {
            var converter = new SqlServer.Converters.SqlServerGeometryTypeConverter();

            string stringValue = null;
            var shape = SqlGeometry.Parse(stringValue); // NULL
            var str = converter.ToQuotedString(typeof(SqlGeometry), shape);

            Assert.AreEqual($"CAST(null AS {ColumnDefinition})", str);

            stringValue = "LINESTRING(0 0, 4 4)";
            shape = SqlGeometry.Parse(stringValue);
            stringValue = shape.ToString(); // to fix any whitespace issues

            str = converter.ToQuotedString(typeof(SqlGeometry), shape);

            Assert.AreEqual($"CAST('{stringValue}' AS {ColumnDefinition})", str);
        }
    }

    public class GeometryTable
    {
        [AutoIncrement]
        public long Id { get; set; }

        public SqlGeometry Shape { get; set; }

        public string StringShape { get; set; }
    }

    [Alias("GeometryTable")]
    public class FlippedGeometryTable
    {
        [AutoIncrement]
        public long Id { get; set; }

        [Alias("StringShape")]
        public SqlGeometry Shape { get; set; }

        [Alias("Shape")]
        public string StringShape { get; set; }
    }
}
