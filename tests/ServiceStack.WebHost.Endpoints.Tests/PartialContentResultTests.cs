using System.IO;
using Funq;
using NUnit.Framework;
using ServiceStack.Common.Web;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Testing;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Support.Mocks;
using ServiceStack.WebHost.Endpoints.Tests.Mocks;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [TestFixture]
    public class PartialContentResultTests : TestBase
    {
        protected override void Configure(Container container)
        {
        }

        [Test]
        public void Can_seek_from_beginning_to_middle()
        {
            var mockRequest = new HttpRequestMock();
            mockRequest.Headers.Add("Range","bytes=0-2");
            var mockResponse = new HttpResponseMock();

            var customText = "1234567890";
            var customTextBytes = customText.ToUtf8Bytes();
            var ms = new MemoryStream();
            ms.Write(customTextBytes, 0, customTextBytes.Length);


            var httpResult = new PartialContentResult(ms, ContentType.PlainText);

            var reponseWasAutoHandled = mockResponse.WriteToResponse(mockRequest,httpResult);
            Assert.That(reponseWasAutoHandled, Is.True);

            var writtenString = mockResponse.GetOutputStreamAsString();
            Assert.That(writtenString, Is.EqualTo("123"));

            Assert.That(mockResponse.Headers["Content-Range"], Is.EqualTo("bytes 0-2/10"));
            Assert.That(mockResponse.Headers["Content-Length"], Is.EqualTo(customTextBytes.Length.ToString()));
            Assert.That(mockResponse.Headers["Accept-Ranges"], Is.EqualTo("bytes"));
            Assert.That(mockResponse.StatusCode, Is.EqualTo(206));
        }

        [Test]
        public void Can_seek_from_beginning_to_end()
        {
            var mockRequest = new HttpRequestMock();
            mockRequest.Headers.Add("Range", "bytes=0");
            var mockResponse = new HttpResponseMock();

            var customText = "1234567890";
            var customTextBytes = customText.ToUtf8Bytes();
            var ms = new MemoryStream();
            ms.Write(customTextBytes, 0, customTextBytes.Length);


            var httpResult = new PartialContentResult(ms, ContentType.PlainText);

            var reponseWasAutoHandled = mockResponse.WriteToResponse(mockRequest, httpResult);
            Assert.That(reponseWasAutoHandled, Is.True);

            var writtenString = mockResponse.GetOutputStreamAsString();
            Assert.That(writtenString, Is.EqualTo(customText));

            Assert.That(mockResponse.Headers["Content-Range"], Is.EqualTo("bytes 0-9/10"));
            Assert.That(mockResponse.Headers["Content-Length"], Is.EqualTo(customTextBytes.Length.ToString()));
            Assert.That(mockResponse.Headers["Accept-Ranges"], Is.EqualTo("bytes"));
            Assert.That(mockResponse.StatusCode, Is.EqualTo(206));
        }

        [Test] public void Can_seek_from_middle_to_end()
        {
            var mockRequest = new HttpRequestMock();
            mockRequest.Headers.Add("Range", "bytes=4-");
            var mockResponse = new HttpResponseMock();

            var customText = "1234567890";
            var customTextBytes = customText.ToUtf8Bytes();
            var ms = new MemoryStream();
            ms.Write(customTextBytes, 0, customTextBytes.Length);


            var httpResult = new PartialContentResult(ms, ContentType.PlainText);

            var reponseWasAutoHandled = mockResponse.WriteToResponse(mockRequest, httpResult);
            Assert.That(reponseWasAutoHandled, Is.True);

            var writtenString = mockResponse.GetOutputStreamAsString();
            Assert.That(writtenString, Is.EqualTo("567890"));

            Assert.That(mockResponse.Headers["Content-Range"], Is.EqualTo("bytes 4-9/10"));
            Assert.That(mockResponse.Headers["Content-Length"], Is.EqualTo(customTextBytes.Length.ToString()));
            Assert.That(mockResponse.Headers["Accept-Ranges"], Is.EqualTo("bytes"));
            Assert.That(mockResponse.StatusCode, Is.EqualTo(206));
        }

        [Test]
        public void Can_seek_from_middle_to_middle()
        {
            var mockRequest = new HttpRequestMock();
            mockRequest.Headers.Add("Range", "bytes=3-5");
            var mockResponse = new HttpResponseMock();

            var customText = "1234567890";
            var customTextBytes = customText.ToUtf8Bytes();
            var ms = new MemoryStream();
            ms.Write(customTextBytes, 0, customTextBytes.Length);


            var httpResult = new PartialContentResult(ms, ContentType.PlainText);

            var reponseWasAutoHandled = mockResponse.WriteToResponse(mockRequest, httpResult);
            Assert.That(reponseWasAutoHandled, Is.True);

            var writtenString = mockResponse.GetOutputStreamAsString();
            Assert.That(writtenString, Is.EqualTo("456"));

            Assert.That(mockResponse.Headers["Content-Range"], Is.EqualTo("bytes 3-5/10"));
            Assert.That(mockResponse.Headers["Content-Length"], Is.EqualTo(customTextBytes.Length.ToString()));
            Assert.That(mockResponse.Headers["Accept-Ranges"], Is.EqualTo("bytes"));
            Assert.That(mockResponse.StatusCode, Is.EqualTo(206));
        }

        [Test]
        public void Can_respond_to_non_range_requests_with_200_OK_response()
        {
            var mockRequest = new HttpRequestMock();
            var mockResponse = new HttpResponseMock();

            var customText = "1234567890";
            var customTextBytes = customText.ToUtf8Bytes();
            var ms = new MemoryStream();
            ms.Write(customTextBytes, 0, customTextBytes.Length);

            var httpResult = new PartialContentResult(ms, ContentType.PlainText);

            var reponseWasAutoHandled = mockResponse.WriteToResponse(mockRequest, httpResult);
            Assert.That(reponseWasAutoHandled, Is.True);

            var writtenString = mockResponse.GetOutputStreamAsString();
            Assert.That(writtenString, Is.EqualTo(customText));

            Assert.That(mockResponse.Headers["Content-Range"], Is.Null);
            Assert.That(mockResponse.Headers["Content-Length"], Is.EqualTo(customTextBytes.Length.ToString()));
            Assert.That(mockResponse.Headers["Accept-Ranges"], Is.EqualTo("bytes"));
            Assert.That(mockResponse.StatusCode, Is.EqualTo(200));
        }
    }
}
