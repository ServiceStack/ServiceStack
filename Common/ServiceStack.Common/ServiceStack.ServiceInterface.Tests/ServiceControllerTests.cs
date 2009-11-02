using System;
using NUnit.Framework;
using ServiceStack.Service;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.Tests.Support.Version100.Operations;

namespace ServiceStack.ServiceInterface.Tests
{
	[TestFixture]
	public class ServiceControllerTests : TestBase
	{
		[Port(typeof(InternalOnly), EndpointAttributes.Internal)]
		public class InternalOnlyHandler
		{
		}

		[Port(typeof(HttpGet), EndpointAttributes.External | EndpointAttributes.HttpGet)]
		public class HttpGetHandler
		{
		}

		[Test]
		public void Internal_only_handler_can_be_called_internally()
		{
			ServiceControllerContext.AssertServiceRestrictions(
				new InternalOnlyHandler(), EndpointAttributes.Internal, "name");
		}

		[Test]
		public void Internal_only_handler_cannot_be_called_externally()
		{
			try
			{
				ServiceControllerContext.AssertServiceRestrictions(
					new InternalOnlyHandler(), EndpointAttributes.External, "name");
			}
			catch (UnauthorizedAccessException uae)
			{
				Assert.That(uae.Message.Contains(string.Format("'{0}'", EndpointAttributes.Internal)));
				return;
			}
			Assert.Fail("UnauthorizedAccessException should've been thrown");
		}

		[Test]
		public void Can_call_HttpGetHandler_with_HttpGet_request()
		{
			ServiceControllerContext.AssertServiceRestrictions(
				new HttpGetHandler(), EndpointAttributes.External | EndpointAttributes.HttpGet | EndpointAttributes.InSecure, "name");
		}

		[Test]
		public void Cannot_call_HttpGetHandler_with_HttpPost_request()
		{
			try
			{
				ServiceControllerContext.AssertServiceRestrictions(
					new HttpGetHandler(), EndpointAttributes.External | EndpointAttributes.HttpPost | EndpointAttributes.InSecure, "name");
			}
			catch (UnauthorizedAccessException uae)
			{
				Assert.That(uae.Message.Contains(string.Format("'{0}'", EndpointAttributes.HttpGet)));
				return;
			}
			Assert.Fail("UnauthorizedAccessException should've been thrown");
		}
	}
}