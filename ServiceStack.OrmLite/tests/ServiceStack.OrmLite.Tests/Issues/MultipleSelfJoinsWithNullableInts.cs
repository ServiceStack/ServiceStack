using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Issues
{
    public class ParentSelfRef
    {
        [AutoIncrement]
        public int Id { get; set; }

        [References(typeof(ChildSelfRef))]
        public int? Child1Id { get; set; }

        [Reference]
        public ChildSelfRef Child1 { get; set; }

        [References(typeof(ChildSelfRef))]
        public int? Child2Id { get; set; }

        [Reference]
        public ChildSelfRef Child2 { get; set; }
        
        public ulong RowVersion { get; set; }
    }

    public class ChildSelfRef
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
    }

    [TestFixtureOrmLite]
    public class MultipleSelfJoinsWithNullableInts : OrmLiteProvidersTestBase
    {
        public MultipleSelfJoinsWithNullableInts(DialectContext context) : base(context) {}

        [Test]
        public void Does_support_multiple_self_joins_with_nullable_ints()
        {
            using (var db = OpenDbConnection())
            {
                db.DropTable<ParentSelfRef>();
                db.DropTable<ChildSelfRef>();

                db.CreateTable<ChildSelfRef>();
                db.CreateTable<ParentSelfRef>();

                var row = new ParentSelfRef
                {
                    Child1 = new ChildSelfRef
                    {
                        Name = "Child 1"
                    },
                    Child2 = new ChildSelfRef
                    {
                        Name = "Child 2"
                    },
                };

                db.Save(row, references: true);

                row.PrintDump();

                Assert.That(row.Id, Is.EqualTo(1));
                Assert.That(row.Child1Id, Is.EqualTo(1));
                Assert.That(row.Child1.Id, Is.EqualTo(1));
                Assert.That(row.Child1.Name, Is.EqualTo("Child 1"));
                Assert.That(row.Child2Id, Is.EqualTo(2));
                Assert.That(row.Child2.Id, Is.EqualTo(2));
                Assert.That(row.Child2.Name, Is.EqualTo("Child 2"));

                row = db.LoadSingleById<ParentSelfRef>(row.Id);

                Assert.That(row.Id, Is.EqualTo(1));
                Assert.That(row.Child1Id, Is.EqualTo(1));
                Assert.That(row.Child1.Id, Is.EqualTo(1));
                Assert.That(row.Child1.Name, Is.EqualTo("Child 1"));
                Assert.That(row.Child2Id, Is.EqualTo(2));
                Assert.That(row.Child2.Id, Is.EqualTo(2));
                Assert.That(row.Child2.Name, Is.EqualTo("Child 2"));
            }
        } 
    }
}