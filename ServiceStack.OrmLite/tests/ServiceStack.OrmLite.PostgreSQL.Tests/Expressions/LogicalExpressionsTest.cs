using NUnit.Framework;
using ServiceStack.OrmLite.Tests;

namespace ServiceStack.OrmLite.PostgreSQL.Tests.Expressions
{
    [TestFixtureOrmLiteDialects(Dialect.AnyPostgreSql)]
    public class LogicalExpressionsTest : ExpressionsTestBase
    {
        public LogicalExpressionsTest(DialectContext context) : base(context) {}

        // Unlikely 
        // OpenDbConnection().Select<TestType>(q => q.BoolColumn == (true & false));

        [Test]
        public void Can_select_logical_and_variable_expression()
        {
            // ReSharper disable ConvertToConstant.Local
            var a = true;
            var b = false;
            // ReSharper restore ConvertToConstant.Local

            var expected = new TestType()
            {
                IntColumn = 12,
                BoolColumn = false,
                StringColumn = "test"
            };

            EstablishContext(10, expected);

            using (var con = OpenDbConnection())
            {
                var actual = con.Select<TestType>(q => q.BoolColumn == (a & b));

                Assert.IsNotNull(actual);
                Assert.Greater(actual.Count, 0);
                CollectionAssert.Contains(actual, expected);
            }
        }

        [Test]
        public void Can_select_logical_or_variable_expression()
        {
            // ReSharper disable ConvertToConstant.Local
            var a = true;
            var b = false;
            // ReSharper restore ConvertToConstant.Local

            var expected = new TestType()
            {
                IntColumn = 12,
                BoolColumn = true,
                StringColumn = "test"
            };

            EstablishContext(10, expected);

            using (var con = OpenDbConnection())
            {
                var actual = con.Select<TestType>(q => q.BoolColumn == (a | b));

                Assert.IsNotNull(actual);
                Assert.Greater(actual.Count, 0);
                CollectionAssert.Contains(actual, expected);
            }
        }

        [Test]
        public void Can_select_logical_xor_variable_expression()
        {
            // ReSharper disable ConvertToConstant.Local
            var a = true;
            var b = false;
            // ReSharper restore ConvertToConstant.Local

            var expected = new TestType()
            {
                IntColumn = 12,
                BoolColumn = true,
                StringColumn = "test"
            };

            EstablishContext(10, expected);

            using (var con = OpenDbConnection())
            {
                var actual = con.Select<TestType>(q => q.BoolColumn == (a ^ b));

                Assert.IsNotNull(actual);
                Assert.Greater(actual.Count, 0);
                CollectionAssert.Contains(actual, expected);
            }
        }

        [Test]
        public void Can_select_logical_and_method_expression()
        {
            var expected = new TestType()
            {
                IntColumn = 12,
                BoolColumn = false,
                StringColumn = "test"
            };

            EstablishContext(10, expected);

            using (var con = OpenDbConnection())
            {
                var actual = con.Select<TestType>(q => q.BoolColumn == (GetValue(true) & GetValue(false)));

                Assert.IsNotNull(actual);
                Assert.Greater(actual.Count, 0);
                CollectionAssert.Contains(actual, expected);
            }
        }

        [Test]
        public void Can_select_logical_or_method_expression()
        {
            var expected = new TestType()
            {
                IntColumn = 12,
                BoolColumn = true,
                StringColumn = "test"
            };

            EstablishContext(10, expected);

            using (var con = OpenDbConnection())
            {
                var actual = con.Select<TestType>(q => q.BoolColumn == (GetValue(true) | GetValue(false)));

                Assert.IsNotNull(actual);
                Assert.Greater(actual.Count, 0);
                CollectionAssert.Contains(actual, expected);
            }
        }

        [Test]
        public void Can_select_logical_xor_method_expression()
        {
            var expected = new TestType()
            {
                IntColumn = 12,
                BoolColumn = true,
                StringColumn = "test"
            };

            EstablishContext(10, expected);

            using (var con = OpenDbConnection())
            {
                var actual = con.Select<TestType>(q => q.BoolColumn == (GetValue(true) ^ GetValue(false)));

                Assert.IsNotNull(actual);
                Assert.Greater(actual.Count, 0);
                CollectionAssert.Contains(actual, expected);
            }
        }
    }
}