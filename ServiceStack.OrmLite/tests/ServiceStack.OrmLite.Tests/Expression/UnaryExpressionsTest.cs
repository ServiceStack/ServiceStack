using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests.Expression;

[TestFixtureOrmLite]
public class UnaryExpressionsTest(DialectContext context) : ExpressionsTestBase(context)
{
    [Test]
    public void Can_select_unary_plus_constant_expression()
    {
        var expected = new TestType
        {
            IntColumn = 12,
            BoolColumn = true,
            StringColumn = "test"
        };

        Init(10, expected);

        using (var db = OpenDbConnection())
        {
            var actual = db.Select<TestType>(q => q.IntColumn == +12);

            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            CollectionAssert.Contains(actual, expected);
        }
    }

    [Test]
    public void Can_select_unary_minus_constant_expression()
    {
        var expected = new TestType
        {
            IntColumn = -12,
            BoolColumn = true,
            StringColumn = "test"
        };

        Init(10, expected);

        using (var db = OpenDbConnection())
        {
            var actual = db.Select<TestType>(q => q.IntColumn == -12);

            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            CollectionAssert.Contains(actual, expected);
        }
    }

    [Test]
    public void Can_select_unary_not_constant_expression()
    {
        var expected = new TestType
        {
            IntColumn = 12,
            BoolColumn = false,
            StringColumn = "test"
        };

        Init(10, expected);

        using (var db = OpenDbConnection())
        {
            var actual = db.Select<TestType>(q => q.BoolColumn == !true);

            Assert.IsNotNull(actual);
            Assert.Greater(actual.Count, 0);
            CollectionAssert.Contains(actual, expected);
        }
    }

    [Test]
    public void Can_select_unary_not_constant_expression2()
    {
        var expected = new TestType
        {
            IntColumn = 12,
            BoolColumn = false,
            StringColumn = "test"
        };

        Init(10, expected);

        using (var db = OpenDbConnection())
        {
            var actual = db.Select<TestType>(q => !q.BoolColumn);

            Assert.IsNotNull(actual);
            Assert.Greater(actual.Count, 0);
            CollectionAssert.Contains(actual, expected);
        }
    }

    [Test]
    public void Can_select_unary_plus_variable_expression()
    {
        // ReSharper disable ConvertToConstant.Local
        var intVal = +12;
        // ReSharper restore ConvertToConstant.Local

        var expected = new TestType
        {
            IntColumn = 12,
            BoolColumn = true,
            StringColumn = "test"
        };

        Init(10, expected);

        using (var db = OpenDbConnection())
        {
            var actual = db.Select<TestType>(q => q.IntColumn == intVal);

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

        var expected = new TestType
        {
            IntColumn = -12,
            BoolColumn = true,
            StringColumn = "test"
        };

        Init(10, expected);

        using (var db = OpenDbConnection())
        {
            var actual = db.Select<TestType>(q => q.IntColumn == intVal);

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

        var expected = new TestType
        {
            IntColumn = 12,
            BoolColumn = false,
            StringColumn = "test"
        };

        Init(10, expected);

        using (var db = OpenDbConnection())
        {
            var actual = db.Select<TestType>(q => q.BoolColumn == !boolVal);

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

        var expected = new TestType
        {
            IntColumn = 12,
            BoolColumn = true,
            StringColumn = "test"
        };

        Init(10, expected);

        using (var db = OpenDbConnection())
        {
            var actual = db.Select<TestType>(q => q.IntColumn == (int)intVal);

            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            CollectionAssert.Contains(actual, expected);
        }
    }

    [Test]
    public void Can_select_unary_not_method_expression()
    {
        var expected = new TestType
        {
            IntColumn = 12,
            BoolColumn = false,
            StringColumn = "test"
        };

        Init(10, expected);

        using (var db = OpenDbConnection())
        {
            var actual = db.Select<TestType>(q => q.BoolColumn == !GetValue(true));

            Assert.IsNotNull(actual);
            Assert.Greater(actual.Count, 0);
            CollectionAssert.Contains(actual, expected);
        }
    }

    [Test]
    public void Can_select_unary_cast_method_expression()
    {
        var expected = new TestType
        {
            IntColumn = 12,
            BoolColumn = true,
            StringColumn = "test"
        };

        Init(10, expected);

        using (var db = OpenDbConnection())
        {
            var actual = db.Select<TestType>(q => q.IntColumn == (int)GetValue((object)12));

            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            CollectionAssert.Contains(actual, expected);
        }
    }

}