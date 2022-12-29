using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.FluentValidation;
using ServiceStack.Text;
using ServiceStack.Validation;
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

    [Route("/test/async-filter")]
    public class TestAsyncFilterRestHandler : TestAsyncFilter { }

    [Route("/test/async-validator")]
    public class TestAsyncValidator : IReturn<TestAsyncValidator>
    {
        public int Age { get; set; }
        public string Name { get; set; }
    }

    public class MyTestAsyncValidator : AbstractValidator<TestAsyncValidator>
    {
        public MyTestAsyncValidator()
        {
            RuleSet(ApplyTo.Post, () =>
            {
                RuleFor(x => x.Name).MustAsync(async (s, token) =>
                    {
                        await Task.Delay(10, token);
                        return !string.IsNullOrEmpty(s);
                    })
                    .WithMessage("'Name' should not be empty.")
                    .WithErrorCode("NotEmpty");
            });
            RuleSet(ApplyTo.Put, () =>
            {
                RuleFor(x => x.Name).NotEmpty();
            });
            RuleFor(x => x.Age).GreaterThan(0);
        }
    }

    [Route("/test/all-async-validator")]
    public class TestAllAsyncValidator : IReturn<TestAsyncValidator>
    {
        public int Age { get; set; }
        public string Name { get; set; }
    }

    [Route("/test/async-validator-gateway")]
    public class TestAsyncGatewayValidator : IReturn<TestAsyncGatewayValidator>
    {
        public int Age { get; set; }
        public string Name { get; set; }
    }

    public class MyTestAsyncGatewayValidator : AbstractValidator<TestAsyncGatewayValidator>
    {
        public MyTestAsyncGatewayValidator()
        {
            RuleFor(x => x.Name).MustAsync(async (s, token) => 
                (await Gateway.SendAsync(new GetStringLength { Value = s })).Result > 0)
            .WithMessage("'Name' should not be empty.")
            .WithErrorCode("NotEmpty");
        }
    }

    public class GetStringLength : IReturn<GetStringLengthResponse>
    {
        public string Value { get; set; }
    }

    public class GetStringLengthResponse
    {
        public int Result { get; set; }
    }

    public class MyAllTestAsyncValidator : AbstractValidator<TestAllAsyncValidator>
    {
        public MyAllTestAsyncValidator()
        {
            RuleFor(x => x.Name).MustAsync(async (s, token) =>
                {
                    await Task.Delay(10, token);
                    return !string.IsNullOrEmpty(s);
                })
                .WithMessage("'Name' should not be empty.")
                .WithErrorCode("NotEmpty");

            RuleFor(x => x.Age).MustAsync(async (age, token) =>
                {
                    await Task.Delay(10, token);
                    return age > 0;
                })
                .WithMessage("'Age' must be greater than '0'.")
                .WithErrorCode("GreaterThan");
        }
    }
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

        public object Any(TestAsyncValidator request) => request;

        public object Any(TestAllAsyncValidator request) => request;

        public object Any(TestAsyncGatewayValidator request) => request;

        public async Task<GetStringLengthResponse> Any(GetStringLength request)
        {
            await Task.Yield();
            return new GetStringLengthResponse
            {
                Result = (request.Value ?? "").Length,
            };
        }
    }

    public class AsyncFiltersTests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost() : base(nameof(AsyncFiltersTests), typeof(TestAsyncFilterService).Assembly) { }

            public static int CancelledAt = -1;

            public override void Configure(Container container)
            {
                GlobalRequestFiltersAsync.Add(CreateAsyncFilter(0));
                GlobalRequestFiltersAsync.Add(CreateAsyncFilter(1));
                GlobalRequestFiltersAsync.Add(CreateAsyncFilter(2));

                Plugins.Add(new ValidationFeature());
                container.RegisterValidators(typeof(MyTestAsyncValidator).Assembly);
            }

            private static Func<IRequest, IResponse, object, Task> CreateAsyncFilter(int pos)
            {
                return (req, res, dto) =>
                {
                    if (dto is TestAsyncFilter asyncDto)
                    {
                        CancelledAt++;

                        asyncDto.Results.Add("GlobalRequestFiltersAsync#" + pos);
                        if (asyncDto.ReturnAt == pos)
                        {
                            res.ContentType = MimeTypes.Json;
                            return res.WriteAsync(asyncDto.ToJson())
                                .ContinueWith(t => res.EndRequest(skipHeaders: true));
                        }

                        if (asyncDto.ErrorAt == pos)
                            throw new ArgumentException("ErrorAt#" + pos);

                        if (asyncDto.DirectErrorAt == pos)
                        {
                            res.ContentType = MimeTypes.Json;
                            return res.WriteError(new ArgumentException("DirectErrorAt#" + pos));
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
                var responseBatch = client.SendAll(new[]
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

        [Test]
        public void Does_execute_mixed_async_validator_as_sync()
        {
            try
            {
                var response = client.Put(new TestAsyncValidator());
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                ex.ResponseStatus.PrintDump();
                var status = ex.ResponseStatus;

                Assert.That(status.ErrorCode, Is.EqualTo("NotEmpty"));
                Assert.That(status.Message, Is.EqualTo("'Name' must not be empty."));

                Assert.That(status.Errors.Count, Is.EqualTo(2));
                Assert.That(status.Errors[0].ErrorCode, Is.EqualTo("NotEmpty"));
                Assert.That(status.Errors[0].FieldName, Is.EqualTo("Name"));
                Assert.That(status.Errors[0].Message, Is.EqualTo("'Name' must not be empty."));

                Assert.That(status.Errors[1].ErrorCode, Is.EqualTo("GreaterThan"));
                Assert.That(status.Errors[1].FieldName, Is.EqualTo("Age"));
                Assert.That(status.Errors[1].Message, Is.EqualTo("'Age' must be greater than '0'."));
            }
        }

        [Test]
        public void Does_execute_mixed_async_validator_as_async()
        {
            try
            {
                var response = client.Post(new TestAsyncValidator());
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                ex.ResponseStatus.PrintDump();
                var status = ex.ResponseStatus;

                Assert.That(status.ErrorCode, Is.EqualTo("NotEmpty"));
                Assert.That(status.Message, Is.EqualTo("'Name' should not be empty."));

                Assert.That(status.Errors.Count, Is.EqualTo(2));
                Assert.That(status.Errors[0].ErrorCode, Is.EqualTo("NotEmpty"));
                Assert.That(status.Errors[0].FieldName, Is.EqualTo("Name"));
                Assert.That(status.Errors[0].Message, Is.EqualTo("'Name' should not be empty."));

                Assert.That(status.Errors[1].ErrorCode, Is.EqualTo("GreaterThan"));
                Assert.That(status.Errors[1].FieldName, Is.EqualTo("Age"));
                Assert.That(status.Errors[1].Message, Is.EqualTo("'Age' must be greater than '0'."));
            }
        }

        [Test]
        public void Can_send_valid_mixed_AsyncValidator_request()
        {
            var syncResponse = client.Put(new TestAsyncValidator { Age = 1, Name = "one" });
            var asyncResponse = client.Post(new TestAsyncValidator { Age = 2, Name = "two" });
        }

        [Test]
        public void Does_execute_all_async_validator()
        {
            try
            {
                var response = client.Post(new TestAllAsyncValidator());
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                ex.ResponseStatus.PrintDump();
                var status = ex.ResponseStatus;

                Assert.That(status.ErrorCode, Is.EqualTo("NotEmpty"));
                Assert.That(status.Message, Is.EqualTo("'Name' should not be empty."));

                Assert.That(status.Errors.Count, Is.EqualTo(2));
                Assert.That(status.Errors[0].ErrorCode, Is.EqualTo("NotEmpty"));
                Assert.That(status.Errors[0].FieldName, Is.EqualTo("Name"));
                Assert.That(status.Errors[0].Message, Is.EqualTo("'Name' should not be empty."));

                Assert.That(status.Errors[1].ErrorCode, Is.EqualTo("GreaterThan"));
                Assert.That(status.Errors[1].FieldName, Is.EqualTo("Age"));
                Assert.That(status.Errors[1].Message, Is.EqualTo("'Age' must be greater than '0'."));
            }
        }

        [Test]
        public void Does_execute_async_validator_calling_async_Gateway()
        {
            try
            {
                var response = client.Post(new TestAsyncGatewayValidator());
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                ex.ResponseStatus.PrintDump();
                var status = ex.ResponseStatus;

                Assert.That(status.ErrorCode, Is.EqualTo("NotEmpty"));
                Assert.That(status.Message, Is.EqualTo("'Name' should not be empty."));

                Assert.That(status.Errors.Count, Is.EqualTo(1));
                Assert.That(status.Errors[0].ErrorCode, Is.EqualTo("NotEmpty"));
                Assert.That(status.Errors[0].FieldName, Is.EqualTo("Name"));
                Assert.That(status.Errors[0].Message, Is.EqualTo("'Name' should not be empty."));
            }
        }
    }
}