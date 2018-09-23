using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Funq;
using NUnit.Framework;
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

        [Test]
        public void Can_zip_and_unzip_bytes_using_DeflateStream()
        {
            var text = "hello zip";
            var zipBytes = StreamExt.DeflateProvider.Deflate(text);
            var unzip = StreamExt.DeflateProvider.Inflate(zipBytes);
            Assert.That(unzip, Is.EqualTo(text));
        }

        [Test]
        public void Can_zip_and_unzip_bytes_using_Gzip()
        {
            var text = "hello zip";
            var zipBytes = StreamExt.GZipProvider.GZip(text);
            var unzip = StreamExt.GZipProvider.GUnzip(zipBytes);
            Assert.That(unzip, Is.EqualTo(text));
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

            JsConfig.UTF8Encoding = null;
        }
    }
}