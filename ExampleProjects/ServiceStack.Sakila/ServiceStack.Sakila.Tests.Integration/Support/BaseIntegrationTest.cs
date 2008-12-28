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
using System.Runtime.Remoting.Messaging;
using NUnit.Framework;
using ServiceStack.Sakila.DataAccess;
using ServiceStack.Sakila.Logic;
using ServiceStack.Sakila.Tests.Support;
using DataModel = ServiceStack.Sakila.DataAccess.DataModel;

namespace ServiceStack.Sakila.Tests.Integration.Support
{
	public class BaseIntegrationTest 
	{
		public BaseIntegrationTest() 
		{
		}

		/// <summary>
		/// DataModel user list
		/// </summary>
		protected List<DataModel.Customer> Customers { get; set; }

		///// <summary>
		///// DataModel Customer Id list
		///// </summary>
		protected List<int> CustomerIds
		{
			get { return this.Customers.ConvertAll(x => (int)x.Id); }
		}

		///// <summary>
		///// Customer catalogue facade
		///// </summary>
		//protected SakilaServiceFacade Facade { get; private set; }

		//[TestFixtureSetUp]
		//public void FixtureSetUp()
		//{
		//    try
		//    {
		//        using (var persistenceProvider = this.ProviderManager.CreateProvider())
		//        {
		//            // Wrap active connection in a user data access provider
		//            var dataAccessProvider = new SakilaServiceDataAccessProvider(persistenceProvider);

		//            // Insert test users into database
		//            this.Customers = TestData.LoadCustomers(dataAccessProvider, 3);
		//        }

		//        // Create user catalogue facade
		//        this.Facade = new SakilaServiceFacade(base.AppContext, this.ProviderManager, "127.0.0.1");
		//    }
		//    catch (Exception ex)
		//    {
		//        base.LogException(ex);
		//        throw;
		//    }
		//}

		//[TestFixtureTearDown]
		//public void FixtureTearDown()
		//{
		//    if (this.Facade != null)
		//    {
		//        // Close the facade connection
		//        this.Facade.Dispose();
		//        this.Facade = null;
		//    }

		//    this.Customers = null;

		//    // Must call the base class test fixture tear down
		//    base.FixtureTearDown();
		//}

		protected object ExecuteService(object requestDto)
		{
			return null;
			//CallContext context = base.CreateCallContext(this.Facade, requestDto);
			//return base.ServiceController.Execute(context);
		}

		//protected object ExecuteXmlService(string xml, ServiceModelInfo modelInfo)
		//{
		//    CallContext context = base.CreateCallContext(this.Facade, xml, modelInfo);
		//    return base.ServiceController.ExecuteXml(context);
		//}

	}
}