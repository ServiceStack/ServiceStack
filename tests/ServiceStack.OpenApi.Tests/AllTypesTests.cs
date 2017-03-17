using AutorestClient;
using AutorestClient.Models;
using NUnit.Framework;
using ServiceStack.OpenApi.Tests.Host;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ServiceStack.OpenApi.Tests
{
    [TestFixture]
    class AllTypesTests : GeneratedClientTestBase
    {
        [Test]
        public void Sleep()
        {
            Thread.Sleep(20000);
        }


        [Test]
        public void Can_post_all_types()
        {
            var client = new ServiceStackAutorestClient(new Uri(Config.AbsoluteBaseUri));

            HelloAllTypes helloAllTypes = new HelloAllTypes()
            {
                Name = "Hello",
                AllTypes = MakeDtoHelper.GetAllTypes(),
                AllCollectionTypes = MakeDtoHelper.GetAllCollectionTypes()
            };
            
            var result = client.HelloAllTypes.Post("123", null, null, helloAllTypes);
        }

        [Test]
        public void Can_post_all_types_with_result()
        {
            var client = new ServiceStackAutorestClient(new Uri(Config.AbsoluteBaseUri));

            var dto = new HelloAllTypesWithResult()
            {
                Name = "Hello",
                AllTypes = MakeDtoHelper.GetAllTypes(),
                AllCollectionTypes = MakeDtoHelper.GetAllCollectionTypes()
            };

            var result = client.HelloAllTypesWithResult.Post(body: dto);

            Assert.That(result.Result, Is.EqualTo(dto.Name));
            MakeDtoHelper.AssertAllTypes(result.AllTypes, dto.AllTypes);
            MakeDtoHelper.AssertAllCollectionTypes(result.AllCollectionTypes, dto.AllCollectionTypes);
            
        }
    }
}
