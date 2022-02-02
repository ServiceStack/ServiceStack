using NUnit.Framework;

namespace ServiceStack.OrmLite.FirebirdTests.Expressions
{
    public class RelationalExpressionsTest : ExpressionsTestBase
    {
        [Test]
        public void Can_select_greater_than_expression()
        {
            var expected = new TestType()
            {
                IntColumn = 1,
                BoolColumn = true,
                StringColumn = "test"
            };

            EstablishContext(10, expected);

            using (var con = OpenDbConnection())
            {
                var actual = con.Select<TestType>(q => q.IntColumn > 1);

                Assert.IsNotNull(actual);
                Assert.AreEqual(10, actual.Count);
                CollectionAssert.DoesNotContain(actual, expected);
            }
        }

        [Test]
        public void Can_select_greater_or_equal_than_expression()
        {
            var expected = new TestType()
            {
                IntColumn = 1,
                BoolColumn = true,
                StringColumn = "test"
            };

            EstablishContext(10, expected);

            using (var con = OpenDbConnection())
            {
                var actual = con.Select<TestType>(q => q.IntColumn >= 1);

                Assert.IsNotNull(actual);
                Assert.AreEqual(11, actual.Count);
                CollectionAssert.Contains(actual, expected);
            }
        }

        [Test]
        public void Can_select_smaller_than_expression()
        {
            var expected = new TestType()
            {
                IntColumn = 1,
                BoolColumn = true,
                StringColumn = "test"
            };

            EstablishContext(10, expected);

            using (var con = OpenDbConnection())
            {
                var actual = con.Select<TestType>(q => q.IntColumn < 1);

                Assert.IsNotNull(actual);
                Assert.AreEqual(0, actual.Count);
            }
        }

        [Test]
        public void Can_select_smaller_or_equal_than_expression()
        {
            var expected = new TestType()
            {
                IntColumn = 1,
                BoolColumn = true,
                StringColumn = "test"
            };

            EstablishContext(10, expected);

            using (var con = OpenDbConnection())
            {
                var actual = con.Select<TestType>(q => q.IntColumn <= 1);

                Assert.IsNotNull(actual);
                Assert.AreEqual(1, actual.Count);
                CollectionAssert.Contains(actual, expected);
            }
        }
    }
}