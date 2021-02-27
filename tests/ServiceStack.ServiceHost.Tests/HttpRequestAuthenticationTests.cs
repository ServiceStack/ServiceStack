using System.Collections.Specialized;
using NUnit.Framework;
using ServiceStack.Host;
using ServiceStack.Testing;

namespace ServiceStack.ServiceHost.Tests
{
    [TestFixture]
    class HttpRequestAuthenticationTests
    {
        private readonly ServiceStackHost appHost;
        public HttpRequestAuthenticationTests() => appHost = new BasicAppHost().Init();
        [OneTimeTearDown] public void OneTimeTearDown() => appHost.Dispose();

        private static BasicRequest CreateRequest(string authHeader) => new BasicRequest {
            Headers = new NameValueCollection { {HttpHeaders.Authorization, authHeader} }
        };

        [Test]
        public void Correct_commas_in_digestAuth_parsing()
        {
            const string authHeader = "Digest username=\"кенкен\", realm=\"SWP\", nonce=\"NjM1MDk1NjA0NjExMjMuMTozOGVkMDcyYWQ1ODY5NzhhYTIxODAwNzkyYzRiNzZmYw==\", uri=\"/api/v1/projects/2969/tests?select=metadata,results\", response=\"5f818c8d263e26e787d75b60b78157d1\", qop=auth, nc=00000001, cnonce=\"7e06df0b911151b2\", ";
            var req = CreateRequest(authHeader);

            var res = req.GetDigestAuth();

            Assert.NotNull(res);
        }

        [Test]
        public void Should_Return_Null_When_Authorization_Header_Is_Missing()
        {
            var req = CreateRequest(null);
            var bearerToken = req.GetBearerToken();

            Assert.Null(bearerToken);
        }

        [Test]
        public void Should_Return_Null_As_Bearer_Token_When_Authorization_Header_Is_Empty()
        {
            var req = CreateRequest(string.Empty);
            var bearerToken = req.GetBearerToken();

            Assert.Null(bearerToken);
        }

        [Test]
        public void Should_Return_Null_As_Bearer_Token_When_Authorization_Header_Does_Not_Prefix()
        {
            var req = CreateRequest("Blablabla");
            var bearerToken = req.GetBearerToken();

            Assert.Null(bearerToken);
        }

        [Test]
        public void Should_Return_Null_As_Bearer_Token_When_Authorization_Header_Does_Not_Contain_Bearer()
        {
            var req = CreateRequest("Basic blablabla");
            var bearerToken = req.GetBearerToken();

            Assert.Null(bearerToken);
        }

        [Test]
        public void Can_Return_Bearer_Token()
        {
            var req = CreateRequest("Bearer blablabla");
            var bearerToken = req.GetBearerToken();

            Assert.True("blablabla" == bearerToken);
        }
    }
}
