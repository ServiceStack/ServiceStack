using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests.Expression
{
    [TestFixtureOrmLite]
    public class StringFunctionTests : ExpressionsTestBase
    {
        public StringFunctionTests(DialectContext context) : base(context) {}

        [Test]
        public void Can_select_using_contains()
        {
            var stringVal = "stringValue";

            var expected = new TestType()
            {
                IntColumn = 7,
                BoolColumn = true,
                StringColumn = stringVal
            };

            Init(10, expected);

            using (var db = OpenDbConnection())
            {
                var actual = db.Select<TestType>(q => q.StringColumn.Contains(stringVal));

                Assert.IsNotNull(actual);
                Assert.AreEqual(1, actual.Count);
                CollectionAssert.Contains(actual, expected);
            }
        }

        [Test]
        public void Can_select_using_contains_with_quote_in_string()
        {
            var stringVal = "string'ContainingAQuote";

            var expected = new TestType()
            {
                IntColumn = 7,
                BoolColumn = true,
                StringColumn = stringVal
            };

            Init(10, expected);

            using (var db = OpenDbConnection())
            {
                var actual = db.Select<TestType>(q => q.StringColumn.Contains(stringVal));

                Assert.IsNotNull(actual);
                Assert.AreEqual(1, actual.Count);
                CollectionAssert.Contains(actual, expected);
            }
        }

        [Test]
        public void Can_select_using_contains_with_double_quote_in_string()
        {
            var stringVal = "string\"ContainingAQuote";

            var expected = new TestType()
            {
                IntColumn = 7,
                BoolColumn = true,
                StringColumn = stringVal
            };

            Init(10, expected);

            using (var db = OpenDbConnection())
            {
                var actual = db.Select<TestType>(q => q.StringColumn.Contains(stringVal));

                Assert.IsNotNull(actual);
                Assert.AreEqual(1, actual.Count);
                CollectionAssert.Contains(actual, expected);
            }
        }

        [Test]
        public void Can_select_using_contains_with_backtick_in_string()
        {
            var stringVal = "string`ContainingAQuote";

            var expected = new TestType()
            {
                IntColumn = 7,
                BoolColumn = true,
                StringColumn = stringVal
            };

            Init(10, expected);

            using (var db = OpenDbConnection())
            {
                var actual = db.Select<TestType>(q => q.StringColumn.Contains(stringVal));

                Assert.IsNotNull(actual);
                Assert.AreEqual(1, actual.Count);
                CollectionAssert.Contains(actual, expected);
            }
        }

        [Test]
        public void Can_select_using_startsWith()
        {
            var prefix = "prefix";

            var expected = new TestType()
            {
                IntColumn = 7,
                BoolColumn = true,
                StringColumn = prefix + "asdfasdfasdf"
            };

            Init(10, expected);

            using (var db = OpenDbConnection())
            {
                var actual = db.Select<TestType>(q => q.StringColumn.StartsWith(prefix));

                Assert.IsNotNull(actual);
                Assert.AreEqual(1, actual.Count);
                CollectionAssert.Contains(actual, expected);
            }
        }

        [Test]
        public void Can_select_using_startsWith_with_quote_in_string()
        {
            var prefix = "prefix";

            var expected = new TestType()
            {
                IntColumn = 7,
                BoolColumn = true,
                StringColumn = prefix + "'asdfasdfasdf"
            };

            Init(10, expected);

            using (var db = OpenDbConnection())
            {
                var actual = db.Select<TestType>(q => q.StringColumn.StartsWith(prefix));

                Assert.IsNotNull(actual);
                Assert.AreEqual(1, actual.Count);
                CollectionAssert.Contains(actual, expected);
            }
        }

        [Test]
        public void Can_select_using_startsWith_with_double_quote_in_string()
        {
            var prefix = "prefix";

            var expected = new TestType()
            {
                IntColumn = 7,
                BoolColumn = true,
                StringColumn = prefix + "\"asdfasdfasdf"
            };

            Init(10, expected);

            using (var db = OpenDbConnection())
            {
                var actual = db.Select<TestType>(q => q.StringColumn.StartsWith(prefix));

                Assert.IsNotNull(actual);
                Assert.AreEqual(1, actual.Count);
                CollectionAssert.Contains(actual, expected);
            }
        }

        [Test]
        public void Can_select_using_startsWith_with_backtick_in_string()
        {
            var prefix = "prefix";

            var expected = new TestType()
            {
                IntColumn = 7,
                BoolColumn = true,
                StringColumn = prefix + "`asdfasdfasdf"
            };

            Init(10, expected);

            using (var db = OpenDbConnection())
            {
                var actual = db.Select<TestType>(q => q.StringColumn.StartsWith(prefix));

                Assert.IsNotNull(actual);
                Assert.AreEqual(1, actual.Count);
                CollectionAssert.Contains(actual, expected);
            }
        }

        [Test]
        public void Can_select_using_Equals()
        {
            var postfix = "postfix";

            var expected = new TestType {
                IntColumn = 7,
                BoolColumn = true,
                StringColumn = postfix
            };

            Init(10, expected);

            using (var db = OpenDbConnection())
            {
                var actual = db.Select<TestType>(q => q.StringColumn.Equals(postfix));

                Assert.That(actual, Is.Not.Null);
                Assert.That(actual.Count, Is.EqualTo(1));
                Assert.That(actual, Is.EquivalentTo(new[]{ expected }));
            }
        }

        [Test]
        public void Can_select_using_endsWith()
        {
            var postfix = "postfix";

            var expected = new TestType()
            {
                IntColumn = 7,
                BoolColumn = true,
                StringColumn = "asdfasdfasdf" + postfix
            };

            Init(10, expected);

            using (var db = OpenDbConnection())
            {
                var actual = db.Select<TestType>(q => q.StringColumn.EndsWith(postfix));

                Assert.IsNotNull(actual);
                Assert.AreEqual(1, actual.Count);
                CollectionAssert.Contains(actual, expected);
            }
        }

        [Test]
        public void Can_select_using_endsWith_with_quote_in_string()
        {
            var postfix = "postfix";

            var expected = new TestType()
            {
                IntColumn = 7,
                BoolColumn = true,
                StringColumn = "asdfasd'fasdf" + postfix
            };

            Init(10, expected);

            using (var db = OpenDbConnection())
            {
                var actual = db.Select<TestType>(q => q.StringColumn.EndsWith(postfix));

                Assert.IsNotNull(actual);
                Assert.AreEqual(1, actual.Count);
                CollectionAssert.Contains(actual, expected);
            }
        }

        [Test]
        public void Can_select_using_endsWith_with_double_quote_in_string()
        {
            var postfix = "postfix";

            var expected = new TestType()
            {
                IntColumn = 7,
                BoolColumn = true,
                StringColumn = "asdfasd\"fasdf" + postfix
            };

            Init(10, expected);

            using (var db = OpenDbConnection())
            {
                var actual = db.Select<TestType>(q => q.StringColumn.EndsWith(postfix));

                Assert.IsNotNull(actual);
                Assert.AreEqual(1, actual.Count);
                CollectionAssert.Contains(actual, expected);
            }
        }

        [Test]
        public void Can_select_using_endsWith_with_backtick_in_string()
        {
            var postfix = "postfix";

            var expected = new TestType()
            {
                IntColumn = 7,
                BoolColumn = true,
                StringColumn = "asdfasd`fasdf" + postfix
            };

            Init(10, expected);

            using (var db = OpenDbConnection())
            {
                var actual = db.Select<TestType>(q => q.StringColumn.EndsWith(postfix));

                Assert.IsNotNull(actual);
                Assert.AreEqual(1, actual.Count);
                CollectionAssert.Contains(actual, expected);
            }
        }
    }
}
