using NUnit.Framework;

namespace ServiceStack.AuthWeb.Tests
{
    [TestFixture]
    public class AuthWebTests
    {
        public const string BaseUri = "http://localhost:11001/";

        [Test]
        public void Can_authenticate_with_WindowsAuth()
        {
            var client = new JsonServiceClient(BaseUri);

            var response = client.Get(new RequiresAuth { Name = "Haz Access!" });

            Assert.That(response.Name, Is.EqualTo("Haz Access!"));
        }
    }
}