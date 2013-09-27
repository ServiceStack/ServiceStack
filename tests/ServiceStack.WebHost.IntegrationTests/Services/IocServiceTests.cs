using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Server;
using ServiceStack.Clients;
using ServiceStack.WebHost.IntegrationTests.Tests;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
    public class FunqRequestScope
    {
        public static int Count = 0;
        public FunqRequestScope() { Count++; }
    }

    public class FunqSingletonScope
    {
        public static int Count = 0;
        public FunqSingletonScope() { Count++; }
    }

    public class FunqNoneScope
    {
        public static int Count = 0;
        public FunqNoneScope() { Count++; }
    }
    
    public class IocScope { }

    public class IocScopeResponse : IHasResponseStatus
    {
        public IocScopeResponse()
        {
            this.ResponseStatus = new ResponseStatus();
            this.Results = new Dictionary<string, int>();
        }

        public Dictionary<string, int> Results { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }

    [IocRequestFilter]
    public class IocScopeService : IService, IDisposable
    {
        public FunqRequestScope FunqRequestScope { get; set; }
        public FunqSingletonScope FunqSingletonScope { get; set; }
        public FunqNoneScope FunqNoneScope { get; set; }

        public object Any(IocScope request)
        {
            var response = new IocScopeResponse {
                Results = {
                    { typeof(FunqSingletonScope).Name, FunqSingletonScope.Count },
                    { typeof(FunqRequestScope).Name, FunqRequestScope.Count },
                    { typeof(FunqNoneScope).Name, FunqNoneScope.Count },
                },
            };

            return response;
        }

        public static int DisposedCount = 0;
        public static bool ThrowErrors = false;

        public void Dispose()
        {
            DisposedCount++;
        }
    }
    
    public class IocRequestFilterAttribute : Attribute, IHasRequestFilter
    {
        public FunqSingletonScope FunqSingletonScope { get; set; }
        public FunqRequestScope FunqRequestScope { get; set; }
        public FunqNoneScope FunqNoneScope { get; set; }

        public int Priority { get; set; }

        public void RequestFilter(IHttpRequest req, IHttpResponse res, object requestDto)
        {
        }

        public IHasRequestFilter Copy()
        {
            return (IHasRequestFilter)this.MemberwiseClone();
        }
    }

    [TestFixture]
    public class IocServiceTests
    {
        [Test]
        public void Does_create_correct_instances_per_scope()
        {

            var restClient = new JsonServiceClient(Config.ServiceStackBaseUri);
            var response1 = restClient.Get<IocScopeResponse>("iocscope");
            var response2 = restClient.Get<IocScopeResponse>("iocscope");

            Assert.That(response2.Results[typeof(FunqSingletonScope).Name], Is.EqualTo(1));

            var requestScopeCounter = response2.Results[typeof(FunqRequestScope).Name] - response1.Results[typeof(FunqRequestScope).Name];
            Assert.That(requestScopeCounter, Is.EqualTo(1));
            var noneScopeCounter = response2.Results[typeof(FunqNoneScope).Name] - response1.Results[typeof(FunqNoneScope).Name];
            Assert.That(noneScopeCounter, Is.EqualTo(2));
        }
    }
}