using NUnit.Framework;
using Moq;
using ServiceStack.Host;
using ServiceStack.Web;

namespace ServiceStack.ServiceHost.Tests
{
    [TestFixture]
    class HttpRequestAuthenticatonTests
    {
        [Test]
        public void Correct_commas_in_digestAuth_parsing()
        {
            var requestMock = new Mock<IHttpRequest>();
            const string authHeader = "Digest username=\"кенкен\", realm=\"SWP\", nonce=\"NjM1MDk1NjA0NjExMjMuMTozOGVkMDcyYWQ1ODY5NzhhYTIxODAwNzkyYzRiNzZmYw==\", uri=\"/api/v1/projects/2969/tests?select=metadata,results\", response=\"5f818c8d263e26e787d75b60b78157d1\", qop=auth, nc=00000001, cnonce=\"7e06df0b911151b2\", ";
            var headers = PclExportClient.Instance.NewNameValueCollection();
            headers.Add("Authorization", authHeader);

            requestMock.Setup(e => e.Headers).Returns(headers);

            var res = requestMock.Object.GetDigestAuth();

            Assert.NotNull(res);
        }

        [Test]
        public void Should_Return_Null_When_Authorization_Header_Is_Missing()
        {
            // PREPARE
            var sut = new Mock<IRequest>();
            var headers = PclExportClient.Instance.NewNameValueCollection();
            sut.Setup(e => e.Headers).Returns(headers);

            // RUN
            var bearerToken = sut.Object.GetBearerToken();

            // ASSERT
            Assert.Null(bearerToken);
        }

        [Test]
        public void Should_Return_Null_As_Bearer_Token_When_Authorization_Header_Is_Empty()
        {
            // PREPARE
            var sut = new Mock<IRequest>();
            var headers = PclExportClient.Instance.NewNameValueCollection();
            headers.Add("Authorization", string.Empty);

            sut.Setup(e => e.Headers).Returns(headers);

            // RUN
            var bearerToken = sut.Object.GetBearerToken();

            // ASSERT
            Assert.Null(bearerToken);
        }

        [Test]
        public void Should_Return_Null_As_Bearer_Token_When_Authorization_Header_Does_Not_Prefix()
        {
            // PREPARE
            var sut = new Mock<IRequest>();
            var headers = PclExportClient.Instance.NewNameValueCollection();
            headers.Add("Authorization", "Blablabla");
            sut.Setup(e => e.Headers).Returns(headers);

            // RUN
            var bearerToken = sut.Object.GetBearerToken();

            // ASSERT
            Assert.Null(bearerToken);
        }

        [Test]
        public void Should_Return_Null_As_Bearer_Token_When_Authorization_Header_Does_Not_Contain_Bearer()
        {
            // PREPARE
            var sut = new Mock<IRequest>();
            var headers = PclExportClient.Instance.NewNameValueCollection();
            headers.Add("Authorization", "Basic blablabla");
            sut.Setup(e => e.Headers).Returns(headers);

            // RUN
            var bearerToken = sut.Object.GetBearerToken();

            // ASSERT
            Assert.Null(bearerToken);
        }



        [Test]
        public void Can_Return_Bearer_Token()
        {
            // PREPARE
            var sut = new Mock<IRequest>();
            var headers = PclExportClient.Instance.NewNameValueCollection();
            headers.Add("Authorization", "Bearer blablabla");
            sut.Setup(e => e.Headers).Returns(headers);

            // RUN
            var bearerToken = sut.Object.GetBearerToken();

            // ASSERT
            Assert.True("blablabla" == bearerToken);
        }
    }
}
