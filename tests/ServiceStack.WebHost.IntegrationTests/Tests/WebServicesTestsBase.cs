using System;
using NUnit.Framework;
using ServiceStack.Service;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.IntegrationTests.Operations;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
	/// <summary>
	/// This base class executes all Web Services ignorant of the endpoints its hosted on.
	/// The same tests below are re-used by the Unit and Integration TestFixture's declared below
	/// </summary>
	public abstract class WebServicesTestsBase
	{
		//All integration tests call the webservices at the following Website url
		protected const string ServiceClientBaseUri = "http://localhost/ServiceStack.WebHost.IntegrationTests/ServiceStack";

		protected abstract IServiceClient CreateNewServiceClient();

		private const string TestString = "ServiceStack";

		[Test]
		public void Does_Execute_ReverseService()
		{
			var client = CreateNewServiceClient();
			var response = client.Send<ReverseResponse>(
				new Reverse { Value = TestString });

			var expectedValue = ReverseService.Execute(TestString);
			Assert.That(response.Result, Is.EqualTo(expectedValue));
		}

		[Test]
		public void Does_Execute_Rot13Service()
		{
			var client = CreateNewServiceClient();
			var response = client.Send<Rot13Response>(new Rot13 { Value = TestString });

			var expectedValue = TestString.ToRot13();
			Assert.That(response.Result, Is.EqualTo(expectedValue));
		}

		[Test]
		public void Can_Handle_Exception_from_AlwaysThrowService()
		{
			var client = CreateNewServiceClient();
			var response = client.Send<AlwaysThrowsResponse>(
				new AlwaysThrows { Value = TestString });

			var expectedError = AlwaysThrowsService.GetErrorMessage(TestString);
			Assert.That(response.ResponseStatus.ErrorCode,
				Is.EqualTo(typeof(NotImplementedException).Name));
			Assert.That(response.ResponseStatus.Message,
				Is.EqualTo(expectedError));
		}
	}


	/// <summary>
	/// Unit tests simulates an in-process ServiceStack host where all services 
	/// are executed all in-memory so DTO's are not even serialized.
	/// </summary>
	[TestFixture]
	public class UnitTests : WebServicesTestsBase
	{
		public class DirectServiceClient : IServiceClient
		{
			readonly ServiceManager serviceManager = new ServiceManager(true, typeof(ReverseService).Assembly);

			public void SendOneWay(object request)
			{
				serviceManager.Execute(request);
			}

			public TResponse Send<TResponse>(object request)
			{
				var response = serviceManager.Execute(request);
				return (TResponse)response;
			}

			public void Dispose() { }
		}

		protected override IServiceClient CreateNewServiceClient()
		{
			return new DirectServiceClient();
		}
	}

	[TestFixture]
	public class XmlIntegrationTests : WebServicesTestsBase
	{
		protected override IServiceClient CreateNewServiceClient()
		{
			return new XmlServiceClient(ServiceClientBaseUri);
		}
	}

	[TestFixture]
	public class JsonIntegrationTests : WebServicesTestsBase
	{
		protected override IServiceClient CreateNewServiceClient()
		{
			return new JsonServiceClient(ServiceClientBaseUri);
		}
	}

	[TestFixture]
	public class JsvIntegrationTests : WebServicesTestsBase
	{
		protected override IServiceClient CreateNewServiceClient()
		{
			return new JsvServiceClient(ServiceClientBaseUri);
		}
	}

	[TestFixture]
	public class Soap11IntegrationTests : WebServicesTestsBase
	{
		protected override IServiceClient CreateNewServiceClient()
		{
			return new Soap11ServiceClient(ServiceClientBaseUri);
		}
	}

	[TestFixture]
	public class Soap12IntegrationTests : WebServicesTestsBase
	{
		protected override IServiceClient CreateNewServiceClient()
		{
			return new Soap12ServiceClient(ServiceClientBaseUri);
		}
	}

}