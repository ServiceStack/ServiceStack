#if !NET6_0_OR_GREATER
// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
    [TestFixture]
    public class HttpUtilsMockTests
    {
        private const string ExampleGoogleUrl = "http://google.com";
        private const string ExampleYahooUrl = "http://yahoo.com";

        [Test]
        public void Can_Mock_String_Api_responses()
        {
            using (new HttpResultsFilter
            {
                StringResult = "mocked"
            })
            {
                Assert.That(ExampleGoogleUrl.GetJsonFromUrl(), Is.EqualTo("mocked"));
                Assert.That(ExampleGoogleUrl.GetXmlFromUrl(), Is.EqualTo("mocked"));
                Assert.That(ExampleGoogleUrl.GetStringFromUrl(), Is.EqualTo("mocked"));
                Assert.That(ExampleGoogleUrl.GetStringFromUrl(accept: "text/csv"), Is.EqualTo("mocked"));

                Assert.That(ExampleGoogleUrl.PostJsonToUrl(json: "{\"postdata\":1}"), Is.EqualTo("mocked"));
                Assert.That(ExampleGoogleUrl.PostXmlToUrl(xml: "<postdata>1</postdata>"), Is.EqualTo("mocked"));
                Assert.That(ExampleGoogleUrl.PostToUrl(formData: "postdata=1"), Is.EqualTo("mocked"));
                Assert.That(ExampleGoogleUrl.PostStringToUrl(requestBody: "postdata=1"), Is.EqualTo("mocked"));
            }
        }

        [Test]
        public async Task Can_Mock_String_Api_responses_async()
        {
            using (new HttpResultsFilter
            {
                StringResult = "mocked"
            })
            {
                Assert.That(await ExampleGoogleUrl.GetJsonFromUrlAsync(), Is.EqualTo("mocked"));
                Assert.That(await ExampleGoogleUrl.GetXmlFromUrlAsync(), Is.EqualTo("mocked"));
                Assert.That(await ExampleGoogleUrl.GetStringFromUrlAsync(), Is.EqualTo("mocked"));
                Assert.That(await ExampleGoogleUrl.GetStringFromUrlAsync(accept: "text/csv"), Is.EqualTo("mocked"));

                Assert.That(await ExampleGoogleUrl.PostJsonToUrlAsync(json: "{\"postdata\":1}"), Is.EqualTo("mocked"));
                Assert.That(await ExampleGoogleUrl.PostXmlToUrlAsync(xml: "<postdata>1</postdata>"), Is.EqualTo("mocked"));
                Assert.That(await ExampleGoogleUrl.PostToUrlAsync(formData: "postdata=1"), Is.EqualTo("mocked"));
                Assert.That(await ExampleGoogleUrl.PostStringToUrlAsync(requestBody: "postdata=1"), Is.EqualTo("mocked"));
            }
        }

        [Test]
        public void Can_Mock_Bytes_Api_responses()
        {
            using (new HttpResultsFilter
            {
                BytesResult = "mocked".ToUtf8Bytes()
            })
            {
                Assert.That(ExampleGoogleUrl.GetBytesFromUrl(), Is.EqualTo("mocked".ToUtf8Bytes()));
                Assert.That(ExampleGoogleUrl.GetBytesFromUrl(accept: "image/png"), Is.EqualTo("mocked".ToUtf8Bytes()));

                Assert.That(ExampleGoogleUrl.PostBytesToUrl(requestBody: "postdata=1".ToUtf8Bytes()), Is.EqualTo("mocked".ToUtf8Bytes()));
            }
        }

        [Test]
        public async Task Can_Mock_Bytes_Api_responses_Async()
        {
            using (new HttpResultsFilter
            {
                BytesResult = "mocked".ToUtf8Bytes()
            })
            {
                Assert.That(await ExampleGoogleUrl.GetBytesFromUrlAsync(), Is.EqualTo("mocked".ToUtf8Bytes()));
                Assert.That(await ExampleGoogleUrl.GetBytesFromUrlAsync(accept: "image/png"), Is.EqualTo("mocked".ToUtf8Bytes()));

                Assert.That(await ExampleGoogleUrl.PostBytesToUrlAsync(requestBody: "postdata=1".ToUtf8Bytes()), Is.EqualTo("mocked".ToUtf8Bytes()));
            }
        }

#if !NETCORE
        [Test]
        public void Can_Mock_UploadFile()
        {
            string tempTextPath = Path.Combine (Path.GetTempPath (), "test.txt");
            using (File.CreateText(tempTextPath)){}

            var fileNamesUploaded = new List<string>();
            using (new HttpResultsFilter
            {
                UploadFileFn = (webReq, stream, fileName) => fileNamesUploaded.Add(fileName)
            })
            {
                ExampleGoogleUrl.PostFileToUrl(new FileInfo(tempTextPath), "text/plain");
                Assert.That(fileNamesUploaded, Is.EquivalentTo(new[] { "test.txt" }));

                fileNamesUploaded.Clear();

                ExampleGoogleUrl.PutFileToUrl(new FileInfo(tempTextPath), "text/plain");
                Assert.That(fileNamesUploaded, Is.EquivalentTo(new[] { "test.txt" }));

                fileNamesUploaded.Clear();

                var webReq =  WebRequest.CreateHttp(ExampleGoogleUrl);
                webReq.UploadFile(new FileInfo(tempTextPath), "text/plain");
                Assert.That(fileNamesUploaded, Is.EquivalentTo(new[] { "test.txt" }));
            }
        }
#endif

        [Test]
        public void Can_Mock_StringFn_Api_responses()
        {
            using (new HttpResultsFilter
            {
                StringResultFn = (webReq, reqBody) =>
                {
                    if (reqBody != null && reqBody.Contains("{\"a\":1}")) return "mocked-by-body";

                    return webReq.RequestUri.ToString().Contains("google")
                        ? "mocked-google"
                        : "mocked-yahoo";
                }
            })
            {
                Assert.That(ExampleGoogleUrl.GetJsonFromUrl(), Is.EqualTo("mocked-google"));
                Assert.That(ExampleYahooUrl.GetJsonFromUrl(), Is.EqualTo("mocked-yahoo"));

                Assert.That(ExampleGoogleUrl.PostJsonToUrl(json: "{\"postdata\":1}"), Is.EqualTo("mocked-google"));
                Assert.That(ExampleYahooUrl.PostJsonToUrl(json: "{\"postdata\":1}"), Is.EqualTo("mocked-yahoo"));

                Assert.That(ExampleYahooUrl.PostJsonToUrl(json: "{\"a\":1}"), Is.EqualTo("mocked-by-body"));
            }
        }

        [Test]
        public async Task Can_Mock_StringFn_Api_responses_Async()
        {
            using (new HttpResultsFilter
            {
                StringResultFn = (webReq, reqBody) =>
                {
                    if (reqBody != null && reqBody.Contains("{\"a\":1}")) return "mocked-by-body";

                    return webReq.RequestUri.ToString().Contains("google")
                        ? "mocked-google"
                        : "mocked-yahoo";
                }
            })
            {
                Assert.That(await ExampleGoogleUrl.GetJsonFromUrlAsync(), Is.EqualTo("mocked-google"));
                Assert.That(await ExampleYahooUrl.GetJsonFromUrlAsync(), Is.EqualTo("mocked-yahoo"));

                Assert.That(await ExampleGoogleUrl.PostJsonToUrlAsync(json: "{\"postdata\":1}"), Is.EqualTo("mocked-google"));
                Assert.That(await ExampleYahooUrl.PostJsonToUrlAsync(json: "{\"postdata\":1}"), Is.EqualTo("mocked-yahoo"));

                Assert.That(await ExampleYahooUrl.PostJsonToUrlAsync(json: "{\"a\":1}"), Is.EqualTo("mocked-by-body"));
            }
        }

        [Test]
        public void Can_Mock_BytesFn_Api_responses()
        {
            using (new HttpResultsFilter
            {
                BytesResultFn = (webReq, reqBody) =>
                {
                    if (reqBody != null && reqBody.FromUtf8Bytes().Contains("{\"a\":1}")) return "mocked-by-body".ToUtf8Bytes();

                    return webReq.RequestUri.ToString().Contains("google")
                        ? "mocked-google".ToUtf8Bytes()
                        : "mocked-yahoo".ToUtf8Bytes();
                }
            })
            {
                Assert.That(ExampleGoogleUrl.GetBytesFromUrl(), Is.EqualTo("mocked-google".ToUtf8Bytes()));
                Assert.That(ExampleYahooUrl.GetBytesFromUrl(), Is.EqualTo("mocked-yahoo".ToUtf8Bytes()));

                Assert.That(ExampleGoogleUrl.PostBytesToUrl(requestBody: "postdata=1".ToUtf8Bytes()), Is.EqualTo("mocked-google".ToUtf8Bytes()));
                Assert.That(ExampleYahooUrl.PostBytesToUrl(requestBody: "postdata=1".ToUtf8Bytes()), Is.EqualTo("mocked-yahoo".ToUtf8Bytes()));

                Assert.That(ExampleYahooUrl.PostBytesToUrl(requestBody: "{\"a\":1}".ToUtf8Bytes()), Is.EqualTo("mocked-by-body".ToUtf8Bytes()));
            }
        }

        [Test]
        public async Task Can_Mock_BytesFn_Api_responses_Async()
        {
            using (new HttpResultsFilter
            {
                BytesResultFn = (webReq, reqBody) =>
                {
                    if (reqBody != null && reqBody.FromUtf8Bytes().Contains("{\"a\":1}")) return "mocked-by-body".ToUtf8Bytes();

                    return webReq.RequestUri.ToString().Contains("google")
                        ? "mocked-google".ToUtf8Bytes()
                        : "mocked-yahoo".ToUtf8Bytes();
                }
            })
            {
                Assert.That(await ExampleGoogleUrl.GetBytesFromUrlAsync(), Is.EqualTo("mocked-google".ToUtf8Bytes()));
                Assert.That(await ExampleYahooUrl.GetBytesFromUrlAsync(), Is.EqualTo("mocked-yahoo".ToUtf8Bytes()));

                Assert.That(await ExampleGoogleUrl.PostBytesToUrlAsync(requestBody: "postdata=1".ToUtf8Bytes()), Is.EqualTo("mocked-google".ToUtf8Bytes()));
                Assert.That(await ExampleYahooUrl.PostBytesToUrlAsync(requestBody: "postdata=1".ToUtf8Bytes()), Is.EqualTo("mocked-yahoo".ToUtf8Bytes()));

                Assert.That(await ExampleYahooUrl.PostBytesToUrlAsync(requestBody: "{\"a\":1}".ToUtf8Bytes()), Is.EqualTo("mocked-by-body".ToUtf8Bytes()));
            }
        }

    }
}
#endif
