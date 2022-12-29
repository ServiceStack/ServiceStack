using System;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.Issues
{
    [TestFixtureOrmLite]
    public class CustomTypeTests : OrmLiteProvidersTestBase
    {
        public CustomTypeTests(DialectContext context) : base(context) {}

        public class PocoWithCustomTypes
        {
            [AutoIncrement]
            public int Id { get; set; }

            [Index]
            public Guid Guid { get; set; }
            
            public Uri Uri { get; set; }
        }

        [Test]
        public void Can_select_Guid()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<PocoWithCustomTypes>();

                var dto = new PocoWithCustomTypes
                {
                    Guid = Guid.NewGuid()
                };

                long id = db.Insert(dto, selectIdentity: true);
                var row = db.Single<PocoWithCustomTypes>(r => r.Id == id);

                Assert.That(row.Guid, Is.EqualTo(dto.Guid));
            }
        }

        [Test]
        public void Can_select_Uri()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<PocoWithCustomTypes>();

                var dto = new PocoWithCustomTypes
                {
                    Uri = new Uri("http://a.com")
                };

                long id = db.Insert(dto, selectIdentity: true);
                var row = db.Single<PocoWithCustomTypes>(r => r.Id == id);

                Assert.That(row.Uri.ToString(), Is.EqualTo(dto.Uri.ToString()));
            }
        }
    }
}