using System.Runtime.Serialization;
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

    [Route("/route/{Id}")]
    public class RequestWithIgnoredDataMembers : IReturn
    {
        public long Id { get; set; }

        public string Included { get; set; }

        [IgnoreDataMember]
        public string Excluded { get; set; }
    }

    [DataContract]
    [Route("/route/{Id}")]
    public class RequestWithDataMembers : IReturn
    {
        [DataMember]
        public long Id { get; set; }

        [DataMember]
        public string Included { get; set; }

        public string Excluded { get; set; }
    }

    [DataContract]
    [Route("/route/{Key}")]
    public class RequestWithNamedDataMembers : IReturn
    {
        [DataMember(Name = "Key")]
        public long Id { get; set; }

        [DataMember(Name = "Inc")]
        public string Included { get; set; }

        public string Excluded { get; set; }
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
            var url = new ArrayIds(1, 2, 3).ToUrl("GET");
            Assert.That(url, Is.EqualTo("/route/1,2,3"));
        }

        [Test]
        public void Cannot_include_ignored_data_members_on_querystring()
        {
            var url = new RequestWithIgnoredDataMembers { Id = 1, Included = "Yes", Excluded = "No" }.ToUrl("GET");
            Assert.That(url, Is.EqualTo("/route/1?included=Yes"));
        }

        [Test]
        public void Can_include_only_data_members_on_querystring()
        {
            var url = new RequestWithDataMembers { Id = 1, Included = "Yes", Excluded = "No" }.ToUrl("GET");
            Assert.That(url, Is.EqualTo("/route/1?included=Yes"));
        }

        [Test]
        public void Use_data_member_names_on_querystring()
        {
            var url = new RequestWithNamedDataMembers { Id = 1, Included = "Yes", Excluded = "No" }.ToUrl("GET");
            Assert.That(url, Is.EqualTo("/route/1?inc=Yes"));
        }
    }
}