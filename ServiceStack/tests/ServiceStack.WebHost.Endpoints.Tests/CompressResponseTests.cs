using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.IO;

namespace ServiceStack.WebHost.Endpoints.Tests;

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

public class CompressError : IReturn<CompressError> { }

[Route("/compress/dto-result/{Name}")]
public class CompressDtoResult : IReturn<CompressDtoResult>
{
    public string Name { get; set; }
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

    public object Any(CompressDtoResult request) => new HttpResult(request, MimeTypes.Xml);

    public object Any(CompressError request)
    {
        throw HttpError.NotFound("Always NotFound");
    }
}

public class CompressResponseTests
{
    class AppHost() : AppSelfHostBase(nameof(CompressResponseTests), typeof(CompressedServices).Assembly)
    {
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
            var memFs = new MemoryVirtualFiles();

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
    public async Task Does_compress_RequestDto_responses_HttpClient()
    {
        var client = new JsonHttpClient(Config.ListeningOn);
        var response = await client.PostAsync(new CompressData
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
    public async Task Does_compress_raw_String_responses_HttpClient()
    {
        var client = new JsonHttpClient(Config.ListeningOn);
        var response = await client.PostAsync(new CompressString
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
    public async Task Does_compress_raw_Bytes_responses_HttpClient()
    {
        var client = new JsonHttpClient(Config.ListeningOn);
        var response = await client.PostAsync(new CompressBytes
        {
            Bytes = "foo".ToUtf8Bytes(),
        });

        Assert.That(response, Is.EquivalentTo("foo".ToUtf8Bytes()));
    }

    [Test]
    public void Does_not_compress_error_responses()
    {
        var client = new JsonServiceClient(Config.ListeningOn);

        try
        {
            client.Post(new CompressError());
            Assert.Fail("Should throw");
        }
        catch (WebServiceException ex)
        {
            Assert.That(ex.StatusCode, Is.EqualTo(404));
            Assert.That(ex.ErrorCode, Is.EqualTo("NotFound"));
            Assert.That(ex.ErrorMessage, Is.EqualTo("Always NotFound"));
        }
    }

    [Test]
    public async Task Does_not_compress_error_responses_HttpClient()
    {
        var client = new JsonHttpClient(Config.ListeningOn);

        try
        {
            await client.PostAsync(new CompressError());
            Assert.Fail("Should throw");
        }
        catch (WebServiceException ex)
        {
            Assert.That(ex.StatusCode, Is.EqualTo(404));
            Assert.That(ex.ErrorCode, Is.EqualTo("NotFound"));
            Assert.That(ex.ErrorMessage, Is.EqualTo("Always NotFound"));
        }
    }

    [Test]
    public void Does_compress_using_ContenType_in_HttpResult()
    {
        var url = Config.ListeningOn.CombineWith(new CompressDtoResult { Name = "foo" }.ToGetUrl());

        var xml = url.GetJsonFromUrl(responseFilter: res =>
        {
            Assert.That(res.GetHeader(HttpHeaders.ContentType), Does.StartWith(MimeTypes.Xml));
        });

        Assert.That(xml, Does.StartWith("<?xml"));
    }

    [Test]
    public async Task Does_compress_using_ContenType_in_HttpResult_Async()
    {
        var url = Config.ListeningOn.CombineWith(new CompressDtoResult { Name = "foo" }.ToGetUrl());

        var xml = await url.GetJsonFromUrlAsync(responseFilter: res =>
        {
            Assert.That(res.GetHeader(HttpHeaders.ContentType), Does.StartWith(MimeTypes.Xml));
        });

        Assert.That(xml, Does.StartWith("<?xml"));
    }

#if NETFX        
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
        public async Task Does_compress_file_returned_in_HttpResult_Async()
        {
            var url = Config.ListeningOn.CombineWith("/compress/file.js");
            var zipBytes = await url.GetBytesFromUrlAsync(
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
        public async Task Does_compress_static_file_in_CompressFilesWithExtensions_Async()
        {
            var url = Config.ListeningOn.CombineWith("/file.css");
            var zipBytes = await url.GetBytesFromUrlAsync(
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

        [Test]
        public async Task Does_compress_default_page_in_CompressFilesWithExtensions_Async()
        {
            var url = Config.ListeningOn.CombineWith("/default.html");
            var zipBytes = await url.GetBytesFromUrlAsync(
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
#endif
}