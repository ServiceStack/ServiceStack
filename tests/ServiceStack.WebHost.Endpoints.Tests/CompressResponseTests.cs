using System.Collections.Generic;
using System.Net;
using Funq;
using NUnit.Framework;
using ServiceStack.IO;
using ServiceStack.VirtualPath;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class CompressData : IReturn<CompressData>
    {
        public string String { get; set; }
        public byte[] Bytes { get; set; }
    }

    public class CompressString : IReturn<string>
    {
        public string String { get; set; }
    }

    public class CompressBytes : IReturn<byte[]>
    {
        public byte[] Bytes { get; set; }
    }

    [Route("/compress/{Path*}")]
    public class CompressFile
    {
        public string Path { get; set; }
    }

    [CompressResponse]
    public class CompressedServices : Service
    {
        public object Any(CompressData request) => request;
        public object Any(CompressString request) => request.String;
        public object Any(CompressBytes request) => request.Bytes;

        public object Any(CompressFile request)
        {
            var file = VirtualFileSources.GetFile(request.Path);
            if (file == null)
                throw HttpError.NotFound($"{request.Path} does not exist");

            return new HttpResult(file);
        }
    }

    public class CompressResponseTests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost()
                : base(nameof(CompressResponseTests), typeof(CompressedServices).GetAssembly()) { }

            public override void Configure(Container container)
            {
                SetConfig(new HostConfig
                {
                    CompressFilesWithExtensions = { "html", "css" }
                });
            }

            public override List<IVirtualPathProvider> GetVirtualFileSources()
            {
                var existingProviders = base.GetVirtualFileSources();
                var memFs = new InMemoryVirtualPathProvider(this);

                memFs.WriteFile("/file.js", "console.log('foo')");
                memFs.WriteFile("/file.css", ".foo{}");
                memFs.WriteFile("/file.txt", "foo");
                memFs.WriteFile("/default.html", "<body>foo</body>");

                //Give new Memory FS highest priority
                existingProviders.Insert(0, memFs);
                return existingProviders;
            }
        }

        private ServiceStackHost appHost;

        public CompressResponseTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

        [Test]
        public void Does_compress_RequestDto_responses()
        {
            var client = new JsonServiceClient(Config.ListeningOn);
            var response = client.Post(new CompressData
            {
                String = "Hello",
                Bytes = "World".ToUtf8Bytes()
            });

            Assert.That(response.String, Is.EqualTo("Hello"));
            Assert.That(response.Bytes, Is.EqualTo("World".ToUtf8Bytes()));
        }

        [Test]
        public void Does_compress_raw_String_responses()
        {
            var client = new JsonServiceClient(Config.ListeningOn);
            var response = client.Post(new CompressString
            {
                String = "foo",
            });

            Assert.That(response, Is.EqualTo("foo"));
        }

        [Test]
        public void Does_compress_raw_Bytes_responses()
        {
            var client = new JsonServiceClient(Config.ListeningOn);
            var response = client.Post(new CompressBytes
            {
                Bytes = "foo".ToUtf8Bytes(),
            });

            Assert.That(response, Is.EquivalentTo("foo".ToUtf8Bytes()));
        }

        [Test]
        public void Does_compress_file_returned_in_HttpResult()
        {
            var url = Config.ListeningOn.CombineWith("/compress/file.js");
            var zipBytes = url.GetBytesFromUrl(
                requestFilter: req =>
                {
                    req.AutomaticDecompression = DecompressionMethods.None;
                    req.Headers[HttpRequestHeader.AcceptEncoding] = "deflate";
                }, responseFilter: res =>
                {
                    Assert.That(res.Headers[HttpResponseHeader.ContentEncoding], Is.EqualTo("deflate"));
                });

            var bytes = zipBytes.DecompressBytes("deflate");
            Assert.That(bytes.FromUtf8Bytes(), Is.EqualTo("console.log('foo')"));
        }

        [Test]
        public void Does_compress_static_file_in_CompressFilesWithExtensions()
        {
            var url = Config.ListeningOn.CombineWith("/file.css");
            var zipBytes = url.GetBytesFromUrl(
                requestFilter: req =>
                {
                    req.AutomaticDecompression = DecompressionMethods.None;
                    req.Headers[HttpRequestHeader.AcceptEncoding] = "deflate";
                }, responseFilter: res =>
                {
                    Assert.That(res.Headers[HttpResponseHeader.ContentEncoding], Is.EqualTo("deflate"));
                });

            var bytes = zipBytes.DecompressBytes("deflate");
            Assert.That(bytes.FromUtf8Bytes(), Is.EqualTo(".foo{}"));
        }

        [Test]
        public void Does_compress_default_page_in_CompressFilesWithExtensions()
        {
            var url = Config.ListeningOn.CombineWith("/default.html");
            var zipBytes = url.GetBytesFromUrl(
                requestFilter: req =>
                {
                    req.AutomaticDecompression = DecompressionMethods.None;
                    req.Headers[HttpRequestHeader.AcceptEncoding] = "deflate";
                }, responseFilter: res =>
                {
                    Assert.That(res.Headers[HttpResponseHeader.ContentEncoding], Is.EqualTo("deflate"));
                });

            var bytes = zipBytes.DecompressBytes("deflate");
            Assert.That(bytes.FromUtf8Bytes(), Is.EqualTo("<body>foo</body>"));
        }
    }
}