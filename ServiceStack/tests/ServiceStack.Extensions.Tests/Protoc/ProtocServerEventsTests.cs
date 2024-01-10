using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Funq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ProtoBuf.Grpc.Client;
using ServiceStack.Text;

namespace ServiceStack.Extensions.Tests.Protoc;

public class ProtocServerEventsTests
{
    public class AppHost() : AppSelfHostBase(nameof(GrpcServerEventsTests), typeof(MyServices).Assembly)
    {
        public override void Configure(Container container)
        {
            Plugins.Add(new GrpcFeature(App));
            Plugins.Add(new ServerEventsFeature());
        }

        public override void ConfigureKestrel(KestrelServerOptions options)
        {
            options.ListenLocalhost(TestsConfig.Port, listenOptions => {
                listenOptions.Protocols = HttpProtocols.Http2;
            });
        }

        public override void Configure(IServiceCollection services) => services.AddServiceStackGrpc();

        public override void Configure(IApplicationBuilder app) => app.UseRouting();
    }

    private readonly ServiceStackHost appHost;
    public ProtocServerEventsTests()
    {
        GrpcClientFactory.AllowUnencryptedHttp2 = true;
        appHost = new AppHost()
            .Init()
            .Start(TestsConfig.ListeningOn);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => appHost.Dispose();

    private static GrpcServices.GrpcServicesClient GetClient(Action<GrpcClientConfig> init = null) =>
        ProtocTests.GetClient(init);

    [Test]
    public async Task Can_subscribe_to_ServerEvents()
    {
        var client = GetClient();

        void AssertMessage(StreamServerEventsResponse msg)
        {
            Assert.That(msg.EventId, Is.GreaterThan(0));
            Assert.That(msg.Channels, Is.EqualTo(new[] { "home" }));
            Assert.That(msg.Json, Is.Not.Null);
            Assert.That(msg.Op, Is.EqualTo("cmd"));
            Assert.That(msg.UserId, Is.EqualTo("-1"));
            Assert.That(msg.DisplayName, Is.Not.Null);
            Assert.That(msg.ProfileUrl, Is.Not.Null);
            Assert.That(msg.IsAuthenticated, Is.False);
        }

        var i = 0;
        var stream = client.ServerStreamServerEvents(new StreamServerEvents { Channels = {"home"} }).ResponseStream;
        while (await stream.MoveNext(default))
        {
            var msg = stream.Current;
            if (i == 0)
            {
                Assert.That(msg.Selector, Is.EqualTo("cmd.onConnect"));
                Assert.That(msg.Id, Is.Not.Null);
                Assert.That(msg.UnRegisterUrl, Is.Not.Null);
                Assert.That(msg.UpdateSubscriberUrl, Is.Not.Null);
                Assert.That(msg.HeartbeatUrl, Is.Not.Null);
                Assert.That(msg.HeartbeatIntervalMs, Is.GreaterThan(0));
                Assert.That(msg.IdleTimeoutMs, Is.GreaterThan(0));
                AssertMessage(msg);
            }
            else if (i == 1)
            {
                Assert.That(msg.Selector, Is.EqualTo("cmd.onJoin"));
                AssertMessage(msg);
            }
                
            $"\n\n{i}".Print();
            msg.PrintDump();

            if (++i == 2)
                break;
        }
        Assert.That(i, Is.EqualTo(2));
    }

    [Test]
    public async Task Does_receive_all_messages()
    {
        var client1 = GetClient();
        var client2 = GetClient();

#pragma warning disable CS4014
        Task.Factory.StartNew(async () => {
#pragma warning restore CS4014
            await Task.Delay(500);
            await client2.CallPostChatToChannelAsync(new PostChatToChannel {
                Channel = "send",
                From = nameof(client2),
                Message = "Hello from client2",
                Selector = "cmd.chat",
            });
        });

        var responses = new List<StreamServerEventsResponse>();
        var stream = client1.ServerStreamServerEvents(new StreamServerEvents { Channels = {"send"} }).ResponseStream;
        while (await stream.MoveNext(default))
        {
            var msg = stream.Current;
            responses.Add(msg);
                
            if (msg.Selector == "cmd.chat")
                break;
        }
            
        Assert.That(responses[0].Selector, Is.EqualTo("cmd.onConnect"));
        Assert.That(responses[1].Selector, Is.EqualTo("cmd.onJoin"));
        Assert.That(responses[2].Selector, Is.EqualTo("cmd.chat"));
        var obj = (Dictionary<string, object>) JSON.parse(responses[2].Json);
        Assert.That(obj["message"], Is.EqualTo("Hello from client2"));
    }
}