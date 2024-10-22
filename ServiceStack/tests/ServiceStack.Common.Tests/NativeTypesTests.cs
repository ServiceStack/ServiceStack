using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using NUnit.Framework;
using ServiceStack.Host;
using ServiceStack.NativeTypes;
using ServiceStack.NativeTypes.CSharp;
using ServiceStack.NativeTypes.Java;
using ServiceStack.Testing;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests
{

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

        [Test]
        public void Does_generate_python_in_correct_order()
        {
            var result = appHost.ExecuteService(new TypesPython()
            {
            });

            var stringResult = result.ToString();
            
            // Ensure the index of the class `Tools` comes before `OpenAiChat`
            
            var toolsIndex = stringResult.IndexOf("class ToolCall:", StringComparison.Ordinal);
            // Ensure the toolsIndex is the lowest of any other instance of 'ToolCall'
            var firstRef = stringResult.IndexOf("ToolCall", StringComparison.Ordinal);
            Assert.That(toolsIndex, Is.LessThan(firstRef));
            
            Console.WriteLine($"{stringResult}");
            Assert.That(false, Is.True);
            
        }
        
    }

    public class NativeTypesTestService : Service
    {
        public object Any(Dto request) => request;

        public object Any(DtoRequestWithStructProperty request) => request;
        
        public object Post(OpenAiChatCompletion request) => request;
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
    
    [Route("/v1/chat/completions", "POST")]
    public class OpenAiChatCompletion : OpenAiChat, IPost, IReturn<OpenAiChatResponse>
    {

        public string? RefId { get; set; }
    

        public string? Provider { get; set; }
    

        public string? Tag { get; set; }
    }
    
    [DataContract]
    public class OpenAiChat
    {

        [DataMember(Name = "messages")]
        public List<OpenAiMessage> Messages { get; set; }
        

        [DataMember(Name = "model")]
        public string Model { get; set; }
        

        [DataMember(Name = "frequency_penalty")]
        public double? FrequencyPenalty { get; set; }


        [DataMember(Name = "logit_bias")]
        public Dictionary<int,int>? LogitBias { get; set; }
        

        [DataMember(Name = "logprobs")]
        public bool? LogProbs { get; set; }


        [DataMember(Name = "top_logprobs")]
        public int? TopLogProbs { get; set; }


        [DataMember(Name = "max_tokens")]
        public int? MaxTokens { get; set; }
        

        [DataMember(Name = "n")]
        public int? N { get; set; }
        

        [DataMember(Name = "presence_penalty")]
        public double? PresencePenalty { get; set; }
        

        [DataMember(Name = "response_format")]
        public OpenAiResponseFormat? ResponseFormat { get; set; }
        

        [DataMember(Name = "seed")]
        public int? Seed { get; set; }
        

        [DataMember(Name = "stop")]
        public List<string>? Stop { get; set; }
        

        [DataMember(Name = "stream")]
        public bool? Stream { get; set; }
        

        [DataMember(Name = "temperature")]
        public double? Temperature { get; set; }
        

        [DataMember(Name = "top_p")]
        public double? TopP { get; set; }
        

        [DataMember(Name = "tools")]
        public List<OpenAiTools>? Tools { get; set; }


        [DataMember(Name = "user")]
        public string? User { get; set; }
    }


    [DataContract]
    public class OpenAiMessage
    {
        
        [DataMember(Name = "content")]
        public string Content { get; set; }
        
        
        [DataMember(Name = "role")]
        public string Role { get; set; }
        
        
        [DataMember(Name = "name")]
        public string? Name { get; set; }
        
        
        [DataMember(Name = "tool_calls")]
        public ToolCall[]? ToolCalls { get; set; }
        
        
        [DataMember(Name = "tool_call_id")]
        public string? ToolCallId { get; set; }
    }

    [DataContract]
    public class OpenAiTools
    {
        
        [DataMember(Name = "type")]
        public OpenAiToolType Type { get; set; }
    }

    public enum OpenAiToolType
    {
        [EnumMember(Value = "function")]
        Function,
    }

    [DataContract]
    public class OpenAiToolFunction
    {
        
        [DataMember(Name = "name")]
        public string? Name { get; set; }

        
        [DataMember(Name = "description")]
        public string? Description { get; set; }
        
        
        [DataMember(Name = "parameters")]
        public Dictionary<string,string>? Parameters { get; set; }
    }

    [DataContract]
    public class OpenAiResponseFormat
    {
        public const string Text = "text";
        public const string JsonObject = "json_object";
        
        [DataMember(Name = "response_format")]
        public ResponseFormat Type { get; set; }
    }

    public enum ResponseFormat
    {
        [EnumMember(Value = "text")]
        Text,
        [EnumMember(Value = "json_object")]
        JsonObject
    }

    [DataContract]
    public class OpenAiChatResponse
    {

        [DataMember(Name = "id")]
        public string Id { get; set; }
        

        [DataMember(Name = "choices")]
        public List<Choice> Choices { get; set; }
        

        [DataMember(Name = "created")]
        public long Created { get; set; }
        

        [DataMember(Name = "model")]
        public string Model { get; set; }
        

        [DataMember(Name = "system_fingerprint")]
        public string SystemFingerprint { get; set; }
        

        [DataMember(Name = "object")]
        public string Object { get; set; }
        

        [DataMember(Name = "usage")]
        public OpenAiUsage Usage { get; set; }
        
        [DataMember(Name = "responseStatus")]
        public ResponseStatus? ResponseStatus { get; set; }
    }


    [DataContract]
    public class OpenAiUsage
    {

        [DataMember(Name = "completion_tokens")]
        public int CompletionTokens { get; set; }


        [DataMember(Name = "prompt_tokens")]
        public int PromptTokens { get; set; }
        

        [DataMember(Name = "total_tokens")]
        public int TotalTokens { get; set; }
    }

    public class Choice
    {

        [DataMember(Name = "finish_reason")]
        public string FinishReason { get; set; }


        [DataMember(Name = "index")]
        public int Index { get; set; }
        

        [DataMember(Name = "message")]
        public ChoiceMessage Message { get; set; }
    }

    [DataContract]
    public class ChoiceMessage
    {

        [DataMember(Name = "content")]
        public string Content { get; set; }
        

        [DataMember(Name = "tool_calls")]
        public ToolCall[] ToolCalls { get; set; }


        [DataMember(Name = "role")]
        public string Role { get; set; }
    }


    [DataContract]
    public class ToolCall
    {

        [DataMember(Name = "id")]
        public string Id { get; set; }
        

        [DataMember(Name = "type")]
        public string Type { get; set; }
        

        [DataMember(Name = "function")]
        public string Function { get; set; }
    }


    [DataContract]
    public class ToolFunction
    {

        [DataMember(Name = "name")]
        public string Name { get; set; }


        [DataMember(Name = "arguments")]
        public string Arguments { get; set; }
    }


    [DataContract]
    public class Logprobs
    {

        [DataMember(Name = "content")]
        public LogprobItem[] Content { get; set; }
    }


    [DataContract]
    public class LogprobItem
    {
        
        [DataMember(Name = "token")]
        public string Token { get; set; }

        
        [DataMember(Name = "logprob")]
        public double Logprob { get; set; }
        
        
        [DataMember(Name = "bytes")]
        public byte[] Bytes { get; set; }
        
        
        [DataMember(Name = "top_logprobs")]
        public LogprobItem[] TopLogprobs { get; set; }
    }
}
