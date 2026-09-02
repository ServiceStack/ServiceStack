using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using NUnit.Framework;
using ServiceStack.Text.Common;
using ServiceStack.Text.Json;

namespace ServiceStack.Text.Tests
{
    public interface ITestService
    {
        string Echo(string message, int count);
    }

    [TestFixture]
    public class SecurityFixesTests
    {
        [Test]
        public void HttpClientHandlerFactory_defaults_to_no_default_credentials()
        {
            var handler = HttpUtils.HttpClientHandlerFactory();
            Assert.That(handler.UseDefaultCredentials, Is.False);
        }

        [Test]
        public void XmlSerializer_DeserializeFromStream_prohibits_DTDs()
        {
            var maliciousDtdXml = @"<?xml version=""1.0""?>
<!DOCTYPE foo [
  <!ELEMENT foo ANY>
  <!ENTITY xxe SYSTEM ""http://127.0.0.1:9999/test"">
]>
<foo>&xxe;</foo>";

            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(maliciousDtdXml));
            Assert.Throws<SerializationException>(() => XmlSerializer.DeserializeFromStream<string>(stream));
        }

        [Test]
        public void JsonTypeSerializer_UnescapeJsString_advances_index_for_escaped_keys()
        {
            var json = "{\"escaped\\\"key\": \"value\", \"normal\": 123}";
            var span = json.AsSpan();
            var index = 1; // start after '{'
            var result = JsonTypeSerializer.UnescapeJsString(span, '"', removeQuotes: true, index);
            Assert.That(result.Span.ToString(), Is.EqualTo("escaped\"key"));
            Assert.That(result.Index, Is.GreaterThan(index));
        }

        [Test]
        public void StringExtensions_StripHtml_and_StripMarkdown_do_not_reDoS()
        {
            var pathologicalHtml = "<" + new string('a', 5000);
            var result = pathologicalHtml.StripHtml();
            Assert.That(result, Is.EqualTo(pathologicalHtml));

            var pathologicalMarkdown = "[" + new string('b', 5000);
            var mdResult = pathologicalMarkdown.StripMarkdownMarkup();
            Assert.That(mdResult, Is.EqualTo(pathologicalMarkdown));
        }

        [Test]
        public void CsvConfig_EscapeFormulas_sanitizes_dangerous_prefixes()
        {
            using (JsConfig.With(new Config()))
            {
                CsvConfig.Reset();
                CsvConfig.EscapeFormulas = true;

                // Values without delimiter characters get prefixed with '
                Assert.That("=cmd".ToCsvField(), Is.EqualTo("'=cmd"));
                Assert.That("@SUM".ToCsvField(), Is.EqualTo("'@SUM"));
                Assert.That("\tTab".ToCsvField(), Is.EqualTo("'\tTab"));
                Assert.That("\rReturn".ToCsvField(), Is.EqualTo("\"'\rReturn\""));

                // Values with delimiters (like commas) get prefixed with ' and quoted
                Assert.That("=SUM(1,2)".ToCsvField(), Is.EqualTo("\"'=SUM(1,2)\""));
                Assert.That("+cmd|' /C calc'!A0".ToCsvField(), Is.EqualTo("'+cmd|' /C calc'!A0"));

                // Numbers should remain unescaped
                Assert.That("-123.45".ToCsvField(), Is.EqualTo("-123.45"));
                Assert.That("+50".ToCsvField(), Is.EqualTo("+50"));

                // Round-trip unescaping
                Assert.That("'=cmd".FromCsvField().ToString(), Is.EqualTo("=cmd"));
                Assert.That("\"'=SUM(1,2)\"".FromCsvField().ToString(), Is.EqualTo("=SUM(1,2)"));

                // Opt-out
                CsvConfig.EscapeFormulas = false;
                Assert.That("=cmd".ToCsvField(), Is.EqualTo("=cmd"));
                CsvConfig.Reset();
            }
        }

        [Test]
        public void AssemblyUtils_caps_negative_type_lookups()
        {
            for (int i = 0; i < AssemblyUtils.MaxNegativeCacheSize + 50; i++)
            {
                var type = AssemblyUtils.UncheckedFindType($"NonExistentType_{i}, NonExistentAssembly");
                Assert.That(type, Is.Null);
            }
        }

        [Test]
        public void DynamicProxy_creates_instance_with_interface_method_parameters()
        {
            var instance = DynamicProxy.GetInstanceFor<ITestService>();
            Assert.That(instance, Is.Not.Null);
            Assert.DoesNotThrow(() => instance.Echo("test", 3));
        }

        [Test]
        public void LicenseUtils_VerifySignedHash_disposes_cleanly()
        {
            using var rsa = RSA.Create();
            var parameters = rsa.ExportParameters(false);
            var data = new byte[] { 1, 2, 3 };
            var badSignature = new byte[] { 4, 5, 6 };
            using var sha = TextConfig.CreateSha();

            var result = LicenseUtils.VerifySignedHash(data, badSignature, parameters, sha);
            Assert.That(result, Is.False);
        }

        [Test]
        public void HttpUtils_AddQueryParam_encodes_keys_and_values()
        {
            var url = "http://example.com/api";
            var result = url.AddQueryParam("key with spaces", "val&special");
            Assert.That(result, Is.EqualTo("http://example.com/api?key+with+spaces=val%26special"));

            var unencoded = url.AddQueryParam("key with spaces", "val&special", encode: false);
            Assert.That(unencoded, Is.EqualTo("http://example.com/api?key with spaces=val&special"));
        }

        [Test]
        public void PathUtils_ResolvePaths_handles_rooted_and_relative_traversal()
        {
            // Rooted paths cannot escape root
            Assert.That("/../../etc/passwd".ResolvePaths(), Is.EqualTo("/etc/passwd"));
            Assert.That("/a/b/../../c".ResolvePaths(), Is.EqualTo("/c"));
            Assert.That("/a/b/../../../c".ResolvePaths(), Is.EqualTo("/c"));

            // URLs cannot escape host/scheme root
            Assert.That("http://example.org/../../etc/passwd".ResolvePaths(), Is.EqualTo("http://etc/passwd"));

            // Relative paths preserve leading '..'
            Assert.That("a/../..".ResolvePaths(), Is.EqualTo(".."));
            Assert.That("../../a/b".ResolvePaths(), Is.EqualTo("../../a/b"));
            Assert.That("a/../../../b".ResolvePaths(), Is.EqualTo("../../b"));
        }
    }
}
