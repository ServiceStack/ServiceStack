/*
// $Id: Get@ModelName@sTests.cs 678 2008-12-22 19:23:55Z DDNGLOBAL\Demis $
//
// Revision      : $Revision: 678 $
// Modified Date : $LastChangedDate: 2008-12-22 19:23:55 +0000 (Mon, 22 Dec 2008) $ 
// Modified By   : $LastChangedBy: DDNGLOBAL\Demis $
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using System.Collections.Generic;
using Ddn.Common.Services.Service;
using Ddn.Common.Testing;
using Moq;
using NUnit.Framework;
using @ServiceNamespace@.Logic.LogicInterface;
using @ServiceNamespace@.Logic.LogicInterface.Requests;
using @ServiceNamespace@.ServiceInterface.Version100;
using @DomainModelNamespace@.@ServiceName@;
using @ServiceNamespace@.Tests.Support;
using DtoOps = @ServiceModelNamespace@.Version100.Operations.@ServiceName@;
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
			this.CallContext.Request.Dto = new DtoOps.Get@ModelName@s { Ids = new DtoTypes.ArrayOfIntId { 1, 2, 3 } };
			
			// Set return value upon successful call to the moq
			List<@ModelName@> returnValue = new List<@ModelName@> { new @ModelName@ { Id = 1 }, new @ModelName@ { Id = 2 }, new @ModelName@ { Id = 3 } };

			// Set facade to expect provided values
			this.MoqFacade.Expect(facade => facade.Get@ModelName@s(It.Is<@ModelName@sRequest>(req => req.@ModelName@Ids.Count == 3 && req.@ModelName@Ids.Contains(1) && req.@ModelName@Ids.Contains(2) && req.@ModelName@Ids.Contains(3))))
				 .Returns(returnValue)
				 .AtMostOnce();

			// Execute port
			var response = (DtoOps.Get@ModelName@sResponse) new Get@ModelName@sPort().Execute(this.CallContext);

			this.MoqFacade.VerifyAll();

			Assert.That(response.@ModelName@s.Count == 3);
		}
	}
}