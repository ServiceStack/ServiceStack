using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.Issues
{
    [TestFixtureOrmLite]
    public class SqlExpressionIssues : OrmLiteProvidersTestBase
    {
        public SqlExpressionIssues(DialectContext context) : base(context) {}

        public class MetadataEntity
        {
            [AutoIncrement]
            public int Id { get; set; }
            public int ObjectTypeCode { get; set; }
            public string LogicalName { get; set; }
        }

        [Test]
        public void Can_Equals_method_and_operator_with_Scalar()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<MetadataEntity>();

                db.Insert(new MetadataEntity {ObjectTypeCode = 1, LogicalName = "inno_subject"});
                
                Assert.That(db.Scalar<MetadataEntity, int>(e => e.ObjectTypeCode, e => e.LogicalName == "inno_subject"), Is.EqualTo(1));
                Assert.That(db.Scalar<MetadataEntity, int>(e => e.ObjectTypeCode, e => e.LogicalName.Equals("inno_subject")), Is.EqualTo(1));
            }
        }
    }
}