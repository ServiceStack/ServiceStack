using System;
using System.Data;
using System.Runtime.Serialization;
using NUnit.Framework;

namespace ServiceStack.Text.Tests;

[NonParallelizable]
public class RuntimeTypeWhitelistTests
{
    [SetUp]
    public void SetUp() => JsConfig.Reset();

    [TearDown]
    public void TearDown() => JsConfig.Reset();

    [TestCase(typeof(DataSet))]
    [TestCase(typeof(DataTable))]
    [TestCase(typeof(SerializableOnly))]
    public void Rejects_runtime_types_authorized_only_by_ISerializable(Type type)
    {
        var json = CreateJson(type);
        var jsv = $"{{__type:\"{JsConfig.TypeWriter(type)}\"}}";

        AssertRejected(type, () => json.FromJson<object>());
        AssertRejected(type, () => jsv.FromJsv<object>());
    }

    [TestCase(typeof(DataSet))]
    [TestCase(typeof(DataTable))]
    public void Rejects_runtime_types_in_object_properties(Type type)
    {
        var json = "{\"Object\":" + CreateJson(type) + "}";

        AssertRejected(type, () => json.FromJson<RuntimeObject>());
    }

    [TestCase(typeof(DataSet))]
    [TestCase(typeof(DataTable))]
    public void Rejects_runtime_types_in_dictionary_properties(Type type)
    {
        var json = "{\"Dictionary\":{\"value\":" + CreateJson(type) + "}}";

        AssertRejected(type, () => json.FromJson<JsonType>());
    }

    [TestCase(typeof(DtoType))]
    [TestCase(typeof(RuntimeSerializableType))]
    public void Allows_runtime_types_with_serialization_attributes(Type type)
    {
        var json = "{\"Object\":" + CreateJson(type) + "}";

        Assert.That(json.FromJson<RuntimeObject>().Object, Is.TypeOf(type));
    }

    [Test]
    public void Allows_explicitly_typed_deserialization()
    {
        var dataSet = "{\"DataSetName\":\"Example\"}".FromJson<DataSet>();
        var dataTable = "{\"TableName\":\"Example\"}".FromJson<DataTable>();

        Assert.That(dataSet.DataSetName, Is.EqualTo("Example"));
        Assert.That(dataTable.TableName, Is.EqualTo("Example"));
    }

    [Test]
    public void Can_opt_in_to_ISerializable_and_Reset_restores_defaults()
    {
        var json = CreateJson(typeof(SerializableOnly));

        JsConfig.AllowRuntimeTypeWithInterfacesNamed.Add("ISerializable");
        Assert.That(json.FromJson<object>(), Is.TypeOf<SerializableOnly>());

        JsConfig.Reset();

        Assert.That(JsConfig.AllowRuntimeTypeWithInterfacesNamed, Does.Not.Contain("ISerializable"));
        AssertRejected(typeof(SerializableOnly), () => json.FromJson<object>());
    }

    static string CreateJson(Type type) => $"{{\"__type\":\"{JsConfig.TypeWriter(type)}\"}}";

    static void AssertRejected(Type type, TestDelegate deserialize) =>
        Assert.That(Assert.Throws<NotSupportedException>(deserialize).Message,
            Does.StartWith($"{type.Name} is not an allowed Runtime Type"));

    public class SerializableOnly : ISerializable
    {
        public void GetObjectData(SerializationInfo info, StreamingContext context) { }
    }
}
