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

    public class NoVerbRequest : IReturn<string> { }
	
    public class GetRequest : IReturn<string>, IGet { }

    public class PostRequest : IReturn<string>, IPost { }

    public class PutRequest : IReturn<string>, IPut { }

    public class DeleteRequest : IReturn<string>, IDelete { }

    public class PatchRequest : IReturn<string>, IPatch { }

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

        public object Any(NoVerbRequest request) => "NoVerb";
        
        public object Any(NoVerbRequest[] request) => "NoVerbAutoBatched";
				
        public object Get(GetRequest request) => HttpMethods.Get;
		
        public object Get(GetRequest[] request) => $"{HttpMethods.Get}AutoBatched";
		
        public object Post(PostRequest request) => HttpMethods.Post;
		
        public object Post(PostRequest[] request) => $"{HttpMethods.Post}AutoBatched";
		
        public object Put(PutRequest request) => HttpMethods.Put;
		
        public object Put(PutRequest[] request) => $"{HttpMethods.Put}AutoBatched";
		
        public object Delete(DeleteRequest request) => HttpMethods.Delete;
		
        public object Delete(DeleteRequest[] request) => $"{HttpMethods.Delete}AutoBatched";
		
        public object Patch(PatchRequest request) => HttpMethods.Patch;

        public object Patch(PatchRequest[] request) => $"{HttpMethods.Patch}AutoBatched";
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

        [Test]
        public void Send_request_for_IGet_calls_Get_Method()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);

            var response = client.Send(new GetRequest());

            Assert.That(response, Is.EqualTo(HttpMethods.Get));
        }

        [Test]
        public void SendAll_request_for_IGet_calls_Get_Method()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);

            var response = client.SendAll(new[] { new GetRequest() });

            Assert.That(response, Is.All.EqualTo($"{HttpMethods.Get}AutoBatched"));
        }

        [Test]
        public void Send_request_for_IPost_calls_Post_Method()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);

            var response = client.Send(new PostRequest());

            Assert.That(response, Is.EqualTo(HttpMethods.Post));
        }

        [Test]
        public void SendAll_request_for_IPost_calls_Post_Method()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);

            var response = client.SendAll(new[] { new PostRequest() });

            Assert.That(response, Is.All.EqualTo($"{HttpMethods.Post}AutoBatched"));
        }

        [Test]
        public void Send_request_for_IPut_calls_Put_Method()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);

            var response = client.Send(new PutRequest());

            Assert.That(response, Is.EqualTo(HttpMethods.Put));
        }

        [Test]
        public void SendAll_request_for_IPut_calls_Put_Method()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);

            var response = client.SendAll(new[] { new PutRequest() });

            Assert.That(response, Is.All.EqualTo($"{HttpMethods.Put}AutoBatched"));
        }

        [Test]
        public void Send_request_for_IDelete_calls_Delete_Method()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);

            var response = client.Send(new DeleteRequest());

            Assert.That(response, Is.EqualTo(HttpMethods.Delete));
        }

        [Test]
        public void SendAll_request_for_IDelete_calls_Delete_Method()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);

            var response = client.SendAll(new[] { new DeleteRequest() });

            Assert.That(response, Is.All.EqualTo($"{HttpMethods.Delete}AutoBatched"));
        }

        [Test]
        public void Send_request_for_IPatch_calls_Patch_Method()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);

            var response = client.Send(new PatchRequest());

            Assert.That(response, Is.EqualTo(HttpMethods.Patch));
        }

        [Test]
        public void SendAll_request_for_IPatch_calls_Patch_Method()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);

            var response = client.SendAll(new[] { new PatchRequest() });

            Assert.That(response, Is.All.EqualTo($"{HttpMethods.Patch}AutoBatched"));
        }

        [Test]
        public void Send_request_for_request_with_no_marker_calls_Any_Method()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);

            var response = client.Send(new NoVerbRequest());

            Assert.That(response, Is.EqualTo("NoVerb"));
        }

        [Test]
        public void SendAll_request_for_request_with_no_marker_calls_Any_Method()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);

            var response = client.SendAll(new[] { new NoVerbRequest() });

            Assert.That(response, Is.All.EqualTo("NoVerbAutoBatched"));
        }
    }
}
