using System;
using Microsoft.SqlServer.Types;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.SqlServerTests.Converters;

namespace ServiceStack.OrmLite.SqlServerTests.Issues
{
    public class ModelWithGeo
    {
        [AutoIncrement]
        public long Id { get; set; }
        public string Name { get; set; }
        public SqlGeography Location { get; set; }
    }

    [TestFixture]
    public class DeleteWithGeoTypesIssue : SqlServer2012ConvertersOrmLiteTestBase
    {
        [Test]
        public void Can_delete_entity_with_Geo_Type()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<ModelWithGeo>();

                db.Insert(new ModelWithGeo { Name = "Foo", Location = RandomPosition() });
                db.Insert(new ModelWithGeo { Name = "Bar", Location = RandomPosition() });
                db.Insert(new ModelWithGeo { Name = "Qux", Location = RandomPosition() });

                var rows = db.Select<ModelWithGeo>();
                Assert.That(rows.Count, Is.EqualTo(3));

                var bar = db.Single<ModelWithGeo>(x => x.Name == "Bar");
                db.SqlScalar<int>(
                    "DELETE FROM ModelWithGeo WHERE Id=@Id AND Location.STEquals(@Location) = 1", 
                    new { Id = bar.Id, Location = bar.Location });

                Assert.That(db.Select<ModelWithGeo>().Count, Is.EqualTo(2));

                var qux = db.Single<ModelWithGeo>(x => x.Name == "Qux");
                db.Delete(qux);

                Assert.That(db.Select<ModelWithGeo>().Count, Is.EqualTo(1));

                var foo = db.Single<ModelWithGeo>(x => x.Name == "Foo");
                db.DeleteById<ModelWithGeo>(foo.GetId());

                Assert.That(db.Select<ModelWithGeo>().Count, Is.EqualTo(0));
            }
        }

        private SqlGeography RandomPosition()
        {
            var rand = new Random(DateTime.Now.Millisecond * (int)DateTime.Now.Ticks);
            double lat = Math.Round(rand.NextDouble() * 160 - 80, 6);
            double lon = Math.Round(rand.NextDouble() * 360 - 180, 6);
            var result = SqlGeography.Point(lat, lon, 4326).MakeValid();
            return result;
        }
    }
}