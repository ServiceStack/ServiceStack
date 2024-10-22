#if NETFRAMEWORK
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ServiceStack.Host;
using ServiceStack.NativeTypes.CSharp;
using ServiceStack.NativeTypes.Java;
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

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            appHost =
                new BasicAppHost(typeof(Dto).Assembly)
                {
                    TestMode = true,
                    Plugins = { new NativeTypesFeature() },
                    Config = new HostConfig()
                }.Init();
        }

        [OneTimeTearDown]
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

            // StringAssert.DoesNotContain("class DtoResponse", stringResult);
            // StringAssert.DoesNotContain("EmbeddedRequest", stringResult);
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
        public void AnnotatedDtoTypes_ApiMemberNonDefaultProperties_AreSorted()
        {
            var result = appHost.ExecuteService(new TypesCSharp
            {
                IncludeTypes = new List<string> { "DtoResponse" }
            });

            var stringResult = result.ToString();

            StringAssert.Contains("class DtoResponse", stringResult);
            StringAssert.Contains("[ApiMember(Description=\"ShouldBeFirstInGeneratedCode\", IsRequired=true, Name=\"ShouldBeLastInGeneratedCode\")]", stringResult);
        }

        [Test]
        public void IncludeTypes_DoesNotReturnReferenceTypes_If_IncludeTypes_NoWildcard_Fsharp()
        {
            var result = appHost.ExecuteService(new TypesFSharp
            {
                IncludeTypes = new List<string> { "Dto" }
            });

            var stringResult = result.ToString();

            // StringAssert.DoesNotContain("type DtoResponse", stringResult);
            // StringAssert.DoesNotContain("EmbeddedRequest", stringResult);
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

            // StringAssert.DoesNotContain("Class DtoResponse", stringResult);
            // StringAssert.DoesNotContain("EmbeddedRequest", stringResult);
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

            // StringAssert.DoesNotContain("class DtoResponse", stringResult);
            // StringAssert.DoesNotContain("EmbeddedRequest", stringResult);
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

            // StringAssert.DoesNotContain("class DtoResponse", stringResult);
            // StringAssert.DoesNotContain("EmbeddedRequest", stringResult);
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

            // StringAssert.DoesNotContain("class DtoResponse", stringResult);
            // StringAssert.DoesNotContain("EmbeddedRequest", stringResult);
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

        [Test]
        public void Custom_ValueTypes_defaults_to_use_opaque_strings_csharp()
        {
            var result = appHost.ExecuteService(new TypesCSharp
            {
                IncludeTypes = new List<string> { "DtoRequestWithStructProperty" },
            });

            var stringResult = result.ToString();

            StringAssert.Contains("class DtoRequestWithStructProperty", stringResult);
            StringAssert.Contains("public virtual string StructType { get; set; }", stringResult);
            StringAssert.Contains("public virtual string NullableStructType { get; set; }", stringResult);
        }

        [Test]
        public void Custom_ValueTypes_can_be_exported_csharp()
        {
            var result = appHost.ExecuteService(new TypesCSharp
            {
                IncludeTypes = new List<string> { "DtoRequestWithStructProperty" },
                ExportValueTypes = true,
            });

            var stringResult = result.ToString();

            StringAssert.Contains("class DtoRequestWithStructProperty", stringResult);
            StringAssert.Contains("public virtual StructType StructType { get; set; }", stringResult);
            StringAssert.Contains("public virtual StructType? NullableStructType { get; set; }", stringResult);
        }

        [Test]
        public void Custom_ValueTypes_can_be_exported_as_different_Type_in_java()
        {
            JavaGenerator.TypeAliases["StructType"] = "JavaStruct";

            var result = appHost.ExecuteService(new TypesJava
            {
                IncludeTypes = new List<string> { "DtoRequestWithStructProperty" },
                ExportValueTypes = true,
            });

            var stringResult = result.ToString();

            StringAssert.Contains("class DtoRequestWithStructProperty", stringResult);
            StringAssert.Contains("public JavaStruct StructType = null;", stringResult);
            StringAssert.Contains("public JavaStruct NullableStructType = null;", stringResult);

            string value;
            JavaGenerator.TypeAliases.TryRemove("StructType", out value);
        }

        public enum ComparisonOperator
        {
            Equals = 0,
            NotEqual = 1,
        }

        [Test]
        public void Can_access_enum_with_Equals_member()
        {
            var enumNames = new List<string>();
            var enumValues = new List<string>();
            
            var type = typeof(ComparisonOperator);
            var names = Enum.GetNames(type);
            for (var i = 0; i < names.Length; i++)
            {
                var name = names[i];
                var enumMember = MetadataTypesGenerator.GetEnumMember(type, name);
                var value = enumMember.GetRawConstantValue();
                var enumValue = Convert.ToInt64(value).ToString();

                enumNames.Add(name);
                enumValues.Add(enumValue);
            }
            
            Assert.That(enumNames, Is.EquivalentTo(new[]{ "Equals", "NotEqual" }));
            Assert.That(enumValues, Is.EquivalentTo(new[]{ "0", "1" }));
        }

        [Test]
        public void Can_write_ValidateRequestAttribute()
        {
            var nativeTypes = appHost.AssertPlugin<NativeTypesFeature>();
            var gen = nativeTypes.DefaultGenerator;
            var attr = new ValidateRequestAttribute("HasRole('Accounts')") {
                ErrorCode = "ExCode",
                Message = "'Id' Is Required",
            };
            var metaAttr = gen.ToAttribute(attr);
            string argValue(string name) => metaAttr.Args.First(x => x.Name == name).Value;
            Assert.That(metaAttr.Name, Is.EqualTo("ValidateRequest"));
            Assert.That(metaAttr.Args.Count, Is.EqualTo(3));
            Assert.That(argValue(nameof(ValidateRequestAttribute.Validator)), Is.EqualTo("HasRole('Accounts')"));
            Assert.That(argValue(nameof(ValidateRequestAttribute.ErrorCode)), Is.EqualTo("ExCode"));
            Assert.That(argValue(nameof(ValidateRequestAttribute.Message)), Is.EqualTo("'Id' Is Required"));
            
            var csharp = new CSharpGenerator(new MetadataTypesConfig {
                DefaultNamespaces = new List<string> {
                    "ServiceStack"
                }
            });
            var src = csharp.GetCode(new MetadataTypes {
                Types = new List<MetadataType> {
                    new MetadataType {
                        Name = "TheType",
                        Attributes = new List<MetadataAttribute> {
                            metaAttr,
                        }
                    }
                }
            }, new BasicRequest(), appHost.TryResolve<INativeTypesMetadata>());
            
            src.Print();
            
            Assert.That(src, Does.Contain(
            "[ValidateRequest(\"HasRole('Accounts')\", ErrorCode=\"ExCode\", Message=\"'Id' Is Required\")]"));
        }

        [Test]
        public void Can_generate_Swift_PocoLookupMap()
        {
            var typeName = "Dictionary`2";
            var genericArgs = new[] { "String", "List<Dictionary<String,Poco>>" };

            var gen = new ServiceStack.NativeTypes.Swift.SwiftGenerator(new MetadataTypesConfig());
            var type = gen.Type(typeName, genericArgs);
            
            Assert.That(type, Is.EqualTo("[String:[[String:Poco]]]"));
        }

        [Test]
        public void Does_generate_Swift_IntArray()
        {
            var genericArgs = new string[] { };

            var gen = new ServiceStack.NativeTypes.Swift.SwiftGenerator(new MetadataTypesConfig());
            Assert.That(gen.Type("Int32[]", genericArgs), Is.EqualTo("[Int]"));
            Assert.That(gen.Type("Int64[]", genericArgs), Is.EqualTo("[Int]"));
        }

        [Test]
        public void Does_generate_Swift_IntList()
        {
            var gen = new ServiceStack.NativeTypes.Swift.SwiftGenerator(new MetadataTypesConfig());
            Assert.That(gen.Type("List`1", new[] { "Int32" }), Is.EqualTo("[Int]"));
            Assert.That(gen.Type("List`1", new[] { "Int64" }), Is.EqualTo("[Int]"));
        }
        
    }

    public class NativeTypesTestService : Service
    {
        public object Any(Dto request) => request;

        public object Any(DtoRequestWithStructProperty request) => request;
    }

    public class Dto : IReturn<DtoResponse>
    {
        public EmbeddedResponse ReferencedType { get; set; }
    }

    public class DtoResponse
    {
        [ApiMember(Name = "ShouldBeLastInGeneratedCode", Description = "ShouldBeFirstInGeneratedCode", IsRequired = true)]
        public EmbeddedRequest ReferencedType { get; set; }
    }

    public class EmbeddedResponse { }
    public class EmbeddedRequest { }


    [Route("/Request1/", "GET")]
    public partial class GetRequest1 : IReturn<List<ReturnedDto>>, IGet { }

    [Route("/Request3", "GET")]
    public partial class GetRequest2 : IReturn<ReturnedDto>, IGet {}

    public partial class ReturnedDto
    {
        public virtual int Id { get; set; }
    }

    public class ReturnGenericListServices : Service
    {
        public object Any(GetRequest1 request) => request;
        public object Any(GetRequest2 request) => request;
    }

    public class DtoRequestWithStructProperty : IReturn<DtoResponse>
    {
        public StructType StructType { get; set; }
        public StructType? NullableStructType { get; set; }
    }

    public struct StructType
    {
        public int Id;
    }
}
#endif
