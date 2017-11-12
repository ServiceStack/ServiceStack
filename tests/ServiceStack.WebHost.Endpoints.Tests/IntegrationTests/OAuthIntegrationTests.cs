using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.IntegrationTests
{
    [Ignore("Integration Test")]
    public class OAuthIntegrationTests
    {
        private Dictionary<string,string> AccessTokens { get; set; }

        public OAuthIntegrationTests()
        {
            AccessTokens = "~/App_Data/accesstokens.txt".MapProjectPath()
                .ReadAllText()
                .ParseKeyValueText(delimiter:" ");
        }

        [Test]
        public void Can_authenticate_twitter_with_AccessToken()
        {
            var client = new JsonServiceClient("http://localhost:11001/");

            var request = new Authenticate
            {
                provider = TwitterAuthProvider.Name,
                AccessToken = "2931572242-zmVKk5leFHJXJWRUpQqyEkdlRlNbDMjNlUcXViJ",
                AccessTokenSecret = AccessTokens[TwitterAuthProvider.Name]
            };

            var response = client.Post(request);

            response.PrintDump();

            Assert.That(response.UserId, Is.Not.Null);
            Assert.That(response.SessionId, Is.Not.Null);
            Assert.That(response.DisplayName, Is.EqualTo("TechStacks"));
        }

        [Test]
        public void Can_authenticate_facebook_with_AccessToken()
        {
            var client = new JsonServiceClient("http://localhost:11001/");

            var request = new Authenticate
            {
                provider = FacebookAuthProvider.Name,
                AccessToken = AccessTokens[FacebookAuthProvider.Name],
            };

            var response = client.Post(request);

            response.PrintDump();

            Assert.That(response.UserId, Is.Not.Null);
            Assert.That(response.SessionId, Is.Not.Null);
            Assert.That(response.DisplayName, Is.EqualTo("Demis Bellot"));
        }

        [Test]
        public void Can_authenticate_github_with_AccessToken()
        {
            var client = new JsonServiceClient("http://localhost:11001/");

            var request = new Authenticate
            {
                provider = GithubAuthProvider.Name,
                AccessToken = AccessTokens[GithubAuthProvider.Name],
            };

            var response = client.Post(request);

            response.PrintDump();

            Assert.That(response.UserId, Is.Not.Null);
            Assert.That(response.SessionId, Is.Not.Null);
            Assert.That(response.UserName, Is.EqualTo("mythz"));
            Assert.That(response.DisplayName, Is.EqualTo("Demis Bellot"));
        }

        [Test]
        public void Can_authenticate_GoogleOAuth2_with_AccessToken()
        {
            //var client = new JsonServiceClient("http://localhost:11001/");
            var client = new JsonServiceClient("http://localhost:1337/");

            var request = new Authenticate
            {
                provider = "GoogleOAuth",
                AccessToken = AccessTokens["GoogleOAuth"],
            };

            var response = client.Post(request);

            response.PrintDump();

            Assert.That(response.UserId, Is.Not.Null);
            Assert.That(response.SessionId, Is.Not.Null);
        }
    }
}