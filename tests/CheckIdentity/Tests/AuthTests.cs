using System.Collections.Generic;
using CheckIdentity.ServiceInterface;
using CheckIdentity.ServiceModel;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Text;

namespace CheckIdentity.Tests
{
    public class AuthTests
    {
        const string BaseUri = "https://localhost:5001/";
        const string Password = "testTest1!";
        public IServiceClient CreateClient() => new JsonServiceClient(BaseUri);
        
        public (IServiceClient, T) CreateClient<T>(IReturn<T> request)
        {
            var client = new JsonServiceClient(BaseUri);
            var response = client.Send(request);
            return (client, response);
        }

        private IServiceClient CreateAdminClient()
        {
            var client = CreateClient();
            client.AddHeader(HttpHeaders.XParamOverridePrefix + "authsecret", "secretz");
            return client;
        }

        private static Authenticate CreateAuthenticateUser()
        {
            return new Authenticate {
                provider = "credentials",
                UserName = "as@if.com",
                Password = Password,
                RememberMe = true,
            };
        }

        [Test]
        public void Can_credentials_auth_with_IdentityCredentialsAuthProvider_and_populate_JWTs()
        {
            var (client, response) = CreateClient(CreateAuthenticateUser());
            response.PrintDump();
            Assert.That(response.UserId, Is.Not.Empty);
            Assert.That(response.UserName, Is.Not.Empty);
            Assert.That(response.DisplayName, Is.Not.Empty);
            Assert.That(response.BearerToken, Is.Not.Empty);
            Assert.That(response.ProfileUrl, Is.Not.Empty);

            var obj = JwtAuthProviderReader.ExtractPayload(response.BearerToken);
            obj.PrintDump();
            Assert.That(obj["name"], Is.EqualTo("as@if.com"));

            var authResponse = client.Post(new HelloAuth { Name = "as@if.com" });
            Assert.That(authResponse.Result, Is.EqualTo("HelloAuth, as@if.com!"));
        }

        [Test]
        public void Can_credentials_auth_with_Admin_User()
        {
            var client = CreateAdminClient();

            var response = client.Post(new Authenticate {
                provider = "credentials",
                UserName = Keywords.AuthSecret,
                RememberMe = true,
            });
            
            response.PrintDump();
            Assert.That(response.UserId, Is.Not.Empty);
            Assert.That(response.UserName, Is.Not.Empty);
            Assert.That(response.DisplayName, Is.Not.Empty);
            Assert.That(response.BearerToken, Is.Not.Empty);
            Assert.That(response.ProfileUrl, Is.Not.Empty);

            var obj = JwtAuthProviderReader.ExtractPayload(response.BearerToken);
            obj.PrintDump();
            Assert.That(obj["name"], Is.EqualTo("authsecret"));

            var authResponse = client.Post(new HelloAuth { Name = "authsecret" });
            Assert.That(authResponse.Result, Is.EqualTo("HelloAuth, authsecret!"));
        }

        [Test]
        public void Can_authenticate_with_Admin_User()
        {
            var client = CreateAdminClient();

            var response = client.Post(new HelloAuth { Name = "Admin" });
            
            Assert.That(response.Result, Is.EqualTo("HelloAuth, Admin!"));
        }

        [Test]
        public void Can_authenticate_with_User_with_Role()
        {
            var (client, authResponse) = CreateClient(CreateAuthenticateUser());

            var response = client.Send(new HelloRole { Name = "as@if.com" });

            Assert.That(response.Result, Is.EqualTo("HelloRole, as@if.com!"));
        }

        [Test]
        public void Can_Assign_Roles()
        {
            var client = CreateAdminClient();

            var response = client.Post(new AssignRoles {
                UserName = "as@if.com",
                Roles = new List<string> { "TheRole" },
            });
            
            Assert.That(response.AllRoles, Is.EquivalentTo(new[]{ "TheRole" }));
        }

        [Test]
        public void Can_Create_Role()
        {
            var client = CreateAdminClient();
            var response = client.Post(new CreateRole { Role = "TheRole" });
            response.PrintDump();
        }

        [Test]
        public void Can_Delete_user()
        {
            var client = CreateAdminClient();
            var response = client.Post(new DeleteUser { UserName = "new@user.com" });
            response.PrintDump();
        }

        [Test]
        public void Can_register_new_user()
        {
            var client = CreateClient();

            var response = client.Post(new Register {
                Email = "new@user.com",
                Password = Password,
                ConfirmPassword = Password,
                FirstName = "New",
                LastName = "User",
                DisplayName = "New User",
            });
            
            response.PrintDump();
        }

        [Test]
        public void Does_validate_min_password()
        {
            var client = CreateClient();

            try
            {
                var response = client.Post(new Register {
                    Email = "weak@password.com",
                    Password = "weak",
                    ConfirmPassword = "weak",
                    FirstName = "Weak",
                    LastName = "Password",
                    DisplayName = "Weak Password",
                });
                
                Assert.Fail("Should throw");
            }
            catch (WebServiceException e)
            {
                var status = e.GetResponseStatus();
                status.PrintDump();
                Assert.That(status.ErrorCode, Is.EqualTo("PasswordTooShort"));
                Assert.That(status.Message, Is.EqualTo("Passwords must be at least 6 characters."));

                var errors = status.Errors;
                Assert.That(errors[0].ErrorCode, Is.EqualTo("PasswordTooShort"));
                Assert.That(errors[0].Message, Is.EqualTo("Passwords must be at least 6 characters."));
                Assert.That(errors[1].ErrorCode, Is.EqualTo("PasswordRequiresNonAlphanumeric"));
                Assert.That(errors[1].Message, Is.EqualTo("Passwords must have at least one non alphanumeric character."));
                Assert.That(errors[2].ErrorCode, Is.EqualTo("PasswordRequiresDigit"));
                Assert.That(errors[2].Message, Is.EqualTo("Passwords must have at least one digit ('0'-'9')."));
                Assert.That(errors[3].ErrorCode, Is.EqualTo("PasswordRequiresUpper"));
                Assert.That(errors[3].Message, Is.EqualTo("Passwords must have at least one uppercase ('A'-'Z')."));
            }
        }

    }
}