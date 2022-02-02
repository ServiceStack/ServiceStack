using System;
using System.Net;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class ServerEventsErrorHandlingTests
    {
        private readonly ServiceStackHost appHost;

        public ServerEventsErrorHandlingTests()
        {
            appHost = new ServerEventsAppHost()
                .Init()
                .Start(Config.AbsoluteBaseUri);;

            appHost.GetPlugin<ServerEventsFeature>().OnInit = req =>
                throw new Exception("Always throws");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

        [Ignore("Hangs"), Test]
        public async Task Does_dispose_SSE_Connection_when_Exception_in_OnInit_handler()
        {
            ServerEventsClient client = null;
            using (client = new ServerEventsClient(Config.AbsoluteBaseUri) {
                // OnException = e => client.Dispose() 
            })
            {
                try
                {
                    await client.Connect();
                }
                catch (WebException e)
                {
                    Assert.That(e.GetStatus(), Is.EqualTo(HttpStatusCode.InternalServerError));
                }
                catch (Exception e)
                {
                    Assert.Fail($"Unexpected Exception: {e.GetType().Name}: {e.Message}");
                }
            }
        }
    }
}