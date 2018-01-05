using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    [Ignore("Load Test"), TestFixture]
    public class LoadTests
    {
        string BaseUrl = "http://localhost:50000/api/";
        private int BytesSize = 333930;

        [Test]
        public void Can_download_sync_pdf()
        {
            var client = new ImagingService(BaseUrl);

            var pdfBytes = client.GetBytes("/sample.pdf");

            "PDF Size: {0} bytes".Print(pdfBytes.Length);

            Assert.That(pdfBytes.Length, Is.EqualTo(BytesSize));
        }

        [Test]
        public async Task Can_download_async_pdf()
        {
            var client = new ImagingService(BaseUrl);

            var pdfBytes = await client.GetBytesAsync("/sample.pdf");

            "PDF Size: {0} bytes".Print(pdfBytes.Length);

            Assert.That(pdfBytes.Length, Is.EqualTo(BytesSize));
        }

        [Test]
        public async Task Load_test_download_async_pdf()
        {
            const int NoOfTimes = 1000;

            var client = new ImagingService(BaseUrl);

            var fetchTasks = new List<Task>();

            for (var i = 0; i < NoOfTimes; i++)
            {
                var asyncResponse = client.GetBytesAsync("/sample.pdf")
                .ContinueWith(task =>
                {
                    var pdfBytes = task.Result;
                    "PDF Size: {0} bytes".Print(pdfBytes.Length);
                    Assert.That(pdfBytes.Length, Is.EqualTo(BytesSize));
                });

                fetchTasks.Add(asyncResponse);
            }

            await Task.WhenAll(fetchTasks);
        }
    }

    public class ImagingService : JsonServiceClient
    {
        public ImagingService(string url)
            : base(url)
        {
            //Headers.Add("X-ApiKey", "blah ");  //This does not work on Async methods, but is should.
            RequestFilter += (request) =>
            {
                request.Headers.Add("X-ApiKey", "BLAH");
            };
        }

        public byte[] GetBytes(string relativePath)
        {
            return Get<byte[]>(relativePath);
        }

        public async Task<byte[]> GetBytesAsync(string relativePath)
        {
            return await GetAsync<byte[]>(relativePath);
        }
    }

    public class ImagingServiceWithHighTimeout : ImagingService
    {
        public ImagingServiceWithHighTimeout(string url)
            : base(url)
        {
            Timeout = new TimeSpan(0, 3, 0);
        }
    }
}