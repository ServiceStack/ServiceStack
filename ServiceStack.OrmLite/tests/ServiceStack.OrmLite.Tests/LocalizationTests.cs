using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests;

[TestFixtureOrmLite, SetUICulture("vi-VN"), SetCulture("vi-VN")]
public class LocalizationTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public void Can_query_using_float_in_alternate_culture()
    {
        using (var db = OpenDbConnection())
        {
            db.CreateTable<Point>(true);

            db.Insert(new Point { Width = 4, Height = 1.123f, Top = 3.456d, Left = 2.345m });
            db.GetLastSql().Print();

            var sql = "Height = @height".PreNormalizeSql(db);
            var points = db.Select<Point>(sql, new { height = 1.123 });
            db.GetLastSql().Print();

            Assert.That(points[0].Width, Is.EqualTo(4));
            Assert.That(points[0].Height, Is.EqualTo(1.123f));
            Assert.That(points[0].Top, Is.EqualTo(3.456d).Within(1d));
            Assert.That(points[0].Left, Is.EqualTo(2.345m).Within(1m));
        }
    }

    public class Point
    {
        [AutoIncrement]
        public int Id { get; set; }
        public short Width { get; set; }
        public float Height { get; set; }
        public double Top { get; set; }
        public decimal Left { get; set; }
    }
}