using Funq;
using NUnit.Framework;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests.Issues
{
    public class ResponseFilterHeadersIssue
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost() : base(nameof(ResponseFilterHeadersIssue), typeof(ResponseFilterHeadersIssue).Assembly) { }
            public override void Configure(Container container)
            {
//                Config.GlobalResponseHeaders.Remove(HttpHeaders.Vary);
                
                SetConfig(new HostConfig {
                    GlobalResponseHeaders = {
                        [HttpHeaders.Vary] = "accept,origin,authorization"
                    }
                });
            }
        }

        private readonly ServiceStackHost appHost;
        public ResponseFilterHeadersIssue()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();
        
        [Test]
        public void ResponseFilterIShouldHaveVaryFilterTest()
        {
            using (var client = new JsonHttpClient(Config.ListeningOn))
            {
                client.ResponseFilter = message => 
                {
                    var headers = message.Headers.Vary.Join(",");
                    Assert.That(headers, Is.EqualTo("accept,origin,authorization"));
                    Assert.That(message.Headers.CacheControl.ToString(), Is.EqualTo("no-cache"));
                };

                var response = client.Get(new ResponseFilterWithVaryRequest());
                Assert.That(response, Is.EqualTo("Should have vary headers."));
            }
        }
    }
    
    [NoCacheResponseFilter]
    public class ResponseFilterService : Service
    {
        public object Get(ResponseFilterWithVaryRequest withVaryRequest)
        {
            return "Should have vary headers.";
        }
    }

    public class ResponseFilterWithVaryRequest: IGet, IReturn<string>
    {
    }

    public class NoCacheResponseFilterAttribute : ResponseFilterAttribute
    {
        public override void Execute(IRequest req, IResponse res, object responseDto)
        {
            res.AddHeader("Cache-Control", "no-cache");
        }
    }

}