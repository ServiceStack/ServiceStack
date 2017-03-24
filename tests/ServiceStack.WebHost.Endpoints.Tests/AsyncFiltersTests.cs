using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class TestAsyncFilter : IReturn<TestAsyncFilter>
    {
        public int? ReturnAt { get; set; }
        public int? CancelAt { get; set; }
        public int? ErrorAt { get; set; }
        public int? DirectErrorAt { get; set; }
        public List<string> Results { get; set; }

        public TestAsyncFilter()
        {
            this.Results = new List<string>();
        }
    }

    [Route("/test/asyncfilter")]
    public class TestAsyncFilterRestHandler : TestAsyncFilter {}

    public class TestAsyncFilterService : Service
    {
        public object Any(TestAsyncFilter request)
        {
            request.Results.Add("Service#" + request.GetType().Name);
            return request;
        }

        public object Any(TestAsyncFilterRestHandler request)
        {
            request.Results.Add("Service#" + request.GetType().Name);
            return request;
        }
    }

    public class AsyncFiltersTests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost() : base(nameof(AsyncFiltersTests), typeof(TestAsyncFilterService).GetAssembly()) {}

            public static int CancelledAt = -1;

            public override void Configure(Container container)
            {
                GlobalRequestFiltersAsync.Add(CreateAsyncFilter(0));
                GlobalRequestFiltersAsync.Add(CreateAsyncFilter(1));
                GlobalRequestFiltersAsync.Add(CreateAsyncFilter(2));
            }

            private static Func<IRequest, IResponse, object, Task> CreateAsyncFilter(int pos)
            {
                return (req, res, dto) =>
                {
                    var asyncDto = dto as TestAsyncFilter;
                    if (asyncDto != null)
                    {
                        CancelledAt++;

                        asyncDto.Results.Add("GlobalRequestFiltersAsync#" + pos);
                        if (asyncDto.ReturnAt == pos)
                        {
                            res.ContentType = MimeTypes.Json;
                            res.Write(asyncDto.ToJson());
                            res.EndRequest(skipHeaders: true);
                            return TypeConstants.EmptyTask;
                        }

                        if (asyncDto.ErrorAt == pos)
                            throw new ArgumentException("ErrorAt#" + pos);

                        if (asyncDto.DirectErrorAt == pos)
                        {
                            res.ContentType = MimeTypes.Json;
                            res.WriteError(new ArgumentException("DirectErrorAt#" + pos));
                            return TypeConstants.EmptyTask;
                        }

                        if (asyncDto.CancelAt == pos)
                        {
                            var tcs = new TaskCompletionSource<object>();
                            tcs.SetCanceled();
                            return tcs.Task;
                        }

                        return Task.Delay(10);
                    }

                    return TypeConstants.EmptyTask;
                };
            }
        }

        private readonly ServiceStackHost appHost;
        private JsonServiceClient client;

        public AsyncFiltersTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
            client = new JsonServiceClient(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

        [Test]
        public void Does_Execute_all_RequestFilters()
        {
            var response = client.Post(new TestAsyncFilter());

            response.PrintDump();

            Assert.That(response.Results, Is.EquivalentTo(new[]
            {
                "GlobalRequestFiltersAsync#0",
                "GlobalRequestFiltersAsync#1",
                "GlobalRequestFiltersAsync#2",
                "Service#TestAsyncFilter",
            }));
        }

        [Test]
        public void Does_Execute_all_RequestFilters_RestHandler()
        {
            var response = client.Post(new TestAsyncFilterRestHandler());

            response.PrintDump();

            Assert.That(response.Results, Is.EquivalentTo(new[]
            {
                "GlobalRequestFiltersAsync#0",
                "GlobalRequestFiltersAsync#1",
                "GlobalRequestFiltersAsync#2",
                "Service#TestAsyncFilterRestHandler",
            }));
        }

        [Test]
        public void Does_Execute_all_RequestFilters_in_AutoBatch_Request()
        {
            var responseBatch = client.SendAll(new[]
            {
                new TestAsyncFilter(),
                new TestAsyncFilter(),
                new TestAsyncFilter(),
            });

            Assert.That(responseBatch.Count, Is.EqualTo(3));
            foreach (var response in responseBatch)
            {
                Assert.That(response.Results, Is.EquivalentTo(new[]
                {
                    "GlobalRequestFiltersAsync#0",
                    "GlobalRequestFiltersAsync#1",
                    "GlobalRequestFiltersAsync#2",
                    "Service#TestAsyncFilter",
                }));
            }
        }

        [Test]
        public void Does_return_Error_in_AsyncFilter()
        {
            try
            {
                var response = client.Post(new TestAsyncFilter
                {
                    ErrorAt = 1
                });

                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                ex.ResponseStatus.PrintDump();

                Assert.That(ex.ResponseStatus.ErrorCode, Is.EqualTo(nameof(ArgumentException)));
                Assert.That(ex.ResponseStatus.Message, Is.EqualTo("ErrorAt#1"));
            }
        }

        [Test]
        public void Does_return_Error_in_AsyncFilter_in_AutoBatch_Request()
        {
            try
            {
                var responseBatch = client.SendAll(new []
                {
                    new TestAsyncFilter { ErrorAt = 1 },
                    new TestAsyncFilter { ErrorAt = 1 },
                    new TestAsyncFilter { ErrorAt = 1 },
                });

                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                ex.ResponseStatus.PrintDump();

                Assert.That(ex.ResponseStatus.ErrorCode, Is.EqualTo(nameof(ArgumentException)));
                Assert.That(ex.ResponseStatus.Message, Is.EqualTo("ErrorAt#1"));
            }
        }

        [Test]
        public void Does_return_DirectError_in_AsyncFilter()
        {
            try
            {
                var response = client.Post(new TestAsyncFilter
                {
                    DirectErrorAt = 1
                });

                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                ex.ResponseStatus.PrintDump();

                Assert.That(ex.ResponseStatus.ErrorCode, Is.EqualTo(nameof(ArgumentException)));
                Assert.That(ex.ResponseStatus.Message, Is.EqualTo("DirectErrorAt#1"));
            }
        }

        [Test]
        public void Can_Cancel_in_AsyncFilter()
        {
            AppHost.CancelledAt = -1;

            var client = new JsonServiceClient(Config.ListeningOn)
            {
                ResponseFilter = res =>
                {
                    Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.PartialContent));
                }
            };

            var response = client.Post(new TestAsyncFilter
            {
                CancelAt = 1
            });

            Assert.That(response, Is.Null);
            Assert.That(AppHost.CancelledAt, Is.EqualTo(1));
        }

        [Test]
        public void Can_Cancel_in_AsyncFilter_in_AutoBatch_Request()
        {
            AppHost.CancelledAt = -1;

            var client = new JsonServiceClient(Config.ListeningOn)
            {
                ResponseFilter = res =>
                {
                    Assert.That(res.StatusCode, Is.EqualTo(HttpStatusCode.PartialContent));
                }
            };

            var responseBatch = client.SendAll(new[]
            {
                new TestAsyncFilter { CancelAt = 1 },
                new TestAsyncFilter { CancelAt = 1 },
                new TestAsyncFilter { CancelAt = 1 },
            });

            Assert.That(responseBatch, Is.Null);
            Assert.That(AppHost.CancelledAt, Is.EqualTo(1));
        }
    }
}