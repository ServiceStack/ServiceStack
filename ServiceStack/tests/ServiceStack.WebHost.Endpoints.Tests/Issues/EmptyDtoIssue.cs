using System;
using System.Collections.Generic;
using System.Linq;
using Funq;
using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests.Issues;

public class PostEmptyArray : IReturnVoid
{
    public int[] Ids { get; set; }
}

public class GetEmptyArray : IReturn<List<int>>
{
    public int[] Ids { get; set; }
}

public class TestService : Service
{
    public void Post(PostEmptyArray e)
    {
        if (e.Ids == null)
            throw new Exception();
    }

    public List<int> Get(GetEmptyArray e)
    {
        if (e.Ids == null)
            throw new Exception();

        return e.Ids.ToList();
    }
}

public class EmptyDtoIssue
{
    public class EmptyArrayDtoTest
    {
        public class AppHost() : AppHostHttpListenerBase(nameof(EmptyArrayDtoTest), typeof(GetEmptyArray).Assembly)
        {
            public override void Configure(Container container) { }
        }

        ServiceStackHost appHost;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Can_POST_empty_array()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);
            client.Post(new PostEmptyArray { Ids = [] });
        }

        [Test]
        public void Can_GET_empty_array()
        {

            var client = new JsonServiceClient(Config.AbsoluteBaseUri);
            client.Get(new GetEmptyArray { Ids = [] });
        }
    }
}