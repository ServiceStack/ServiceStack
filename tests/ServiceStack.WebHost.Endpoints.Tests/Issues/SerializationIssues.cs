using System.Collections.Generic;
using System.Reflection;
using Funq;
using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests.Issues
{
    [Route("/confirmations", "PUT")]
    public class PutConfirmed : IReturn<PutConfirmedResponse>
    {
        public List<Confirmation> Confirmations { get; set; }
    }

    public class Confirmation
    {
        public Confirmation()
        {
            ChangeId = 0;
            Confirmed = false;
        }
        public int ChangeId { get; set; }
        public bool Confirmed { get; set; }
    }

    public class PutConfirmedResponse
    {
        public bool IsSucceed { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class SerializationIssuesService : Service
    {
        public object Put(PutConfirmed request) => new PutConfirmedResponse
        {
            IsSucceed = request.Confirmations[0].Confirmed && request.Confirmations[0].ChangeId == 126552616
        };
    }
    
    public class SerializationIssues
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost() : base(nameof(SerializationIssues), typeof(SerializationIssuesService).Assembly)
            {
            }

            public override void Configure(Container container)
            {
            }
        }

        private readonly ServiceStackHost appHost;

        public SerializationIssues()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

        [Test]
        public void Does_serialize_request()
        {
            var client = new JsonHttpClient(Config.ListeningOn);

            var response = client.Put(new PutConfirmed
            {
                Confirmations = new List<Confirmation>
                {
                    new Confirmation
                    {
                        ChangeId = 126552616,
                        Confirmed = true,
                    }
                }
            });
            
            Assert.That(response.IsSucceed);
        }
    }
}