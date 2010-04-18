using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ServiceStack.Examples.Host.Console;
using ServiceStack.ServiceClient.Web;

namespace ServiceStack.Examples.Tests.Integration
{
	[TestFixture]
	public class IntegrationTestBase
	{
		private const string BaseUrl = "http://127.0.0.1:8080/";
		private AppHost appHost;

		[TestFixtureSetUp]
		public virtual void TestFixtureSetUp()
		{
			appHost = new AppHost();
			appHost.Init();
			try
			{
				appHost.Start(BaseUrl);
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error trying to run ConsoleHost: " + ex.Message);
			}
		}

		/// <summary>
		/// Run the request against each Endpoint
		/// </summary>
		/// <typeparam name="TRes"></typeparam>
		/// <param name="request"></param>
		/// <param name="validate"></param>
		public void SendToEachEndpoint<TRes>(object request, Action<TRes> validate)
		{
			using (var xmlClient = new XmlServiceClient(BaseUrl))
			using (var jsonClient = new JsonServiceClient(BaseUrl))
			using (var jsvClient = new JsvServiceClient(BaseUrl))
			{
				var xmlResponse = xmlClient.Send<TRes>(request);
				validate(xmlResponse);

				var jsonResponse = jsonClient.Send<TRes>(request);
				validate(jsonResponse);

				var jsvResponse = jsvClient.Send<TRes>(request);
				validate(jsvResponse);
			}
		}
	}
}
