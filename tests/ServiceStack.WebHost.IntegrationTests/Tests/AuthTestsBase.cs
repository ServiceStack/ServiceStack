using System;
using System.Net;
using NUnit.Framework;
using ServiceStack.Service;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.Text;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
	public class AuthTestsBase
	{
		private const string BaseUri = Config.ServiceStackBaseUri;
		public const string AdminEmail = "admin@servicestack.com";
		private const string AdminPassword = "E8828A3E26884CE0B345D0D2DFED358A";

		private IServiceClient serviceClient;
		public IServiceClient ServiceClient
		{
			get
			{
				return serviceClient
					?? (serviceClient = new JsonServiceClient(BaseUri));
			}
		}

		public Registration CreateAdminUser()
		{
			var registration = new Registration {
				UserName = "Admin",
				DisplayName = "The Admin User",
				Email = AdminEmail, //this email is automatically assigned as Admin in Web.Config
				FirstName = "Admin",
				LastName = "User",
				Password = AdminPassword,
			};
			try
			{
				ServiceClient.Send<RegistrationResponse>(registration);
			}
			catch (WebServiceException ex)
			{
				Console.WriteLine("Error while creating Admin User: " + ex.Message);
				Console.WriteLine(ex.ResponseDto.Dump());
			}
			return registration;
		}

		public JsonServiceClient Login(string userName, string password)
		{
			var client = new JsonServiceClient(BaseUri);
			client.Send<AuthResponse>(new Auth {
				UserName = userName,
				Password = password,
				RememberMe = true,
			});

			return client;
		}

		public JsonServiceClient AuthenticateWithAdminUser()
		{
			var registration = CreateAdminUser();
			var adminServiceClient = new JsonServiceClient(BaseUri);
			adminServiceClient.Send<AuthResponse>(new Auth {
				UserName = registration.UserName,
				Password = registration.Password,
				RememberMe = true,
			});

			return adminServiceClient;
		}

		protected void AssertUnAuthorized(WebServiceException webEx)
		{
			Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
			Assert.That(webEx.StatusDescription, Is.EqualTo(HttpStatusCode.Unauthorized.ToString()));
		}

	}

}