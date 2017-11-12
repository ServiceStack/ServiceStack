using AutorestClient;
using AutorestClient.Models;
using NUnit.Framework;
using ServiceStack.OpenApi.Tests.Host;
using System;
using System.Threading;

namespace ServiceStack.OpenApi.Tests
{
    [TestFixture]
    class AllTypesTests : GeneratedClientTestBase
    {
        [Ignore("Debug Test"), Test]
        public void Sleep()
        {
            Thread.Sleep(1000000);
        }


        [Test]
        public void Can_post_all_types()
        {
            var dto = new HelloAllTypes
            {
                Name = "Hello",
                AllTypes = DtoHelper.GetAllTypes(),
                AllCollectionTypes = DtoHelper.GetAllCollectionTypes()
            };

            using (var client = new ServiceStackAutorestClient(new Uri(Config.AbsoluteBaseUri)))
            {
                var result = client.HelloAllTypes.Post("123", null, null, dto);
            }
        }

        [Test]
        public void Can_get_all_types()
        {
            var dto = new HelloAllTypes
            {
                Name = "Hello",
                AllTypes = DtoHelper.GetAllTypes(),
                AllCollectionTypes = DtoHelper.GetAllCollectionTypes()
            };

            using (var client = new ServiceStackAutorestClient(new Uri(Config.AbsoluteBaseUri)))
            {
                var result = client.HelloAllTypes.Get("123", dto.AllTypes.ToJsv(), null);
            }
        }

        [Test]
        public void Can_get_all_types_with_result()
        {
            var dto = new HelloAllTypesWithResult
            {
                Name = "Hello",
                AllTypes = DtoHelper.GetAllTypes(),
                AllCollectionTypes = DtoHelper.GetAllCollectionTypes()
            };

            using (var client = new ServiceStackAutorestClient(new Uri(Config.AbsoluteBaseUri)))
            {
                var at = dto.AllTypes.ToJsv();

                var result = client.HelloAllTypesWithResult.Get(dto.Name, dto.AllTypes.ToJsv(), dto.AllCollectionTypes.ToJsv());

                Assert.That(result.Result, Is.EqualTo(dto.Name));
                DtoHelper.AssertAllTypes(result.AllTypes, dto.AllTypes);
                DtoHelper.AssertAllCollectionTypes(result.AllCollectionTypes, dto.AllCollectionTypes);
            }
        }

        [Test]
        public void Can_post_all_types_with_result()
        {
            var dto = new HelloAllTypesWithResult
            {
                Name = "Hello",
                AllTypes = DtoHelper.GetAllTypes(),
                AllCollectionTypes = DtoHelper.GetAllCollectionTypes()
            };

            using (var client = new ServiceStackAutorestClient(new Uri(Config.AbsoluteBaseUri)))
            {
                var result = client.HelloAllTypesWithResult.Post(body: dto);

                Assert.That(result.Result, Is.EqualTo(dto.Name));
                DtoHelper.AssertAllTypes(result.AllTypes, dto.AllTypes);
                DtoHelper.AssertAllCollectionTypes(result.AllCollectionTypes, dto.AllCollectionTypes);
            }
        }
    }
}
