using System.Linq;
using System.Runtime.Serialization;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.MySql.Tests;

public class EnumTests : OrmLiteTestBase
{
    [Test]
    public void CanCreateTable()
    {
        OpenDbConnection().CreateTable<TypeWithEnum>(true);
    }

    [Test]
    public void CanStoreEnumValue()
    {
        using (var con = OpenDbConnection())
        {
            con.CreateTable<TypeWithEnum>(true);
            con.Save(new TypeWithEnum { Id = 1, EnumValue = SomeEnum.Value1 });
        }
    }

    [Test]
    public void CanGetEnumValue()
    {
        using (var con = OpenDbConnection())
        {
            con.CreateTable<TypeWithEnum>(true);
            var obj = new TypeWithEnum { Id = 1, EnumValue = SomeEnum.Value1 };
            con.Save(obj);
            var target = con.SingleById<TypeWithEnum>(obj.Id);
            Assert.AreEqual(obj.Id, target.Id);
            Assert.AreEqual(obj.EnumValue, target.EnumValue);
        }
    }

    [Test]
    public void CanCreateTableNullableEnum()
    {
        OpenDbConnection().CreateTable<TypeWithNullableEnum>(true);
    }

    [Test]
    public void CanStoreNullableEnumValue()
    {
        using (var con = OpenDbConnection())
        {
            con.CreateTable<TypeWithNullableEnum>(true);
            con.Save(new TypeWithNullableEnum { Id = 1, EnumValue = SomeEnum.Value1 });
        }
    }

    [Test]
    public void CanGetNullableEnumValue()
    {
        using (var con = OpenDbConnection())
        {
            con.CreateTable<TypeWithNullableEnum>(true);
            var obj = new TypeWithNullableEnum { Id = 1, EnumValue = SomeEnum.Value1 };
            con.Save(obj);
            var target = con.SingleById<TypeWithNullableEnum>(obj.Id);
            Assert.AreEqual(obj.Id, target.Id);
            Assert.AreEqual(obj.EnumValue, target.EnumValue);
        }
    }

    [Test]
    public void CanQueryByEnumValue_using_select_with_expression()
    {
        using (var con = OpenDbConnection())
        {
            con.CreateTable<TypeWithEnum>(true);
            con.Save(new TypeWithEnum { Id = 1, EnumValue = SomeEnum.Value1 });
            con.Save(new TypeWithEnum { Id = 2, EnumValue = SomeEnum.Value1 });
            con.Save(new TypeWithEnum { Id = 3, EnumValue = SomeEnum.Value2 });

            var target = con.Select<TypeWithEnum>(q => q.EnumValue == SomeEnum.Value1);

            Assert.AreEqual(2, target.Count());
        }
    }

    [Test]
    public void CanQueryByEnumValue_using_select_with_string()
    {
        using (var con = OpenDbConnection())
        {
            con.CreateTable<TypeWithEnum>(true);
            con.Save(new TypeWithEnum { Id = 1, EnumValue = SomeEnum.Value1 });
            con.Save(new TypeWithEnum { Id = 2, EnumValue = SomeEnum.Value1 });
            con.Save(new TypeWithEnum { Id = 3, EnumValue = SomeEnum.Value2 });

            var target = con.Select<TypeWithEnum>("EnumValue = @value", new { value = SomeEnum.Value1 });

            Assert.AreEqual(2, target.Count());
        }
    }

    [Test]
    public void CanQueryByEnumValue_using_where_with_AnonType()
    {
        using (var con = OpenDbConnection())
        {
            con.CreateTable<TypeWithEnum>(true);
            con.Save(new TypeWithEnum { Id = 1, EnumValue = SomeEnum.Value1 });
            con.Save(new TypeWithEnum { Id = 2, EnumValue = SomeEnum.Value1 });
            con.Save(new TypeWithEnum { Id = 3, EnumValue = SomeEnum.Value2 });

            var target = con.Where<TypeWithEnum>(new { EnumValue = SomeEnum.Value1 });

            Assert.AreEqual(2, target.Count());
        }
    }

    [Test]
    public void CanQueryByNullableEnumValue_using_where_with_AnonType()
    {
        using (var con = OpenDbConnection())
        {
            con.CreateTable<TypeWithEnum>(true);
            con.Save(new TypeWithNullableEnum { Id = 1, EnumValue = SomeEnum.Value1, IntEnum = SomeIntEnum.One });
            con.Save(new TypeWithNullableEnum { Id = 2, EnumValue = SomeEnum.Value1, IntEnum = SomeIntEnum.One });
            con.Save(new TypeWithNullableEnum { Id = 3, EnumValue = SomeEnum.Value2, IntEnum = SomeIntEnum.Two });

            var target = con.Where<TypeWithNullableEnum>(new { EnumValue = SomeEnum.Value1 });
            var enumInt = con.Where<TypeWithNullableEnum>(new { IntEnum = SomeIntEnum.One });

            Assert.AreEqual(2, target.Count);
            Assert.AreEqual(2, enumInt.Count);
        }
    }

    [Test]
    public void CanSaveNullableEnum_with_specific_id_select_with_anon_type()
    {
        using (var db = OpenDbConnection())
        {
            db.DropAndCreateTable<MyObj>();
            var myObj = new MyObj();
            myObj.Id = 1;
            myObj.Test = MyEnum.One;
            db.Insert(myObj);

            myObj = db.Single<MyObj>(new { Id = 1 });

            Assert.That(myObj.Id, Is.EqualTo(1));
            Assert.That(myObj.Test, Is.Not.EqualTo(null));
            Assert.That(myObj.Test, Is.EqualTo(MyEnum.One));
        }
    }

    [Test]
    public void CanSaveNullableEnum_with_specific_id_select_with_type()
    {
        using (var db = OpenDbConnection())
        {
            db.DropAndCreateTable<MyObj>();
            var exists = db.SingleById<MyObj>(1);
            if (exists != null)
            {
                db.DeleteById<MyObj>(1);
            }
        }

        using (var con = OpenDbConnection())
        {

            var myObj = new MyObj();
            myObj.Id = 1;
            myObj.Test = MyEnum.One;
            con.Insert(myObj);

            myObj = con.Single<MyObj>(x => x.Id == 1);

            Assert.That(myObj.Id, Is.EqualTo(1));
            Assert.That(myObj.Test, Is.Not.EqualTo(null));
            Assert.That(myObj.Test, Is.EqualTo(MyEnum.One));
        }
    }

    [Test]
    public void ShouldUseEnumName_in_Sql_TableAlias()
    {
        using var db = OpenDbConnection();
        var q = db.From<EntityMonitor>();
        var stateType = StateType.完好;
        q.Or<ChnStateResult>((x) => Sql.TableAlias(x.State, nameof(LinkResult)) == StateType.离线);
        q.Or<ChnStateResult>((x) => x.State == StateType.离线);
        q.Or<ChnStateResult>((x) => Sql.TableAlias(x.State, nameof(LinkResult)) == stateType);
        q.And<ChnStateResult>((x) => Sql.TableAlias(x.Age, nameof(LinkResult)) > 18);
        q.And<ChnStateResult>((x) => Sql.TableAlias(x.SomeIntEnum, nameof(LinkResult)) == SomeIntEnum.One);
        q.And<ChnStateResult>((x) => x.SomeIntEnum== SomeIntEnum.One);

        var sql = q.ToMergedParamsSelectStatement();
        Assert.That(q.Params[0].Value, Is.EqualTo(nameof(StateType.离线))); 
        Assert.That(q.Params[1].Value, Is.EqualTo(nameof(StateType.离线))); 
        Assert.That(q.Params[2].Value, Is.EqualTo(nameof(StateType.完好))); 
        Assert.That(q.Params[3].Value, Is.EqualTo(18)); 
        Assert.That(q.Params[4].Value, Is.EqualTo((int)SomeIntEnum.One)); 
        Assert.That(q.Params[5].Value, Is.EqualTo((int)SomeIntEnum.One)); 
    }
}

public class EntityMonitor
{
    [Alias("文1")]
    public StateType State { get; set; }

    public int Age { get; set; }

    public SomeIntEnum SomeIntEnum { get; set; }
}

public class LinkResult : EntityMonitor
{

}

public class ChnStateResult : EntityMonitor
{

}

public enum StateType
{
    完好,
    离线
}

public enum SomeEnum : long
{
    Value1 = 2147483648,
    Value2,
    Value3
}

[EnumAsInt]
public enum SomeIntEnum
{
    Zero = 0,
    One = 1,
    Two = 2
}

[EnumAsInt]
public enum MyEnum : int
{
    Zero = 0,
    One = 1
}

public class MyObj
{
    public int Id { get; set; }
    public MyEnum? Test { get; set; }
}

public class TypeWithEnum
{
    public int Id { get; set; }
    public SomeEnum EnumValue { get; set; }
    public SomeIntEnum IntEnum { get; set; }
}

public class TypeWithNullableEnum
{
    public int Id { get; set; }
    public SomeEnum? EnumValue { get; set; }
    public SomeIntEnum? IntEnum { get; set; }
}