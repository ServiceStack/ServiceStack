using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.ServiceModel.Tests.DataContracts.Operations;

namespace ServiceStack.ServiceInterface.Tests
{
	[TestFixture]
	public class ZeroConfigServiceResolverTests
	{
		[Test]
		public void ZeroConfig_works()
		{
			var exampleOperation = typeof(GetCustomers);
			var resolver = new ZeroConfigServiceResolver(GetType().Assembly, 
			                                             exampleOperation.Assembly, exampleOperation.Namespace);
			
			Assert.That(resolver.OperationTypes.Count, Is.GreaterThan(0));
			Assert.That(resolver.FindService(exampleOperation.Name), Is.Not.Null);
		}
	}
}