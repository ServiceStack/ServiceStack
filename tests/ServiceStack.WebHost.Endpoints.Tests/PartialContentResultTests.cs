using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Funq;
using NUnit.Framework;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Support.Mocks;
using ServiceStack.WebHost.Endpoints.Tests.Mocks;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [Route("/partialfiles/{RelativePath*}")]
    public class PartialFile
    {
        public string RelativePath { get; set; }

        public string MimeType { get; set; }
    }

    [Route("/partialfiles/memory")]
    public class PartialFromMemory { }

    [Route("/partialfiles/text")]
    public class PartialFromText { }

    public class PartialContentService : ServiceInterface.Service
    {
        public object Get(PartialFile request)
        {
            if (request.RelativePath.IsNullOrEmpty())
                throw new ArgumentNullException("RelativePath");

            string filePath = "~/{0}".Fmt(request.RelativePath).MapProjectPath();
            if (!File.Exists(filePath))
                throw new FileNotFoundException(request.RelativePath);

            return new HttpResult(new FileInfo(filePath), request.MimeType);
        }

        public object Get(PartialFromMemory request)
        {
            var customText = "123456789012345678901234567890";
            var customTextBytes = customText.ToUtf8Bytes();
            var ms = new MemoryStream();
            ms.Write(customTextBytes, 0, customTextBytes.Length);

            var httpResult = new HttpResult(ms, "audio/mpeg");
            return httpResult;
        }

        public object Get(PartialFromText request)
        {
            const string customText = "123456789012345678901234567890";
            var httpResult = new HttpResult(customText, "text/plain");
            return httpResult;
        }
    }

    public class PartialContentAppHost : AppHostHttpListenerBase
    {
        public PartialContentAppHost() : base(typeof(PartialFile).Name, typeof(PartialFile).Assembly) { }
        public override void Configure(Container container) {}
    }

    [TestFixture]
    public class PartialContentResultTests
    {
        public const string BaseUri = Config.ServiceStackBaseUri;
        public const string ListeningOn = Config.AbsoluteBaseUri;

        private PartialContentAppHost appHost;

        readonly FileInfo uploadedFile = new FileInfo("~/TestExistingDir/upload.html".MapProjectPath());
        readonly FileInfo uploadedTextFile = new FileInfo("~/TestExistingDir/textfile.txt".MapProjectPath());

        [TestFixtureSetUp]
        public void TextFixtureSetUp()
        {
            try
            {
                appHost = new PartialContentAppHost();
                appHost.Init();
                appHost.Start(ListeningOn);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            if (appHost != null) appHost.Dispose();
            appHost = null;
        }

        [Test]
        public void Can_StaticFile_GET_200_OK_response_for_file_with_no_range_header()
        {
            "File size {0}".Print(uploadedFile.Length);

            byte[] actualContents = "{0}/TestExistingDir/upload.html".Fmt(BaseUri).GetBytesFromUrl(
                responseFilter: httpRes => "Content-Length header {0}".Print(httpRes.Headers["Content-Length"]));

            "response size {0}".Fmt(actualContents.Length);

            Assert.That(actualContents.Length, Is.EqualTo(uploadedFile.Length));
        }

        [Test]
        public void Can_GET_200_OK_response_for_file_with_no_range_header()
        {
            "File size {0}".Print(uploadedFile.Length);

            byte[] actualContents = "{0}/partialfiles/TestExistingDir/upload.html".Fmt(BaseUri).GetBytesFromUrl(
                responseFilter: httpRes => "Content-Length header {0}".Print(httpRes.Headers["Content-Length"]));

            "response size {0}".Fmt(actualContents.Length);

            Assert.That(actualContents.Length, Is.EqualTo(uploadedFile.Length));
        }

        [Test]
        public void Can_StaticFile_GET_206_Partial_response_for_file_with_range_header()
        {
            var actualContents = "{0}/TestExistingDir/upload.html".Fmt(BaseUri).GetStringFromUrl(
                requestFilter: httpReq => httpReq.AddRange(5, 11),
                responseFilter: httpRes =>
                {
                    "Content-Length header {0}".Print(httpRes.Headers["Content-Length"]);
                    Assert.That(httpRes.ContentType, Is.EqualTo(MimeTypes.GetMimeType(uploadedFile.Name)));
                });

            "Response length {0}".Print(actualContents.Length);
            Assert.That(actualContents, Is.EqualTo("DOCTYPE"));
        }

        [Test]
        public void Can_GET_206_Partial_response_for_file_with_range_header()
        {
            var actualContents = "{0}/partialfiles/TestExistingDir/upload.html".Fmt(BaseUri).GetStringFromUrl(
                requestFilter: httpReq => httpReq.AddRange(5, 11),
                responseFilter: httpRes =>
                {
                    "Content-Length header {0}".Print(httpRes.Headers["Content-Length"]);
                    Assert.That(httpRes.ContentType, Is.EqualTo(MimeTypes.GetMimeType(uploadedFile.Name)));
                });

            "Response length {0}".Print(actualContents.Length);
            Assert.That(actualContents, Is.EqualTo("DOCTYPE"));
        }

        [Test]
        public void Can_GET_206_Partial_response_for_memory_with_range_header()
        {
            var actualContents = "{0}/partialfiles/memory?mimeType=audio/mpeg".Fmt(BaseUri).GetStringFromUrl(
                requestFilter: httpReq => httpReq.AddRange(5, 9),
                responseFilter: httpRes => "Content-Length header {0}".Print(httpRes.Headers["Content-Length"]));

            "Response Length {0}".Print(actualContents.Length);
            Assert.That(actualContents, Is.EqualTo("67890"));
        }

        [Test]
        public void Can_GET_206_Partial_response_for_text_with_range_header()
        {
            var actualContents = "{0}/partialfiles/text".Fmt(BaseUri).GetStringFromUrl(
                requestFilter: httpReq => httpReq.AddRange(5, 9),
                responseFilter: httpRes => "Content-Length header {0}".Print(httpRes.Headers["Content-Length"]));

            "Response Length {0}".Print(actualContents.Length);
            Assert.That(actualContents, Is.EqualTo("67890"));
        }

        [Test]
        public void Can_respond_to_non_range_requests_with_200_OK_response()
        {
            var mockRequest = new HttpRequestMock();
            var mockResponse = new HttpResponseMock();

            string customText = "1234567890";
            byte[] customTextBytes = customText.ToUtf8Bytes();
            var ms = new MemoryStream();
            ms.Write(customTextBytes, 0, customTextBytes.Length);

            var httpResult = new HttpResult(ms, "audio/mpeg");            

            bool reponseWasAutoHandled = mockResponse.WriteToResponse(mockRequest, httpResult);
            Assert.That(reponseWasAutoHandled, Is.True);

            string writtenString = mockResponse.GetOutputStreamAsString();
            Assert.That(writtenString, Is.EqualTo(customText));

            Assert.That(mockResponse.Headers["Content-Range"], Is.Null);
            Assert.That(mockResponse.Headers["Accept-Ranges"], Is.EqualTo("bytes"));
            Assert.That(mockResponse.StatusCode, Is.EqualTo(200));
        }

        [Test]
        public void Can_seek_from_beginning_to_end()
        {
            var mockRequest = new HttpRequestMock();
            var mockResponse = new HttpResponseMock();

            mockRequest.Headers[HttpHeaders.Range] = "bytes=0";

            string customText = "1234567890";
            byte[] customTextBytes = customText.ToUtf8Bytes();
            var ms = new MemoryStream();
            ms.Write(customTextBytes, 0, customTextBytes.Length);

            var httpResult = new HttpResult(ms, "audio/mpeg");

            bool reponseWasAutoHandled = mockResponse.WriteToResponse(mockRequest, httpResult);
            Assert.That(reponseWasAutoHandled, Is.True);

            string writtenString = mockResponse.GetOutputStreamAsString();
            Assert.That(writtenString, Is.EqualTo(customText));

            Assert.That(mockResponse.Headers["Content-Range"], Is.EqualTo("bytes 0-9/10"));
            Assert.That(mockResponse.Headers["Content-Length"], Is.EqualTo(writtenString.Length.ToString()));
            Assert.That(mockResponse.Headers["Accept-Ranges"], Is.EqualTo("bytes"));
            Assert.That(mockResponse.StatusCode, Is.EqualTo(206));
        }

        [Test]
        public void Can_seek_from_beginning_to_middle()
        {
            var mockRequest = new HttpRequestMock();
            var mockResponse = new HttpResponseMock();

            mockRequest.Headers[HttpHeaders.Range] = "bytes=0-2";

            string customText = "1234567890";
            byte[] customTextBytes = customText.ToUtf8Bytes();
            var ms = new MemoryStream();
            ms.Write(customTextBytes, 0, customTextBytes.Length);


            var httpResult = new HttpResult(ms, "audio/mpeg");

            bool reponseWasAutoHandled = mockResponse.WriteToResponse(mockRequest, httpResult);
            Assert.That(reponseWasAutoHandled, Is.True);

            string writtenString = mockResponse.GetOutputStreamAsString();
            Assert.That(writtenString, Is.EqualTo("123"));

            Assert.That(mockResponse.Headers["Content-Range"], Is.EqualTo("bytes 0-2/10"));
            Assert.That(mockResponse.Headers["Content-Length"], Is.EqualTo(writtenString.Length.ToString()));
            Assert.That(mockResponse.Headers["Accept-Ranges"], Is.EqualTo("bytes"));
            Assert.That(mockResponse.StatusCode, Is.EqualTo(206));
        }

        [Test]
        public void Can_seek_from_middle_to_end()
        {
            var mockRequest = new HttpRequestMock();
            mockRequest.Headers.Add("Range", "bytes=4-");
            var mockResponse = new HttpResponseMock();

            string customText = "1234567890";
            byte[] customTextBytes = customText.ToUtf8Bytes();
            var ms = new MemoryStream();
            ms.Write(customTextBytes, 0, customTextBytes.Length);


            var httpResult = new HttpResult(ms, "audio/mpeg");

            bool reponseWasAutoHandled = mockResponse.WriteToResponse(mockRequest, httpResult);
            Assert.That(reponseWasAutoHandled, Is.True);

            string writtenString = mockResponse.GetOutputStreamAsString();
            Assert.That(writtenString, Is.EqualTo("567890"));

            Assert.That(mockResponse.Headers["Content-Range"], Is.EqualTo("bytes 4-9/10"));
            Assert.That(mockResponse.Headers["Content-Length"], Is.EqualTo(writtenString.Length.ToString()));
            Assert.That(mockResponse.Headers["Accept-Ranges"], Is.EqualTo("bytes"));
            Assert.That(mockResponse.StatusCode, Is.EqualTo(206));
        }

        [Test]
        public void Can_seek_from_middle_to_middle()
        {
            var mockRequest = new HttpRequestMock();
            mockRequest.Headers.Add("Range", "bytes=3-5");
            var mockResponse = new HttpResponseMock();

            string customText = "1234567890";
            byte[] customTextBytes = customText.ToUtf8Bytes();
            var ms = new MemoryStream();
            ms.Write(customTextBytes, 0, customTextBytes.Length);


            var httpResult = new HttpResult(ms, "audio/mpeg");

            bool reponseWasAutoHandled = mockResponse.WriteToResponse(mockRequest, httpResult);
            Assert.That(reponseWasAutoHandled, Is.True);

            string writtenString = mockResponse.GetOutputStreamAsString();
            Assert.That(writtenString, Is.EqualTo("456"));

            Assert.That(mockResponse.Headers["Content-Range"], Is.EqualTo("bytes 3-5/10"));
            Assert.That(mockResponse.Headers["Content-Length"], Is.EqualTo(writtenString.Length.ToString()));
            Assert.That(mockResponse.Headers["Accept-Ranges"], Is.EqualTo("bytes"));
            Assert.That(mockResponse.StatusCode, Is.EqualTo(206));
        }

        [Test]
        public void Can_use_fileStream()
        {
            byte[] fileBytes = uploadedTextFile.ReadFully();
            string fileText = Encoding.ASCII.GetString(fileBytes);

            "File content size {0}".Print(fileBytes.Length);
            "File content is {0}".Print(fileText);

            var mockRequest = new HttpRequestMock();
            var mockResponse = new HttpResponseMock();
            mockRequest.Headers.Add("Range", "bytes=6-8");

            var httpResult = new HttpResult(uploadedTextFile, "audio/mpeg");

            bool reponseWasAutoHandled = mockResponse.WriteToResponse(mockRequest, httpResult);
            Assert.That(reponseWasAutoHandled, Is.True);

            string writtenString = mockResponse.GetOutputStreamAsString();
            Assert.That(writtenString, Is.EqualTo(fileText.Substring(6, 3)));

            Assert.That(mockResponse.Headers["Content-Range"], Is.EqualTo("bytes 6-8/33"));
            Assert.That(mockResponse.Headers["Content-Length"], Is.EqualTo(writtenString.Length.ToString()));
            Assert.That(mockResponse.Headers["Accept-Ranges"], Is.EqualTo("bytes"));
            Assert.That(mockResponse.StatusCode, Is.EqualTo(206));
        }

        [Test]
        [Explicit("Helps debugging when you need to find out WTF is going on")]
        public void Run_for_30secs()
        {
            Thread.Sleep(30000);
        }
    }
}