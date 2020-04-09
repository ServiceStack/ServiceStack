using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Funq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Validation;

namespace ServiceStack.Extensions.Tests.Issues
{
    [Route("/endswith/{Suffix}", Summary = "suffix")]
    [DataContract]
    public class EndsWithSuffixRequest : IReturn<EndsWithSuffixResponse>
    {
        [DataMember(Order = 1), ApiMember(Name = "Suffix", Description = "Suffix", DataType = "string", IsRequired = true)]
        public string Suffix { get; set; }
    }

    [DataContract]
    public class EndsWithSuffixResponse
    {
        [DataMember(Order = 1)]
        public SearchResult Result { get; set; }

        [DataMember(Order = 2)]
        public int Count { get; set; }

        [DataMember(Order = 3)]
        public List<string> Words { get; set; }
    }

    [DataContract]
    public class SearchResult
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public string Suffix { get; set; }
    }

    public class GrpcIssueServices : Service
    {
        public object Any(EndsWithSuffixRequest request) => new EndsWithSuffixResponse {
            Result = new SearchResult { Suffix = request.Suffix }
        };
    }

    public class GrpcServiceIssues
    {
        public class AppHost : AppSelfHostBase
        {
            public AppHost() 
                : base(nameof(GrpcServiceIssues), typeof(MyServices).Assembly) { }

            public override void Configure(Container container)
            {
                Plugins.Add(new ValidationFeature());
                Plugins.Add(new GrpcFeature(App));
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
            }
        }

        private readonly ServiceStackHost appHost;
        public GrpcServiceIssues()
        {
            appHost = new AppHost()
                .Init()
                .Start(TestsConfig.ListeningOn);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

        [Test]
        public async Task Can_call_EndsWithSuffixRequest()
        {
            var client = TestsConfig.GetInsecureClient();
            var request = new EndsWithSuffixRequest { Suffix = "TheSuffix" };
            var response = await client.GetAsync(request);
            Assert.That(response.Result.Suffix, Is.EqualTo(request.Suffix));
        }

    }
}