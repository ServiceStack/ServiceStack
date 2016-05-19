using NUnit.Framework;

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
        public void GetIncludeList_Returns_IncludeList_If_IncludeRequestReferenceTypes_False()
        {
            var includeTypes = new List<string> { "Dto1", "Dto2" };
            var config = new MetadataTypesConfig
            {
                IncludeTypes = includeTypes,
                IncludeRequestReferenceTypes = false
            };

            var result = MetadataExtensions.GetIncludeList(new MetadataTypes(), config);
            Assert.AreEqual(includeTypes, result);
        }

        [Test]
        public void IncludeTypes_DoesNotReturnReferenceTypes_If_IncludeRequestReferenceTypes_False_Csharp()
        {
            var result = appHost.ExecuteService(new TypesCSharp
            {
                IncludeTypes = new List<string> { "Dto" },
                IncludeRequestReferenceTypes = false
            });

            var stringResult = result.ToString();

            StringAssert.DoesNotContain("class DtoResponse", stringResult);
            StringAssert.DoesNotContain("EmbeddedRequest", stringResult);
            StringAssert.DoesNotContain("class EmbeddedResponse", stringResult);
        }

        [Test]
        public void IncludeTypes_ReturnsReferenceTypes_If_IncludeRequestReferenceTypes_True_Csharp()
        {
            var result = appHost.ExecuteService(new TypesCSharp
            {
                IncludeTypes = new List<string> { "Dto" },
                IncludeRequestReferenceTypes = true
            });

            var stringResult = result.ToString();

            StringAssert.Contains("class DtoResponse", stringResult);
            StringAssert.Contains("class EmbeddedRequest", stringResult);
            StringAssert.Contains("class EmbeddedResponse", stringResult);
        }

        [Test]
        public void IncludeTypes_DoesNotReturnReferenceTypes_If_IncludeRequestReferenceTypes_False_Fsharp()
        {
            var result = appHost.ExecuteService(new TypesFSharp
            {
                IncludeTypes = new List<string> { "Dto" },
                IncludeRequestReferenceTypes = false
            });

            var stringResult = result.ToString();

            StringAssert.DoesNotContain("type DtoResponse", stringResult);
            StringAssert.DoesNotContain("EmbeddedRequest", stringResult);
            StringAssert.DoesNotContain("type EmbeddedResponse", stringResult);
        }

        [Test]
        public void IncludeTypes_ReturnsReferenceTypes_If_IncludeRequestReferenceTypes_True_Fsharp()
        {
            var result = appHost.ExecuteService(new TypesFSharp
            {
                IncludeTypes = new List<string> { "Dto" },
                IncludeRequestReferenceTypes = true
            });

            var stringResult = result.ToString();

            StringAssert.Contains("type DtoResponse", stringResult);
            StringAssert.Contains("type EmbeddedRequest", stringResult);
            StringAssert.Contains("type EmbeddedResponse", stringResult);
        }

        [Test]
        public void IncludeTypes_DoesNotReturnReferenceTypes_If_IncludeRequestReferenceTypes_False_VbNet()
        {
            var result = appHost.ExecuteService(new TypesVbNet()
            {
                IncludeTypes = new List<string> { "Dto" },
                IncludeRequestReferenceTypes = false
            });

            var stringResult = result.ToString();

            StringAssert.DoesNotContain("Class DtoResponse", stringResult);
            StringAssert.DoesNotContain("EmbeddedRequest", stringResult);
            StringAssert.DoesNotContain("Class EmbeddedResponse", stringResult);
        }

        [Test]
        public void IncludeTypes_ReturnsReferenceTypes_If_IncludeRequestReferenceTypes_True_VbNet()
        {
            var result = appHost.ExecuteService(new TypesVbNet
            {
                IncludeTypes = new List<string> { "Dto" },
                IncludeRequestReferenceTypes = true
            });

            var stringResult = result.ToString();

            StringAssert.Contains("Class DtoResponse", stringResult);
            StringAssert.Contains("Class EmbeddedRequest", stringResult);
            StringAssert.Contains("Class EmbeddedResponse", stringResult);
        }

        [Test]
        public void IncludeTypes_DoesNotReturnReferenceTypes_If_IncludeRequestReferenceTypes_False_Kotlin()
        {
            var result = appHost.ExecuteService(new TypesKotlin
            {
                IncludeTypes = new List<string> { "Dto" },
                IncludeRequestReferenceTypes = false
            });

            var stringResult = result.ToString();

            StringAssert.DoesNotContain("class DtoResponse", stringResult);
            StringAssert.DoesNotContain("EmbeddedRequest", stringResult);
            StringAssert.DoesNotContain("class EmbeddedResponse", stringResult);
        }

        [Test]
        public void IncludeTypes_ReturnsReferenceTypes_If_IncludeRequestReferenceTypes_True_Kotlin()
        {
            var result = appHost.ExecuteService(new TypesKotlin
            {
                IncludeTypes = new List<string> { "Dto" },
                IncludeRequestReferenceTypes = true
            });

            var stringResult = result.ToString();

            StringAssert.Contains("class DtoResponse", stringResult);
            StringAssert.Contains("class EmbeddedRequest", stringResult);
            StringAssert.Contains("class EmbeddedResponse", stringResult);
        }

        [Test]
        public void IncludeTypes_DoesNotReturnReferenceTypes_If_IncludeRequestReferenceTypes_False_Java()
        {
            var result = appHost.ExecuteService(new TypesJava
            {
                IncludeTypes = new List<string> { "Dto" },
                IncludeRequestReferenceTypes = false
            });

            var stringResult = result.ToString();

            StringAssert.DoesNotContain("class DtoResponse", stringResult);
            StringAssert.DoesNotContain("EmbeddedRequest", stringResult);
            StringAssert.DoesNotContain("class EmbeddedResponse", stringResult);
        }

        [Test]
        public void IncludeTypes_ReturnsReferenceTypes_If_IncludeRequestReferenceTypes_True_Java()
        {
            var result = appHost.ExecuteService(new TypesJava
            {
                IncludeTypes = new List<string> { "Dto" },
                IncludeRequestReferenceTypes = true
            });

            var stringResult = result.ToString();

            StringAssert.Contains("class DtoResponse", stringResult);
            StringAssert.Contains("class EmbeddedRequest", stringResult);
            StringAssert.Contains("class EmbeddedResponse", stringResult);
        }

        [Test]
        public void IncludeTypes_DoesNotReturnReferenceTypes_If_IncludeRequestReferenceTypes_False_Swift()
        {
            var result = appHost.ExecuteService(new TypesSwift
            {
                IncludeTypes = new List<string> { "Dto" },
                IncludeRequestReferenceTypes = false
            });

            var stringResult = result.ToString();

            StringAssert.DoesNotContain("class DtoResponse", stringResult);
            StringAssert.DoesNotContain("EmbeddedRequest", stringResult);
            StringAssert.DoesNotContain("class EmbeddedResponse", stringResult);
        }

        [Test]
        public void IncludeTypes_ReturnsReferenceTypes_If_IncludeRequestReferenceTypes_True_Swift()
        {
            var result = appHost.ExecuteService(new TypesSwift
            {
                IncludeTypes = new List<string> { "Dto" },
                IncludeRequestReferenceTypes = true
            });

            var stringResult = result.ToString();

            StringAssert.Contains("class DtoResponse", stringResult);
            StringAssert.Contains("class EmbeddedRequest", stringResult);
            StringAssert.Contains("class EmbeddedResponse", stringResult);
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
}
