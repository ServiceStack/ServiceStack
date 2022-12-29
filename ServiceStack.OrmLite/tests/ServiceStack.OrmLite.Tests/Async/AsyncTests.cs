using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

using ServiceStack.OrmLite.Tests.Shared;

namespace ServiceStack.OrmLite.Tests.Async
{
    [TestFixtureOrmLite]
    public class AsyncTests : OrmLiteProvidersTestBase
    {
        public AsyncTests(DialectContext context) : base(context) {}

        [Test]
        public async Task Can_Insert_and_SelectAsync()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Poco>();

                for (var i = 0; i < 3; i++)
                {
                    await db.InsertAsync(new Poco { Id = i + 1, Name = ((char)('A' + i)).ToString() });
                }

                var results = (await db.SelectAsync<Poco>()).Map(x => x.Name);
                Assert.That(results, Is.EqualTo(new[] { "A", "B", "C" }));

                results = (await db.SelectAsync<Poco>(x => x.Name == "A")).Map(x => x.Name);
                Assert.That(results, Is.EqualTo(new[] { "A" }));

                results = (await db.SelectAsync(db.From<Poco>().Where(x => x.Name == "A"))).Map(x => x.Name);
                Assert.That(results, Is.EqualTo(new[] { "A" }));
            }
        }

        [Test]
        public async Task Does_throw_async_errors()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Poco>();

                try
                {
                    var results = await db.SelectAsync(db.From<Poco>().Where("NotExists = 1"));
                    Assert.Fail("Should throw");
                }
                catch (Exception ex)
                {
                    Assert.That(ex.Message.ToLower(), Does.Contain("id")
                                                       .Or.Contain("notexists")
                                                       .Or.Contain("not_exists"));
                }

                try
                {
                    await db.InsertAsync(new DifferentPoco { NotExists = "Foo" });
                    Assert.Fail("Should throw");
                }
                catch (Exception ex)
                {
                    Assert.That(ex.Message.ToLower(), Does.Contain("id")
                                                       .Or.Contain("notexists")
                                                       .Or.Contain("not_exists"));
                }

                try
                {
                    await db.InsertAllAsync(new[] {
                        new DifferentPoco { NotExists = "Foo" },
                        new DifferentPoco { NotExists = "Bar" }
                    });
                    Assert.Fail("Should throw");
                }
                catch (Exception ex)
                {
                    var innerEx = ex.UnwrapIfSingleException();
                    Assert.That(innerEx.Message.ToLower(), Does.Contain("id")
                                                        .Or.Contain("notexists")
                                                        .Or.Contain("not_exists"));
                }

                try
                {
                    await db.UpdateAllAsync(new[] {
                        new DifferentPoco { NotExists = "Foo" },
                        new DifferentPoco { NotExists = "Bar" }
                    });
                    Assert.Fail("Should throw");
                }
                catch (Exception ex)
                {
                    var innerEx = ex.UnwrapIfSingleException();
                    Assert.That(innerEx.Message.ToLower(), Does.Contain("id")
                                                        .Or.Contain("notexists")
                                                        .Or.Contain("not_exists"));
                }
            }
        }

        [Alias("Poco")]
        public class DifferentPoco
        {
            public int Id { get; set; }
            public string NotExists { get; set; }
        }

        [Test]
        public async Task Test_Thread_Affinity()
        {
            var delayMs = 100;
            var db = OpenDbConnection();

            "Root: {0}".Print(Thread.CurrentThread.ManagedThreadId);
            var task = Task.Factory.StartNew(() =>
            {
                "Before Delay: {0}".Print(Thread.CurrentThread.ManagedThreadId);
                return Task.Delay(delayMs);
            })
            .Then(async t =>
            {
                "After Delay: {0}".Print(Thread.CurrentThread.ManagedThreadId);
                await Task.Delay(delayMs);
            })
            .Then(async t =>
            {
                "Before SQL: {0}".Print(Thread.CurrentThread.ManagedThreadId);
                await db.ExistsAsync<Person>(x => x.Age < 50)
                    .Then(t1 =>
                    {
                        "After SQL: {0}".Print(Thread.CurrentThread.ManagedThreadId);
                        return Task.Delay(delayMs);
                    });
            })
            .Then(async inner =>
            {
                "Before Inner: {0}".Print(Thread.CurrentThread.ManagedThreadId);
                await Task.Delay(delayMs);
                "After Inner: {0}".Print(Thread.CurrentThread.ManagedThreadId);
            });

            await task;
            "Await t: {0}".Print(Thread.CurrentThread.ManagedThreadId);
        }
    }
}
