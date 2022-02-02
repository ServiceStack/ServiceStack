using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
    [TestFixture]
    public class HttpUtilsTests
    {
        [Test]
        public void Can_AddQueryParam()
        {
            Assert.That("http://example.com".AddQueryParam("f", "1"), Is.EqualTo("http://example.com?f=1"));
            Assert.That("http://example.com?s=0".AddQueryParam("f", "1"), Is.EqualTo("http://example.com?s=0&f=1"));
            Assert.That("http://example.com?f=1".AddQueryParam("f", "2"), Is.EqualTo("http://example.com?f=1&f=2"));
            Assert.That("http://example.com?s=0&f=1&s=1".AddQueryParam("f", "2"), Is.EqualTo("http://example.com?s=0&f=1&s=1&f=2"));
            Assert.That("http://example.com?s=rf&f=1".AddQueryParam("f", "2"), Is.EqualTo("http://example.com?s=rf&f=1&f=2"));
            Assert.That("http://example.com?".AddQueryParam("f", "1"), Is.EqualTo("http://example.com?f=1"));
            Assert.That("http://example.com?f=1&".AddQueryParam("f", "2"), Is.EqualTo("http://example.com?f=1&f=2"));
            Assert.That("http://example.com?ab=0".AddQueryParam("a", "1"), Is.EqualTo("http://example.com?ab=0&a=1"));
            
            Assert.That("".AddQueryParam("a", ""), Is.EqualTo("?a="));
            Assert.That("".AddQueryParam("a", null), Is.EqualTo(""));
            Assert.That("/".AddQueryParam("a", null), Is.EqualTo("/"));
            Assert.That("/".AddQueryParam("a", ""), Is.EqualTo("/?a="));
            Assert.That("/".AddQueryParam("a", "b"), Is.EqualTo("/?a=b"));
            Assert.That((null as string).AddQueryParam("a", "b"), Is.EqualTo("?a=b"));
        }

        [Test]
        public void Can_SetQueryParam()
        {
            Assert.That("http://example.com".SetQueryParam("f", "1"), Is.EqualTo("http://example.com?f=1"));
            Assert.That("http://example.com?s=0".SetQueryParam("f", "1"), Is.EqualTo("http://example.com?s=0&f=1"));
            Assert.That("http://example.com?f=1".SetQueryParam("f", "2"), Is.EqualTo("http://example.com?f=2"));
            Assert.That("http://example.com?s=0&f=1&s=1".SetQueryParam("f", "2"), Is.EqualTo("http://example.com?s=0&f=2&s=1"));
            Assert.That("http://example.com?s=rf&f=1".SetQueryParam("f", "2"), Is.EqualTo("http://example.com?s=rf&f=2"));
            Assert.That("http://example.com?ab=0".SetQueryParam("a", "1"), Is.EqualTo("http://example.com?ab=0&a=1"));
        }

        [Test]
        public void Can_AddHashParam()
        {
            Assert.That("http://example.com".AddHashParam("f", "1"), Is.EqualTo("http://example.com#f=1"));
            Assert.That("http://example.com#s=0".AddHashParam("f", "1"), Is.EqualTo("http://example.com#s=0/f=1"));
            Assert.That("http://example.com#f=1".AddHashParam("f", "2"), Is.EqualTo("http://example.com#f=1/f=2"));
            Assert.That("http://example.com#s=0/f=1/s=1".AddHashParam("f", "2"), Is.EqualTo("http://example.com#s=0/f=1/s=1/f=2"));
            Assert.That("http://example.com#s=rf/f=1".AddHashParam("f", "2"), Is.EqualTo("http://example.com#s=rf/f=1/f=2"));
            Assert.That("http://example.com#ab=0".AddHashParam("a", "1"), Is.EqualTo("http://example.com#ab=0/a=1"));
            
            Assert.That("".AddHashParam("a", ""), Is.EqualTo("#a="));
            Assert.That("".AddHashParam("a", null), Is.EqualTo(""));
            Assert.That("/".AddHashParam("a", null), Is.EqualTo("/"));
            Assert.That("/".AddHashParam("a", ""), Is.EqualTo("/#a="));
            Assert.That("/".AddHashParam("a", "b"), Is.EqualTo("/#a=b"));
            Assert.That((null as string).AddHashParam("a", "b"), Is.EqualTo("#a=b"));
        }

        [Test]
        public void Can_SetHashParam()
        {
            Assert.That("http://example.com".SetHashParam("f", "1"), Is.EqualTo("http://example.com#f=1"));
            Assert.That("http://example.com#s=0".SetHashParam("f", "1"), Is.EqualTo("http://example.com#s=0/f=1"));
            Assert.That("http://example.com#f=1".SetHashParam("f", "2"), Is.EqualTo("http://example.com#f=2"));
            Assert.That("http://example.com#s=0/f=1/s=1".SetHashParam("f", "2"), Is.EqualTo("http://example.com#s=0/f=2/s=1"));
            Assert.That("http://example.com#s=rf/f=1".SetHashParam("f", "2"), Is.EqualTo("http://example.com#s=rf/f=2"));
            Assert.That("http://example.com#ab=0".SetHashParam("a", "1"), Is.EqualTo("http://example.com#ab=0/a=1"));
        }

        [Test]
        public void Can_get_MimeType_file_extension()
        {
            Assert.That(MimeTypes.GetExtension(MimeTypes.Html), Is.EqualTo(".html"));
            Assert.That(MimeTypes.GetExtension(MimeTypes.HtmlUtf8), Is.EqualTo(".html"));
            Assert.That(MimeTypes.GetExtension(MimeTypes.ImagePng), Is.EqualTo(".png"));
            Assert.That(MimeTypes.GetExtension(MimeTypes.ImageSvg), Is.EqualTo(".svg"));
        }
    }
}