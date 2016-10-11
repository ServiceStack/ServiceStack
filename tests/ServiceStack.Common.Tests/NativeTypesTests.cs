#if !NETCORE_SUPPORT
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests
{
    using System.Collections.Generic;
    using NativeTypes;
    using Testing;

    [TestFixture]
    public class NativeTypesTests
    {
        ServiceStackHost appHost;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            appHost =
                new BasicAppHost(typeof(Dto).Assembly, typeof(TypesCSharp).Assembly)
                {
                    TestMode = true,
                    Config = new HostConfig()
                }.Init();
        }

        [TestFixtureTearDown]
        public void OnTestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void GetIncludeList_Returns_IncludeList_If_NoIncludeTypes_HaveWildcard()
        {
            var includeTypes = new List<string> { "Dto1", "DTO2" };
            var config = new MetadataTypesConfig
            {
                IncludeTypes = includeTypes
            };

            var result = MetadataExtensions.GetIncludeList(new MetadataTypes(), config);
            Assert.AreEqual(includeTypes, result);
        }

        [Test]
        public void IncludeTypes_DoesNotReturnReferenceTypes_If_IncludeTypes_NoWildcard_Csharp()
        {
            var result = appHost.ExecuteService(new TypesCSharp
            {
                IncludeTypes = new List<string> { "Dto" }
            });

            var stringResult = result.ToString();

            StringAssert.DoesNotContain("class DtoResponse", stringResult);
            StringAssert.DoesNotContain("EmbeddedRequest", stringResult);
            StringAssert.DoesNotContain("class EmbeddedResponse", stringResult);
        }

        [Test]
        public void IncludeTypes_ReturnsReferenceTypes_If_IncludeTypes_HasWildcard_Csharp()
        {
            var result = appHost.ExecuteService(new TypesCSharp
            {
                IncludeTypes = new List<string> { "Dto.*" }
            });

            var stringResult = result.ToString();

            StringAssert.Contains("class DtoResponse", stringResult);
            StringAssert.Contains("class EmbeddedRequest", stringResult);
            StringAssert.Contains("class EmbeddedResponse", stringResult);
        }

        [Test]
        public void IncludeTypes_DoesNotReturnReferenceTypes_If_IncludeTypes_NoWildcard_Fsharp()
        {
            var result = appHost.ExecuteService(new TypesFSharp
            {
                IncludeTypes = new List<string> { "Dto" }
            });

            var stringResult = result.ToString();

            StringAssert.DoesNotContain("type DtoResponse", stringResult);
            StringAssert.DoesNotContain("EmbeddedRequest", stringResult);
            StringAssert.DoesNotContain("type EmbeddedResponse", stringResult);
        }

        [Test]
        public void IncludeTypes_ReturnsReferenceTypes_If_IncludeTypes_HasWildcard_Fsharp()
        {
            var result = appHost.ExecuteService(new TypesFSharp
            {
                IncludeTypes = new List<string> { "Dto.*" }
            });

            var stringResult = result.ToString();

            StringAssert.Contains("type DtoResponse", stringResult);
            StringAssert.Contains("type EmbeddedRequest", stringResult);
            StringAssert.Contains("type EmbeddedResponse", stringResult);
        }

        [Test]
        public void IncludeTypes_DoesNotReturnReferenceTypes_If_IncludeTypes_NoWildcard_VbNet()
        {
            var result = appHost.ExecuteService(new TypesVbNet()
            {
                IncludeTypes = new List<string> { "Dto" }
            });

            var stringResult = result.ToString();

            StringAssert.DoesNotContain("Class DtoResponse", stringResult);
            StringAssert.DoesNotContain("EmbeddedRequest", stringResult);
            StringAssert.DoesNotContain("Class EmbeddedResponse", stringResult);
        }

        [Test]
        public void IncludeTypes_ReturnsReferenceTypes_If_IncludeTypes_HasWildcard_VbNet()
        {
            var result = appHost.ExecuteService(new TypesVbNet
            {
                IncludeTypes = new List<string> { "Dto.*" }
            });

            var stringResult = result.ToString();

            StringAssert.Contains("Class DtoResponse", stringResult);
            StringAssert.Contains("Class EmbeddedRequest", stringResult);
            StringAssert.Contains("Class EmbeddedResponse", stringResult);
        }

        [Test]
        public void IncludeTypes_DoesNotReturnReferenceTypes_If_IncludeTypes_NoWildcard_Kotlin()
        {
            var result = appHost.ExecuteService(new TypesKotlin
            {
                IncludeTypes = new List<string> { "Dto" }
            });

            var stringResult = result.ToString();

            StringAssert.DoesNotContain("class DtoResponse", stringResult);
            StringAssert.DoesNotContain("EmbeddedRequest", stringResult);
            StringAssert.DoesNotContain("class EmbeddedResponse", stringResult);
        }

        [Test]
        public void IncludeTypes_ReturnsReferenceTypes_If_IncludeTypes_HasWildcard_Kotlin()
        {
            var result = appHost.ExecuteService(new TypesKotlin
            {
                IncludeTypes = new List<string> { "Dto.*" }
            });

            var stringResult = result.ToString();

            StringAssert.Contains("class DtoResponse", stringResult);
            StringAssert.Contains("class EmbeddedRequest", stringResult);
            StringAssert.Contains("class EmbeddedResponse", stringResult);
        }

        [Test]
        public void IncludeTypes_DoesNotReturnReferenceTypes_If_IncludeTypes_NoWildcard_Java()
        {
            var result = appHost.ExecuteService(new TypesJava
            {
                IncludeTypes = new List<string> { "Dto" }
            });

            var stringResult = result.ToString();

            StringAssert.DoesNotContain("class DtoResponse", stringResult);
            StringAssert.DoesNotContain("EmbeddedRequest", stringResult);
            StringAssert.DoesNotContain("class EmbeddedResponse", stringResult);
        }

        [Test]
        public void IncludeTypes_ReturnsReferenceTypes_If_IncludeTypes_HasWildcard_Java()
        {
            var result = appHost.ExecuteService(new TypesJava
            {
                IncludeTypes = new List<string> { "Dto.*" }
            });

            var stringResult = result.ToString();

            StringAssert.Contains("class DtoResponse", stringResult);
            StringAssert.Contains("class EmbeddedRequest", stringResult);
            StringAssert.Contains("class EmbeddedResponse", stringResult);
        }

        [Test]
        public void IncludeTypes_DoesNotReturnReferenceTypes_If_IncludeTypes_NoWildcard_Swift()
        {
            var result = appHost.ExecuteService(new TypesSwift
            {
                IncludeTypes = new List<string> { "Dto" }
            });

            var stringResult = result.ToString();

            StringAssert.DoesNotContain("class DtoResponse", stringResult);
            StringAssert.DoesNotContain("EmbeddedRequest", stringResult);
            StringAssert.DoesNotContain("class EmbeddedResponse", stringResult);
        }

        [Test]
        public void IncludeTypes_ReturnsReferenceTypes_If_IncludeTypes_HasWildcard_Swift()
        {
            var result = appHost.ExecuteService(new TypesSwift
            {
                IncludeTypes = new List<string> { "Dto.*" }
            });

            var stringResult = result.ToString();

            StringAssert.Contains("class DtoResponse", stringResult);
            StringAssert.Contains("class EmbeddedRequest", stringResult);
            StringAssert.Contains("class EmbeddedResponse", stringResult);
        }

        [Test]
        public void GetIncludeList_Returns_IncludeList_when_Returning_generic_List()
        {
            var includeTypes = new List<string> { "GetRequest1", "ReturnedDto" };
            var config = new MetadataTypesConfig
            {
                IncludeTypes = includeTypes
            };

            var result = MetadataExtensions.GetIncludeList(new MetadataTypes(), config);
            result.PrintDump();

            Assert.AreEqual(includeTypes, result);
        }
    }

    public class NativeTypesTestService : Service
    {
        public object Any(Dto request)
        {
            return "just a test";
        }
    }

    public class Dto : IReturn<DtoResponse>
    {
        public EmbeddedResponse ReferencedType { get; set; }
    }

    public class DtoResponse
    {
        public EmbeddedRequest ReferencedType { get; set; }
    }

    public class EmbeddedResponse { }
    public class EmbeddedRequest { }


    public partial class ReturnedDto
    {
        public virtual int Id { get; set; }
    }

    [Route("/Request1", "GET")]
    public partial class GetRequest1 : IReturn<List<ReturnedDto>>, IGet { }

    [Route("/Request2", "GET")]
    public partial class GetRequest2 : IReturn<List<ReturnedDto>>, IGet { }

    public class ReturnGenericListServices : Service
    {
        public object Any(GetRequest1 request) => request;
        public object Any(GetRequest2 request) => request;
    }
}
#endif
