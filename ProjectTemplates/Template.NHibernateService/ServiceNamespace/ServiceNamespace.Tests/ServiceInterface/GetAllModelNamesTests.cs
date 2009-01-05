using System.Linq;
using Moq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using @DomainModelNamespace@;
using @ServiceModelNamespace@.Version100.Operations.@ServiceName@;
using ServiceStack.DataAccess;
using @ServiceNamespace@.Logic.LogicInterface;
using @ServiceNamespace@.ServiceInterface.Version100;
using @ServiceNamespace@.Tests.Support;

namespace @ServiceNamespace@.Tests.ServiceInterface
{
	[TestFixture]
	public class GetAll@ModelName@sTests : TestBase
	{

		[Test]
		public void GetAll@ModelName@sTest()
		{
			var provider = new Mock<IPersistenceProvider>();
			provider.Expect(x => x.GetAll<@ModelName@>());
			RegisterPersistenceProvider(provider.Object);

			var facade = new Mock<I@ServiceName@Facade>();
			var customers = new[] { new @ModelName@ { Id = 1 } }.ToList();
			facade.Expect(x => x.GetAll@ModelName@s()).Returns(customers);

			var request = new GetAll@ModelName@s();
			var operationContext = CreateOperationContext(request, facade.Object);

			var port = new GetAll@ModelName@sHandler();
			var response = (GetAll@ModelName@sResponse)port.Execute(operationContext);

			Assert.That(response.@ModelName@s.Count, Is.EqualTo(customers.Count));
			Assert.That(response.@ModelName@s[0].Id, Is.EqualTo(customers[0].Id));
			provider.Verify();
		}

	}
}