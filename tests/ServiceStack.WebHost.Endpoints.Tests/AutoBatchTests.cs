using System.Collections.Generic;
using System.Linq;
using System.Net;
using Funq;
using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class AutoBatchAppHost : AppSelfHostBase
    {
        public AutoBatchAppHost()
            : base(typeof(AutoBatchTests).Name, typeof(AutoBatchIndexServices).Assembly)
        {
        }

        public override void Configure(Container container)
        {
            GlobalRequestFilters.Add((req, res, dto) =>
            {
                var autoBatchIndex = req.GetItem(Keywords.AutoBatchIndex)?.ToString();
                if (autoBatchIndex != null)
                {
                    res.RemoveHeader("GlobalRequestFilterAutoBatchIndex");
                    res.AddHeader("GlobalRequestFilterAutoBatchIndex", autoBatchIndex);
                }
            });

            GlobalResponseFilters.Add((req, res, dto) =>
            {
                var autoBatchIndex = req.GetItem(Keywords.AutoBatchIndex)?.ToString();

                if (autoBatchIndex != null)
                {
                    if (dto is IMeta response)
                    {
                        response.Meta = new Dictionary<string, string>
                        {
                            ["GlobalResponseFilterAutoBatchIndex"] = autoBatchIndex
                        };
                    }
                }
            });
        }
    }

    public class GetAutoBatchIndex : IReturn<GetAutoBatchIndexResponse>
    {
    }

    public class GetCustomAutoBatchIndex : IReturn<GetAutoBatchIndexResponse>
    {
    }

    public class GetAutoBatchIndexResponse : IMeta
    {
        public string Index { get; set; }
        public Dictionary<string, string> Meta { get; set; }
    }

    public class AutoBatchIndexServices : Service
    {
        public object Any(GetAutoBatchIndex request)
        {
            var autoBatchIndex = Request.GetItem(Keywords.AutoBatchIndex)?.ToString();
            return new GetAutoBatchIndexResponse
            {
                Index = autoBatchIndex
            };
        }

        public GetAutoBatchIndexResponse Any(GetCustomAutoBatchIndex request)
        {
            var autoBatchIndex = Request.GetItem(Keywords.AutoBatchIndex)?.ToString();
            return new GetAutoBatchIndexResponse
            {
                Index = autoBatchIndex
            };
        }

        public object Any(GetCustomAutoBatchIndex[] requests)
        {
            var responses = new List<GetAutoBatchIndexResponse>();

            Request.EachRequest<GetCustomAutoBatchIndex>(dto =>
            {
                responses.Add(Any(dto));
            });

            return responses;
        }
    }

    [TestFixture]
    public class AutoBatchTests
    {
        private ServiceStackHost appHost;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new AutoBatchAppHost()
                .Init()
                .Start(Config.AbsoluteBaseUri);
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Single_requests_dont_set_AutoBatchIndex()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);

            WebHeaderCollection responseHeaders = null;

            client.ResponseFilter = resp => { responseHeaders = resp.Headers; };

            var response = client.Send(new GetAutoBatchIndex());

            Assert.Null(response.Index);
            Assert.Null(response.Meta);
            Assert.IsFalse(responseHeaders.AllKeys.Contains("GlobalRequestFilterAutoBatchIndex"));
        }

        [Test]
        public void Multi_requests_set_AutoBatchIndex()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);

            WebHeaderCollection responseHeaders = null;

            client.ResponseFilter = response => { responseHeaders = response.Headers; };

            var responses = client.SendAll(new[]
            {
                new GetAutoBatchIndex(),
                new GetAutoBatchIndex()
            });

            Assert.AreEqual("0", responses[0].Index);
            Assert.AreEqual("0", responses[0].Meta["GlobalResponseFilterAutoBatchIndex"]);

            Assert.AreEqual("1", responses[1].Index);
            Assert.AreEqual("1", responses[1].Meta["GlobalResponseFilterAutoBatchIndex"]);

            Assert.AreEqual("1", responseHeaders["GlobalRequestFilterAutoBatchIndex"]);
        }

        [Test]
        public void Custom_multi_requests_set_AutoBatchIndex()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);

            WebHeaderCollection responseHeaders = null;

            client.ResponseFilter = response => { responseHeaders = response.Headers; };

            var responses = client.SendAll(new[]
            {
                new GetCustomAutoBatchIndex(),
                new GetCustomAutoBatchIndex()
            });

            Assert.AreEqual("0", responses[0].Index);
            Assert.AreEqual("0", responses[0].Meta["GlobalResponseFilterAutoBatchIndex"]);

            Assert.AreEqual("1", responses[1].Index);
            Assert.AreEqual("1", responses[1].Meta["GlobalResponseFilterAutoBatchIndex"]);

            Assert.AreEqual("1", responseHeaders["GlobalRequestFilterAutoBatchIndex"]);
        }
    }
}
