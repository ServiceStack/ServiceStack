using Moq;
using NUnit.Framework;
using @DomainModelNamespace@;
using @ServiceModelNamespace@.Version100.Operations.@ServiceName@;
using ServiceStack.DataAccess;
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

			var request = new GetAll@ModelName@s();
			var operationContext = CreateOperationContext(request);
			
			var port = new GetAll@ModelName@sHandler();
			port.Execute(operationContext);
			
			provider.Verify();
		}

	}
}