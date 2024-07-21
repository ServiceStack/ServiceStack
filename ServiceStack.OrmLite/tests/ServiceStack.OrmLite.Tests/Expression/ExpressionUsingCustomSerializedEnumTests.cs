using System;
using System.Collections.Generic;
using System.Data;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Expression;

public enum ActivityType
{
    Unknown = 0,
    Unspecified,
    HavingFun,
    Working
}

public class Activity
{
    [AutoIncrement]
    public int Id { get; set; }
    public ActivityType ActivityType { get; set; }
    public string Comment { get; set; }
}

public class NullableActivity
{
    [AutoIncrement]
    public int Id { get; set; }
    public ActivityType? ActivityType { get; set; }
    public string Comment { get; set; }
}

[TestFixtureOrmLite]
public class ExpressionUsingCustomSerializedEnumTests(DialectContext context) : ExpressionsTestBase(context)
{
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [IgnoreDialect(Tests.Dialect.AnyOracle, "Can't work on Oracle because Oracle does not allow empty strings in a varchar column")]
    public void Can_select_on_custom_default_null_serialized_enum(int index)
    {
        EnumSerializerWithNullDefaults.Configure();

        using (var db = OpenDbConnection())
        {
            var expected = Init(db, index);

            var unknownActivities = db.Select<Activity>(
                s => s.ActivityType == expected.ActivityType
                     && s.Comment == expected.Comment);

            Assert.That(unknownActivities.Count, Is.EqualTo(1));
        }
    }

    private static Activity Init(IDbConnection db, int index)
    {
        db.DropAndCreateTable<Activity>();

        var activities = new []
        {
            new Activity { ActivityType = ActivityType.Unknown, Comment = "know nothing about this" },
            new Activity { ActivityType = ActivityType.Unspecified, Comment = "know we don't know about this" },
            new Activity { ActivityType = ActivityType.HavingFun, Comment = "want to be doing this" }
        };
        db.InsertAll(activities);
        return activities[index];
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [IgnoreDialect(Tests.Dialect.AnyOracle, "Can't work on Oracle because Oracle does not allow empty strings in a varchar column")]
    public void Can_select_on_custom_default_empty_serialized_enum(int index)
    {
        EnumSerializerWithEmptyDefaults.Configure();

        using (var db = OpenDbConnection())
        {
            var expected = Init(db, index);

            var unknownActivities = db.Select<Activity>(
                s => s.ActivityType == expected.ActivityType
                     && s.Comment == expected.Comment);

            Assert.That(unknownActivities.Count, Is.EqualTo(1));
        }
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void Can_select_on_custom_prefix_serialized_enum(int index)
    {
        EnumSerializerWithPrefixing.Configure();

        using (var db = OpenDbConnection())
        {
            var expected = Init(db, index);

            var unknownActivities = db.Select<Activity>(
                s => s.ActivityType == expected.ActivityType
                     && s.Comment == expected.Comment);

            Assert.That(unknownActivities.Count, Is.EqualTo(1));
        }
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void Can_select_on_custom_default_null_serialized_nullable_enum(int index)
    {
        EnumSerializerWithNullDefaults.Configure();

        using (var db = OpenDbConnection())
        {
            var expected = InitNullable(db, index);

            var unknownActivities = db.Select<NullableActivity>(
                s => s.ActivityType == expected.ActivityType
                     && s.Comment == expected.Comment);

            Assert.That(unknownActivities.Count, Is.EqualTo(1));
        }
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void Can_select_on_custom_default_empty_serialized_nullable_enum(int index)
    {
        EnumSerializerWithEmptyDefaults.Configure();

        using (var db = OpenDbConnection())
        {
            var expected = InitNullable(db, index);

            var unknownActivities = db.Select<NullableActivity>(
                s => s.ActivityType == expected.ActivityType
                     && s.Comment == expected.Comment);

            Assert.That(unknownActivities.Count, Is.EqualTo(1));
        }
    }

    private static NullableActivity InitNullable(IDbConnection db, int index)
    {
        db.DropAndCreateTable<NullableActivity>();

        var activities = new[]
        {
            new NullableActivity {ActivityType = ActivityType.Unknown, Comment = "know nothing about this"},
            new NullableActivity {ActivityType = ActivityType.Unspecified, Comment = "know we don't know about this"},
            new NullableActivity {ActivityType = ActivityType.HavingFun, Comment = "want to be doing this"}
        };
        db.InsertAll(activities);
        return activities[index];
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void Can_select_on_custom_prefix_serialized_nullable_enum(int index)
    {
        EnumSerializerWithPrefixing.Configure();

        using (var db = OpenDbConnection())
        {
            var expected = InitNullable(db, index);

            var unknownActivities = db.Select<NullableActivity>(
                s => s.ActivityType == expected.ActivityType
                     && s.Comment == expected.Comment);

            Assert.That(unknownActivities.Count, Is.EqualTo(1));
        }
    }
}

public class EnumSerializerWithNullDefaults
{
    public static void Configure()
    {
        if (DefaultEnumValues.Count > 0)
            return;

        var type = typeof(ActivityType);
        InvokeStaticGenericMethod(type, "ConfigureEnumSerialization");
    }

    private static void InvokeStaticGenericMethod(Type genericType, string methodName)
    {
        InvokeGenericMethod(genericType, methodName, null);
    }

    private static void InvokeGenericMethod(Type genericType, string methodName, object obj)
    {
        typeof(EnumSerializerWithNullDefaults).GetMethod(methodName).MakeGenericMethod(genericType).Invoke(obj, null);
    }

    public static void ConfigureEnumSerialization<TEnum>()
    {
        DefaultEnumValues.Add(typeof(TEnum), GetDefault<TEnum>());
        JsConfig<TEnum>.SerializeFn = NonDefaultSerializer;
        JsConfig<TEnum>.DeSerializeFn = NonDefaultDeSerializer<TEnum>;
    }

    private static readonly Dictionary<Type, object> DefaultEnumValues = new Dictionary<Type, object>();

    private static string NonDefaultSerializer<TEnum>(TEnum value)
    {
        return value.Equals(DefaultEnumValues[typeof(TEnum)]) ? null : value.ToString();
    }

    private static TEnum NonDefaultDeSerializer<TEnum>(string value)
    {
        return (String.IsNullOrEmpty(value) ? (TEnum)DefaultEnumValues[typeof(TEnum)] : (TEnum)Enum.Parse(typeof(TEnum), value, true));
    }

    private static T GetDefault<T>()
    {
        return default(T);
    }
}

public class EnumSerializerWithEmptyDefaults
{
    public static void Configure()
    {
        if (DefaultEnumValues.Count > 0)
            return;

        var type = typeof(ActivityType);
        InvokeStaticGenericMethod(type, "ConfigureEnumSerialization");
    }

    private static void InvokeStaticGenericMethod(Type genericType, string methodName)
    {
        InvokeGenericMethod(genericType, methodName, null);
    }

    private static void InvokeGenericMethod(Type genericType, string methodName, object obj)
    {
        typeof(EnumSerializerWithEmptyDefaults).GetMethod(methodName).MakeGenericMethod(genericType).Invoke(obj, null);
    }

    public static void ConfigureEnumSerialization<TEnum>()
    {
        DefaultEnumValues.Add(typeof(TEnum), GetDefault<TEnum>());
        JsConfig<TEnum>.SerializeFn = NonDefaultSerializer;
        JsConfig<TEnum>.DeSerializeFn = NonDefaultDeSerializer<TEnum>;
    }

    private static readonly Dictionary<Type, object> DefaultEnumValues = new Dictionary<Type, object>();

    private static string NonDefaultSerializer<TEnum>(TEnum value)
    {
        return value.Equals(DefaultEnumValues[typeof(TEnum)]) ? "" : value.ToString();
    }

    private static TEnum NonDefaultDeSerializer<TEnum>(string value)
    {
        return (String.IsNullOrEmpty(value) ? (TEnum)DefaultEnumValues[typeof(TEnum)] : (TEnum)Enum.Parse(typeof(TEnum), value, true));
    }

    private static T GetDefault<T>()
    {
        return default(T);
    }
}

public class EnumSerializerWithPrefixing
{
    public static void Configure()
    {
        var type = typeof(ActivityType);
        InvokeStaticGenericMethod(type, "ConfigureEnumSerialization");
    }

    private static void InvokeStaticGenericMethod(Type genericType, string methodName)
    {
        InvokeGenericMethod(genericType, methodName, null);
    }

    private static void InvokeGenericMethod(Type genericType, string methodName, object obj)
    {
        typeof(EnumSerializerWithPrefixing).GetMethod(methodName).MakeGenericMethod(genericType).Invoke(obj, null);
    }

    public static void ConfigureEnumSerialization<TEnum>()
    {
        JsConfig<TEnum>.SerializeFn = Serializer;
        JsConfig<TEnum>.DeSerializeFn = Deserializer<TEnum>;
    }

    private const string Prefix = "_prefix_";

    private static string Serializer<TEnum>(TEnum value)
    {
        return Prefix + value;
    }

    private static TEnum Deserializer<TEnum>(string value)
    {
        return (TEnum)Enum.Parse(typeof(TEnum), value.Replace(Prefix, ""), true);
    }
}