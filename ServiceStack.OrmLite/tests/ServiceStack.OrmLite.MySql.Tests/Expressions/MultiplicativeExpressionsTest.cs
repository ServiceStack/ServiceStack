using NUnit.Framework;

namespace ServiceStack.OrmLite.MySql.Tests.Expressions
{
    public class MultiplicativeExpressionsTest : ExpressionsTestBase
    {
        [Test]
        public void Can_select_constant_multiply_expression()
        {
            var expected = new TestType()
                               {
                                   IntColumn = 12,
                                   BoolColumn = true,
                                   StringColumn = "test"
                               };

            EstablishContext(10, expected);

            var actual = OpenDbConnection().Select<TestType>(q => q.IntColumn == 4 * 3);

            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            CollectionAssert.Contains(actual, expected);
        }

        [Test]
        public void Can_select_constant_divide_expression()
        {
            var expected = new TestType()
                               {
                                   IntColumn = 12,
                                   BoolColumn = true,
                                   StringColumn = "test"
                               };

            EstablishContext(10, expected);

            var actual = OpenDbConnection().Select<TestType>(q => q.IntColumn == 36 / 3);

            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            CollectionAssert.Contains(actual, expected);
        }

        [Test]
        public void Can_select_constant_modulo_expression()
        {
            var expected = new TestType()
            {
                IntColumn = 7,
                BoolColumn = true,
                StringColumn = "test"
            };

            EstablishContext(10, expected);

            var actual = OpenDbConnection().Select<TestType>(q => q.IntColumn == 37 % 10);

            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            CollectionAssert.Contains(actual, expected);
        }

        [Test]
        public void Can_select_variable_multiply_expression()
        {
            // ReSharper disable ConvertToConstant.Local
            var a = 4;
            var b = 3;
            // ReSharper restore ConvertToConstant.Local

            var expected = new TestType()
                               {
                                   IntColumn = 12,
                                   BoolColumn = true,
                                   StringColumn = "test"
                               };

            EstablishContext(10, expected);

            var actual = OpenDbConnection().Select<TestType>(q => q.IntColumn == a * b);

            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            CollectionAssert.Contains(actual, expected);
        }

        [Test]
        public void Can_select_variable_divide_expression()
        {
            // ReSharper disable ConvertToConstant.Local
            var a = 36;
            var b = 3;
            // ReSharper restore ConvertToConstant.Local

            var expected = new TestType()
                               {
                                   IntColumn = 12,
                                   BoolColumn = true,
                                   StringColumn = "test"
                               };

            EstablishContext(10, expected);

            var actual = OpenDbConnection().Select<TestType>(q => q.IntColumn == a / b);

            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            CollectionAssert.Contains(actual, expected);
        }

        [Test]
        public void Can_select_variablemodulo_expression()
        {
            // ReSharper disable ConvertToConstant.Local
            var a = 37;
            var b = 10;
            // ReSharper restore ConvertToConstant.Local

            var expected = new TestType()
            {
                IntColumn = 7,
                BoolColumn = true,
                StringColumn = "test"
            };

            EstablishContext(10, expected);

            var actual = OpenDbConnection().Select<TestType>(q => q.IntColumn == a % b);

            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            CollectionAssert.Contains(actual, expected);
        }

        [Test]
        public void Can_select_method_multiply_expression()
        {
            var expected = new TestType()
                               {
                                   IntColumn = 12,
                                   BoolColumn = true,
                                   StringColumn = "test"
                               };

            EstablishContext(10, expected);

            var actual = OpenDbConnection().Select<TestType>(q => q.IntColumn == GetValue(4) * GetValue(3));

            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            CollectionAssert.Contains(actual, expected);
        }

        [Test]
        public void Can_select_method_divide_expression()
        {
            var expected = new TestType()
                               {
                                   IntColumn = 12,
                                   BoolColumn = true,
                                   StringColumn = "test"
                               };

            EstablishContext(10, expected);

            var actual = OpenDbConnection().Select<TestType>(q => q.IntColumn == GetValue(36) / GetValue(3));

            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            CollectionAssert.Contains(actual, expected);
        }

        [Test]
        public void Can_select_method_modulo_expression()
        {
            var expected = new TestType()
            {
                IntColumn = 7,
                BoolColumn = true,
                StringColumn = "test"
            };

            EstablishContext(10, expected);

            var actual = OpenDbConnection().Select<TestType>(q => q.IntColumn == GetValue(37) % GetValue(10));

            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            CollectionAssert.Contains(actual, expected);
        }
    }
}