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
            Thread.Sleep(10000);
        }


        [Test]
        public void Can_post_all_types()
        {
            var client = new ServiceStackAutorestClient(new Uri(Config.AbsoluteBaseUri));

            HelloAllTypes helloAllTypes = new HelloAllTypes()
            {
                Name = "Hello",
                AllTypes = DtoHelper.GetAllTypes(),
                AllCollectionTypes = DtoHelper.GetAllCollectionTypes()
            };
            
            var result = client.HelloAllTypes.Post("123", null, null, helloAllTypes);
        }

        [Test]
        public void Can_get_all_types()
        {
            var client = new ServiceStackAutorestClient(new Uri(Config.AbsoluteBaseUri));

            HelloAllTypes helloAllTypes = new HelloAllTypes()
            {
                Name = "Hello",
                AllTypes = DtoHelper.GetAllTypes(),
                AllCollectionTypes = DtoHelper.GetAllCollectionTypes()
            };

            var result = client.HelloAllTypes.Get("123", helloAllTypes.AllTypes.ToJsv(), null);
        }


        [Test]
        public void Can_post_all_types_with_result()
        {
            var client = new ServiceStackAutorestClient(new Uri(Config.AbsoluteBaseUri));

            var dto = new HelloAllTypesWithResult()
            {
                Name = "Hello",
                AllTypes = DtoHelper.GetAllTypes(),
                AllCollectionTypes = DtoHelper.GetAllCollectionTypes()
            };

            var result = client.HelloAllTypesWithResult.Post(body: dto);

            Assert.That(result.Result, Is.EqualTo(dto.Name));
            DtoHelper.AssertAllTypes(result.AllTypes, dto.AllTypes);
            DtoHelper.AssertAllCollectionTypes(result.AllCollectionTypes, dto.AllCollectionTypes);
            
        }
    }
}
