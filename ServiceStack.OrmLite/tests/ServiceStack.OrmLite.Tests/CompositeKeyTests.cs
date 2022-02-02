using System;
using System.Data;
using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests
{
    [TestFixtureOrmLite]
    public class CompositeKeyTests : OrmLiteProvidersTestBase
    {
        public CompositeKeyTests(DialectContext context) : base(context) {}
        
        const long SubId1Value = 1;
        const long SubId2Value = 1;

        [Test]
        public void Can_select_single_from_empty_composite_key_table()
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<CompositeKey>();

            var result = db.Single<CompositeKey>(ck => ck.SubId1 == SubId1Value && ck.SubId2 == SubId2Value);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void Can_select_single_from_composite_key_table_with_one_matching_row()
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<CompositeKey>();
            InsertData(db, 1);

            var result = db.Single<CompositeKey>(ck => ck.SubId1 == SubId1Value && ck.SubId2 == SubId2Value);
            Assert.That(result.SubId1, Is.EqualTo(SubId1Value));
            Assert.That(result.SubId2, Is.EqualTo(SubId2Value));
        }

        private void InsertData(IDbConnection db, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var data = new CompositeKey {SubId1 = SubId1Value, SubId2 = SubId2Value, Data = Guid.NewGuid().ToString()};
                db.Insert(data);
            }
        }

        [Test]
        public void Can_select_single_from_composite_key_table_with_several_matching_rows()
        {
            using var db = OpenDbConnection();
            db.DropAndCreateTable<CompositeKey>();
            InsertData(db, 4);

            var result = db.Single<CompositeKey>(ck => ck.SubId1 == SubId1Value && ck.SubId2 == SubId2Value);
            Assert.That(result.SubId1, Is.EqualTo(SubId1Value));
            Assert.That(result.SubId2, Is.EqualTo(SubId2Value));
        }

        public class CompositeKey
        {
            [DataAnnotations.Ignore]
            public string Id { get { return SubId1 + "/" + SubId2; } }
            public long SubId1 { get; set; }
            public long SubId2 { get; set; }
            public string Data { get; set; }
        }
    }
}
