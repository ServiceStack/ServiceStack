using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using ServiceStack.Formats;
using ServiceStack.Testing;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests;

public class TestXmlDto
{
    public int Id { get; set; }
    public string Name { get; set; }
}

[TestFixture]
public class FormatsModernizationTests
{
    [Test]
    public void XmlSerializerFormat_RoundTrip_Works()
    {
        var dto = new TestXmlDto { Id = 42, Name = "Modernized" };
        using var ms = new MemoryStream();
        var mockReq = new MockHttpRequest();

        XmlSerializerFormat.Serialize(mockReq, dto, ms);
        Assert.That(ms.Length, Is.GreaterThan(0));

        ms.Position = 0;
        var deserialized = XmlSerializerFormat.Deserialize(typeof(TestXmlDto), ms) as TestXmlDto;

        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized.Id, Is.EqualTo(42));
        Assert.That(deserialized.Name, Is.EqualTo("Modernized"));
    }

    [Test]
    public void XmlSerializerFormat_NullGuards()
    {
        using var ms = new MemoryStream();
        var mockReq = new MockHttpRequest();

        Assert.DoesNotThrow(() => XmlSerializerFormat.Serialize(mockReq, null, ms));
        Assert.That(ms.Length, Is.EqualTo(0));

        Assert.DoesNotThrow(() => XmlSerializerFormat.Serialize(mockReq, new TestXmlDto(), null));
        Assert.DoesNotThrow(() => XmlSerializerFormat.Serialize(null, new TestXmlDto(), ms));

        Assert.That(XmlSerializerFormat.Deserialize(null, ms), Is.Null);
        Assert.That(XmlSerializerFormat.Deserialize(typeof(TestXmlDto), null), Is.Null);
    }

    [Test]
    public void HtmlFormat_EncodeForJavaScriptString_EscapesDangerousCharacters()
    {
        var input = "</script><script>alert('xss & \"test\"')</script>";
        var encoded = HtmlFormat.EncodeForJavaScriptString(input);

        Assert.That(encoded, Does.Not.Contain("<"));
        Assert.That(encoded, Does.Not.Contain(">"));
        Assert.That(encoded, Does.Not.Contain("&"));
        Assert.That(encoded, Does.Contain("\\u003c"));
        Assert.That(encoded, Does.Contain("\\u003e"));
        Assert.That(encoded, Does.Contain("\\u0026"));

        Assert.That(HtmlFormat.EncodeForJavaScriptString(null), Is.EqualTo(string.Empty));
    }

    [Test]
    public void HtmlFormat_ReplaceTokens_ReplacesDefaultProfileUrl()
    {
        var template = "var profile = '${NoProfileImgUrl}';";
        var mockReq = new MockHttpRequest();
        var result = HtmlFormat.ReplaceTokens(template, mockReq);

        Assert.That(result, Does.Contain(JwtClaimTypes.DefaultProfileUrl));
        Assert.That(result, Does.Not.Contain("${NoProfileImgUrl}"));
    }

    [Test]
    public void CsvFormat_SerializeToStream_NullGuards()
    {
        var csvFormat = new CsvFormat();
        var mockReq = new MockHttpRequest();
        Assert.DoesNotThrow(() => csvFormat.SerializeToStream(mockReq, new TestXmlDto { Id = 1 }, null));
        using var ms = new MemoryStream();
        Assert.DoesNotThrow(() => csvFormat.SerializeToStream(mockReq, null, ms));
        Assert.That(ms.Length, Is.EqualTo(0));
    }

    [Test]
    public void JsonlFormat_SerializeToStream_NullGuards()
    {
        var jsonlFormat = new JsonlFormat();
        var mockReq = new MockHttpRequest();
        Assert.DoesNotThrow(() => jsonlFormat.SerializeToStream(mockReq, new TestXmlDto { Id = 1 }, null));
        using var ms = new MemoryStream();
        Assert.DoesNotThrow(() => jsonlFormat.SerializeToStream(mockReq, null, ms));
        Assert.That(ms.Length, Is.EqualTo(0));
    }

    [Test]
    public void ContentTypes_GetFormatContentType_HandlesNullAndFormats()
    {
        var contentTypes = new Host.ContentTypes();
        Assert.That(contentTypes.GetFormatContentType(null), Is.Null);
        Assert.That(contentTypes.GetFormatContentType(""), Is.Null);
        Assert.That(contentTypes.GetFormatContentType("json"), Is.EqualTo(MimeTypes.Json));
        Assert.That(contentTypes.GetFormatContentType("xml"), Is.EqualTo(MimeTypes.Xml));
        Assert.That(contentTypes.GetFormatContentType("csv"), Is.EqualTo(MimeTypes.Csv));
        Assert.That(contentTypes.GetFormatContentType("jsv"), Is.EqualTo(MimeTypes.Jsv));
        Assert.That(contentTypes.GetFormatContentType("unknown_format"), Is.Null);
    }

    [Test]
    public void ContentTypes_GetStreamSerializer_And_Deserializer_HandleNull()
    {
        var contentTypes = new Host.ContentTypes();
        Assert.That(contentTypes.GetStreamSerializer(null), Is.Null);
        Assert.That(contentTypes.GetStreamSerializer(""), Is.Null);
        Assert.That(contentTypes.GetStreamSerializer("unknown/content-type"), Is.Null);

        Assert.That(contentTypes.GetStreamDeserializer(null), Is.Null);
        Assert.That(contentTypes.GetStreamDeserializer(""), Is.Null);
        Assert.That(contentTypes.GetStreamDeserializer("unknown/content-type"), Is.Null);

        Assert.That(contentTypes.GetStreamSerializer(MimeTypes.Json), Is.Not.Null);
        Assert.That(contentTypes.GetStreamDeserializer(MimeTypes.Json), Is.Not.Null);
    }

    [Test]
    public void ContentTypes_Remove_HandlesNullAndEmpty()
    {
        var contentTypes = new Host.ContentTypes();
        Assert.DoesNotThrow(() => contentTypes.Remove(null));
        Assert.DoesNotThrow(() => contentTypes.Remove(""));
    }

    [Test]
    public void ContentTypes_SerializeToBytes_And_ToString_NullGuards()
    {
        var contentTypes = new Host.ContentTypes();
        Assert.That(contentTypes.SerializeToBytes(null, new TestXmlDto()), Is.EqualTo(TypeConstants.EmptyByteArray));
        Assert.That(contentTypes.SerializeToBytes(new MockHttpRequest(), null), Is.EqualTo(TypeConstants.EmptyByteArray));
        Assert.That(contentTypes.SerializeToString(null, new TestXmlDto()), Is.Null);

        var mockReq = new MockHttpRequest { ResponseContentType = MimeTypes.Json };
        var json = contentTypes.SerializeToString(mockReq, new TestXmlDto { Id = 10, Name = "Test" });
        Assert.That(json, Does.Contain("\"Id\":10"));
        Assert.That(json, Does.Contain("\"Name\":\"Test\""));
    }

    [Test]
    public void ContentTypes_SerializeToStreamAsync_HandlesNullStream()
    {
        var contentTypes = new Host.ContentTypes();
        var mockReq = new MockHttpRequest { ResponseContentType = MimeTypes.Json };
        var task = contentTypes.SerializeToStreamAsync(mockReq, new TestXmlDto { Id = 1 }, null);
        Assert.That(task.IsCompleted, Is.True);
        var task2 = contentTypes.SerializeToStreamAsync(mockReq, new TestXmlDto { Id = 1 }, Stream.Null);
        Assert.That(task2.IsCompleted, Is.True);
    }

    [Test]
    public void ContentTypes_DeserializeFromString_And_FromStream_NullGuards()
    {
        var contentTypes = new Host.ContentTypes();
        Assert.That(contentTypes.DeserializeFromString(MimeTypes.Json, null, "{}"), Is.Null);
        Assert.That(contentTypes.DeserializeFromString(MimeTypes.Json, typeof(TestXmlDto), null), Is.Null);
        Assert.That(contentTypes.DeserializeFromString(MimeTypes.Json, typeof(int), null), Is.EqualTo(0));

        Assert.That(contentTypes.DeserializeFromStream(MimeTypes.Json, null, new MemoryStream()), Is.Null);
        Assert.That(contentTypes.DeserializeFromStream(MimeTypes.Json, typeof(TestXmlDto), null), Is.Null);
        Assert.That(contentTypes.DeserializeFromStream(MimeTypes.Json, typeof(TestXmlDto), Stream.Null), Is.Null);

        Assert.Throws<ArgumentNullException>(() => contentTypes.DeserializeFromString(null, typeof(TestXmlDto), "{}"));
        Assert.Throws<ArgumentNullException>(() => contentTypes.DeserializeFromStream(null, typeof(TestXmlDto), new MemoryStream()));
    }

    [Test]
    public void ContentFormat_GetRequestAttribute_HandlesNullAndMethods()
    {
        Assert.That(ContentFormat.GetRequestAttribute(null), Is.EqualTo(RequestAttributes.None));
        Assert.That(ContentFormat.GetRequestAttribute("GET"), Is.EqualTo(RequestAttributes.HttpGet));
        Assert.That(ContentFormat.GetRequestAttribute("get"), Is.EqualTo(RequestAttributes.HttpGet));
        Assert.That(ContentFormat.GetRequestAttribute("post"), Is.EqualTo(RequestAttributes.HttpPost));
        Assert.That(ContentFormat.GetRequestAttribute("PUT"), Is.EqualTo(RequestAttributes.HttpPut));
        Assert.That(ContentFormat.GetRequestAttribute("delete"), Is.EqualTo(RequestAttributes.HttpDelete));
        Assert.That(ContentFormat.GetRequestAttribute("patch"), Is.EqualTo(RequestAttributes.HttpPatch));
        Assert.That(ContentFormat.GetRequestAttribute("HEAD"), Is.EqualTo(RequestAttributes.HttpHead));
        Assert.That(ContentFormat.GetRequestAttribute("options"), Is.EqualTo(RequestAttributes.HttpOptions));
        Assert.That(ContentFormat.GetRequestAttribute("CUSTOM"), Is.EqualTo(RequestAttributes.HttpOther));
    }

    [Test]
    public void JsonDataContractSerializer_SerializeToStream_NullGuards()
    {
        var serializer = new Serialization.JsonDataContractSerializer();
        Assert.DoesNotThrow(() => serializer.SerializeToStream<TestXmlDto>(null, new MemoryStream()));
        Assert.DoesNotThrow(() => serializer.SerializeToStream(new TestXmlDto { Id = 1 }, null));
        Assert.DoesNotThrow(() => serializer.SerializeToStream(new TestXmlDto { Id = 1 }, Stream.Null));

        using var ms = new MemoryStream();
        serializer.SerializeToStream(new TestXmlDto { Id = 5, Name = "Five" }, ms);
        Assert.That(ms.Length, Is.GreaterThan(0));

        ms.Position = 0;
        var deserialized = serializer.DeserializeFromStream<TestXmlDto>(ms);
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized.Id, Is.EqualTo(5));
        Assert.That(deserialized.Name, Is.EqualTo("Five"));
    }

    [Test]
    public void JsonDataContractSerializer_DeserializeFromStream_NullGuards()
    {
        var serializer = new Serialization.JsonDataContractSerializer();
        Assert.That(serializer.DeserializeFromStream<TestXmlDto>(null), Is.Null);
        Assert.That(serializer.DeserializeFromStream<TestXmlDto>(Stream.Null), Is.Null);
        Assert.That(serializer.DeserializeFromStream(null, new MemoryStream()), Is.Null);
        Assert.That(serializer.DeserializeFromStream(typeof(TestXmlDto), null), Is.Null);
        Assert.That(serializer.DeserializeFromStream(typeof(TestXmlDto), Stream.Null), Is.Null);
    }

#if !NETCORE
    [Test]
    public void SoapFormat_Register_NullAppHost_DoesNotThrow()
    {
        var soapFormat = new SoapFormat();
        Assert.DoesNotThrow(() => soapFormat.Register(null));
    }
#endif
}
