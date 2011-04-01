using System.Net;
using NUnit.Framework;
using ServiceStack.WebHost.Endpoints.Metadata;
using ServiceStack.WebHost.Endpoints.Support;
using ServiceStack.WebHost.Endpoints.Tests.Support;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	[TestFixture]
	public class OperationTests : MetadataTestBase
	{
		[Test]
		public void Sorts_operations_into_correct_operation_groups()
		{
			var groupedOperations = new Operations(base.AllOperations);

			Assert.That(groupedOperations.ReplyOperations.Names.Count, Is.EqualTo(base.ReplyOperations.Count));
			Assert.That(groupedOperations.OneWayOperations.Names.Count, Is.EqualTo(base.OneWayOperations.Count));
		}
	}
}