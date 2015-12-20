using NUnit.Framework;
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
            var response = Client.Post<AuthenticateResponse>("/auth",
                new Authenticate { UserName = "test1", Password = "test1" });

        }

        [TestFixtureTearDown]
        public void OnTestFixtureTearDown()
        {
            Client.Get<AuthenticateResponse>("/auth/logout");
        }
    }
}
