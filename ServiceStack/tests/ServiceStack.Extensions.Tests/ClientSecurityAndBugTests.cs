#nullable enable

using System;
using System.Xml;
using NUnit.Framework;
using ServiceStack.Caching;

namespace ServiceStack.Extensions.Tests;

[Route("/items/{Id:int}")]
public class ItemWithConstraintRequest : IReturnVoid
{
    public int Id { get; set; }
}

[Route("/files/{Path*}")]
public class FileWildcardRequest : IReturnVoid
{
    public string? Path { get; set; }
}

[TestFixture]
public class ClientSecurityAndBugTests
{
    [Test]
    public void ExtractFromXml_Safely_Terminates_On_Malformed_Or_Truncated_Xml()
    {
        const string truncatedXml = "<RSAKeyValue><Modulus>";
        // Previously this entered an infinite loop because reader.Read() returning false was not checked;
        // now it terminates promptly (throwing XmlException on unexpected EOF).
        Assert.Throws<XmlException>(() => PlatformRsaUtils.ExtractFromXml(truncatedXml));

        const string emptyElementsXml = "<RSAKeyValue><Modulus></Modulus></RSAKeyValue>";
        var parameters = PlatformRsaUtils.ExtractFromXml(emptyElementsXml);
        Assert.That(parameters.Modulus, Is.Null);
    }

    [Test]
    public void ExtractFromXml_Prohibits_Dtd_Processing()
    {
        const string xxeXml = """
            <?xml version="1.0" encoding="utf-8"?>
            <!DOCTYPE test [
                <!ENTITY xxe "evil">
            ]>
            <RSAKeyValue>
                <Modulus>&xxe;</Modulus>
            </RSAKeyValue>
            """;

        Assert.Throws<XmlException>(() => PlatformRsaUtils.ExtractFromXml(xxeXml));
    }

    [Test]
    public void AuthenticationInfo_Parses_Unquoted_Tokens_Without_IndexOutOfRangeException()
    {
        // Header ends with unquoted token: qop=auth
        const string headerWithUnquotedEnd = @"Digest realm=""testrealm@host.com"", stale=FALSE, nonce=""dcd98b7102dd2f0e8b11d0f600bfb0c093"", qop=auth";

        var authInfo = new AuthenticationInfo(headerWithUnquotedEnd);
        Assert.That(authInfo.method, Is.EqualTo("digest"));
        Assert.That(authInfo.realm, Is.EqualTo("testrealm@host.com"));
        Assert.That(authInfo.nonce, Is.EqualTo("dcd98b7102dd2f0e8b11d0f600bfb0c093"));
        Assert.That(authInfo.qop, Is.EqualTo("auth"));
    }

    [Test]
    public void AuthenticationInfo_Parses_Quoted_Values_With_Commas()
    {
        const string headerWithQuotedCommas = @"Digest realm=""testrealm@host.com"", qop=""auth,auth-int"", nonce=""xyz""";

        var authInfo = new AuthenticationInfo(headerWithQuotedCommas);
        Assert.That(authInfo.realm, Is.EqualTo("testrealm@host.com"));
        Assert.That(authInfo.qop, Is.EqualTo("auth,auth-int"));
        Assert.That(authInfo.nonce, Is.EqualTo("xyz"));
    }

    [Test]
    public void UserAgentHelper_Detects_Browsers_And_Bots_Correctly()
    {
        var chromeUa = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
        var (browser, version) = UserAgentHelper.GetBrowserInfo(chromeUa);
        Assert.That(browser, Is.EqualTo("Google Chrome"));
        Assert.That(version, Is.EqualTo("120.0.0.0"));

        var botUa = "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)";
        var isBot = UserAgentHelper.IsBotUserAgent(botUa, out var botName);
        Assert.That(isBot, Is.True);
        Assert.That(botName, Is.EqualTo("Googlebot 2.1"));
    }

    [Test]
    public void StreamCompressors_GetRequired_Throws_Clean_Error_Message()
    {
        var ex = Assert.Throws<NotSupportedException>(() => StreamCompressors.GetRequired("unknown-compressor"));
        Assert.That(ex!.Message.Contains("System.Collections.Generic.Dictionary"), Is.False);
        Assert.That(ex.Message.Contains("gzip"), Is.True);
    }

    [Test]
    public void UrlExtensions_RestRoute_Supports_Constraints_And_Wildcard_Slashes()
    {
        // Route constraint {Id:int}
        var request = new ItemWithConstraintRequest { Id = 42 };
        var url = request.ToUrl("GET");
        Assert.That(url, Is.EqualTo("/items/42"));

        // Wildcard {Path*} preserving slashes
        var fileRequest = new FileWildcardRequest { Path = "folder/subfolder/file.png" };
        var fileUrl = fileRequest.ToUrl("GET");
        Assert.That(fileUrl, Is.EqualTo("/files/folder/subfolder/file.png"));
    }
}
