using System;
using NUnit.Framework;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.Text;

namespace ServiceStack.Auth.Tests
{
    public class TestBase
    {
        protected JsonServiceClient Client { get; set; }
        protected static readonly string BaseUri = "http://localhost:8080/api";

        [TestFixtureSetUp]
        public void OnTestFixtureSetUp()
        {
            Client = new JsonServiceClient(BaseUri);
            var response= Client.Post<AuthResponse>("/auth",
                new ServiceInterface.Auth.Auth { UserName = "test1", Password = "test1" });

            Console.WriteLine(response.Dump());
        }

        [TestFixtureTearDown]
        public void OnTestFixtureTearDown()
        {
            Client.Get<AuthResponse>("/auth/logout");
        }
    }

}
