using System;
using System.ServiceModel;
using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
	[TestFixture]
	public class Soap11AddServiceReferenceTests
	{
		private const string EndpointUri = "http://localhost/ServiceStack.WebHost.IntegrationTests/ServiceStack/Soap11";
		private Soap11ServiceReference.ISyncReply client;

		[SetUp]
		public void OnBeforeEachTest()
		{
			//Generated proxy when using 'Add Service Reference' on the EndpointUri above.
			//Thank WCF for the config ugliness
			client = new Soap11ServiceReference.SyncReplyClient(
				new BasicHttpBinding
				{
					MaxReceivedMessageSize = int.MaxValue,
					HostNameComparisonMode = HostNameComparisonMode.StrongWildcard
				},
				new EndpointAddress(EndpointUri));
		}

		private const string TestString = "ServiceStack";

		[Test]
		public void Does_Execute_ReverseService()
		{
			var response = client.Reverse(TestString);
			var expectedValue = ReverseService.Execute(TestString);
			Assert.That(response, Is.EqualTo(expectedValue));
		}

		[Test]
		public void Does_Execute_Rot13Service()
		{
			var response = client.Rot13(TestString);
			var expectedValue = TestString.ToRot13();
			Assert.That(response, Is.EqualTo(expectedValue));
		}

		[Test]
		public void Can_Handle_Exception_from_AlwaysThrowService()
		{
			string result;
			var responseStatus = client.AlwaysThrows(out result, TestString);

			var expectedError = AlwaysThrowsService.GetErrorMessage(TestString);
			Assert.That(responseStatus.ErrorCode,
				Is.EqualTo(typeof(NotImplementedException).Name));
			Assert.That(responseStatus.Message, Is.EqualTo(expectedError));
		}

	}
}