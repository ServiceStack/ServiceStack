using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests.Expression;

[TestFixtureOrmLite]
public class EqualityExpressionsTest(DialectContext context) : ExpressionsTestBase(context)
{
    [Test]
    public void Can_select_equals_constant_int_expression()
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
            var actual = db.Select<TestType>(q => q.IntColumn == 3);

            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            CollectionAssert.Contains(actual, expected);
        }
    }

    [Test]
    public void Can_select_equals_variable_int_expression()
    {
        // ReSharper disable ConvertToConstant.Local
        var columnValue = 3;
        // ReSharper restore ConvertToConstant.Local

        var expected = new TestType
        {
            IntColumn = columnValue,
            BoolColumn = true,
            StringColumn = "4"
        };

        Init(10, expected);

        using (var db = OpenDbConnection())
        {
            var actual = db.Select<TestType>(q => q.IntColumn == columnValue);

            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            CollectionAssert.Contains(actual, expected);
        }
    }

    [Test]
    public void Can_select_equals_int_method_expression()
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
            var actual = db.Select<TestType>(q => q.IntColumn == GetValue(3));

            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            CollectionAssert.Contains(actual, expected);
        }
    }

    [Test]
    public void Can_select_not_equals_constant_int_expression()
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
            var actual = db.Select<TestType>(q => q.IntColumn != 3);

            Assert.IsNotNull(actual);
            Assert.AreEqual(10, actual.Count);
            CollectionAssert.DoesNotContain(actual, expected);
        }
    }

    [Test]
    public void Can_select_not_equals_variable_int_expression()
    {
        // ReSharper disable ConvertToConstant.Local
        var columnValue = 3;
        // ReSharper restore ConvertToConstant.Local

        var expected = new TestType
        {
            IntColumn = columnValue,
            BoolColumn = true,
            StringColumn = "4"
        };

        Init(10, expected);

        using (var db = OpenDbConnection())
        {
            var actual = db.Select<TestType>(q => q.IntColumn != columnValue);

            Assert.IsNotNull(actual);
            Assert.AreEqual(10, actual.Count);
            CollectionAssert.DoesNotContain(actual, expected);
        }
    }

    [Test]
    public void Can_select_not_equals_int_method_expression()
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
            var actual = db.Select<TestType>(q => q.IntColumn != GetValue(3));

            Assert.IsNotNull(actual);
            Assert.AreEqual(10, actual.Count);
            CollectionAssert.DoesNotContain(actual, expected);
        }
    }

    [Test]
    public void Can_select_equals_constant_bool_expression()
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
            var actual = db.Select<TestType>(q => q.BoolColumn == true);

            Assert.IsNotNull(actual);
            Assert.Greater(actual.Count, 0);
            CollectionAssert.Contains(actual, expected);
        }
    }

    [Test]
    public void Can_select_equals_constant_bool_expression2()
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
            var actual = db.Select<TestType>(q => q.BoolColumn);

            Assert.IsNotNull(actual);
            Assert.Greater(actual.Count, 0);
            CollectionAssert.Contains(actual, expected);
        }
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

        Init(10, expected);

        using (var db = OpenDbConnection())
        {
            var actual = db.Select<TestType>(q => q.BoolColumn == columnValue);

            Assert.IsNotNull(actual);
            Assert.Greater(actual.Count, 0);
            CollectionAssert.Contains(actual, expected);
        }
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

        Init(10, expected);

        using (var db = OpenDbConnection())
        {
            var actual = db.Select<TestType>(q => q.BoolColumn == GetValue(true));

            Assert.IsNotNull(actual);
            Assert.Greater(actual.Count, 0);
            CollectionAssert.Contains(actual, expected);
        }
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

        Init(10, expected);

        using (var db = OpenDbConnection())
        {
            var actual = db.Select<TestType>(q => q.NullableCol == null);

            Assert.IsNotNull(actual);
            Assert.AreEqual(actual.Count, 10);
            CollectionAssert.DoesNotContain(actual, expected);
        }
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

        Init(10, expected);

        using (var db = OpenDbConnection())
        {
            var actual = db.Select<TestType>(q => q.NullableCol != null);

            Assert.IsNotNull(actual);
            Assert.AreEqual(actual.Count, 1);
            CollectionAssert.Contains(actual, expected);
        }
    }

    // Assume not equal works ;-)
    [Test]
    public void Can_select_equals_variable_null_expression()
    {
        object columnValue = null;

        var expected = new TestType()
        {
            IntColumn = 12,
            BoolColumn = false,
            StringColumn = "test",
            NullableCol = new TestType { StringColumn = "sometext" }
        };

        Init(10, expected);

        var actual = OpenDbConnection().Select<TestType>(q => q.NullableCol == columnValue);

        Assert.IsNotNull(actual);
        Assert.AreEqual(actual.Count, 10);
        CollectionAssert.DoesNotContain(actual, expected);
    }
}