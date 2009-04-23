using System.Net;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.WebHost.Endpoints.Metadata;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	[TestFixture]
	public class OperationTests : TestBase
	{
		[Test]
		public void Sorts_operations_into_correct_operation_groups()
		{
			var groupedOperations = new Operations(base.AllOperations);

			Assert.That(groupedOperations.ReplyOperations.Names.Count, Is.EqualTo(base.ReplyOperations.Count));
			Assert.That(groupedOperations.OneWayOperations.Names.Count, Is.EqualTo(base.OneWayOperations.Count));
		}

		[Test]
		public void Test_local_ipAddresses()
		{
			var isInternal = EndpointHandlerBase.IsPrivateNetworkAddress(IPAddress.Parse("172.20.0.96"));
			Assert.That(isInternal, Is.True);
		}
	}
}