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
}
