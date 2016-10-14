using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [Ignore, TestFixture] //requires SSE AppHost on 
    public class ServerEventIntegrationTests
    {
        [Test]
        public async Task Does_reconnect_when_remote_AppServer_restarts()
        {
            var client = new ServerEventsClient("http://localhost:11001", "home")
            {
                OnConnect = ctx => "OnConnect: {0}".Print(ctx.Channel),
                OnCommand = msg => "OnCommand: {0}".Print(msg.Data),
                OnException = ex => "OnException: {0}".Print(ex.Message),
                OnMessage = msg => "OnMessage: {0}".Print(msg.Data),
                OnHeartbeat = () => "OnHeartbeat".Print()
            };

            client.Handlers["chat"] = (source, msg) =>
            {
                "Received Chat: {0}".Print(msg.Data);
            };

            await client.Connect();

            await Task.Delay(TimeSpan.FromMinutes(10));
        }
    }
}