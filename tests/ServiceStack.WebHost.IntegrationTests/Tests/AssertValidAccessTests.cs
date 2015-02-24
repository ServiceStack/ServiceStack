using System;
using System.Collections.Generic;
using System.Net;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Text;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
	[TestFixture]
	public class AssertValidAccessTests : AuthTestsBase
	{
		protected Register register;

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
            Tracer.Instance = new Tracer.ConsoleTracer();
			register = CreateAdminUser();
		}

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            Tracer.Instance = new Tracer.NullTracer();
        }

		public string RoleName1 = "Role1";
		public string RoleName2 = "Role2";
		public const string ContentManager = "ContentManager";
		public const string ContentPermission = "ContentPermission";

		public string Permission1 = "Permission1";
		public string Permission2 = "Permission2";

		public Register RegisterNewUser(bool? autoLogin = null)
		{
			var userId = Environment.TickCount % 10000;

			var newUserRegistration = new Register {
				UserName = "UserName" + userId,
				DisplayName = "DisplayName" + userId,
				Email = "user{0}@sf.com".Fmt(userId),
				FirstName = "FirstName" + userId,
				LastName = "LastName" + userId,
				Password = "Password" + userId,
				AutoLogin = autoLogin,
			};

			ServiceClient.Send(newUserRegistration);

			return newUserRegistration;
		}

	    [Test]
	    public void Authenticationg_does_return_session_cookies()
	    {
            var client = AuthenticateWithAdminUser();
	        Assert.That(client.CookieContainer.Count, Is.GreaterThan(0));
	    }

		[Test]
		public void Cannot_assign_roles_with_normal_user()
		{
			var newUser = RegisterNewUser(autoLogin: true);

			try
			{
				var response = ServiceClient.Send(
				new AssignRoles {
					UserName = newUser.UserName,
					Roles = { RoleName1, RoleName2 },
					Permissions = { Permission1, Permission2 }
				});

				response.PrintDump();
				Assert.Fail("Should not be allowed");
			}
			catch (WebServiceException webEx)
			{
				Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Forbidden));
				//StatusDescription is ignored in WebDevServer
				//Assert.That(webEx.StatusDescription, Is.EqualTo("Invalid Role"));
			}
		}

		[Test]
		public void Can_Assign_Roles_and_Permissions_to_new_User()
		{
			var newUser = RegisterNewUser();

			var client = AuthenticateWithAdminUser();

			var response = client.Send(
				new AssignRoles {
					UserName = newUser.UserName,
					Roles = { RoleName1, RoleName2 },
					Permissions = { Permission1, Permission2 }
				});

			Console.WriteLine("Assigned Roles: " + response.Dump());

			Assert.That(response.AllRoles, Is.EquivalentTo(new[] { RoleName1, RoleName2 }));
			Assert.That(response.AllPermissions, Is.EquivalentTo(new[] { Permission1, Permission2 }));
		}

		[Test]
		public void Can_UnAssign_Roles_and_Permissions_to_new_User()
		{
			var newUser = RegisterNewUser();

			var client = AuthenticateWithAdminUser();

			client.Send(
			new AssignRoles {
				UserName = newUser.UserName,
				Roles = new List<string> { RoleName1, RoleName2 },
				Permissions = new List<string> { Permission1, Permission2 }
			});

			var response = client.Send(
			new UnAssignRoles {
				UserName = newUser.UserName,
				Roles = { RoleName1 },
				Permissions = { Permission2 },
			});

			Console.WriteLine("Remaining Roles: " + response.Dump());

			Assert.That(response.AllRoles, Is.EquivalentTo(new[] { RoleName2 }));
			Assert.That(response.AllPermissions, Is.EquivalentTo(new[] { Permission1 }));
		}

		[Test]
		public void Can_only_access_ContentManagerOnlyService_service_after_Assigned_Role()
		{
			var newUser = RegisterNewUser(autoLogin: true);

			try
			{
				ServiceClient.Send(new ContentManagerOnly());
				Assert.Fail("Should not be allowed - no roles");
			}
			catch (WebServiceException webEx)
			{
				Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Forbidden));
				//StatusDescription is ignored in WebDevServer
				//Assert.That(webEx.StatusDescription, Is.EqualTo("Invalid Role"));
			}

			var client = AuthenticateWithAdminUser();

			client.Send(
				new AssignRoles {
					UserName = newUser.UserName,
					Roles = new List<string> { RoleName1 },
				});

			var newUserClient = Login(newUser.UserName, newUser.Password);

			try
			{
				newUserClient.Send(new ContentManagerOnly());
				Assert.Fail("Should not be allowed - wrong roles");
			}
			catch (WebServiceException webEx)
			{
				Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Forbidden));
				//StatusDescription is ignored in WebDevServer
				//Assert.That(webEx.StatusDescription, Is.EqualTo("Invalid Role"));
			}

			var assignResponse = client.Send(
				new AssignRoles {
					UserName = newUser.UserName,
					Roles = new List<string> { ContentManager },
				});

			Assert.That(assignResponse.AllRoles, Is.EquivalentTo(new[] { RoleName1, ContentManager }));

			var response = newUserClient.Send(new ContentManagerOnly());

			Assert.That(response.Result, Is.EqualTo("Haz Access"));
		}

		[Test]
		public void Can_only_access_ContentPermissionOnlyService_service_after_Assigned_Permission()
		{
			var newUser = RegisterNewUser(autoLogin: true);

			try
			{
				ServiceClient.Send(new ContentPermissionOnly());
				Assert.Fail("Should not be allowed - no permissions");
			}
			catch (WebServiceException webEx)
			{
				Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Forbidden));
				//StatusDescription is ignored in WebDevServer
				//Assert.That(webEx.StatusDescription, Is.EqualTo("Invalid Permissions"));
			}

			var client = AuthenticateWithAdminUser();

			client.Send(
				new AssignRoles {
					UserName = newUser.UserName,
					Permissions = new List<string> { RoleName1 },
				});

			var newUserClient = Login(newUser.UserName, newUser.Password);

			try
			{
				newUserClient.Send(new ContentPermissionOnly());
				Assert.Fail("Should not be allowed - wrong permissions");
			}
			catch (WebServiceException webEx)
			{
				Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Forbidden));
				//StatusDescription is ignored in WebDevServer
				//Assert.That(webEx.StatusDescription, Is.EqualTo("Invalid Permissions"));
			}

			var assignResponse = client.Send(
				new AssignRoles {
					UserName = newUser.UserName,
					Permissions = new List<string> { ContentPermission },
				});

			Assert.That(assignResponse.AllPermissions, Is.EquivalentTo(new[] { RoleName1, ContentPermission }));

			var response = newUserClient.Send(new ContentPermissionOnly());

			Assert.That(response.Result, Is.EqualTo("Haz Access"));
		}

        [Test]
        public void Cannot_access_Admin_service_by_default()
        {
            try
            {
                BaseUri.AppendPath("requiresadmin").GetStringFromUrl();

                Assert.Fail("Should not allow access to protected resource");
            }
            catch (Exception ex)
            {
                if (ex.IsUnauthorized() || ex.IsAny400()) //redirect to login
                    return;

                throw;
            }
        }

        [Test]
        public void Can_access_Admin_service_with_AuthSecret()
        {
            BaseUri.AppendPath("requiresadmin")
                .AddQueryParam("authsecret", AuthSecret).GetStringFromUrl();
        }

	}
}