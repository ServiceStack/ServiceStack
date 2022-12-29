using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Funq;
using NUnit.Framework;
using ServiceStack.Caching;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class ZipTests
    {
        private readonly bool hold;
        public ZipTests()
        {
            hold = MemoryStreamFactory.UseRecyclableMemoryStream;
            MemoryStreamFactory.UseRecyclableMemoryStream = true;
        }
        
        [OneTimeTearDown]
        public void OneTimeTearDown() => MemoryStreamFactory.UseRecyclableMemoryStream = hold;

        private static void DoesCompress(IStreamCompressor compressor, string text)
        {
            var zipBytes = compressor.Compress(text);
            var unzip = compressor.Decompress(zipBytes);
            Assert.That(unzip, Is.EqualTo(text));
        }

#if NET6_0_OR_GREATER
        [Test]
        public void Can_zip_and_unzip_bytes_using_BrotliStream()
        {
            DoesCompress(StreamCompressors.GetRequired(CompressionTypes.Brotli), "hello zip");
        }
#endif

        [Test]
        public void Can_zip_and_unzip_bytes_using_DeflateStream()
        {
            DoesCompress(StreamCompressors.GetRequired(CompressionTypes.Deflate), "hello zip");
        }

        [Test]
        public void Can_zip_and_unzip_bytes_using_Gzip()
        {
            DoesCompress(StreamCompressors.GetRequired(CompressionTypes.GZip), "hello zip");
        }
    }

    public class ZipRequestLoggerTests
    {
        private readonly ServiceStackHost appHost;

        public ZipRequestLoggerTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

        class AppHost : AppSelfHostBase
        {
            public AppHost() 
                : base(nameof(ZipRequestLoggerTests), typeof(HelloZipService).Assembly) { }
            
            public override void Configure(Container container)
            {
                SetConfig(new HostConfig {
                    StrictMode = true,
                });
                
                Plugins.Add(new RequestLogsFeature {
                    EnableRequestBodyTracking = true,
                });
            }
        }
        
        [Test]
        public void Does_log_compressed_requests()
        {
            var hold = JsConfig.UTF8Encoding; 
            JsConfig.UTF8Encoding = new UTF8Encoding(false, true);
            
            var client = new JsonServiceClient(Config.ListeningOn)
            {
                RequestCompressionType = CompressionTypes.GZip,
            };
            var response = client.Post(new HelloZip
            {
                Name = "GZIP",
                Test = new List<string> { "Test" }
            });
            Assert.That(response.Result, Is.EqualTo("Hello, GZIP (1)"));

            JsConfig.UTF8Encoding = hold;
        }
    }
}