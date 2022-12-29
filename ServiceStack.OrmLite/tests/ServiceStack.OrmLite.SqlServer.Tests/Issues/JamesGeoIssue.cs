using Microsoft.SqlServer.Types;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.SqlServerTests.Converters;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.SqlServerTests.Issues
{
    public class James
    {
        [AutoIncrement]
        public int id { get; set; }

        public SqlGeography loc { get; set; }
    }

    [TestFixture]
    public class JamesGeoIssue : SqlServer2012ConvertersOrmLiteTestBase
    {
        [Test]
        public void Can_insert_and_select_GeoPoint()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<James>();

                var james = new James
                {
                    loc = SqlGeography.Point(40.6898329, -74.0452177, 4326)
                };
                db.Insert(james);


                var result = db.SingleById<James>(1);

                result.PrintDump();

                Assert.That(result.id, Is.EqualTo(1));
                Assert.That(result.loc.Lat, Is.EqualTo(james.loc.Lat));
                Assert.That(result.loc.Long, Is.EqualTo(james.loc.Long));
                Assert.That(result.loc.STSrid, Is.EqualTo(james.loc.STSrid));
            }
        }         
    }
}