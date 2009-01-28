using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.WebHost.Endpoints.Metadata;

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
	}
}