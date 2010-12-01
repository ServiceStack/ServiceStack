using System;
using NUnit.Framework;
using ServiceStack.Service;
using ServiceStack.ServiceClient.Web;
using ServiceStack.Text;
using ServiceStack.WebHost.IntegrationTests.Operations;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
	public abstract class IntegrationTestsBase
	{
		protected const string ServiceClientBaseUri = "http://localhost/ServiceStack.WebHost.IntegrationTests/ServiceStack";

		protected abstract IServiceClient CreateNewServiceClient();

		private const string TestString = "ServiceStack";

		[Test]
		public void Can_ReverseService()
		{
			using (var client = CreateNewServiceClient())
			{
				var response = client.Send<ReverseResponse>(new Reverse { Value = TestString });

				var expectedValue = ReverseService.Execute(TestString);
				Assert.That(response.Result, Is.EqualTo(expectedValue));
			}
		}

		[Test]
		public void Can_Rot13Service()
		{
			using (var client = CreateNewServiceClient())
			{
				var response = client.Send<Rot13Response>(new Rot13 { Value = TestString });

				var expectedValue = TestString.ToRot13();
				Assert.That(response.Result, Is.EqualTo(expectedValue));
			}
		}

		[Test]
		public void Can_Handle_Exception_from_AlwaysThrowService()
		{
			using (var client = CreateNewServiceClient())
			{
				var response = client.Send<AlwaysThrowsResponse>(new AlwaysThrows { Value = TestString });

				var expectedError = AlwaysThrowsService.GetErrorMessage(TestString);
				Assert.That(response.ResponseStatus.ErrorCode, 
					Is.EqualTo(typeof(NotImplementedException).Name));
				Assert.That(response.ResponseStatus.Message, 
					Is.EqualTo(expectedError));
			}
		}
	}

	[TestFixture]
	public class XmlIntegrationTests : IntegrationTestsBase
	{
		protected override IServiceClient CreateNewServiceClient()
		{
			return new XmlServiceClient(ServiceClientBaseUri);
		}
	}

	[TestFixture]
	public class JsonIntegrationTests : IntegrationTestsBase
	{
		protected override IServiceClient CreateNewServiceClient()
		{
			return new JsonServiceClient(ServiceClientBaseUri);
		}
	}

	[TestFixture]
	public class JsvIntegrationTests : IntegrationTestsBase
	{
		protected override IServiceClient CreateNewServiceClient()
		{
			return new JsvServiceClient(ServiceClientBaseUri);
		}
	}

	[TestFixture]
	public class Soap11IntegrationTests : IntegrationTestsBase
	{
		protected override IServiceClient CreateNewServiceClient()
		{
			return new Soap11ServiceClient(ServiceClientBaseUri);
		}
	}

	[TestFixture]
	public class Soap12IntegrationTests : IntegrationTestsBase
	{
		protected override IServiceClient CreateNewServiceClient()
		{
			return new Soap12ServiceClient(ServiceClientBaseUri);
		}
	}
}