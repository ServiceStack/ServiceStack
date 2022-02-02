using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Types;
using NUnit.Framework;

namespace ServiceStack.OrmLite.SqlServerTests.Converters
{
    public class InheritanceTest : SqlServer2012ConvertersOrmLiteTestBase
    {
        public class GeoSuper
        {
            public long Id { get; set; }
            public string Other { get; set; }
        }

        public class GeoTest : GeoSuper
        {
            public SqlGeography Location { get; set; }
            public SqlGeography NullLocation { get; set; }
            public SqlGeometry Shape { get; set; }
        }

        [Test]
        public void Can_limit_on_inherited_Type()
        {
            InsertData(100);
            List<GeoTest> data = null;
            using (var db = OpenDbConnection())
            {
                data = db.Select(db.From<GeoTest>().Limit(0, int.MaxValue));
            }

            Assert.IsNotNull(data);
            Assert.IsTrue(data.Count == 100);
        }

        private void InsertData(int count)
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<GeoTest>();

                for (var i = 0; i < count; i++)
                {
                    db.Insert(new GeoTest { Id = i, Location = RandomPosition() });
                }
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