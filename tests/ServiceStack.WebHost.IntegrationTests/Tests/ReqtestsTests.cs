// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using NUnit.Framework;
using ServiceStack.Host.Handlers;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    [Route("/reqtests")]
    [Route("/reqtests/{PathInfoParam}")]
    public class Reqtests : IReturn<Reqtests>
    {
        public string PathInfo { get; set; }
        public string QueryString { get; set; }
    }

    public class ReqtestsService : Service
    {
        public Reqtests Any(Reqtests request)
        {
            return request;
        }

        public RequestInfoResponse Any(RequestInfo request)
        {
            var requestInfo = RequestInfoHandler.GetRequestInfo(base.Request);
            return requestInfo;
        }
    }

    [TestFixture]
    public class ReqtestsTests
    {
        [Test]
        public void AspNet_doesnt_transparently_decode_QueryStrings()
        {
            var client = new JsonServiceClient(Config.ServiceStackBaseUri);
            var request = new Reqtests { QueryString = "pathseperator%2Ftest" };
            var response = client.Get(request);
            Assert.That(response.QueryString, Is.EqualTo(request.QueryString));
        }
    }

}