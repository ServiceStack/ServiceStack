using NUnit.Framework;

namespace ServiceStack.OrmLite.SqlServerTests.Expressions
{
    public class EqualityExpressionsTest : ExpressionsTestBase
    {
        [Test]
        public void Can_select_equals_constant_int_expression()
        {
            var expected = new TestType()
                               {
                                   IntColumn = 3,
                                   BoolColumn = true,
                                   StringColumn = "4"
                               };

            EstablishContext(10, expected);

            var actual = OpenDbConnection().Select<TestType>(q => q.IntColumn == 3);

            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            CollectionAssert.Contains(actual, expected);
        }

        [Test]
        public void Can_select_equals_variable_int_expression()
        {
// ReSharper disable ConvertToConstant.Local
            var columnValue = 3;
// ReSharper restore ConvertToConstant.Local

            var expected = new TestType()
            {
                IntColumn = columnValue,
                BoolColumn = true,
                StringColumn = "4"
            };

            EstablishContext(10, expected);

            var actual = OpenDbConnection().Select<TestType>(q => q.IntColumn == columnValue);

            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            CollectionAssert.Contains(actual, expected);
        }

        [Test]
        public void Can_select_equals_int_method_expression()
        {
            var expected = new TestType()
            {
                IntColumn = 3,
                BoolColumn = true,
                StringColumn = "4"
            };

            EstablishContext(10, expected);

            var actual = OpenDbConnection().Select<TestType>(q => q.IntColumn == GetValue(3));

            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            CollectionAssert.Contains(actual, expected);
        }

        [Test]
        public void Can_select_not_equals_constant_int_expression()
        {
            var expected = new TestType()
            {
                IntColumn = 3,
                BoolColumn = true,
                StringColumn = "4"
            };

            EstablishContext(10, expected);

            var actual = OpenDbConnection().Select<TestType>(q => q.IntColumn != 3);

            Assert.IsNotNull(actual);
            Assert.AreEqual(10, actual.Count);
            CollectionAssert.DoesNotContain(actual, expected);
        }

        [Test]
        public void Can_select_not_equals_variable_int_expression()
        {
            // ReSharper disable ConvertToConstant.Local
            var columnValue = 3;
            // ReSharper restore ConvertToConstant.Local

            var expected = new TestType()
            {
                IntColumn = columnValue,
                BoolColumn = true,
                StringColumn = "4"
            };

            EstablishContext(10, expected);

            var actual = OpenDbConnection().Select<TestType>(q => q.IntColumn != columnValue);

            Assert.IsNotNull(actual);
            Assert.AreEqual(10, actual.Count);
            CollectionAssert.DoesNotContain(actual, expected);
        }

        [Test]
        public void Can_select_not_equals_int_method_expression()
        {
            var expected = new TestType()
            {
                IntColumn = 3,
                BoolColumn = true,
                StringColumn = "4"
            };

            EstablishContext(10, expected);

            var actual = OpenDbConnection().Select<TestType>(q => q.IntColumn != GetValue(3));

            Assert.IsNotNull(actual);
            Assert.AreEqual(10, actual.Count);
            CollectionAssert.DoesNotContain(actual, expected);
        }


        [Test]
        public void Can_select_equals_constant_bool_expression()
        {
            var expected = new TestType()
            {
                IntColumn = 3,
                BoolColumn = true,
                StringColumn = "4"
            };

            EstablishContext(10, expected);

// ReSharper disable RedundantBoolCompare
            var actual = OpenDbConnection().Select<TestType>(q => q.BoolColumn == true);
// ReSharper restore RedundantBoolCompare

            Assert.IsNotNull(actual);
            Assert.Greater(actual.Count, 0);
            CollectionAssert.Contains(actual, expected);
        }

        [Test]
        public void Can_select_equals_constant_bool_expression2()
        {
            var expected = new TestType()
            {
                IntColumn = 3,
                BoolColumn = true,
                StringColumn = "4"
            };

            EstablishContext(10, expected);

            // ReSharper disable RedundantBoolCompare
            var actual = OpenDbConnection().Select<TestType>(q => q.BoolColumn);
            // ReSharper restore RedundantBoolCompare

            Assert.IsNotNull(actual);
            Assert.Greater(actual.Count, 0);
            CollectionAssert.Contains(actual, expected);
        }

        [Test]
        public void Can_select_equals_variable_bool_expression()
        {
            // ReSharper disable ConvertToConstant.Local
            var columnValue = true;
            // ReSharper restore ConvertToConstant.Local

            var expected = new TestType()
            {
                IntColumn = 3,
                BoolColumn = true,
                StringColumn = "4"
            };

            EstablishContext(10, expected);

            var actual = OpenDbConnection().Select<TestType>(q => q.BoolColumn == columnValue);

            Assert.IsNotNull(actual);
            Assert.Greater(actual.Count, 0);
            CollectionAssert.Contains(actual, expected);
        }

        [Test]
        public void Can_select_equals_bool_method_expression()
        {
            var expected = new TestType()
            {
                IntColumn = 3,
                BoolColumn = true,
                StringColumn = "4"
            };

            EstablishContext(10, expected);

            var actual = OpenDbConnection().Select<TestType>(q => q.BoolColumn == GetValue(true));

            Assert.IsNotNull(actual);
            Assert.Greater(actual.Count, 0);
            CollectionAssert.Contains(actual, expected);
        }

        [Test]
        public void Can_select_equals_null_espression()
        {
            var expected = new TestType()
            {
                IntColumn = 12,
                BoolColumn = false,
                StringColumn = "test",
                NullableCol = new TestType { StringColumn = "sometext" }
            };

            EstablishContext(10, expected);

            var actual = OpenDbConnection().Select<TestType>(q => q.NullableCol == null);

            Assert.IsNotNull(actual);
            Assert.AreEqual(actual.Count, 10);
            CollectionAssert.DoesNotContain(actual, expected);
        }

        [Test]
        public void Can_select_not_equals_null_espression()
        {
            var expected = new TestType()
            {
                IntColumn = 12,
                BoolColumn = false,
                StringColumn = "test",
                NullableCol = new TestType { StringColumn = "sometext" }
            };

            EstablishContext(10, expected);

            var actual = OpenDbConnection().Select<TestType>(q => q.NullableCol != null);

            Assert.IsNotNull(actual);
            Assert.AreEqual(actual.Count, 1);
            CollectionAssert.Contains(actual, expected);
        }

        // Assume not equal works ;-)

        [Test]
        public void Can_select_equals_coalesce_on_the_left_expression()
        {
            // ReSharper disable ConvertToConstant.Local
            object stringVal = "sometext";
            // ReSharper restore ConvertToConstant.Local

            var expected = new TestType()
            {
                IntColumn = 12,
                BoolColumn = false,
                StringColumn = "test",
                NullableCol = "sometext"
            };

            EstablishContext(10, expected);

            var actual = OpenDbConnection().Select<TestType>(q => (q.NullableCol ?? "othertext") == stringVal);

            Assert.IsNotNull(actual);
            Assert.AreEqual(actual.Count, 1);                   // this passes, because PARAMS: @0=othertext, @1=sometext
            CollectionAssert.Contains(actual, expected);
        }

        [Test]
        public void Can_select_equals_coalesce_on_the_right_expression()
        {
            // ReSharper disable ConvertToConstant.Local
            object stringVal = "sometext";
            // ReSharper restore ConvertToConstant.Local

            var expected = new TestType()
            {
                IntColumn = 12,
                BoolColumn = false,
                StringColumn = "test",
                NullableCol = "sometext"
            };

            EstablishContext(10, expected);

            var actual = OpenDbConnection().Select<TestType>(q => stringVal == (q.NullableCol ?? "othertext"));

            Assert.IsNotNull(actual);
            Assert.AreEqual(actual.Count, 1);                  // this will fail for now, because PARAMS: @0=othertext, @1={Text:"COALESCE(""NullableCol"",@0)"}
            CollectionAssert.Contains(actual, expected);       // this will fail as well
        }

    }
}
