using System;
using System.Runtime.Serialization;
using NUnit.Framework;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceHost;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests
{
    [Route("/route/{Id}")]
    public class JustId : IReturn
    {
        public long Id { get; set; }
    }

    [Route("/route/{Id}")]
	public class FieldId : IReturn
	{
		public readonly long Id;

		public FieldId(long id)
		{
			Id = id;
		}
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

	public enum Gender
	{
		None = 0,
		Male,
		Female
	}

	[Route("/route/{Id}")]
	public class RequestWithValueTypes : IReturn
	{
		public long Id { get; set; }

		public Gender Gender1 { get; set; }

		public Gender? Gender2 { get; set; }
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
		public void Can_create_url_with_FieldId()
		{
			using (JsConfig.BeginScope())
			{
				JsConfig.IncludePublicFields = true;
				var url = new FieldId(1).ToUrl("GET");
				Assert.That(url, Is.EqualTo("/route/1"));

			}
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

		[Test]
		public void Cannot_use_default_for_non_nullable_value_types_on_querystring()
		{
			var url = new RequestWithValueTypes {Id = 1, Gender1 = Gender.None}.ToUrl("GET");
			Assert.That(url, Is.EqualTo("/route/1"));
		}

		[Test]
		public void Can_use_non_default_for_non_nullable_value_types_on_querystring()
		{
			var url = new RequestWithValueTypes { Id = 1, Gender1 = Gender.Male }.ToUrl("GET");
			Assert.That(url, Is.EqualTo("/route/1?gender1=Male"));
		}

		[Test]
		public void Can_use_default_for_nullable_value_types_on_querystring()
		{
			var url = new RequestWithValueTypes { Id = 1, Gender2 = Gender.None }.ToUrl("GET");
			Assert.That(url, Is.EqualTo("/route/1?gender2=None"));
		}

		[Test]
		public void Cannot_use_null_for_nullable_value_types_on_querystring()
		{
			var url = new RequestWithValueTypes { Id = 1, Gender2 = null }.ToUrl("GET");
			Assert.That(url, Is.EqualTo("/route/1"));
		}

		[Test]
		public void Can_use_non_default_for_nullable_value_types_on_querystring()
		{
			var url = new RequestWithValueTypes { Id = 1, Gender2 = Gender.Male }.ToUrl("GET");
			Assert.That(url, Is.EqualTo("/route/1?gender2=Male"));
		}

        [Test]
        public void Can_combine_Uris_with_toUrl()
        {
            var serviceEndpoint = new Uri("http://localhost/api/", UriKind.Absolute);
            var actionUrl = new Uri(new JustId { Id = 1 }.ToUrl("GET").Substring(1), UriKind.Relative);

            Assert.That(new Uri(serviceEndpoint, actionUrl).ToString(), Is.EqualTo("http://localhost/api/route/1"));
        }

		[Test]
		public void Can_use_default_for_non_nullable_value_types_on_path()
		{
			var url = new RequestWithValueTypes { Id = 0 }.ToUrl("GET");
			Assert.That(url, Is.EqualTo("/route/0"));
		}

    }
}