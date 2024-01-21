using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using Funq;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Grpc.Client;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Grpc.Server;
using ServiceStack.Auth;
using ServiceStack.FluentValidation;
using ServiceStack.FluentValidation.Validators;
using ServiceStack.Model;
using ServiceStack.Text;
using ServiceStack.Validation;

namespace ServiceStack.Extensions.Tests;

[ServiceContract(Name = "Hyper.Calculator")]
public interface ICalculator
{
    ValueTask<MultiplyResult> MultiplyAsync(MultiplyRequest request);
}

[DataContract]
public class MultiplyRequest
{
    [DataMember(Order = 1)]
    public int X { get; set; }

    [DataMember(Order = 2)]
    public int Y { get; set; }
}

[DataContract]
public class MultiplyResult
{
    [DataMember(Order = 1)]
    public int Result { get; set; }
}
    
//    [ServiceContract]
//    public interface ITimeService
//    {
//        IAsyncEnumerable<TimeResult> SubscribeAsync(CallContext context = default);
//    }

//    [ProtoContract]
//    public class TimeResult
//    {
//        [ProtoMember(1, DataFormat = DataFormat.WellKnown)]
//        public DateTime Time { get; set; }
//    }
    
public class MyCalculator : ICalculator
{
    ValueTask<MultiplyResult> ICalculator.MultiplyAsync(MultiplyRequest request)
    {
        var result = new MultiplyResult { Result = request.X * request.Y };
        return new ValueTask<MultiplyResult>(result);
    }
}
    
//    public class MyTimeService : ITimeService
//    {
//        public IAsyncEnumerable<TimeResult> SubscribeAsync(CallContext context = default)
//            => SubscribeAsyncImpl(default); // context.CancellationToken);
//
//        private async IAsyncEnumerable<TimeResult> SubscribeAsyncImpl([EnumeratorCancellation] CancellationToken cancel)
//        {
//            while (!cancel.IsCancellationRequested)
//            {
//                await Task.Delay(TimeSpan.FromSeconds(10));
//                yield return new TimeResult { Time = DateTime.UtcNow };
//            }
//        }
//    }    
    
public class Startup
{
    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddCodeFirstGrpc();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGrpcService<MyCalculator>();
//                endpoints.MapGrpcService<MyTimeService>();
        });
    }
}

[DataContract]
public class Multiply : IReturn<MultiplyResponse>
{
    [DataMember(Order = 1)]
    public int X { get; set; }

    [DataMember(Order = 2)]
    public int Y { get; set; }
}

[DataContract]
public class MultiplyResponse
{
    [DataMember(Order = 1)]
    public int Result { get; set; }
    public ResponseStatus ResponseStatus { get; set; }
}

[DataContract]
public class Incr : IReturnVoid
{
    internal static int Counter = 0;
        
    [DataMember(Order = 1)]
    public int Amount { get; set; }
}

[DataContract]
public class GetHello : IReturn<HelloResponse>, IGet
{
    [DataMember(Order = 1)]
    public string Name { get; set; }
}

[DataContract]
public class AnyHello : IReturn<HelloResponse>
{
    [DataMember(Order = 1)]
    public string Name { get; set; }
}

[DataContract]
public class HelloResponse
{
    [DataMember(Order = 1)]
    public string Result { get; set; }
    [DataMember(Order = 2)]
    public ResponseStatus ResponseStatus { get; set; }
}

[DataContract]
public class Throw : IReturn<HelloResponse>
{
    [DataMember(Order = 1)]
    public string Message { get; set; }
}

[DataContract]
public class ThrowVoid : IReturnVoid
{
    [DataMember(Order = 1)]
    public string Message { get; set; }
}

[DataContract]
public class AddHeader : IReturnVoid
{
    [DataMember(Order = 1)]
    public string Name { get; set; }
    [DataMember(Order = 2)]
    public string Value { get; set; }
}
    
[DataContract]
public class TriggerValidators : IReturn<EmptyResponse>
{
    [DataMember(Order = 1)]
    public string CreditCard { get; set; }
    [DataMember(Order = 2)]
    public string Email { get; set; }
    [DataMember(Order = 3)]
    public string Empty { get; set; }
    [DataMember(Order = 4)]
    public string Equal { get; set; }
    [DataMember(Order = 5)]
    public int ExclusiveBetween { get; set; }
    [DataMember(Order = 6)]
    public int GreaterThanOrEqual { get; set; }
    [DataMember(Order = 7)]
    public int GreaterThan { get; set; }
    [DataMember(Order = 8)]
    public int InclusiveBetween { get; set; }
    [DataMember(Order = 9)]
    public string Length { get; set; }
    [DataMember(Order = 10)]
    public int LessThanOrEqual { get; set; }
    [DataMember(Order = 11)]
    public int LessThan { get; set; }
    [DataMember(Order = 12)]
    public string NotEmpty { get; set; }
    [DataMember(Order = 13)]
    public string NotEqual { get; set; }
    [DataMember(Order = 14)]
    public string Null { get; set; }
    [DataMember(Order = 15)]
    public string RegularExpression { get; set; }
    [DataMember(Order = 16)]
    public decimal ScalePrecision { get; set; }
}

public class TriggerValidatorsValidator : AbstractValidator<TriggerValidators>
{
    public TriggerValidatorsValidator()
    {
        RuleFor(x => x.CreditCard).CreditCard();
        RuleFor(x => x.Email).EmailAddress();
        RuleFor(x => x.Empty).Empty();
        RuleFor(x => x.Equal).Equal("Equal");
        RuleFor(x => x.ExclusiveBetween).ExclusiveBetween(10, 20);
        RuleFor(x => x.GreaterThanOrEqual).GreaterThanOrEqualTo(10);
        RuleFor(x => x.GreaterThan).GreaterThan(10);
        RuleFor(x => x.InclusiveBetween).InclusiveBetween(10, 20);
        RuleFor(x => x.Length).Length(10);
        RuleFor(x => x.LessThanOrEqual).LessThanOrEqualTo(10);
        RuleFor(x => x.LessThan).LessThan(10);
        RuleFor(x => x.NotEmpty).NotEmpty();
        RuleFor(x => x.NotEqual).NotEqual("NotEqual");
        RuleFor(x => x.Null).Null();
        RuleFor(x => x.RegularExpression).Matches(@"^[a-z]*$");
        RuleFor(x => x.ScalePrecision).SetValidator(new ScalePrecisionValidator(1, 1));
    }
}

[Route("/channels/{Channel}/chat")]
[DataContract]
public class PostChatToChannel : IReturn<ChatMessage>, IPost
{
    [DataMember(Order = 1)]
    public string From { get; set; }
    [DataMember(Order = 2)]
    public string ToUserId { get; set; }
    [DataMember(Order = 3)]
    public string Channel { get; set; }
    [DataMember(Order = 4)]
    public string Message { get; set; }
    [DataMember(Order = 5)]
    public string Selector { get; set; }
}
    
[DataContract]
public class ChatMessage
{
    [DataMember(Order = 1)]
    public long Id { get; set; }
    [DataMember(Order = 2)]
    public string Channel { get; set; }
    [DataMember(Order = 3)]
    public string FromUserId { get; set; }
    [DataMember(Order = 4)]
    public string FromName { get; set; }
    [DataMember(Order = 5)]
    public string DisplayName { get; set; }
    [DataMember(Order = 6)]
    public string Message { get; set; }
    [DataMember(Order = 7)]
    public string UserAuthId { get; set; }
    [DataMember(Order = 8)]
    public bool Private { get; set; }
}
    
public class CustomException : Exception, IResponseStatusConvertible, IHasStatusCode
{
    public ResponseStatus ToResponseStatus() => new ResponseStatus
    {
        ErrorCode = "CustomErrorCode",
        Message = "Custom Error Message",
    };

    public int StatusCode { get; } = 401;
}
    
[DataContract]
public class ThrowCustom : IReturn<ThrowCustomResponse> {}

[DataContract]
public class ThrowCustomResponse
{
    [DataMember(Order = 1)]
    public ResponseStatus ResponseStatus { get; set; }
}
    

public class MyServices : Service
{
    public Task<MultiplyResponse> Post(Multiply request)
    {
        var result = new MultiplyResponse { Result = request.X * request.Y };
        return Task.FromResult(result);
    }

    public void Any(Incr request)
    {
        request.Amount.Times(x => Interlocked.Increment(ref Incr.Counter));
    }

    public object Get(GetHello request) => new HelloResponse { Result = $"Hello, {request.Name}!" };

    public object Any(AnyHello request) => new HelloResponse { Result = $"Hello, {request.Name}!" };
        
    public object Get(Throw request) => throw new Exception(request.Message ?? "Error in Throw");
        
    public void Get(ThrowVoid request) => throw new Exception(request.Message ?? "Error in ThrowVoid");

    public object Get(ThrowCustom request) => request; //thrown in Global Request Filters

    public object Post(TriggerValidators request) => new EmptyResponse();

    public void Get(AddHeader request)
    {
        Response.AddHeader(request.Name, request.Value);
    }
        
    public IServerEvents ServerEvents { get; set; }
    public int Id = 0;

    public async Task<object> Any(PostChatToChannel request)
    {
        var msg = new ChatMessage
        {
            Id = Id++,
            Channel = request.Channel,
            FromUserId = request.From,
            FromName = request.From,
            Message = request.Message.HtmlEncode(),
        };

        await ServerEvents.NotifyChannelAsync(request.Channel, request.Selector, msg);

        return msg;
    }        
}
    
/// <summary>
/// TODO:
/// - Exceptions
/// - Validation
/// - Auth
///   - JWT
///   - Basic Auth
/// - AutoQuery
/// - Multitenancy? 
/// </summary>

public class GrpcTests
{
    public class AppHost : AppSelfHostBase
    {
        public AppHost() 
            : base(nameof(GrpcTests), typeof(MyServices).Assembly) { }

        public override void Configure(Container container)
        {
            RegisterService<GetFileService>();
                
            Plugins.Add(new ValidationFeature());
            Plugins.Add(new GrpcFeature(App));
                
            GlobalRequestFilters.Add((req, res, dto) => {
                if (dto is ThrowCustom)
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
    public GrpcTests()
    {
        GrpcClientFactory.AllowUnencryptedHttp2 = true;
        appHost = new AppHost()
            .Init()
            .Start(TestsConfig.ListeningOn);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => appHost.Dispose();

    private static GrpcServiceClient GetClient() => new GrpcServiceClient(TestsConfig.BaseUri);

    [Test]
    public async Task Can_call_MultiplyRequest_Grpc_Service_ICalculator()
    {
        GrpcClientFactory.AllowUnencryptedHttp2 = true;
        using var http = GrpcChannel.ForAddress(TestsConfig.BaseUri);
        var calculator = http.CreateGrpcService<ICalculator>();
        var result = await calculator.MultiplyAsync(new MultiplyRequest { X = 12, Y = 4 });
        Assert.That(result.Result, Is.EqualTo(48));
    }

    [Test]
    public async Task Can_call_Multiply_Grpc_Service_GrpcChannel()
    {
        GrpcClientFactory.AllowUnencryptedHttp2 = true;
        using var http = GrpcChannel.ForAddress(TestsConfig.BaseUri);

        var response = await http.CreateCallInvoker().Execute<Multiply, MultiplyResponse>(new Multiply { X = 12, Y = 4 }, "GrpcServices",
            GrpcConfig.GetServiceName(HttpMethods.Post, nameof(Multiply)));

        Assert.That(response.Result, Is.EqualTo(48));
    }

    [Test]
    public async Task Can_call_Multiply_Grpc_Service_GrpcServiceClient()
    {
        using var client = GetClient();

        var response = await client.PostAsync(new Multiply { X = 12, Y = 4 });
        Assert.That(response.Result, Is.EqualTo(48));
    }

    [Test]
    public void Can_call_Multiply_Grpc_Service_GrpcServiceClient_sync()
    {
        using var client = GetClient();

        var response = client.Post(new Multiply { X = 12, Y = 4 });
        Assert.That(response.Result, Is.EqualTo(48));
    }

    [Test]
    public async Task Can_call_Incr_ReturnVoid_GrpcServiceClient()
    {
        using var client = GetClient();

        Incr.Counter = 0;

        await client.PublishAsync(new Incr { Amount = 1 });
        Assert.That(Incr.Counter, Is.EqualTo(1));

        await client.PublishAsync(new Incr { Amount = 2 });
        Assert.That(Incr.Counter, Is.EqualTo(3));
    }

    [Test]
    public async Task Can_call_GetHello_with_Get_or_Send()
    {
        using var client = GetClient();

        var response = await client.GetAsync(new GetHello { Name = "GET" });
        Assert.That(response.Result, Is.EqualTo($"Hello, GET!"));

        response = await client.SendAsync(new GetHello { Name = "SEND" });
        Assert.That(response.Result, Is.EqualTo($"Hello, SEND!"));
    }

    [Test]
    public void Can_call_GetHello_with_Get_or_Send_sync()
    {
        using var client = GetClient();

        var response = client.Get(new GetHello { Name = "GET" });
        Assert.That(response.Result, Is.EqualTo($"Hello, GET!"));

        response = client.Send(new GetHello { Name = "SEND" });
        Assert.That(response.Result, Is.EqualTo($"Hello, SEND!"));
    }

    [Test]
    public async Task Can_call_AnyHello_with_Get_Post_or_Send()
    {
        using var client = GetClient();

        var response = await client.GetAsync(new AnyHello { Name = "GET" });
        Assert.That(response.Result, Is.EqualTo($"Hello, GET!"));

        response = await client.PostAsync(new AnyHello { Name = "POST" });
        Assert.That(response.Result, Is.EqualTo($"Hello, POST!"));

        response = await client.SendAsync(new GetHello { Name = "SEND" });
        Assert.That(response.Result, Is.EqualTo($"Hello, SEND!"));
    }

    [Test]
    public void Can_call_AnyHello_with_Get_Post_or_Send_sync()
    {
        using var client = GetClient();

        var response = client.Get(new AnyHello { Name = "GET" });
        Assert.That(response.Result, Is.EqualTo($"Hello, GET!"));

        response = client.Post(new AnyHello { Name = "POST" });
        Assert.That(response.Result, Is.EqualTo($"Hello, POST!"));

        response = client.Send(new GetHello { Name = "SEND" });
        Assert.That(response.Result, Is.EqualTo($"Hello, SEND!"));
    }

    [Test]
    public async Task Can_call_AnyHello_Batch()
    {
        using var client = GetClient();

        var requests = new[] {
            new AnyHello {Name = "A"},
            new AnyHello {Name = "B"},
            new AnyHello {Name = "C"},
        };
        var responses = await client.SendAllAsync(requests);
        Assert.That( responses.Map(x => x.Result), Is.EqualTo(new[] {
            $"Hello, A!",
            $"Hello, B!",
            $"Hello, C!",
        }));
    }

    [Test]
    public void Can_call_AnyHello_Batch_sync()
    {
        using var client = GetClient();

        var requests = new[] {
            new AnyHello {Name = "A"},
            new AnyHello {Name = "B"},
            new AnyHello {Name = "C"},
        };
        var responses = client.SendAll(requests);
        Assert.That( responses.Map(x => x.Result), Is.EqualTo(new[] {
            $"Hello, A!",
            $"Hello, B!",
            $"Hello, C!",
        }));
    }

    [Test]
    public async Task Can_call_Incr_Batch_ReturnVoid()
    {
        using var client = GetClient();

        Incr.Counter = 0;
            
        var requests = new[] {
            new Incr {Amount = 1},
            new Incr {Amount = 2},
            new Incr {Amount = 3},
        };
        await client.PublishAllAsync(requests);
            
        Assert.That(Incr.Counter, Is.EqualTo(1 + 2 + 3));
    }

    [Test]
    public void Can_call_Incr_Batch_ReturnVoid_sync()
    {
        using var client = GetClient();

        Incr.Counter = 0;
            
        var requests = new[] {
            new Incr {Amount = 1},
            new Incr {Amount = 2},
            new Incr {Amount = 3},
        };
        client.PublishAll(requests);
            
        Assert.That(Incr.Counter, Is.EqualTo(1 + 2 + 3));
    }

    [Test]
    public async Task Does_throw_WebServiceException()
    {
        using var client = GetClient();

        try
        {
            await client.GetAsync(new Throw { Message = "throw test" });
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
        using var client = GetClient();

        try
        {
            client.Get(new Throw { Message = "throw test" });
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
        using var client = GetClient();

        try
        {
            await client.GetAsync(new ThrowVoid { Message = "throw test" });
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
        using var client = GetClient();

        try
        {
            client.Get(new ThrowVoid { Message = "throw test" });
            Assert.Fail("should throw");
        }
        catch (WebServiceException e)
        {
            Assert.That(e.StatusCode, Is.EqualTo(500));
            Assert.That(e.Message, Is.EqualTo("throw test"));
        }
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
            ScalePrecision = 123.456m
        };

        try
        {
            var response = await client.PostAsync(request);
            Assert.Fail("Should throw");
        }
        catch (WebServiceException ex)
        {
            //ex.ResponseStatus.PrintDump();
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
    }
        
    [Test]
    public async Task Does_throw_WebServiceException_on_CustomException()
    {
        using var client = GetClient();

        try
        {
            await client.GetAsync(new ThrowCustom());
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
        var client = GetClient();
        string customHeader = null;
        client.ResponseFilter = ctx => customHeader = ctx.GetHeader("X-Custom");

        await client.GetAsync(new AddHeader { Name = "X-Custom", Value = "A" });
        Assert.That(customHeader, Is.EqualTo("A"));
    }

    [Test]
    public async Task Can_download_file()
    {
        var client = GetClient();
        var response = await client.GetAsync(new GetFile { Path = "/js/ss-utils.js" });
        AssertSSUtils(response);
    }

    private static void AssertSSUtils(FileContent response)
    {
        Assert.That(response.Name, Is.EqualTo("ss-utils.js"));
        Assert.That(response.Length, Is.GreaterThan(0));
        Assert.That(response.Length, Is.EqualTo(response.Body.Length));
        var str = response.Body.FromUtf8Bytes();
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
    public async Task Can_download_multiple_files()
    {
        var client = GetClient();

        var files = new[] {
            new GetFile { Path = "/js/ss-utils.js" },
            new GetFile { Path = "/js/hot-loader.js" },
            new GetFile { Path = "/js/not-exists.js" },
            new GetFile { Path = "/js/hot-fileloader.js" },
        };

        var responses = await client.SendAllAsync(files);

        Assert.That(responses.Count, Is.EqualTo(files.Length));
        Assert.That(responses[2].ResponseStatus.ErrorCode, Is.EqualTo(nameof(HttpStatusCode.NotFound)));
        responses = responses.Where(x => x.ResponseStatus == null).ToList();
        AssertFiles(responses);
    }

    [Test]
    public async Task Can_stream_multiple_files()
    {
        var client = GetClient();

        var request = new StreamFiles {
            Paths = new List<string> {
                "/js/ss-utils.js",
                "/js/hot-loader.js",
                "/js/not-exists.js",
                "/js/hot-fileloader.js",
            }
        };

        var files = new List<FileContent>();
        await foreach (var file in client.StreamAsync(request))
        {
            files.Add(file);
        }
        Assert.That(files.Count, Is.EqualTo(request.Paths.Count));
        Assert.That(files[2].ResponseStatus.ErrorCode, Is.EqualTo(nameof(HttpStatusCode.NotFound)));
        files = files.Where(x => x.ResponseStatus == null).ToList();
        AssertFiles(files);
    }

    static string GetServiceProto<T>()
        => GrpcConfig.TypeModel.GetSchema(MetaTypeConfig<T>.GetMetaType().Type, ProtoBuf.Meta.ProtoSyntax.Proto3);

    [Test]
    public void CheckServiceProto_BaseType()
    {
        var schema = GetServiceProto<Foo>();
        Assert.AreEqual(@"syntax = ""proto3"";
package ServiceStack.Extensions.Tests;

message Bar {
   string Y = 2;
}
message Foo {
   string X = 1;
   oneof subtype {
      Bar Bar = 210304982;
   }
}
", schema);
    }
 
    [Test]
    public void CheckServiceProto_DerivedType()
    {
        var schema = GetServiceProto<Bar>();
        Assert.AreEqual(@"syntax = ""proto3"";
package ServiceStack.Extensions.Tests;

message Bar {
   string Y = 2;
}
message Foo {
   string X = 1;
   oneof subtype {
      Bar Bar = 210304982;
   }
}
", schema);
    }
 
    [Test]
    public void CheckServiceProto_QueryDb_ShouldBeOffset()
    {
        var schema = GetServiceProto<QueryFoos>();
        Assert.AreEqual(@"syntax = ""proto3"";
package ServiceStack.Extensions.Tests;

message QueryFoos {
   int32 Skip = 1;
   int32 Take = 2;
   string OrderBy = 3;
   string OrderByDesc = 4;
   string Include = 5;
   string Fields = 6;
   map<string,string> Meta = 7;
   string X = 201;
}
", schema);
    }

    [Test]
    public void CheckServiceProto_CustomRequestDto_ShouldBeOffset()
    {
        var schema = GetServiceProto<CustomRequestDto>();
        Assert.AreEqual(@"syntax = ""proto3"";
package ServiceStack.Extensions.Tests;

message CustomRequestDto {
   int32 PageName = 42;
   string Name = 105;
}
", schema);
    }

    [DataContract]
    public class Foo
    {
        [DataMember(Order = 1)]
        public string X { get; set; }
    }

    [DataContract]
    public class Bar : Foo
    {
        [DataMember(Order = 2)]
        public string Y { get; set; }
    }

    [Route("/query/foos")]
    [DataContract]
    public class QueryFoos : QueryDb<Foo>
    {
        [DataMember(Order = 1)]
        public string X { get; set; }
    }

    [DataContract]
    public abstract class CustomRequestDtoBase : IReturnVoid
    {
        [DataMember(Order = 42, Name = "PageName")]
        public int Page { get; set; }
    }

    [DataContract]
    public class CustomRequestDto : CustomRequestDtoBase
    {
        [DataMember(Order = 5)]
        public string Name { get; set; }
    }
}