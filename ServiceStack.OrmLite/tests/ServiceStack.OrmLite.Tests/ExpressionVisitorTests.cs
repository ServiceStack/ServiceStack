using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests;

[TestFixtureOrmLite]
public class ExpressionVisitorTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    private IDbConnection Db;

    [SetUp]
    public void Setup()
    {
        using (var db = OpenDbConnection())
        {
            db.DropTable<TestType>();
            db.DropTable<TestType2>();
            db.DropTable<TestType3>();

            db.CreateTable<TestType3>();
            db.Insert(new TestType3 { Id = 1, BoolCol = true, DateCol = new DateTime(2012, 4, 1), TextCol = "111", EnumCol = TestEnum.Val3, NullableIntCol = 10, TestType3Name = "3.1", CustomInt = 100 });
            db.Insert(new TestType3 { Id = 2, BoolCol = false, DateCol = new DateTime(2012, 4, 1), TextCol = "222", EnumCol = TestEnum.Val3, NullableIntCol = 20, TestType3Name = "3.2", CustomInt = 200 });
            db.Insert(new TestType3 { Id = 3, BoolCol = false, DateCol = new DateTime(2012, 4, 1), TextCol = "222", EnumCol = TestEnum.Val3, NullableIntCol = 30, TestType3Name = "3.3", CustomInt = 300 });
            db.Insert(new TestType3 { Id = 4, BoolCol = false, DateCol = new DateTime(2012, 4, 1), TextCol = "222", EnumCol = TestEnum.Val3, NullableIntCol = 40, TestType3Name = "3.4", CustomInt = 400 });

            db.CreateTable<TestType2>();
            db.Insert(new TestType2 { Id = 1, BoolCol = true, DateCol = new DateTime(2012, 4, 1), TextCol = "111", EnumCol = TestEnum.Val3, NullableIntCol = 10, TestType2Name = "2.1", TestType3ObjColId = 1 });
            db.Insert(new TestType2 { Id = 2, BoolCol = false, DateCol = new DateTime(2012, 4, 1), TextCol = "222", EnumCol = TestEnum.Val3, NullableIntCol = 20, TestType2Name = "2.2", TestType3ObjColId = 2 });
            db.Insert(new TestType2 { Id = 3, BoolCol = true, DateCol = new DateTime(2012, 4, 1), TextCol = "333", EnumCol = TestEnum.Val3, NullableIntCol = 30, TestType2Name = "2.3", TestType3ObjColId = 3 });
            db.Insert(new TestType2 { Id = 4, BoolCol = false, DateCol = new DateTime(2012, 4, 1), TextCol = "444", EnumCol = TestEnum.Val3, NullableIntCol = 40, TestType2Name = "2.4", TestType3ObjColId = 4 });

            db.CreateTable<TestType>();
            db.Insert(new TestType { Id = 1, BoolCol = true, DateCol = new DateTime(2012, 1, 1), TextCol = "asdf", EnumCol = TestEnum.Val0, NullableIntCol = 10, TestType2ObjColId = 1 });
            db.Insert(new TestType { Id = 2, BoolCol = true, DateCol = new DateTime(2012, 2, 1), TextCol = "asdf123", EnumCol = TestEnum.Val1, NullableIntCol = null, TestType2ObjColId = 2 });
            db.Insert(new TestType { Id = 3, BoolCol = false, DateCol = new DateTime(2012, 3, 1), TextCol = "qwer", EnumCol = TestEnum.Val2, NullableIntCol = 30, TestType2ObjColId = 3 });
            db.Insert(new TestType { Id = 4, BoolCol = false, DateCol = new DateTime(2012, 4, 1), TextCol = "qwer123", EnumCol = TestEnum.Val3, NullableIntCol = 40, TestType2ObjColId = 4 });
        }
        Db = OpenDbConnection();
    }

    [TearDown]
    public void TearDown()
    {
        Db.Dispose();
    }

    [Test]
    public void Can_Select_by_const_int()
    {
        var target = Db.Select<TestType>(q => q.Id == 1);
        Assert.AreEqual(1, target.Count);
    }

    [Test]
    public void Can_Select_by_value_returned_by_method_without_params()
    {
        var target = Db.Select<TestType>(q => q.Id == MethodReturningInt());
        Assert.AreEqual(1, target.Count);
    }

    [Test]
    public void Can_Select_by_value_returned_by_method_with_param()
    {
        var target = Db.Select<TestType>(q => q.Id == MethodReturningInt(1));
        Assert.AreEqual(1, target.Count);
    }

    [Test]
    public void Can_Select_by_const_enum()
    {
        var target = Db.Select<TestType>(q => q.EnumCol == TestEnum.Val0);
        Assert.AreEqual(1, target.Count);
    }

    [Test]
    public void Can_Select_by_enum_returned_by_method()
    {
        var target = Db.Select<TestType>(q => q.EnumCol == MethodReturningEnum());
        Assert.AreEqual(1, target.Count);
    }

    [Test]
    public void Can_Select_using_ToUpper_on_string_property_of_T()
    {
        var target =
            Db.Select<TestType>(q => q.TextCol.ToUpper() == "ASDF");
        Assert.AreEqual(1, target.Count);
    }

    [Test]
    public void Can_Select_using_ToLower_on_string_property_of_field()
    {
        var obj = new TestType {TextCol = "ASDF"};

        var target =
            Db.Select<TestType>(q => q.TextCol == obj.TextCol.ToLower());
        Assert.AreEqual(1, target.Count);
    }

    [Test]
    public void Can_Select_using_Constant_Bool_Value()
    {
        var target =
            Db.Select<TestType>(q => q.BoolCol == true);
        Assert.AreEqual(2, target.Count);
    }

    [Test]
    public void Can_Select_using_new_ComplexType()
    {
        Db.Insert(new TestType
        {
            Id = 5,
            BoolCol = false,
            DateCol = new DateTime(2012, 5, 1),
            TextCol = "uiop",
            EnumCol = TestEnum.Val3,
            ComplexObjCol = new TestType { TextCol = "poiu" },
            TestType2ObjColId = 1
        });

        var target = Db.Select<TestType>(
            q => q.ComplexObjCol == new TestType { TextCol = "poiu" });
        Assert.AreEqual(1, target.Count);
    }

    [Test]
    public void Can_Select_using_IN()
    {
        var q = Db.From<TestType>();
        q.Where(x => Sql.In(x.TextCol, "asdf", "qwer"));
        var target = Db.Select(q);
        Assert.AreEqual(2, target.Count);
    }

    [Test]
    public void Can_Select_using_IN_using_params()
    {
        var q = Db.From<TestType>();
        q.Where(x => Sql.In(x.Id, 1, 2, 3));
        var target = Db.Select(q);
        Assert.AreEqual(3, target.Count);
    }

    [Test]
    public void Can_Select_using_IN_using_int_array()
    {
        var q = Db.From<TestType>();
        q.Where(x => Sql.In(x.Id, new[] {1, 2, 3}));
        var target = Db.Select(q);
        Assert.AreEqual(3, target.Count);
    }

    [Test]
    public void Can_Select_using_IN_using_object_array()
    {
        var q = Db.From<TestType>();
        q.Where(x => Sql.In(x.Id, new object[] { 1, 2, 3 }));
        var target = Db.Select(q);
        Assert.AreEqual(3, target.Count);
    }

    [Test]
    public void Can_Select_using_int_array_Contains()
    {
        var ids = new[] { 1, 2 };
        var q = Db.From<TestType>().Where(x => ids.Contains(x.Id));
        var target = Db.Select(q);
        CollectionAssert.AreEquivalent(ids, target.Select(t => t.Id).ToArray());
    }

    [Test]
    public void Can_Select_using_int_list_Contains()
    {
        var ids = new List<int> { 1, 2 };
        var q = Db.From<TestType>().Where(x => ids.Contains(x.Id));
        var target = Db.Select(q);
        CollectionAssert.AreEquivalent(ids, target.Select(t => t.Id).ToArray());
    }

    [Test]
    public void Can_Select_using_int_array_Contains_Value()
    {
        var ints = new[] { 10, 40 };
        var q = Db.From<TestType>().Where(x => ints.Contains(x.NullableIntCol.Value)); // Doesn't compile without ".Value" here - "ints" is not nullable
        var target = Db.Select(q);
        CollectionAssert.AreEquivalent(new[] { 1, 4 }, target.Select(t => t.Id).ToArray());
    }

    [Test]
    public void Can_Select_using_Nullable_HasValue()
    {
        var q = Db.From<TestType>().Where(x => x.NullableIntCol.HasValue); // WHERE NullableIntCol IS NOT NULL
        var target = Db.Select(q);
        CollectionAssert.AreEquivalent(new[] { 1, 3, 4 }, target.Select(t => t.Id).ToArray());

        q = Db.From<TestType>().Where(x => !x.NullableIntCol.HasValue); // WHERE NOT (NullableIntCol IS NOT NULL)
        target = Db.Select(q);
        CollectionAssert.AreEquivalent(new[] { 2 }, target.Select(t => t.Id).ToArray());
    }

    [Test]
    public void Can_Select_using_constant_Yoda_condition()
    {
        var q = Db.From<TestType>().Where(x => null != x.NullableIntCol); // "null != x.NullableIntCol" should be the same as "x.NullableIntCol != null"
        var target = Db.Select(q);
        CollectionAssert.AreEquivalent(new[] { 1, 3, 4 }, target.Select(t => t.Id).ToArray());
    }

    [Test]
    public void Can_Select_using_int_array_constructed_inside_Contains()
    {
        var q = Db.From<TestType>().Where(x => new int?[] { 10, 30 }.Contains(x.NullableIntCol));
        var target = Db.Select(q);
        CollectionAssert.AreEquivalent(new[] { 1, 3 }, target.Select(t => t.Id).ToArray());
    }

    [Test]
    public void Can_Select_using_int_list_constructed_inside_Contains()
    {
        var q = Db.From<TestType>().Where(x => new List<int?> { 10, 30 }.Contains(x.NullableIntCol));
        var target = Db.Select(q);
        CollectionAssert.AreEquivalent(new[] { 1, 3 }, target.Select(t => t.Id).ToArray());
    }

    [Test]
    public void Can_Select_using_Startswith()
    {
        var target = Db.Select<TestType>(q => q.TextCol.StartsWith("asdf"));
        Assert.AreEqual(2, target.Count);
    }

    [Test]
    public void Can_Select_using_Endswith()
    {
        var target = Db.Select<TestType>(q => q.TextCol.EndsWith("123"));
        Assert.AreEqual(2, target.Count);
    }

    [Test]
    public void Can_Select_using_Contains()
    {
        var target = Db.Select<TestType>(q => q.TextCol.Contains("df"));
        Assert.AreEqual(2, target.Count);
    }

    [Test]
    public void Can_Selelct_using_chained_string_operations()
    {
        var value = "ASDF";
        var q = Db.From<TestType>();
        q.Where(x => x.TextCol.ToUpper().StartsWith(value));
        var target = Db.Select(q);
        Assert.AreEqual(2, target.Count);
    }

    [Test]
    public void Can_Select_using_object_Array_Contains()
    {
        var vals = new object[]{ TestEnum.Val0, TestEnum.Val1 };

        var q1 = Db.From<TestType>();
        q1.Where(q => vals.Contains(q.EnumCol) || vals.Contains(q.EnumCol));
        var sql1 = q1.ToSelectStatement();

        var q2 = Db.From<TestType>();
        q2.Where(q => Sql.In(q.EnumCol, vals) || Sql.In(q.EnumCol, vals));
        var sql2 = q2.ToSelectStatement();

        Assert.AreEqual(sql1, sql2);
    }

    [Test]
    public void Can_Select_using_int_Array_Contains()
    {
        var vals = new[] { (int)TestEnum.Val0, (int)TestEnum.Val1 };

        var q1 = Db.From<TestType>();
        q1.Where(q => vals.Contains((int)q.EnumCol) || vals.Contains((int)q.EnumCol));
        var sql1 = q1.ToSelectStatement();

        var q2 = Db.From<TestType>();
        q2.Where(q => Sql.In(q.EnumCol, vals) || Sql.In(q.EnumCol, vals));
        var sql2 = q2.ToSelectStatement();

        Assert.AreEqual(sql1, sql2);
    }

    [Test]
    public void Can_Select_using_boolean_constant()
    {
        var q = Db.From<TestType>().Where(x => true);
        var target = Db.Select(q);
        Assert.AreEqual(4, target.Count);

        q = Db.From<TestType>().Where(x => false);
        target = Db.Select(q);
        Assert.AreEqual(0, target.Count);
    }

    [Test]
    public void Can_Select_using_expression_evaluated_to_constant()
    {
        var a = 5;
        var b = 6;
        int? nullableInt = null;

        var q = Db.From<TestType>().Where(x => a < b); // "a < b" is evaluated by SqlExpression (not at compile time!) to ConstantExpression (true)
        var target = Db.Select(q);
        Assert.AreEqual(4, target.Count);

        q = Db.From<TestType>().Where(x => x.NullableIntCol == nullableInt); // Expression evaluated to "null" in SqlExpression
        target = Db.Select(q);
        CollectionAssert.AreEquivalent(new[] { 2 }, target.Select(t => t.Id).ToArray());

        q = Db.From<TestType>().Where(x => nullableInt == x.NullableIntCol); // Same with the null on the left
        target = Db.Select(q);
        CollectionAssert.AreEquivalent(new[] { 2 }, target.Select(t => t.Id).ToArray());

        // Expression = or <> true or false

        q = Db.From<TestType>().Where(x => x.NullableIntCol.HasValue == 5 < 6); // Evaluated to "true" at compile time: equivalent to "x.NullableIntCol != null"
        target = Db.Select(q);
        CollectionAssert.AreEquivalent(new[] { 1, 3, 4 }, target.Select(t => t.Id).ToArray());

        q = Db.From<TestType>().Where(x => x.NullableIntCol.HasValue == 5 > 6); // Evaluated to "false" at compile time: equivalent to "x.NullableIntCol == null"
        target = Db.Select(q);
        CollectionAssert.AreEquivalent(new[] { 2 }, target.Select(t => t.Id).ToArray());

        q = Db.From<TestType>().Where(x => x.NullableIntCol.HasValue != 5 > 6); // != false
        target = Db.Select(q);
        CollectionAssert.AreEquivalent(new[] { 1, 3, 4 }, target.Select(t => t.Id).ToArray());

        q = Db.From<TestType>().Where(x => x.NullableIntCol.HasValue != 5 < 6); // != true
        target = Db.Select(q);
        CollectionAssert.AreEquivalent(new[] { 2 }, target.Select(t => t.Id).ToArray());

        // Same, but with the constant on the left

        q = Db.From<TestType>().Where(x => 5 < 6 == x.NullableIntCol.HasValue);
        target = Db.Select(q);
        CollectionAssert.AreEquivalent(new[] { 1, 3, 4 }, target.Select(t => t.Id).ToArray());

        q = Db.From<TestType>().Where(x => 5 > 6 != x.NullableIntCol.HasValue);
        target = Db.Select(q);
        CollectionAssert.AreEquivalent(new[] { 1, 3, 4 }, target.Select(t => t.Id).ToArray());

        // Same, but with the expression evaluated inside SqlExpression (not at compile time)

        q = Db.From<TestType>().Where(x => x.NullableIntCol.HasValue == a < b);
        target = Db.Select(q);
        CollectionAssert.AreEquivalent(new[] { 1, 3, 4 }, target.Select(t => t.Id).ToArray());
    }

    [Test]
    public void Can_Where_using_filter_with_nested_properties()
    {
        string filterText2 = "2.1";
        string filterText3 = "3.3";
        bool? nullableTrue = true;

        var q = Db.From<TestType>().
            Join<TestType2>().
            Where(x => (!x.NullableBoolCol.HasValue || x.NullableBoolCol.Value) && x.NullableIntCol.HasValue && x.TestType2ObjCol.BoolCol);
        var target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(2));

        var minDate = new DateTime(1900, 01, 01);

        q = Db.From<TestType>().
            Join<TestType2>().
            Where(x => x.TestType2ObjCol.BoolCol && x.DateCol != minDate);
        target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(2));

        q = Db.From<TestType>().
            Join<TestType2>().
            Where(x => x.TestType2ObjCol.BoolCol && x.TestType2ObjCol.BoolCol == nullableTrue &&
                       x.DateCol != minDate && x.TestType2ObjCol.TestType2Name == filterText2);
        target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(1));

        var intValue = 300;
        q = Db.From<TestType>().
            Join<TestType2>().
            Join<TestType2, TestType3>().
            Where(x => !x.NullableBoolCol.HasValue && x.TestType2ObjCol.BoolCol &&
                       x.TestType2ObjCol.TestType3ObjCol.TestType3Name == filterText3 &&
                       x.TestType2ObjCol.TestType3ObjCol.CustomInt == new CustomInt(intValue));
        target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(1));

        q = Db.From<TestType>().
            Join<TestType2>().
            Join<TestType2, TestType3>().
            Where(x => !x.NullableBoolCol.HasValue && x.TestType2ObjCol.BoolCol &&
                       x.NullableIntCol == new CustomInt(10)).
            GroupBy(x => x.TestType2ObjCol.TestType3ObjCol.CustomInt).
            Having(x => (Sql.Max(x.TestType2ObjCol.TestType3ObjCol.CustomInt) ?? 0) == new CustomInt(100)).
            Select(x => x.TestType2ObjCol.TestType3ObjCol.CustomInt);
        target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(1));

        //q = Db.From<TestType>().
        //    Join<TestType2>().
        //    Join<TestType2, TestType3>().
        //    Where(x => !x.NullableBoolCol.HasValue && x.TestType2ObjCol.BoolCol &&
        //               x.NullableIntCol == new CustomInt(10)).
        //    GroupBy(x => x.TestType2ObjCol.TestType3ObjCol.CustomInt).
        //    Having(x => (Sql.Max(x.TestType2ObjCol.TestType3ObjCol.CustomInt) ?? 0) != 10).
        //    Select(x => x.TestType2ObjCol.TestType3ObjCol.CustomInt);
        //target = Db.Select(q);
        //Assert.That(target.Count, Is.EqualTo(1));
        //Assert.That(q.Params[0].Value, Is.EqualTo(10));
        //Assert.That(q.Params[1].Value, Is.EqualTo(0));  //= "{Value:0}"
        //Assert.That(q.Params[2].Value, Is.EqualTo(10));
    }

    [Test]
    public void Can_Where_using_filter_with_ToString()
    {
        string filterText = "10";
        int filterInt = 10;
            
        var q = Db.From<TestType>().Where(x => x.NullableIntCol.ToString() == filterText);
        var target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(1));

        q = Db.From<TestType>().Where(x => x.NullableIntCol.ToString() == null);
        target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(1));

        q = Db.From<TestType>().Where(x => x.NullableIntCol.ToString() == filterInt.ToString());
        target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(1));
            
        q = Db.From<TestType>().Where(x => x.NullableIntCol.ToString() == "NotNumeric");
        target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(0));

        q = Db.From<TestType>().
            Join<TestType2>().
            Where( x => x.NullableIntCol.ToString() == filterText && x.TestType2ObjCol.NullableIntCol.ToString() == filterText);
        target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(1));

        q = Db.From<TestType>().
            Join<TestType2>().
            Where(x => x.NullableIntCol.ToString() == filterText && x.TestType2ObjCol.NullableIntCol.ToString() == "NotNumeric");
        target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(0));

        var filterText2 = "qwer";

        q = Db.From<TestType>().Where(x => x.TextCol.ToString() == filterText2);
        target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(1));

        q = Db.From<TestType>().Where(x => x.NullableIntCol.ToString().EndsWith("0"));
        target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(3));
    }

    [Test]
    public void Can_Where_using_filter_with_Concat()
    {
        string filterText = "asdf";
        int filterInt = 10;

        System.Linq.Expressions.Expression<Func<TestType, bool>> filter = x => String.Concat(x.TextCol, x.NullableIntCol.ToString()) == filterText + filterInt;
        var q = Db.From<TestType>().Where(filter);
        var target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(1));

        filter = x => String.Concat(x.TextCol, ".", x.NullableIntCol.ToString()) == filterText + "." + filterInt;
        q = Db.From<TestType>().Where(filter);
        target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(1));

        filter = x => x.TextCol + "." + x.NullableIntCol.ToString() == filterText + "." + filterInt;
        q = Db.From<TestType>().Where(filter);
        target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(1));
    }

    [Test]
    public void Can_Where_using_constant_filter()
    {
        object left = null;
        var right = PartialSqlString.Null;

        System.Linq.Expressions.Expression<Func<TestType, bool>> filter = x => left == right;
        var q = Db.From<TestType>().Where(filter);//todo: here Where: null is NULL. May be need to change to 1=1 ?
        var target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(4));
    }

    [Test]
    public void Can_Where_using_Conditional_filter()
    {
        System.Linq.Expressions.Expression<Func<TestType, bool>> filter = x => (x.NullableIntCol == null ? 0 : x.NullableIntCol) == 10;
        var q = Db.From<TestType>().Where(filter);
        var target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(1));
    }

    [Test]
    public void Can_Where_using_Bool_Conditional_filter()
    {
        System.Linq.Expressions.Expression<Func<TestType, bool>> filter = x => (x.BoolCol ? x.NullableIntCol : 0) == 10;
        var q = Db.From<TestType>().Where(filter);
        var target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(1));
    }

    [Test]
    public void Can_Where_using_Method_with_Conditional_filter()
    {
        System.Linq.Expressions.Expression<Func<TestType, bool>> filter = x => (x.TextCol == null ? null : x.TextCol).StartsWith("asdf");
        var q = Db.From<TestType>().Where(filter);
        var target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(2));
    }

    [Test]
    public void Can_Where_using_Constant_Conditional_filter()
    {
        var filterConditional = 10;
        System.Linq.Expressions.Expression<Func<TestType, bool>> filter = x => (filterConditional > 50 ? 123456789 : x.NullableIntCol) == 10;
        var q = Db.From<TestType>().Where(filter);
        Assert.That(q.ToSelectStatement(), Does.Not.Contain("123456789"));

        var target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(1));
    }

    [Test]
    public void Can_Where_using_Bool_Constant_Conditional_filter()
    {
        var filterConditional = true;
        System.Linq.Expressions.Expression<Func<TestType, bool>> filter = x => (filterConditional ? x.NullableIntCol : 123456789) == 10;
        var q = Db.From<TestType>().Where(filter);
        Assert.That(q.ToSelectStatement(), Does.Not.Contain("123456789"));

        var target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(1));
    }

    [Test]
    public void Can_Where_using_SqlIn_filter()
    {
        var subQ = Db.From<TestType>().Where(x=>x.NullableIntCol == 10).Select(x=>x.Id);
        var q = Db.From<TestType>();
        q.PrefixFieldWithTableName = true;
        q.Where(x=>Sql.In(x.Id, subQ));

        var target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(1));
    }

    [Test]
    public void Can_Where_using_IfConcat_filter()
    {
        System.Linq.Expressions.Expression<Func<TestType, bool>> filter = x => (String.Concat("Text: ", x.TextCol) == null ? null : String.Concat("Text: ", x.TextCol)).EndsWith("asdf");
        var q = Db.From<TestType>().Where(filter);
        Assert.That(q.ToSelectStatement().ToLower(), Does.Contain("text"));

        var target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(1));
    }

    [Test]
    public void Can_Where_using_IfWithStringConstant_filter()
    {
        System.Linq.Expressions.Expression<Func<TestType, bool>> filter = x => (String.Concat("Text: ", x.TextCol) == null ? " " : String.Concat("Text: ", x.TextCol)).EndsWith("asdf");
        var q = Db.From<TestType>().Where(filter);
        Assert.That(q.ToSelectStatement().ToLower(), Does.Contain("text"));

        var target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(1));
    }

    [Test]
    public void Can_OrderBy_using_isnull()
    {
        System.Linq.Expressions.Expression<Func<TestType, object>> orderBy = x => x.TextCol == null ? x.TextCol : x.NullableIntCol.ToString();
        var q = Db.From<TestType>().OrderBy(orderBy);
        Assert.That(q.ToSelectStatement().ToLower(), Does.Not.Contain("isnull"));

        var target = Db.Select(q);
        Assert.IsTrue(target.Count > 0);
    }

    [Test]
    public void Can_Where_using_Convert()
    {
        var paramExpr = System.Linq.Expressions.Expression.Parameter(typeof(TestType));
        var propExpr = System.Linq.Expressions.Expression.Property(paramExpr, nameof(TestType.TextCol));
        var convert = System.Linq.Expressions.Expression.Convert(propExpr, typeof(object));
        var methodToString = typeof(object).GetMethod(nameof(ToString));
        var toString = System.Linq.Expressions.Expression.Call(convert, methodToString);
        var equal = System.Linq.Expressions.Expression.Equal(toString, System.Linq.Expressions.Expression.Constant("asdf"));
        var lambda = System.Linq.Expressions.Expression.Lambda<System.Func<TestType, bool>>(equal, paramExpr);

        var q = Db.From<TestType>().Where(lambda);

        var target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(1));
    }

    [Test]
    public void Can_Where_using_filter_with_Compare()
    {
        string filterText = "asdf";

        System.Linq.Expressions.Expression<Func<TestType, bool>> filter = x => String.Compare(x.TextCol, filterText) == 0;
        var q = Db.From<TestType>().Where(filter);
        var target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(1));
    }

    [Test]
    public void Can_Where_using_Only_Conditional_filter()
    {
        System.Linq.Expressions.Expression<Func<TestType, bool>> filter = x => (x.NullableBoolCol.HasValue ? false : x.TextCol.Contains("qwer"));
        var q = Db.From<TestType>().Where(filter);
        Assert.That(q.ToSelectStatement().ToLower(), Does.Contain("="));

        var target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(2));
    }

    [Test]
    public void Can_Where_using_Equal_Conditional_filter()
    {
        System.Linq.Expressions.Expression<Func<TestType, bool>> filter = x => ((x.NullableBoolCol.HasValue ? false : x.TextCol.Contains("qwer")) == true);
        var q = Db.From<TestType>().Where(filter);
        Assert.That(q.ToSelectStatement().ToLower(), Does.Contain("="));

        var target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(2));
    }

    [Test]
    public void Can_Where_using_And_Conditional_filter()
    {
        System.Linq.Expressions.Expression<Func<TestType, bool>> filter = x => x.Id > 2 && (x.NullableBoolCol.HasValue ? false : x.TextCol.Contains("qwer"));
        var q = Db.From<TestType>().Where(filter);
        Assert.That(q.ToSelectStatement().ToLower(), Does.Contain("="));

        var target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(2));
    }

    [Test]
    public void Can_Where_using_And_Equal_Conditional_filter()
    {
        System.Linq.Expressions.Expression<Func<TestType, bool>> filter = x => x.Id > 2 && (x.NullableBoolCol.HasValue ? false : x.TextCol.Contains("qwer")) == true;
        var q = Db.From<TestType>().Where(filter);
        Assert.That(q.ToSelectStatement().ToLower(), Does.Contain("="));

        var target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(2));
    }

    [Test]
    public void Can_Where_using_Constant_in_Conditional_filter1()
    {
        var i = 0;
        System.Linq.Expressions.Expression<Func<TestType, bool>> filter = x => i > 0 ? x.BoolCol : x.TextCol.Contains("qwer");
        var q = Db.From<TestType>().Where(filter);
        Assert.That(q.ToSelectStatement().ToLower(), Does.Not.Contain("="));

        var target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(2));
    }

    [Test]
    public void Can_Where_using_Constant_in_Conditional_filter2()
    {
        var i = 10;
        System.Linq.Expressions.Expression<Func<TestType, bool>> filter = x => i > 0 ? x.BoolCol : x.TextCol.Contains("qwer");
        var q = Db.From<TestType>().Where(filter);
        Assert.That(q.ToSelectStatement().ToLower(), Does.Contain("="));

        var target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(2));
    }

    [Test]
    public void Can_Where_using_Constant_in_Conditional_filter3()
    {
        var i = 0;
        var fake = 1;
        System.Linq.Expressions.Expression<Func<TestType, bool>> filter = x => x.Id > 0 ? fake != fake : x.TextCol.Contains("zxcv");
        var q = Db.From<TestType>().Where(filter);
        Assert.That(q.ToSelectStatement().ToLower(), Does.Contain("="));
            
        var target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(0));
    }

    [Test]
    public void Can_Where_using_Constant_in_Conditional_filter4()
    {
        var i = 10;
        var fake = 1;
        System.Linq.Expressions.Expression<Func<TestType, bool>> filter = x => i > 0 ? fake != fake : x.TextCol.Contains("qwer");
        var q = Db.From<TestType>().Where(filter);
        Assert.That(q.ToSelectStatement().ToLower(), Does.Contain("="));

        var target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(0));
    }


    [Test]
    public void Can_Where_using_Constant_in_Conditional_filter5()
    {
        var i = 10;
        var fake = 1;
        System.Linq.Expressions.Expression<Func<TestType, bool>> filter = x => i > 0 ? x.Id == x.Id : x.TextCol.Contains("qwer");
        var q = Db.From<TestType>().Where(filter);
        Assert.That(q.ToSelectStatement().ToLower(), Does.Contain("="));
        Assert.That(q.ToSelectStatement().ToLower(), Does.Not.Contain("=1"));

        var target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(4));
    }

    [Test]
    public void Can_Where_using_Conditional_order1()
    {
        var i = 0;
        var fake = 1;
        System.Linq.Expressions.Expression<Func<TestType, object>> order = x => x.Id > 2 ? x.BoolCol : x.TextCol.Contains("qwer");
        var q = Db.From<TestType>().OrderBy(order).ThenBy(x => x.Id);
        Assert.That(q.ToSelectStatement().ToLower(), Does.Not.Contain("="));

        var target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(4));
        var text = target[0].TextCol;
        Assert.AreEqual("asdf", text);
    }

    [Test]
    public void Can_Where_using_Constant_in_Conditional_order1()
    {
        var i = 0;
        var fake = 1;
        System.Linq.Expressions.Expression<Func<TestType, object>> order = x => i > 0 ? x.BoolCol : x.TextCol.Contains("qwer");
        var q = Db.From<TestType>().OrderBy(order).ThenBy(x => x.Id);
        Assert.That(q.ToSelectStatement().ToLower(), Does.Not.Contain("="));
        Assert.That(q.ToSelectStatement().ToLower(), Does.Contain(" like "));
        Assert.That(q.ToSelectStatement().ToLower(), Does.Contain("case when "));

        var target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(4));
        var text = target[0].TextCol;
        Assert.AreEqual("asdf", text);
    }

    [Test]
    public void Can_Where_using_Constant_in_Conditional_order2()
    {
        var i = 10;
        var fake = 1;
        System.Linq.Expressions.Expression<Func<TestType, object>> order = x => i > 0 ? x.BoolCol : x.TextCol.Contains("qwer");
        var q = Db.From<TestType>().OrderBy(order).ThenBy(x => x.Id);
        Assert.That(q.ToSelectStatement().ToLower(), Does.Not.Contain("="));
        Assert.That(q.ToSelectStatement().NormalizeSql(), Does.Contain("order by boolcol"));

        var target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(4));
        var text = target[0].TextCol;
        Assert.AreEqual("qwer", text);
    }

    [Test]
    public void Can_Where_using_Constant_in_Conditional_order3()
    {
        var i = 0;
        var fake = 1;
        System.Linq.Expressions.Expression<Func<TestType, object>> order = x => i > 0 ? false : x.TextCol.Contains("qwer");
        var q = Db.From<TestType>().OrderBy(order).ThenBy(x => x.Id);
        Assert.That(q.ToSelectStatement().ToLower(), Does.Not.Contain("="));
        Assert.That(q.ToSelectStatement().ToLower(), Does.Contain(Db.GetDialectProvider().ParamString + "0"));
        Assert.That(q.ToSelectStatement().ToLower(), Does.Contain("case when "));

        var target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(4));
        var text = target[0].TextCol;
        Assert.AreEqual("asdf", text);
    }

    [Test]
    public void Can_Where_using_Constant_in_Conditional_order4()
    {
        var i = 10;
        var fake = 1;
        System.Linq.Expressions.Expression<Func<TestType, object>> order = x => i > 0 ? false : x.TextCol.Contains("qwer");
        var q = Db.From<TestType>().OrderBy(order).ThenBy(x => x.Id);
        Assert.That(q.ToSelectStatement().ToLower(), Does.Not.Contain("="));
        Assert.That(q.ToSelectStatement().ToLower(), Does.Not.Contain(Db.GetDialectProvider().ParamString + "0"));

        var target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(4));
        var text = target[0].TextCol;
        Assert.AreEqual("asdf", text);
    }

    [Test]
    public void Can_Where_using_Constant_in_Conditional_order5()
    {
        var i = 0;
        System.Linq.Expressions.Expression<Func<TestType, object>> order = x => i > 0 ? x.TextCol : "";
        var q = Db.From<TestType>().OrderBy(order).ThenBy(x => x.Id);
        Assert.That(q.ToSelectStatement().ToLower(), Does.Not.Contain("="));
        Assert.That(q.ToSelectStatement().ToLower(), Does.Not.Contain(Db.GetDialectProvider().ParamString + "0"));
        Assert.That(q.ToSelectStatement().ToLower().NormalizeSql(), Does.Contain("order by id")); 

        var target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(4));
        var text = target[0].TextCol;
        Assert.AreEqual("asdf", text);
    }

    [Test]
    public void Can_Where_using_Constant_in_Conditional_order6()
    {
        var i = 10;
        System.Linq.Expressions.Expression<Func<TestType, object>> order = x => i > 0 ? x.TextCol : "www";
        var q = Db.From<TestType>().OrderBy(order).ThenBy(x => x.Id);
        Assert.That(q.ToSelectStatement().ToLower(), Does.Not.Contain("="));
        Assert.That(q.ToSelectStatement().ToLower(), Does.Not.Contain("@0"));

        var target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(4));
        var text = target[0].TextCol;
        Assert.AreEqual("asdf", text);
    }

    [Test]
    public void Can_Where_using_Constant_in_Conditional_order7()
    {
        var i = 10;
        System.Linq.Expressions.Expression<Func<TestType, object>> order = x => x.Id > 0 ? x.TextCol : "www";
        var q = Db.From<TestType>().OrderBy(order).ThenBy(x => x.Id);
        Assert.That(q.ToSelectStatement().ToLower(), Does.Not.Contain("="));
        Assert.That(q.ToSelectStatement().ToLower(), Does.Contain(Db.GetDialectProvider().ParamString + "0"));

        var target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(4));
        var text = target[0].TextCol;
        Assert.AreEqual("asdf", text);
    }

    [Test]
    public void Can_Where_using_StaticInsideNonStaticMethod()
    {
        System.Linq.Expressions.Expression<Func<TestType, bool>> filter = x => String.Concat(x.TextCol, "test").StartsWith("asdf");
        var q = Db.From<TestType>().Where(filter).OrderBy(x => x.Id);
        Assert.That(q.ToSelectStatement().ToLower(), Does.Not.Contain(SqlExpression<TestType>.TrueLiteral));

        var target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(2));
        var text = target[0].TextCol;
        Assert.AreEqual("asdf", text);
    }

    [Test]
    public void Can_Where_using_StringLengthProperty1()
    {
        System.Linq.Expressions.Expression<Func<TestType, bool>> filter = x => x.TextCol.Length == 4;
        var q = Db.From<TestType>().Where(filter).OrderBy(x => x.Id);

        var target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(2));
        var text = target[0].TextCol;
        Assert.AreEqual("asdf", text);
    }

    [Test]
    public void Can_Where_using_StringLengthProperty2()
    {
        System.Linq.Expressions.Expression<Func<TestType, bool>> filter = x => x.TextCol.Length == 0;
        var q = Db.From<TestType>().Where(filter).OrderBy(x => x.Id);

        var target = Db.Select(q);
        Assert.That(target.Count, Is.EqualTo(0));
    }
    
    [Test]
    public void Can_evaluate_invoke_expression()
    {
        System.Func<string> getValueWithFunc = () => "val";
        System.Func<int, string> getValue1WithFunc = (id) => "val" + id;

        var q = Db.From<TestType>().Where(t => t.TextCol.Contains(getValueWithFunc()));
        var sql = q.ToMergedParamsSelectStatement();
        Assert.True(!sql.Contains("%Invoke"));
                
        q = Db.From<TestType>().Where(t => t.TextCol.Contains(getValue1WithFunc(1)));
        sql = q.ToMergedParamsSelectStatement();
        Assert.True(!sql.Contains("%Invoke"));
    }

    private int MethodReturningInt(int val)
    {
        return val;
    }

    private int MethodReturningInt()
    {
        return 1;
    }

    private TestEnum MethodReturningEnum()
    {
        return TestEnum.Val0;
    }
}

public enum TestEnum
{
    Val0 = 0,
    Val1,
    Val2,
    Val3
}

public class TestType
{
    public int Id { get; set; }
    public string TextCol { get; set; }
    public bool BoolCol { get; set; }
    public bool? NullableBoolCol { get; set; }
    public DateTime DateCol { get; set; }
    public TestEnum EnumCol { get; set; }
    public TestType ComplexObjCol { get; set; }
    public int? NullableIntCol { get; set; }

    [ForeignKey(typeof(TestType2))]
    public int TestType2ObjColId { get; set; }
    public TestType2 TestType2ObjCol { get; set; }
}

public class TestType2
{
    public int Id { get; set; }
    public string TextCol { get; set; }
    public bool BoolCol { get; set; }
    public bool? NullableBoolCol { get; set; }
    public DateTime DateCol { get; set; }
    public TestEnum EnumCol { get; set; }
    public TestType ComplexObjCol { get; set; }
    public int? NullableIntCol { get; set; }
    public string TestType2Name { get; set; }

    [ForeignKey(typeof(TestType3))]
    public int TestType3ObjColId { get; set; }
    public TestType3 TestType3ObjCol { get; set; }
}

public class TestType3
{
    public int Id { get; set; }
    public string TextCol { get; set; }
    public bool BoolCol { get; set; }
    public bool? NullableBoolCol { get; set; }
    public DateTime DateCol { get; set; }
    public TestEnum EnumCol { get; set; }
    public TestType3 ComplexObjCol { get; set; }
    public int? NullableIntCol { get; set; }
    public string TestType3Name { get; set; }
    public CustomInt CustomInt { get; set; }
}

/// <summary>
/// For testing VisitUnary with expression "u" that: u.Method != null (implicit conversion)
/// </summary>
public class CustomInt
{
    private readonly int _value;

    public CustomInt(int value)
    {
        _value = value;
    }

    public int Value
    {
        get { return _value; }
    }


    public static implicit operator int(CustomInt s)
    {
        return s.Value;
    }

    public static implicit operator CustomInt(int s)
    {
        return new CustomInt(s);
    }

}