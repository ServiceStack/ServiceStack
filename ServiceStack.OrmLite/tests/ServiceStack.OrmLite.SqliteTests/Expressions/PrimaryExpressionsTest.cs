using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests.Expressions
{
    public class PrimaryExpressionsTest : ExpressionsTestBase
    {
        private static class TestClass
        {
            public static int StaticProperty { get { return 12; } }
            public static int _staticField = 12;
        }

        private class TestClass<T>
        {
            public static T StaticMethod(T value)
            {
                return value;
            }

            public T Property { get; set; }

            public T _field;

            public T Mehtod()
            {
                return _field;
            }

            public TestClass(T value)
            {
                Property = value;
                _field = value;
            }
        }

        private struct TestStruct<T>
        {
            public T Property { get { return _field; } }

            public T _field;

            public T Mehtod()
            {
                return _field;
            }

            public TestStruct(T value)
            {
                _field = value;
            }
        }

        #region int

        [Test]
        public void Can_select_int_property_expression()
        {
            var tmp = new TestClass<int>(12);

            var expected = new TestType()
            {
                IntColumn = 12,
                BoolColumn = false,
                StringColumn = "test"
            };

            EstablishContext(10, expected);

            var actual = OpenDbConnection().Select<TestType>(q => q.IntColumn == tmp.Property);

            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            CollectionAssert.Contains(actual, expected);
        }

        [Test]
        public void Can_select_int_field_expression()
        {
            var tmp = new TestClass<int>(12);

            var expected = new TestType()
            {
                IntColumn = 12,
                BoolColumn = false,
                StringColumn = "test"
            };

            EstablishContext(10, expected);

            var actual = OpenDbConnection().Select<TestType>(q => q.IntColumn == tmp._field);

            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            CollectionAssert.Contains(actual, expected);
        }

        [Test]
        public void Can_select_int_method_expression()
        {
            var tmp = new TestClass<int>(12);

            var expected = new TestType()
            {
                IntColumn = 12,
                BoolColumn = false,
                StringColumn = "test"
            };

            EstablishContext(10, expected);

            var actual = OpenDbConnection().Select<TestType>(q => q.IntColumn == tmp.Mehtod());

            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            CollectionAssert.Contains(actual, expected);
        }

        [Test]
        public void Can_select_static_int_property_expression()
        {
            var expected = new TestType()
            {
                IntColumn = 12,
                BoolColumn = false,
                StringColumn = "test"
            };

            EstablishContext(10, expected);

            var actual = OpenDbConnection().Select<TestType>(q => q.IntColumn == TestClass.StaticProperty);

            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            CollectionAssert.Contains(actual, expected);
        }

        [Test]
        public void Can_select_static_int_field_expression()
        {
            var expected = new TestType()
            {
                IntColumn = 12,
                BoolColumn = false,
                StringColumn = "test"
            };

            EstablishContext(10, expected);

            var actual = OpenDbConnection().Select<TestType>(q => q.IntColumn == TestClass._staticField);

            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            CollectionAssert.Contains(actual, expected);
        }

        [Test]
        public void Can_select_static_int_method_expression()
        {
            var expected = new TestType()
            {
                IntColumn = 12,
                BoolColumn = false,
                StringColumn = "test"
            };

            EstablishContext(10, expected);

            var actual = OpenDbConnection().Select<TestType>(q => q.IntColumn == TestClass<int>.StaticMethod(12));

            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            CollectionAssert.Contains(actual, expected);
        }

        [Test]
        public void Can_select_int_new_expression()
        {
            var expected = new TestType()
            {
                IntColumn = 12,
                BoolColumn = false,
                StringColumn = "test"
            };

            EstablishContext(10, expected);

            var actual = OpenDbConnection().Select<TestType>(q => q.IntColumn == new TestClass<int>(12).Property);

            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            CollectionAssert.Contains(actual, expected);
        }

        [Test]
        public void Can_select_struct_int_field_expression()
        {
            var tmp = new TestStruct<int>(12);

            var expected = new TestType()
            {
                IntColumn = 12,
                BoolColumn = false,
                StringColumn = "test"
            };

            EstablishContext(10, expected);

            var actual = OpenDbConnection().Select<TestType>(q => q.IntColumn == tmp._field);

            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            CollectionAssert.Contains(actual, expected);
        }

        [Test]
        public void Can_select_struct_int_property_expression()
        {
            var tmp = new TestStruct<int>(12);

            var expected = new TestType()
            {
                IntColumn = 12,
                BoolColumn = false,
                StringColumn = "test"
            };

            EstablishContext(10, expected);

            var actual = OpenDbConnection().Select<TestType>(q => q.IntColumn == tmp.Property);

            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            CollectionAssert.Contains(actual, expected);
        }

        [Test]
        public void Can_select_struct_int_method_expression()
        {
            var tmp = new TestStruct<int>(12);

            var expected = new TestType()
            {
                IntColumn = 12,
                BoolColumn = false,
                StringColumn = "test"
            };

            EstablishContext(10, expected);

            var actual = OpenDbConnection().Select<TestType>(q => q.IntColumn == tmp.Mehtod());

            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            CollectionAssert.Contains(actual, expected);
        }

        #endregion int


        #region bool

        [Test]
        public void Can_select_bool_property_expression()
        {
            var tmp = new TestClass<bool>(false);

            var expected = new TestType()
            {
                IntColumn = 12,
                BoolColumn = false,
                StringColumn = "test"
            };

            EstablishContext(10, expected);

            var actual = OpenDbConnection().Select<TestType>(q => q.BoolColumn == tmp.Property);

            Assert.IsNotNull(actual);
            Assert.Greater(actual.Count, 1);
            CollectionAssert.Contains(actual, expected);
        }

        [Test]
        public void Can_select_bool_field_expression()
        {
            var tmp = new TestClass<bool>(false);

            var expected = new TestType()
            {
                IntColumn = 12,
                BoolColumn = false,
                StringColumn = "test"
            };

            EstablishContext(10, expected);

            var actual = OpenDbConnection().Select<TestType>(q => q.BoolColumn == tmp._field);

            Assert.IsNotNull(actual);
            Assert.Greater(actual.Count, 1);
            CollectionAssert.Contains(actual, expected);
        }

        [Test]
        public void Can_select_bool_method_expression()
        {
            var tmp = new TestClass<bool>(false);

            var expected = new TestType()
            {
                IntColumn = 12,
                BoolColumn = false,
                StringColumn = "test"
            };

            EstablishContext(10, expected);

            var actual = OpenDbConnection().Select<TestType>(q => q.BoolColumn == tmp.Mehtod());

            Assert.IsNotNull(actual);
            Assert.Greater(actual.Count, 1);
            CollectionAssert.Contains(actual, expected);
        }

        [Test]
        public void Can_select_static_bool_method_expression()
        {
            var expected = new TestType()
            {
                IntColumn = 12,
                BoolColumn = false,
                StringColumn = "test"
            };

            EstablishContext(10, expected);

            var actual = OpenDbConnection().Select<TestType>(q => q.BoolColumn == TestClass<bool>.StaticMethod(false));

            Assert.IsNotNull(actual);
            Assert.Greater(actual.Count, 1);
            CollectionAssert.Contains(actual, expected);
        }

        [Test]
        public void Can_select_bool_new_expression()
        {
            var expected = new TestType()
            {
                IntColumn = 12,
                BoolColumn = false,
                StringColumn = "test"
            };

            EstablishContext(10, expected);

            var actual = OpenDbConnection().Select<TestType>(q => q.BoolColumn == new TestClass<bool>(false).Property);

            Assert.IsNotNull(actual);
            Assert.Greater(actual.Count, 1);
            CollectionAssert.Contains(actual, expected);
        }

        #endregion bool
    }
}