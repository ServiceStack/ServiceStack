using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Text;
using System.Threading.Tasks;
using AutorestClient;
using NUnit.Framework;
using ServiceStack.OpenApi.Tests.Host;
using ServiceStack.OpenApi.Tests.Services;

namespace ServiceStack.OpenApi.Tests
{
    [TestFixture]
    class AnnotatedPropertiesTests : GeneratedClientTestBase
    {
        [Test]
        public void Can_get_annotated_service_with_array_enum()
        {
            var client = new ServiceStackAutorestClient(new Uri(Config.AbsoluteBaseUri));

            var dto = new GetMovie(){Id = 1, Includes = new[] {"Genres", "Releases" } };

            var result = client.GetMovieId.Post(dto.Id, dto.Includes);

            Assert.That(result.Includes, Is.EquivalentTo(dto.Includes));
        }

    }
}
