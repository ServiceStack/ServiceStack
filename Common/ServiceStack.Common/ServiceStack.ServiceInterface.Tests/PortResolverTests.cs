using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.ServiceInterface.Tests.Support.Version100.Operations;

namespace ServiceStack.ServiceInterface.Tests
{
	[TestFixture]
	public class PortResolverTests : TestBase
	{
		[Test]
		public void PortResolver_basic_test()
		{
			var resolver = new PortResolver(GetType().Assembly);
			Assert.That(resolver.OperationTypes.Count, Is.GreaterThan(0));
			Assert.That(resolver.FindService(typeof(GetCustomers).Name), Is.Not.Null);
		}

		[Test]
		public void PortResolver_returns_the_correct_no_of_operation_types()
		{
			var resolver = new PortResolver(GetType().Assembly);

			Assert.That(resolver.OperationTypes.Count, Is.EqualTo(base.AllOperations.Count));
			Assert.That(resolver.OperationTypes, Is.EquivalentTo(base.AllOperations));
		}

		[Test]
		public void PortResolver_returns_all_operation_types()
		{
			var resolver = new PortResolver(GetType().Assembly);

			Assert.That(resolver.AllOperationTypes.Count, Is.EqualTo(base.AllOperations.Count));
			Assert.That(resolver.AllOperationTypes, Is.EquivalentTo(base.AllOperations));
		}
	}

}