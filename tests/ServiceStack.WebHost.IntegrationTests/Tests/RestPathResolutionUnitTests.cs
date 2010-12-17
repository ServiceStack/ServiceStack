using NUnit.Framework;
using ServiceStack.Common.Web;
using ServiceStack.ServiceInterface.Testing;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
	[TestFixture]
	public class RestPathResolutionUnitTests
		: TestsBase
	{
		public RestPathResolutionUnitTests()
			: base(typeof(ReverseService).Assembly)
		{
		}

		[SetUp]
		public void OnBeforeTest()
		{
			base.OnBeforeEachTest();
		}

		[Test]
		public void Can_execute_EchoRequest_rest_path()
		{
			var request = (EchoRequest)ExecutePath("/echo/1/One");
			Assert.That(request, Is.Not.Null);
			Assert.That(request.Id, Is.EqualTo(1));
			Assert.That(request.String, Is.EqualTo("One"));
		}

		[Test]
		public void Can_call_EchoRequest_with_QueryString()
		{
			var request = (EchoRequest)ExecutePath("/echo/1/One?Long=2&Bool=True");

			Assert.That(request.Id, Is.EqualTo(1));
			Assert.That(request.String, Is.EqualTo("One"));
			Assert.That(request.Long, Is.EqualTo(2));
			Assert.That(request.Bool, Is.EqualTo(true));
		}

		[Test]
		public void Can_call_WildCardRequest_with_alternate_matching_WildCard_defined()
		{
			var request = (WildCardRequest)ExecutePath("/wildcard/1/aPath/edit");
			Assert.That(request.Id, Is.EqualTo(1));
			Assert.That(request.Path, Is.EqualTo("aPath"));
			Assert.That(request.Action, Is.EqualTo("edit"));
			Assert.That(request.RemainingPath, Is.Null);
		}

		[Test]
		public void Can_call_WildCardRequest_WildCard_mapping()
		{
			var request = (WildCardRequest)ExecutePath("/wildcard/1/remaining/path/to/here");
			Assert.That(request.Id, Is.EqualTo(1));
			Assert.That(request.Path, Is.Null);
			Assert.That(request.Action, Is.Null);
			Assert.That(request.RemainingPath, Is.EqualTo("remaining/path/to/here"));
		}

		[Test]
		public void Can_call_WildCardRequest_WildCard_mapping_with_QueryString()
		{
			var request = (WildCardRequest)ExecutePath("/wildcard/1/remaining/path/to/here?Action=edit");
			Assert.That(request.Id, Is.EqualTo(1));
			Assert.That(request.Path, Is.Null);
			Assert.That(request.Action, Is.EqualTo("edit"));
			Assert.That(request.RemainingPath, Is.EqualTo("remaining/path/to/here"));
		}

		[Test]
		public void Can_call_GET_on_VerbMatch_Services()
		{
			var request = (VerbMatch1)ExecutePath(HttpMethods.Get, "/verbmatch");
			Assert.That(request.Name, Is.Null);

			var request2 = (VerbMatch1)ExecutePath(HttpMethods.Get, "/verbmatch/arg");
			Assert.That(request2.Name, Is.EqualTo("arg"));
		}

		[Test]
		public void Can_call_POST_on_VerbMatch_Services()
		{
			var request = (VerbMatch2)ExecutePath(HttpMethods.Post, "/verbmatch");
			Assert.That(request.Name, Is.Null);

			var request2 = (VerbMatch2)ExecutePath(HttpMethods.Post, "/verbmatch/arg");
			Assert.That(request2.Name, Is.EqualTo("arg"));
		}

		[Test]
		public void Can_call_DELETE_on_VerbMatch_Services()
		{
			var request = (VerbMatch1)ExecutePath(HttpMethods.Delete, "/verbmatch");
			Assert.That(request.Name, Is.Null);

			var request2 = (VerbMatch1)ExecutePath(HttpMethods.Delete, "/verbmatch/arg");
			Assert.That(request2.Name, Is.EqualTo("arg"));
		}

		[Test]
		public void Can_call_PUT_on_VerbMatch_Services()
		{
			var request = (VerbMatch2)ExecutePath(HttpMethods.Put, "/verbmatch");
			Assert.That(request.Name, Is.Null);

			var request2 = (VerbMatch2)ExecutePath(HttpMethods.Put, "/verbmatch/arg");
			Assert.That(request2.Name, Is.EqualTo("arg"));
		}

	}
}