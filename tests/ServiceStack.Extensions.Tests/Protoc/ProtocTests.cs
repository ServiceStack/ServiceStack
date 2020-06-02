using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Funq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ProtoBuf.Grpc.Client;
using ServiceStack.Text;
using ServiceStack.Validation;

namespace ServiceStack.Extensions.Tests.Protoc
{
    public class ProtocTests
    {
        public class AppHost : AppSelfHostBase
        {
            public AppHost() 
                : base(nameof(ProtocTests), typeof(MyServices).Assembly) { }

            public override void Configure(Container container)
            {
                RegisterService<GetFileService>();
                
                Plugins.Add(new ValidationFeature());
                Plugins.Add(new GrpcFeature(App));
                
                GlobalRequestFilters.Add((req, res, dto) => {
                    if (dto is ServiceStack.Extensions.Tests.ThrowCustom)
                        throw new CustomException();
                });
            }

            public override void ConfigureKestrel(KestrelServerOptions options)
            {
                options.ListenLocalhost(TestsConfig.Port, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http2;
                });
            }

            public override void Configure(IServiceCollection services)
            {
                services.AddServiceStackGrpc();
            }

            public override void Configure(IApplicationBuilder app)
            {
                app.UseRouting();

                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGrpcService<MyCalculator>();
                });
            }
        }

        private readonly ServiceStackHost appHost;
        public ProtocTests()
        {
            GrpcClientFactory.AllowUnencryptedHttp2 = true;
            appHost = new AppHost()
                .Init()
                .Start(TestsConfig.BaseUri);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();


        public static GrpcServices.GrpcServicesClient GetClient(Action<GrpcClientConfig> init=null)
        {
            GrpcClientFactory.AllowUnencryptedHttp2 = true;
            GrpcServiceStack.ParseResponseStatus = bytes => ResponseStatus.Parser.ParseFrom(bytes);
            
            var config = new GrpcClientConfig();
            init?.Invoke(config);
            var client = new GrpcServices.GrpcServicesClient(
                GrpcServiceStack.Client(TestsConfig.BaseUri, config));
            return client;
        }

        [Test]
        public async Task Can_call_Multiply_Grpc_Service_GrpcServiceClient()
        {
            var client = GetClient();

            var response = await client.PostMultiplyAsync(new Multiply { X = 12, Y = 4 });
            Assert.That(response.Result, Is.EqualTo(48));
        }

        [Test]
        public void Can_call_Multiply_Grpc_Service_GrpcServiceClient_sync()
        {
            var client = GetClient();

            var response = client.PostMultiply(new Multiply { X = 12, Y = 4 });
            Assert.That(response.Result, Is.EqualTo(48));
        }

        [Test]
        public async Task Can_call_Incr_ReturnVoid_GrpcServiceClient()
        {
            var client = GetClient();

            ServiceStack.Extensions.Tests.Incr.Counter = 0;

            await client.PostIncrAsync(new Incr { Amount = 1 });
            Assert.That(ServiceStack.Extensions.Tests.Incr.Counter, Is.EqualTo(1));

            await client.PostIncrAsync(new Incr { Amount = 2 });
            Assert.That(ServiceStack.Extensions.Tests.Incr.Counter, Is.EqualTo(3));
        }

        [Test]
        public async Task Can_call_GetHello_with_Get()
        {
            var client = GetClient();

            var response = await client.CallGetHelloAsync(new GetHello { Name = "GET" });
            Assert.That(response.Result, Is.EqualTo($"Hello, GET!"));
        }

        [Test]
        public void Can_call_GetHello_with_Get_sync()
        {
            var client = GetClient();

            var response = client.CallGetHello(new GetHello { Name = "GET" });
            Assert.That(response.Result, Is.EqualTo($"Hello, GET!"));
        }

        [Test]
        public async Task Can_call_AnyHello_with_Get_Post_or_Send()
        {
            var client = GetClient();

            var response = await client.GetAnyHelloAsync(new AnyHello { Name = "GET" });
            Assert.That(response.Result, Is.EqualTo($"Hello, GET!"));

            response = await client.PostAnyHelloAsync(new AnyHello { Name = "POST" });
            Assert.That(response.Result, Is.EqualTo($"Hello, POST!"));
        }

        [Test]
        public void Can_call_AnyHello_with_Get_Post_or_Send_sync()
        {
            var client = GetClient();

            var response = client.GetAnyHello(new AnyHello { Name = "GET" });
            Assert.That(response.Result, Is.EqualTo($"Hello, GET!"));

            response = client.PostAnyHello(new AnyHello { Name = "POST" });
            Assert.That(response.Result, Is.EqualTo($"Hello, POST!"));
        }

        [Test]
        public async Task Does_throw_WebServiceException()
        {
            var client = GetClient();

            try
            {
                await client.GetThrowAsync(new Throw { Message = "throw test" });
                Assert.Fail("should throw");
            }
            catch (WebServiceException e)
            {
                Assert.That(e.StatusCode, Is.EqualTo(500));
                Assert.That(e.Message, Is.EqualTo("throw test"));
            }
        }

        [Test]
        public void Does_throw_WebServiceException_sync()
        {
            var client = GetClient();

            try
            {
                client.GetThrow(new Throw { Message = "throw test" });
                Assert.Fail("should throw");
            }
            catch (WebServiceException e)
            {
                Assert.That(e.StatusCode, Is.EqualTo(500));
                Assert.That(e.Message, Is.EqualTo("throw test"));
            }
        }

        [Test]
        public async Task Does_throw_WebServiceException_ReturnVoid()
        {
            var client = GetClient();

            try
            {
                await client.GetThrowVoidAsync(new ThrowVoid { Message = "throw test" });
                Assert.Fail("should throw");
            }
            catch (WebServiceException e)
            {
                Assert.That(e.StatusCode, Is.EqualTo(500));
                Assert.That(e.Message, Is.EqualTo("throw test"));
            }
        }

        [Test]
        public void Does_throw_WebServiceException_ReturnVoid_sync()
        {
            var client = GetClient();

            try
            {
                client.GetThrowVoid(new ThrowVoid { Message = "throw test" });
                Assert.Fail("should throw");
            }
            catch (WebServiceException e)
            {
                Assert.That(e.StatusCode, Is.EqualTo(500));
                Assert.That(e.Message, Is.EqualTo("throw test"));
            }
        }

        public static ProtoBuf.Bcl.Decimal ToProtoBufDecimal(decimal value)
        {
            // https://github.com/protobuf-net/protobuf-net/blob/master/src/protobuf-net.Core/Internal/PrimaryTypeProvider.Decimal.cs
            var to = new ProtoBuf.Bcl.Decimal();
            int[] bits = decimal.GetBits(value);
            ulong a = ((ulong) bits[1]) << 32, b = ((ulong) bits[0]) & 0xFFFFFFFFL;
            to.Lo = a | b;
            to.Hi = (uint) bits[2];
            to.SignScale = (uint) (((bits[3] >> 15) & 0x01FE) | ((bits[3] >> 31) & 0x0001));
            return to;
        }

        public static Guid ToGuid(ProtoBuf.Bcl.Guid value)
        {
            // https://github.com/protobuf-net/protobuf-net/blob/master/src/protobuf-net.Core/Internal/PrimaryTypeProvider.Guid.cs
            var low = value.Lo;
            var high = value.Hi;
            uint a = (uint)(low >> 32), b = (uint)low, c = (uint)(high >> 32), d = (uint)high;
            return new Guid((int)b, (short)a, (short)(a >> 16),
                (byte)d, (byte)(d >> 8), (byte)(d >> 16), (byte)(d >> 24),
                (byte)c, (byte)(c >> 8), (byte)(c >> 16), (byte)(c >> 24));
        }

        [Test]
        public async Task Triggering_all_validators_returns_right_ErrorCode()
        {
            var client = GetClient();

            var request = new TriggerValidators
            {
                CreditCard = "NotCreditCard",
                Email = "NotEmail",
                Empty = "NotEmpty",
                Equal = "NotEqual",
                ExclusiveBetween = 1,
                GreaterThan = 1,
                GreaterThanOrEqual = 1,
                InclusiveBetween = 1,
                Length = "Length",
                LessThan = 20,
                LessThanOrEqual = 20,
                NotEmpty = "",
                NotEqual = "NotEqual",
                Null = "NotNull",
                RegularExpression = "FOO",
                ScalePrecision = 123.456m.ToString(CultureInfo.InvariantCulture)
            };

            try
            {
                var response = await client.PostTriggerValidatorsAsync(request);
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                //ex.ResponseStatus.PrintDump();
                AssertTriggerValidatorsResponse(ex);
            }
        }

        [Test]
        public async Task Triggering_all_validators_returns_right_ErrorCode_from_Headers()
        {
            var client = GetClient();

            try
            {
                var response = await client.PostTriggerValidatorsAsync(new TriggerValidators(),
                    GrpcUtils.ToHeaders(new {
                        CreditCard = "NotCreditCard",
                        Email = "NotEmail",
                        Empty = "NotEmpty",
                        Equal = "NotEqual",
                        ExclusiveBetween = 1,
                        GreaterThan = 1,
                        GreaterThanOrEqual = 1,
                        InclusiveBetween = 1,
                        Length = "Length",
                        LessThan = 20,
                        LessThanOrEqual = 20,
                        NotEmpty = "",
                        NotEqual = "NotEqual",
                        Null = "NotNull",
                        RegularExpression = "FOO",
                        ScalePrecision = 123.456m
                    }));
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                //ex.ResponseStatus.PrintDump();
                AssertTriggerValidatorsResponse(ex);
            }
        }

        private static void AssertTriggerValidatorsResponse(WebServiceException ex)
        {
            Assert.That(ex.StatusCode, Is.EqualTo(400));
            var errors = ex.ResponseStatus.Errors;
            Assert.That(errors.First(x => x.FieldName == "CreditCard").ErrorCode, Is.EqualTo("CreditCard"));
            Assert.That(errors.First(x => x.FieldName == "Email").ErrorCode, Is.EqualTo("Email"));
            Assert.That(errors.First(x => x.FieldName == "Email").ErrorCode, Is.EqualTo("Email"));
            Assert.That(errors.First(x => x.FieldName == "Empty").ErrorCode, Is.EqualTo("Empty"));
            Assert.That(errors.First(x => x.FieldName == "Equal").ErrorCode, Is.EqualTo("Equal"));
            Assert.That(errors.First(x => x.FieldName == "ExclusiveBetween").ErrorCode, Is.EqualTo("ExclusiveBetween"));
            Assert.That(errors.First(x => x.FieldName == "GreaterThan").ErrorCode, Is.EqualTo("GreaterThan"));
            Assert.That(errors.First(x => x.FieldName == "GreaterThanOrEqual").ErrorCode, Is.EqualTo("GreaterThanOrEqual"));
            Assert.That(errors.First(x => x.FieldName == "InclusiveBetween").ErrorCode, Is.EqualTo("InclusiveBetween"));
            Assert.That(errors.First(x => x.FieldName == "Length").ErrorCode, Is.EqualTo("Length"));
            Assert.That(errors.First(x => x.FieldName == "LessThan").ErrorCode, Is.EqualTo("LessThan"));
            Assert.That(errors.First(x => x.FieldName == "LessThanOrEqual").ErrorCode, Is.EqualTo("LessThanOrEqual"));
            Assert.That(errors.First(x => x.FieldName == "NotEmpty").ErrorCode, Is.EqualTo("NotEmpty"));
            Assert.That(errors.First(x => x.FieldName == "NotEqual").ErrorCode, Is.EqualTo("NotEqual"));
            Assert.That(errors.First(x => x.FieldName == "Null").ErrorCode, Is.EqualTo("Null"));
            Assert.That(errors.First(x => x.FieldName == "RegularExpression").ErrorCode, Is.EqualTo("RegularExpression"));
            Assert.That(errors.First(x => x.FieldName == "ScalePrecision").ErrorCode, Is.EqualTo("ScalePrecision"));
        }

        [Test]
        public async Task Does_throw_WebServiceException_on_CustomException()
        {
            var client = GetClient();

            try
            {
                await client.GetThrowCustomAsync(new ThrowCustom());
                Assert.Fail("should throw");
            }
            catch (WebServiceException e)
            {
                Assert.That(e.StatusCode, Is.EqualTo(401));
                Assert.That(e.Message, Is.EqualTo("Custom Error Message"));
            }
        }

        [Test]
        public async Task Does_return_Custom_Headers()
        {
            string customHeader = null;
            var client = GetClient(c => {
                c.ResponseFilter = ctx => customHeader = ctx.GetHeader("X-Custom");
            });

            await client.GetAddHeaderAsync(new AddHeader { Name = "X-Custom", Value = "A" });
            Assert.That(customHeader, Is.EqualTo("A"));
        }

        [Test]
        public async Task Can_download_file()
        {
            var client = GetClient();
            var response = await client.CallGetFileAsync(new GetFile { Path = "/js/ss-utils.js" });
            AssertSSUtils(response);
        }

        private static void AssertSSUtils(FileContent response)
        {
            Assert.That(response.Name, Is.EqualTo("ss-utils.js"));
            Assert.That(response.Length, Is.GreaterThan(0));
            Assert.That(response.Length, Is.EqualTo(response.Body.Length));
            var str = response.Body.Span.FromUtf8Bytes();
            Assert.That(str, Does.Contain("if (!$.ss) $.ss = {};"));
        }

        private static void AssertFiles(List<FileContent> responses)
        {
            Assert.That(responses.Count, Is.EqualTo(3));
            AssertSSUtils(responses[0]);
            Assert.That(responses[1].Name, Is.EqualTo("hot-loader.js"));
            Assert.That(responses[2].Name, Is.EqualTo("hot-fileloader.js"));
        }

        [Test]
        public async Task Can_stream_multiple_files()
        {
            var client = GetClient();
        
            var request = new StreamFiles {
                Paths = {
                    "/js/ss-utils.js",
                    "/js/hot-loader.js",
                    "/js/not-exists.js",
                    "/js/hot-fileloader.js",
                }
            };
        
            var files = new List<FileContent>();
            var stream = client.ServerStreamFiles(request).ResponseStream;
            while (await stream.MoveNext(default))
            {
                var file = stream.Current;
                files.Add(file);
            }

            Assert.That(files.Count, Is.EqualTo(request.Paths.Count));
            Assert.That(files[2].ResponseStatus.ErrorCode, Is.EqualTo(nameof(HttpStatusCode.NotFound)));
            files = files.Where(x => x.ResponseStatus == null).ToList();
            AssertFiles(files);
        }
    }
}