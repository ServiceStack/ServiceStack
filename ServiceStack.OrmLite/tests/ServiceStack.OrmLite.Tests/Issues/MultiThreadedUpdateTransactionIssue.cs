using System.Threading;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.Issues
{
    [TestFixtureOrmLite, Explicit]
    public class MultiThreadedUpdateTransactionIssue : OrmLiteProvidersTestBase
    {
        public MultiThreadedUpdateTransactionIssue(DialectContext context) : base(context) {}

        public class ModelWithIdAndName
        {
            [AutoIncrement]
            public int Id { get; set; }
            public string Name { get; set; }
        }

        [Test]
        [NUnit.Framework.Ignore("Needs review - MONOREPO")]
        public void Can_Insert_Update_record_across_multiple_threads()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<ModelWithIdAndName>();
            }

            int count = 0;

            20.Times(i =>
            {
                ThreadPool.QueueUserWorkItem(state =>
                {
                    40.Times(_ =>
                    {
                        using (var db = OpenDbConnection())
                        {
                            var objA = new ModelWithIdAndName { Name = "A" };
                            var objB = new ModelWithIdAndName { Name = "B" };

                            objA.Id = (int)db.Insert(objA, selectIdentity: true);

                            objB.Id = (int)db.Insert(objB, selectIdentity: true);
                            objB.Name = objA.Name;

                            db.Update(objB);
                            Interlocked.Increment(ref count);
                        }
                    });
                });
            });

            Thread.Sleep(5000);
            Assert.That(count, Is.EqualTo(20 * 40));
        }
    }
}