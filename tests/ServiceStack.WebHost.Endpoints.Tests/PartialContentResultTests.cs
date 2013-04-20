using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using NUnit.Framework;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Web;
using ServiceStack.ServiceInterface;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Support.Mocks;
using ServiceStack.WebHost.Endpoints.Tests.Mocks;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [TestFixture]
    public class PartialContentResultTests
    {
        public const string ListeningOn = "http://localhost:8083/";
        private ExampleAppHostHttpListener appHost;

        [TestFixtureSetUp]
        public void TextFixtureSetUp()
        {
            try
            {
                appHost = new ExampleAppHostHttpListener();
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
        public void Can_GET_200_OK_response_for_file_with_no_range_header()
        {
            string path = "~/TestExistingDir/upload.html".MapProjectPath();
            var file = new FileInfo(path);

            Console.WriteLine("File size {0}", file.Length);

            var webRequest =
                (HttpWebRequest)WebRequest.Create(ListeningOn + "/partialfiles/TestExistingDir/upload.html");

            WebResponse webResponse = webRequest.GetResponse();
            byte[] actualContents = webResponse.GetResponseStream().ReadFully();

            Assert.That(actualContents.Length, Is.EqualTo(file.Length));
            Console.WriteLine("response size {0}", actualContents.Length);
            Console.WriteLine("Content-Length header {0}", webResponse.Headers["Content-Length"]);
        }

        [Test]
        public void Can_GET_206_Partial_response_for_file_with_range_header()
        {
            var uploadedFile = new FileInfo("~/TestExistingDir/upload.html".MapProjectPath());
            var webRequest =
                (HttpWebRequest) WebRequest.Create(ListeningOn + "/partialfiles/TestExistingDir/upload.html");
            webRequest.AddRange(5, 11); //first three chars are ascii control characters

            WebResponse webResponse = webRequest.GetResponse();
            string actualContents = new StreamReader(webResponse.GetResponseStream()).ReadToEnd();

            Assert.That(webResponse.ContentType, Is.EqualTo(MimeTypes.GetMimeType(uploadedFile.Name)));
            Assert.That(actualContents, Is.EqualTo("DOCTYPE"));
            Console.WriteLine("Response length {0}", actualContents.Length);
            Console.WriteLine("Content-Length header {0}", webResponse.Headers["Content-Length"]);
        }

        [Test]
        public void Can_GET_206_Partial_response_for_memory_with_range_header()
        {
            var webRequest =
                (HttpWebRequest) WebRequest.Create(ListeningOn + "/partialfiles/memory?mimeType=audio/mpeg");
            webRequest.AddRange(5, 9);

            WebResponse webResponse = webRequest.GetResponse();
            string actualContents = new StreamReader(webResponse.GetResponseStream()).ReadToEnd();
            Assert.That(actualContents, Is.EqualTo("67890"));
            Console.WriteLine("Response Length {0}", actualContents.Length);
            Console.WriteLine("Content-Length header {0}", webResponse.Headers["Content-Length"]);
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

            var httpResult = new PartialContentResult(ms, "audio/mpeg");

            bool reponseWasAutoHandled = mockResponse.WriteToResponse(mockRequest, httpResult);
            Assert.That(reponseWasAutoHandled, Is.True);

            string writtenString = mockResponse.GetOutputStreamAsString();
            Assert.That(writtenString, Is.EqualTo(customText));

            Assert.That(mockResponse.Headers["Content-Range"], Is.Null);
            Assert.That(mockResponse.Headers["Content-Length"], Is.EqualTo(writtenString.Length.ToString()));
            Assert.That(mockResponse.Headers["Accept-Ranges"], Is.EqualTo("bytes"));
            Assert.That(mockResponse.StatusCode, Is.EqualTo(200));
        }

        [Test]
        public void Can_seek_from_beginning_to_end()
        {
            var mockRequest = new HttpRequestMock();
            mockRequest.Headers.Add("Range", "bytes=0");
            var mockResponse = new HttpResponseMock();

            string customText = "1234567890";
            byte[] customTextBytes = customText.ToUtf8Bytes();
            var ms = new MemoryStream();
            ms.Write(customTextBytes, 0, customTextBytes.Length);


            var httpResult = new PartialContentResult(ms, "audio/mpeg");

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
            mockRequest.Headers.Add("Range", "bytes=0-2");
            var mockResponse = new HttpResponseMock();

            string customText = "1234567890";
            byte[] customTextBytes = customText.ToUtf8Bytes();
            var ms = new MemoryStream();
            ms.Write(customTextBytes, 0, customTextBytes.Length);


            var httpResult = new PartialContentResult(ms, "audio/mpeg");

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


            var httpResult = new PartialContentResult(ms, "audio/mpeg");

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


            var httpResult = new PartialContentResult(ms, "audio/mpeg");

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
            var uploadedFile = new FileInfo("~/TestExistingDir/textfile.txt".MapProjectPath());
            byte[] fileBytes = uploadedFile.OpenRead().ReadFully();
            Console.WriteLine("File content size {0}", fileBytes.Length);
            string fileText = Encoding.ASCII.GetString(fileBytes);
            Console.WriteLine("File content is {0}", fileText);

            var mockRequest = new HttpRequestMock();
            var mockResponse = new HttpResponseMock();
            mockRequest.Headers.Add("Range", "bytes=6-8");

            var httpResult = new PartialContentResult(uploadedFile, "audio/mpeg");

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