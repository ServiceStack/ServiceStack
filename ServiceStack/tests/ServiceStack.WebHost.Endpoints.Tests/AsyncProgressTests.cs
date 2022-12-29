// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class AsyncProgressTests
    {
        private const string ListeningOn = "http://localhost:1337/";

        ExampleAppHostHttpListener appHost;

        [OneTimeSetUp]
        public void OnTestFixtureSetUp()
        {
            appHost = new ExampleAppHostHttpListener();
            appHost.Init();
            appHost.Start(ListeningOn);
        }

        [OneTimeTearDown]
        public void OnTestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public async Task Can_report_progress_when_downloading_GET_async()
        {
            var hold = AsyncServiceClient.BufferSize;
            AsyncServiceClient.BufferSize = 100;

            try
            {
                var asyncClient = new JsonServiceClient(ListeningOn);

                var progress = new List<string>();

                //Note: total = -1 when 'Transfer-Encoding: chunked'
                //Available in ASP.NET or in HttpListener when downloading responses with known lengths: 
                //E.g: Strings, Files, etc.
                asyncClient.OnDownloadProgress = (done, total) =>
                    progress.Add("{0}/{1} bytes downloaded".Fmt(done, total));

                var response = await asyncClient.GetAsync(new TestProgressString());

                progress.Each(x => x.Print());

                Assert.That(response.Length, Is.GreaterThan(0));
                Assert.That(progress.Count, Is.GreaterThan(0));
                Assert.That(progress.First(), Is.EqualTo("100/1160 bytes downloaded"));
                Assert.That(progress.Last(), Is.EqualTo("1160/1160 bytes downloaded"));
            }
            finally
            {
                AsyncServiceClient.BufferSize = hold;
            }
        }

        [Test]
        public async Task Can_report_progress_when_downloading_async_with_Post()
        {
            await AsyncDownloadWithProgress(new TestProgressString());
        }

        [Test]
        public async Task Can_report_progress_when_downloading_async_with_Post_bytes()
        {
            await AsyncDownloadWithProgress(new TestProgressBytes());
        }

        [Test]
        public async Task Can_report_progress_when_downloading_async_with_Post_File_bytes()
        {
            await AsyncDownloadWithProgress(new TestProgressBinaryFile());
        }

        [Test]
        public async Task Can_report_progress_when_downloading_async_with_Post_File_text()
        {
            await AsyncDownloadWithProgress(new TestProgressTextFile());
        }

        private static async Task AsyncDownloadWithProgress<TResponse>(IReturn<TResponse> requestDto)
        {
            var hold = AsyncServiceClient.BufferSize;
            AsyncServiceClient.BufferSize = 100;

            try
            {
                var asyncClient = new JsonServiceClient(ListeningOn);

                var progress = new List<string>();

                //Note: total = -1 when 'Transfer-Encoding: chunked'
                //Available in ASP.NET or in HttpListener when downloading responses with known lengths: 
                //E.g: Strings, Files, etc.
                asyncClient.OnDownloadProgress = (done, total) =>
                                                 progress.Add("{0}/{1} bytes downloaded".Fmt(done, total));

                var response = await asyncClient.PostAsync(requestDto);

                progress.Each(x => x.Print());

                Assert.That(progress.Count, Is.GreaterThan(0));
                Assert.That(progress.First(), Is.EqualTo("100/1160 bytes downloaded"));
                Assert.That(progress.Last(), Is.EqualTo("1160/1160 bytes downloaded"));
            }
            finally
            {
                AsyncServiceClient.BufferSize = hold;
            }
        }
    }
}
