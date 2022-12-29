using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Expression
{
    [TestFixtureOrmLite]
    public class ConditionalExpressionTest : ExpressionsTestBase
    {
        public ConditionalExpressionTest(DialectContext context) : base(context) {}

        [Test]
        public void Can_select_conditional_and_expression()
        {
            var expected = new TestType
            {
                IntColumn = 3,
                BoolColumn = true,
                StringColumn = "4"
            };

            Init(10, expected);

            using (var db = OpenDbConnection())
            {
                var actual = db.Select<TestType>(q => q.IntColumn > 2 && q.IntColumn < 4);

                Assert.IsNotNull(actual);
                Assert.AreEqual(1, actual.Count);
                CollectionAssert.Contains(actual, expected);
            }
        }

        [Test]
        public void Can_select_conditional_or_expression()
        {
            var expected = new TestType
            {
                IntColumn = 3,
                BoolColumn = true,
                StringColumn = "4"
            };

            Init(10, expected);

            using (var db = OpenDbConnection())
            {
                var actual = db.Select<TestType>(q => q.IntColumn == 3 || q.IntColumn < 0);

                Assert.IsNotNull(actual);
                Assert.AreEqual(1, actual.Count);
                CollectionAssert.Contains(actual, expected);
            }
        }

        [Test]
        public void Can_select_evaluated_conditional_and_expression()
        {
            // ReSharper disable ConvertToConstant.Local
            var a = 10;
            var b = 5;
            // ReSharper restore ConvertToConstant.Local

            var expected = new TestType
            {
                IntColumn = 3,
                BoolColumn = true,
                StringColumn = "4"
            };

            Init(10, expected);

            using (var db = OpenDbConnection())
            {
                var actual = db.Select<TestType>(q => q.BoolColumn == (a >= b && a > 0));

                Assert.IsNotNull(actual);
                Assert.Greater(actual.Count, 0);
                CollectionAssert.Contains(actual, expected);
            }
        }

        [Test]
        public void Can_select_evaluated_conditional_or_expression()
        {
            // ReSharper disable ConvertToConstant.Local
            var a = 10;
            var b = 5;
            // ReSharper restore ConvertToConstant.Local

            var expected = new TestType
            {
                IntColumn = 3,
                BoolColumn = true,
                StringColumn = "4"
            };

            Init(10, expected);

            using (var db = OpenDbConnection())
            {
                var actual = db.Select<TestType>(q => q.IntColumn == 3 || a > b);

                Assert.IsNotNull(actual);
                Assert.AreEqual(11, actual.Count);
                CollectionAssert.Contains(actual, expected);
            }
        }

        [Test]
        public void Can_select_evaluated_invalid_conditional_or_valid_expression()
        {
            // ReSharper disable ConvertToConstant.Local
            var a = true;
            // ReSharper restore ConvertToConstant.Local

            var expected = new TestType
            {
                IntColumn = 3,
                BoolColumn = true,
                StringColumn = "4"
            };

            Init(10, expected);

            using (var db = OpenDbConnection())
            {
                var actual = db.Select<TestType>(q => !q.BoolColumn || a);

                Assert.IsNotNull(actual);
                Assert.Greater(actual.Count, 0);
                CollectionAssert.Contains(actual, expected);
            }
        }

        [Test]
        public void Can_select_evaluated_conditional_and_valid_expression()
        {
            var model = new
            {
                StringValue = "4"
            };

            var expected = new TestType
            {
                IntColumn = 3,
                BoolColumn = true,
                StringColumn = "4"
            };

            Init(10, expected);

            using (var db = OpenDbConnection())
            {
                var actual = db.Select<TestType>(q => q.BoolColumn && q.StringColumn == model.StringValue);

                Assert.IsNotNull(actual);
                Assert.Greater(actual.Count, 0);
                CollectionAssert.Contains(actual, expected);
            }
        }

        [Test]
        public void Can_use_bitwise_operations_in_typed_query()
        {
            OrmLiteConfig.BeforeExecFilter = dbCmd => dbCmd.GetDebugString().Print();
            
            Init(5);

            List<TestType> results;
                
            using (var db = OpenDbConnection())
            {
                results = db.Select<TestType>(x => (x.Id | 2) == 3);
                Assert.That(results.Map(x => x.Id), Is.EquivalentTo(new[]{ 1, 3 }));
                
                results = db.Select<TestType>(x => (x.Id & 2) == 2);
                Assert.That(results.Map(x => x.Id), Is.EquivalentTo(new[]{ 2, 3 }));

                if (!Dialect.AnySqlServer.HasFlag(Dialect))
                {
                    results = db.Select<TestType>(x => (x.Id << 1) == 4);
                    Assert.That(results.Map(x => x.Id), Is.EquivalentTo(new[]{ 2 }));
                
                    results = db.Select<TestType>(x => (x.Id >> 1) == 1);
                    Assert.That(results.Map(x => x.Id), Is.EquivalentTo(new[]{ 2, 3 }));
                }
                
                if ((Dialect.AnySqlServer | Dialect.AnyMySql).HasFlag(Dialect))
                {
                    results = db.Select<TestType>(x => (x.Id ^ 2) == 3);
                    Assert.That(results.Map(x => x.Id), Is.EquivalentTo(new[]{ 1 }));
                }
            }
        }
    }
}