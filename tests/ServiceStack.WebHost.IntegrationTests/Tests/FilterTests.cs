using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.ServiceClient.Web;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
	[TestFixture]
	public class FilterTests
	{
		[Test]
		public void Can_call_service_returning_string()
		{
			var response = Config.ServiceStackBaseUri.CombineWith("hello2/world")
				.DownloadJsonFromUrl();

			Assert.That(response, Is.EqualTo("world"));
		}
	}
}