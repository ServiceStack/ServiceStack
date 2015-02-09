using System;
using System.IO;
using System.Net;
using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    [TestFixture]
    public class FileUploadTests
        : RestsTestBase
    {
        public WebResponse UploadFile(string pathInfo, FileInfo fileInfo)
        {
            //long length = 0;
            string boundary = "----------------------------" +
            DateTime.Now.Ticks.ToString("x");

            var httpWebRequest = (HttpWebRequest)WebRequest.Create(base.ServiceClientBaseUri + pathInfo);
            httpWebRequest.ContentType = "multipart/form-data; boundary=" + boundary;
            httpWebRequest.Accept = MimeTypes.Json;
            httpWebRequest.Method = "POST";
            httpWebRequest.AllowAutoRedirect = false;
            httpWebRequest.KeepAlive = false;

            Stream memStream = new System.IO.MemoryStream();

            byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary);


            string headerTemplate = "\r\n--" + boundary + "\r\nContent-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";



            string header = string.Format(headerTemplate, "upload", fileInfo.Name, MimeTypes.GetMimeType(fileInfo.Name));



            byte[] headerbytes = System.Text.Encoding.ASCII.GetBytes(header);

            memStream.Write(headerbytes, 0, headerbytes.Length);


            //Image img = null;
            //img = Image.FromFile("C:/Documents and Settings/Dorin Cucicov/My Documents/My Pictures/Sunset.jpg", true);
            //img.Save(memStream, System.Drawing.Imaging.ImageFormat.Jpeg);

            using (var fs = fileInfo.OpenRead())
            {
                fs.WriteTo(memStream);
            }

            memStream.Write(boundarybytes, 0, boundarybytes.Length);


            //string formdataTemplate = "\r\nContent-Disposition: form-data; name=\"{0}\";\r\n\r\n{1}";

            //string formitem = string.Format(formdataTemplate, "headline", "Sunset");
            //byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
            //memStream.Write(formitembytes, 0, formitembytes.Length);

            //memStream.Write(boundarybytes, 0, boundarybytes.Length);


            httpWebRequest.ContentLength = memStream.Length;

            var requestStream = httpWebRequest.GetRequestStream();

            memStream.Position = 0;
            var tempBuffer = new byte[memStream.Length];
            memStream.Read(tempBuffer, 0, tempBuffer.Length);
            memStream.Close();
            requestStream.Write(tempBuffer, 0, tempBuffer.Length);
            requestStream.Close();

            return httpWebRequest.GetResponse();
        }

        [Test]
        public void Can_POST_upload_file()
        {
            var uploadForm = new FileInfo("~/TestExistingDir/upload.html".MapHostAbsolutePath());
            var webResponse = UploadFile("/fileuploads", uploadForm);

            AssertResponse<FileUploadResponse>((HttpWebResponse)webResponse, r =>
            {
                var expectedContents = new StreamReader(uploadForm.OpenRead()).ReadToEnd();
                Assert.That(r.FileName, Is.EqualTo(uploadForm.Name));
                Assert.That(r.ContentLength, Is.EqualTo(uploadForm.Length));
                Assert.That(r.ContentType, Is.EqualTo(MimeTypes.GetMimeType(uploadForm.Name)));
                Assert.That(r.Contents, Is.EqualTo(expectedContents));
            });
        }

        [Test]
        public void Can_GET_upload_file()
        {
            var uploadForm = new FileInfo("~/TestExistingDir/upload.html".MapHostAbsolutePath());
            var webRequest = (HttpWebRequest)WebRequest.Create(base.ServiceClientBaseUri + "/fileuploads/TestExistingDir/upload.html");
            var expectedContents = new StreamReader(uploadForm.OpenRead()).ReadToEnd();

            var webResponse = webRequest.GetResponse();
            var actualContents = webResponse.ReadToEnd();

            Assert.That(webResponse.ContentType, Is.EqualTo(MimeTypes.GetMimeType(uploadForm.Name)));
            Assert.That(actualContents.NormalizeNewLines(), Is.EqualTo(expectedContents.NormalizeNewLines()));
        }

    }


    public static class TestExtensions
    {
        public static string NormalizeNewLines(this string text)
        {
            return text.Replace("\r\n", "\n");
        }
    }
}