using NUnit.Framework;

namespace ServiceStack.OrmLite.FirebirdTests.Expressions
{
    public class UnaryExpressionsTest : ExpressionsTestBase
    {
        #region constants

        [Test]
        public void Can_select_unary_plus_constant_expression()
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
                var actual = con.Select<TestType>(q => q.IntColumn == +12);

                Assert.IsNotNull(actual);
                Assert.AreEqual(1, actual.Count);
                CollectionAssert.Contains(actual, expected);
            }
        }

        [Test]
        public void Can_select_unary_minus_constant_expression()
        {
            var expected = new TestType()
            {
                IntColumn = -12,
                BoolColumn = true,
                StringColumn = "test"
            };

            EstablishContext(10, expected);

            using (var con = OpenDbConnection())
            {
                var actual = con.Select<TestType>(q => q.IntColumn == -12);

                Assert.IsNotNull(actual);
                Assert.AreEqual(1, actual.Count);
                CollectionAssert.Contains(actual, expected);
            }
        }

        [Test]
        public void Can_select_unary_not_constant_expression()
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
                var actual = con.Select<TestType>(q => q.BoolColumn == !true);

                Assert.IsNotNull(actual);
                Assert.Greater(actual.Count, 0);
                CollectionAssert.Contains(actual, expected);
            }
        }

        [Test]
        public void Can_select_unary_not_constant_expression2()
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
                var actual = con.Select<TestType>(q => !q.BoolColumn);

                Assert.IsNotNull(actual);
                Assert.Greater(actual.Count, 0);
                CollectionAssert.Contains(actual, expected);
            }
        }

        #endregion

        #region variables

        [Test]
        public void Can_select_unary_plus_variable_expression()
        {
            // ReSharper disable ConvertToConstant.Local
            var intVal = +12;
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
                var actual = con.Select<TestType>(q => q.IntColumn == intVal);

                Assert.IsNotNull(actual);
                Assert.AreEqual(1, actual.Count);
                CollectionAssert.Contains(actual, expected);
            }
        }

        [Test]
        public void Can_select_unary_minus_variable_expression()
        {
            // ReSharper disable ConvertToConstant.Local
            var intVal = -12;
            // ReSharper restore ConvertToConstant.Local

            var expected = new TestType()
            {
                IntColumn = -12,
                BoolColumn = true,
                StringColumn = "test"
            };

            EstablishContext(10, expected);

            using (var con = OpenDbConnection())
            {
                var actual = con.Select<TestType>(q => q.IntColumn == intVal);

                Assert.IsNotNull(actual);
                Assert.AreEqual(1, actual.Count);
                CollectionAssert.Contains(actual, expected);
            }
        }

        [Test]
        public void Can_select_unary_not_variable_expression()
        {
            // ReSharper disable ConvertToConstant.Local
            var boolVal = true;
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
                var actual = con.Select<TestType>(q => q.BoolColumn == !boolVal);

                Assert.IsNotNull(actual);
                Assert.Greater(actual.Count, 0);
                CollectionAssert.Contains(actual, expected);
            }
        }

        [Test]
        public void Can_select_unary_cast_variable_expression()
        {
            // ReSharper disable ConvertToConstant.Local
            object intVal = 12;
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
                var actual = con.Select<TestType>(q => q.IntColumn == (int) intVal);

                Assert.IsNotNull(actual);
                Assert.AreEqual(1, actual.Count);
                CollectionAssert.Contains(actual, expected);
            }
        }

        #endregion

        #region method

        [Test]
        public void Can_select_unary_not_method_expression()
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
                var actual = con.Select<TestType>(q => q.BoolColumn == !GetValue(true));

                Assert.IsNotNull(actual);
                Assert.Greater(actual.Count, 0);
                CollectionAssert.Contains(actual, expected);
            }
        }

        [Test]
        public void Can_select_unary_cast_method_expression()
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
                var actual = con.Select<TestType>(q => q.IntColumn == (int) GetValue((object) 12));

                Assert.IsNotNull(actual);
                Assert.AreEqual(1, actual.Count);
                CollectionAssert.Contains(actual, expected);
            }
        }

        #endregion
    }
}