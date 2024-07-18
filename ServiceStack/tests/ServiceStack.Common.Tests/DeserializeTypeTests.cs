using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.Text.Common;
using ServiceStack.Text.Json;

namespace ServiceStack.Common.Tests;

[TestFixture]
public class DeserializeTypeTests
{
    [Test]
    public void UnknownType_Returns_Null()
    {
        const string typeName =
            @"{""__type"": ""ServiceStack.Common.Tests.DeserializeTypeTests+InnerStub2, ServiceStack.Common.Tests""}";
        var type = DeserializeType<JsonTypeSerializer>.ExtractType(typeName);
        Assert.That(type, Is.Null);
    }

    [Test]
    public void KnownType_Returns_TheType()
    {
        const string typeName =
            @"{""__type"": ""ServiceStack.Common.Tests.DeserializeTypeTests+InnerStub, ServiceStack.Common.Tests""}";
        var type = DeserializeType<JsonTypeSerializer>.ExtractType(typeName);
        Assert.That(type == typeof(InnerStub));
    }
    
    [Test]
    public void ArrayThatContainsUnknownType_ShouldContainNullItem()
    {
        const string json =
            @"{""Stubs"": [{""__type"": ""ServiceStack.Common.Tests.DeserializeTypeTests+InnerStub2, ServiceStack.Common.Tests""}]}";
        var stub = JsonSerializer.DeserializeFromString<Stub>(json);
        Assert.That(stub.Stubs, Is.Not.Null);
        Assert.That(stub.Stubs.Count, Is.EqualTo(1));
        Assert.That(stub.Stubs[0], Is.Null);
    }
    
    [Test]
    public void ArrayThatContainsKnownType_ShouldContainItem()
    {
        const string json =
            @"{""Stubs"": [{""__type"": ""ServiceStack.Common.Tests.DeserializeTypeTests+InnerStub, ServiceStack.Common.Tests""}]}";
        var stub = JsonSerializer.DeserializeFromString<Stub>(json);
        Assert.That(stub.Stubs, Is.Not.Null);
        Assert.That(stub.Stubs.Count, Is.EqualTo(1));
        Assert.That(stub.Stubs[0], Is.Not.Null);
    }

    private interface IInnerStub { }
    
    [Serializable]
    private class Stub
    {
        public List<IInnerStub> Stubs { get; set; }
    }

    [Serializable]
    private class InnerStub : IInnerStub
    {
    }
}