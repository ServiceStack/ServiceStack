using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.ServiceModel.Tests.DataContracts.Operations;

namespace ServiceStack.ServiceInterface.Tests
{
	[TestFixture]
	public class PortResolverTests
	{
		[Test]
		public void PortResolver_works()
		{
			var resolver = new PortResolver(GetType().Assembly);
			Assert.That(resolver.OperationTypes.Count, Is.GreaterThan(0));
			Assert.That(resolver.FindService(typeof(GetCustomers).Name), Is.Not.Null);
		}
	}
}