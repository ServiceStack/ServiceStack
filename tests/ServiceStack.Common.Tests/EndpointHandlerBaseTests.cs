#if !NETCORE_SUPPORT
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Host;
using ServiceStack.Testing;
using ServiceStack.Text;
using ServiceStack.VirtualPath;
using ServiceStack.Web;

namespace ServiceStack.Common.Tests
{
    [TestFixture]
    public class EndpointHandlerBaseTests
    {
        public IHttpRequest CreateRequest(string userHostAddress)
        {
            var httpReq = new MockHttpRequest("test", HttpMethods.Get, MimeTypes.Json, "/", null, null, null)
            {
                UserHostAddress = userHostAddress
            };
            return httpReq;
        }

        [Test]
        public void Can_parse_Ips()
        {
            using (new BasicAppHost().Init())
            {
                var result = CreateRequest("204.2.145.235").GetAttributes();

                Assert.That(result.Has(RequestAttributes.External));
                Assert.That(result.Has(RequestAttributes.HttpGet));
                Assert.That(result.Has(RequestAttributes.InSecure));
            }
        }

        [Flags]
        enum A : int { B = 0, C = 2, D = 4 }

        [Test]
        public void Can_parse_int_enums()
        {
            var result = A.B | A.C;
            Assert.That(result.Has(A.C));
            Assert.That(!result.Has(A.D));
        }

        [Test]
        public void Can_mock_uploading_files()
        {
            using (new BasicAppHost
            {
                ConfigureAppHost = host => host.VirtualFiles = new InMemoryVirtualPathProvider(host),
            }.Init())
            {
                var ms = new MemoryStream("mocked".ToUtf8Bytes());
                var httpFile = new HttpFile
                {
                    ContentType = "application/x-msaccess",
                    FileName = "C:\\path\\to\\file.txt",
                    InputStream = ms,
                    ContentLength = ms.ToArray().Length,
                };
                var mockReq = new MockHttpRequest
                {
                    Files = new IHttpFile[] { httpFile },
                };
                //Mock Session
                mockReq.Items[Keywords.Session] = new AuthUserSession { Id = "sess-id" };

                var service = new UploadFileService
                {
                    Request = mockReq
                };

                service.Any(new MockUploadFile());

                var files = HostContext.VirtualFiles.GetAllFiles().ToList();
                Assert.That(files[0].ReadAllText(), Is.EqualTo("mocked"));
            }
        }

        public class MockUploadFile { }

        public class UploadFileService : Service
        {
            public object Any(MockUploadFile request)
            {
                for (int i = 0; i < Request.Files.Length; i++)
                {
                    var file = Request.Files[i];

                    string fileId = Guid.NewGuid().ToString();
                    var session = base.GetSession();
                    var fileName = session.Id.CombineWith(fileId);
                    VirtualFiles.WriteFile(fileName, file.InputStream);
                }

                return request;
            }
        }
    }
}
#endif