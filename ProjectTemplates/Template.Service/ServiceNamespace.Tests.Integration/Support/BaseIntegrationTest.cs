/*
// $Id: BaseIntegrationTest.cs 675 2008-12-22 18:39:43Z DDNGLOBAL\Demis $
//
// Revision      : $Revision: 675 $
// Modified Date : $LastChangedDate: 2008-12-22 18:39:43 +0000 (Mon, 22 Dec 2008) $ 
// Modified By   : $LastChangedBy: DDNGLOBAL\Demis $
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using System;
using System.Collections.Generic;
using Ddn.Common.Services.Service;
using Ddn.Common.Testing;
using NUnit.Framework;
using @DomainModelNamespace@;
using @ServiceModelNamespace@.Version100.Operations.@ServiceName@;
using @ServiceNamespace@.DataAccess;
using @ServiceNamespace@.DataAccess.DataModel;
using @ServiceNamespace@.Logic;
using @ServiceNamespace@.ServiceInterface;
using @ServiceNamespace@.Tests.Support;
using DataModel = @ServiceNamespace@.DataAccess.DataModel;

namespace @ServiceNamespace@.Tests.Integration.Support
{
	public class BaseIntegrationTest : BaseServiceTestFixture
	{
		public BaseIntegrationTest() 
			: base(new TestParameters(), new ServiceResolver())
		{
		}

		/// <summary>
		/// DataModel user list
		/// </summary>
		protected List<DataModel.@ModelName@> @ModelName@s { get; set; }

		/// <summary>
		/// DataModel user Id list
		/// </summary>
		protected List<int> @ModelName@Ids
		{
			get { return this.@ModelName@s.ConvertAll(x => (int) x.Id); }
		}

		/// <summary>
		/// @ModelName@ catalogue facade
		/// </summary>
		protected @ServiceName@Facade Facade { get; private set; }

		[TestFixtureSetUp]
		public override void FixtureSetUp()
		{
			// Must call the base class test fixture set up
			base.FixtureSetUp();

			try
			{
				using (var persistenceProvider = this.ProviderManager.CreateProvider())
				{
					// Wrap active connection in a user data access provider
					var dataAccessProvider = new @ServiceName@DataAccessProvider(persistenceProvider);

					// Insert test users into database
					this.@ModelName@s = TestData.Load@ModelName@s(dataAccessProvider, 3);
				}

				// Create user catalogue facade
				this.Facade = new @ServiceName@Facade(base.AppContext, this.ProviderManager, "127.0.0.1");
			}
			catch (Exception ex)
			{
				base.LogException(ex);
				throw;
			}
		}

		[TestFixtureTearDown]
		public override void FixtureTearDown()
		{
			if (this.Facade != null)
			{
				// Close the facade connection
				this.Facade.Dispose();
				this.Facade = null;
			}

			this.@ModelName@s = null;

			// Must call the base class test fixture tear down
			base.FixtureTearDown();
		}

		protected object ExecuteService(object requestDto)
		{
			CallContext context = base.CreateCallContext(this.Facade, requestDto);
			return base.ServiceController.Execute(context);
		}

		protected object ExecuteXmlService(string xml, ServiceModelInfo modelInfo)
		{
			CallContext context = base.CreateCallContext(this.Facade, xml, modelInfo);
			return base.ServiceController.ExecuteXml(context);
		}

		protected SessionId GetSessionId(@ModelName@ existing@ModelName@)
		{
			var loginDto = new GetLoginAuth {
				@ModelName@Name = existing@ModelName@.@ModelName@Name,
				Base64EncryptedPassword = base.ServerPublicKey.EncryptData(TestData.@ModelName@Password),
				Base64ClientModulus = TestData.ClientModulusBase64,
			};

			var responseDto = (GetLoginAuthResponse)ExecuteService(loginDto);
			return new SessionId((int)existing@ModelName@.Id, responseDto.PublicSessionId);
		}
	}
}