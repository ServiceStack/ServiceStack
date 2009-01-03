using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using @DomainModelNamespace@;
using @ServiceModelNamespace@.Version100.Operations.@ServiceName@;
using @ServiceNamespace@.Logic.LogicInterface;
using @ServiceNamespace@.Logic.LogicInterface.Requests;
using @ServiceNamespace@.ServiceInterface.Version100;
using @ServiceNamespace@.Tests.Support;
using ServiceStack.ServiceInterface;
using DtoTypes = @ServiceModelNamespace@.Version100.Types;

namespace @ServiceNamespace@.Tests.ServiceInterface.Version100
{
	[TestFixture]
	public class Get@ModelName@sTests : BaseAppTestFixture
	{
		public Get@ModelName@sTests() 
			: base(new TestParameters())
		{
		}

		private Mock<I@ServiceName@Facade> MoqFacade { get; set; }

		private CallContext CallContext { get; set; }

		private List<@ModelName@> FacadeResult { get; set; }

		[SetUp]
		public void SetUp()
		{
			this.MoqFacade = new Mock<I@ServiceName@Facade>();
			this.CallContext = base.CreateCallContext(this.MoqFacade.Object, null);
		}

		[TearDown]
		public void TearDown()
		{
			this.CallContext = null;
			this.MoqFacade = null;
		}

		[Test]
		public void Get@ModelName@sExecute()
		{
			// Create request DTO and insert into call context
			this.CallContext.Request.Dto = new Get@ModelName@s { @ModelName@Ids = new DtoTypes.ArrayOfIntId { 1, 2, 3 } };
			
			// Set return value upon successful call to the moq
			List<@ModelName@> returnValue = new List<@ModelName@> { new @ModelName@ { Id = 1 }, new @ModelName@ { Id = 2 }, new @ModelName@ { Id = 3 } };

			// Set facade to expect provided values
			this.MoqFacade.Expect(facade => facade.Get@ModelName@s(It.Is<@ModelName@sRequest>(req => req.@ModelName@Ids.Count == 3 && req.@ModelName@Ids.Contains(1) && req.@ModelName@Ids.Contains(2) && req.@ModelName@Ids.Contains(3))))
				 .Returns(returnValue)
				 .AtMostOnce();

			// Execute port
			var response = (Get@ModelName@sResponse) new Get@ModelName@sPort().Execute(this.CallContext);

			this.MoqFacade.VerifyAll();

			Assert.That(response.@ModelName@s.Count == 3);
		}
	}
}