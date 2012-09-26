using NUnit.Framework;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceHost;

namespace ServiceStack.Common.Tests
{
    [Route("/route/{Id}")]
    public class JustId : IReturn
    {
        public long Id { get; set; }
    }

    [Route("/route/{Ids}")]
    public class ArrayIds : IReturn
    {
        public long[] Ids { get; set; }
        public ArrayIds(params long[] ids)
        {
            this.Ids = ids;
        }
    }

    [TestFixture]
    public class UrlExtensionTests
    {
        [Test]
        public void Can_create_url_with_JustId()
        {
            var url = new JustId { Id = 1 }.ToUrl("GET");
            Assert.That(url, Is.EqualTo("/route/1"));
        }

        [Test]
        public void Can_create_url_with_ArrayIds()
        {
            var url = new ArrayIds(1,2,3).ToUrl("GET");
            Assert.That(url, Is.EqualTo("/route/1,2,3"));
        }
    }
}