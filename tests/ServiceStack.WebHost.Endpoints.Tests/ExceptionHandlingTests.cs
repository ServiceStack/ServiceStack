using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.ServiceInterface;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using NUnit.Framework;
using Funq;
using ServiceStack.Service;
using ServiceStack.ServiceClient.Web;
using System.Collections;
using ServiceStack.WebHost.Endpoints.Support;
using ServiceStack.WebHost.Endpoints.Tests.Support;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	[RestService("/users")]
	public class User { }
	public class UserResponse : IHasResponseStatus
	{
		public ResponseStatus ResponseStatus { get; set; }
	}

	public class UserService : RestServiceBase<User>
	{
		public override object  OnGet(User request)
		{
			return new HttpError(System.Net.HttpStatusCode.BadRequest, "CanNotExecute", "Failed to execute!");
		}

		public override object OnPost(User request)
		{
			throw new HttpError(System.Net.HttpStatusCode.BadRequest, "CanNotExecute", "Failed to execute!");
		}

		public override object OnPut(User request)
		{
			throw new ArgumentException();
		}
	}

	[TestFixture]
	public class ExceptionHandlingTests
	{
		private const string ListeningOn = "http://localhost:82/";

		public class ExceptionHandlingAppHostHttpListener
			: AppHostHttpListenerBase
		{

			public ExceptionHandlingAppHostHttpListener()
				: base("Exception handling tests", typeof(UserService).Assembly) { }

			public override void Configure(Container container)
			{
			}
		}

		ExceptionHandlingAppHostHttpListener appHost;

		[TestFixtureSetUp]
		public void OnTestFixtureSetUp()
		{
			appHost = new ExceptionHandlingAppHostHttpListener();
			appHost.Init();
			appHost.Start(ListeningOn);
		}

		[TestFixtureTearDown]
		public void OnTestFixtureTearDown()
		{
			appHost.Dispose();
		}

		static IRestClient[] ServiceClients = 
		{
			new JsonServiceClient(ListeningOn),
			new XmlServiceClient(ListeningOn),
			new JsvServiceClient(ListeningOn)
			//SOAP not supported in HttpListener
			//new Soap11ServiceClient(ServiceClientBaseUri),
			//new Soap12ServiceClient(ServiceClientBaseUri)
		};


		[Test, TestCaseSource("ServiceClients")]
		public void Handles_Returned_Http_Error(IRestClient client)
		{
			try
			{
				client.Get<UserResponse>("/users");
				Assert.Fail();
			}
			catch (WebServiceException ex)
			{
				Assert.That(ex.ErrorCode, Is.EqualTo("CanNotExecute"));
				Assert.That(ex.StatusCode, Is.EqualTo((int)System.Net.HttpStatusCode.BadRequest));
				Assert.That(ex.Message, Is.EqualTo("CanNotExecute"));
			}
		}

		[Test, TestCaseSource("ServiceClients")]
		public void Handles_Thrown_Http_Error(IRestClient client)
		{
			try
			{
				client.Post<UserResponse>("/users", new User());
				Assert.Fail();
			}
			catch (WebServiceException ex)
			{
				Assert.That(ex.ErrorCode, Is.EqualTo("CanNotExecute"));
				Assert.That(ex.StatusCode, Is.EqualTo((int)System.Net.HttpStatusCode.BadRequest));
				Assert.That(ex.Message, Is.EqualTo("CanNotExecute"));
			}
		}

		[Test, TestCaseSource("ServiceClients")]
		public void Handles_Normal_Exception(IRestClient client)
		{
			try
			{
				client.Put<UserResponse>("/users", new User());
				Assert.Fail();
			}
			catch (WebServiceException ex)
			{
				Assert.That(ex.ErrorCode, Is.EqualTo("ArgumentException"));
				Assert.That(ex.StatusCode, Is.EqualTo((int)System.Net.HttpStatusCode.BadRequest));
			}
		}
	}
}
