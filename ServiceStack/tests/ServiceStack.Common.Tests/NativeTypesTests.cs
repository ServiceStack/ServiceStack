#nullable enable

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

namespace ServiceStack.Common.Tests;

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

    public class ResponseBase<T>
    {
        public T Result { get; set; }
    }
    
    [Test]
    public void Does_generate_Cooked_ResponseType()
    {
        var str = typeof(StringResponse).GetFullyQualifiedName();
        Assert.That(str, Is.EqualTo("StringResponse"));
        
        str = typeof(Dictionary<string, StringResponse>).GetFullyQualifiedName();
        Assert.That(str, Is.EqualTo("Dictionary<String,StringResponse>"));
        
        str = typeof(Dictionary<string, List<StringResponse>>).GetFullyQualifiedName();
        Assert.That(str, Is.EqualTo("Dictionary<String,List<StringResponse>>"));

        str = typeof(ResponseBase<Dictionary<string, List<StringResponse>>>).GetFullyQualifiedName();
        Assert.That(str, Is.EqualTo("ResponseBase<Dictionary<String,List<StringResponse>>>"));
    }

    [Test]
    public void Does_generate_correct_csharp_types()
    {
        var src = (string)appHost.ExecuteService(new TypesCSharp());
        src.Print();
        
        // Uses Type Parameters for Types
        Assert.That(src, Does.Contain("class QueryResponseAlt<T>"));
        // But not interfaces/refs
        Assert.That(src, Does.Contain("IPatchDb<Item>"));
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
        var src = (string) appHost.ExecuteService(new TypesCSharp {
            IncludeTypes = ["Dto"]
        });

        StringAssert.DoesNotContain("class EmbeddedResponse", src);
    }

    [Test]
    public void IncludeTypes_ReturnsReferenceTypes_If_IncludeTypes_HasWildcard_Csharp()
    {
        var src = (string) appHost.ExecuteService(new TypesCSharp {
            IncludeTypes = ["Dto.*"]
        });

        StringAssert.Contains("class DtoResponse", src);
        StringAssert.Contains("class EmbeddedRequest", src);
        StringAssert.Contains("class EmbeddedResponse", src);
    }

    [Test]
    public void AnnotatedDtoTypes_ApiMemberNonDefaultProperties_AreSorted()
    {
        var src = (string) appHost.ExecuteService(new TypesCSharp {
            IncludeTypes = ["DtoResponse"]
        });

        StringAssert.Contains("class DtoResponse", src);
        StringAssert.Contains("[ApiMember(Description=\"ShouldBeFirstInGeneratedCode\", IsRequired=true, Name=\"ShouldBeLastInGeneratedCode\")]", src);
    }

    [Test]
    public void IncludeTypes_DoesNotReturnReferenceTypes_If_IncludeTypes_NoWildcard_Fsharp()
    {
        var src = (string) appHost.ExecuteService(new TypesFSharp {
            IncludeTypes = ["Dto"]
        });

        StringAssert.DoesNotContain("type EmbeddedResponse", src);
    }

    [Test]
    public void IncludeTypes_ReturnsReferenceTypes_If_IncludeTypes_HasWildcard_Fsharp()
    {
        var src = (string) appHost.ExecuteService(new TypesFSharp {
            IncludeTypes = ["Dto.*"]
        });

        StringAssert.Contains("type DtoResponse", src);
        StringAssert.Contains("type EmbeddedRequest", src);
        StringAssert.Contains("type EmbeddedResponse", src);
    }

    [Test]
    public void IncludeTypes_DoesNotReturnReferenceTypes_If_IncludeTypes_NoWildcard_VbNet()
    {
        var src = (string) appHost.ExecuteService(new TypesVbNet() {
            IncludeTypes = ["Dto"]
        });

        StringAssert.DoesNotContain("Class EmbeddedResponse", src);
    }

    [Test]
    public void IncludeTypes_ReturnsReferenceTypes_If_IncludeTypes_HasWildcard_VbNet()
    {
        var src = (string) appHost.ExecuteService(new TypesVbNet {
            IncludeTypes = ["Dto.*"]
        });

        StringAssert.Contains("Class DtoResponse", src);
        StringAssert.Contains("Class EmbeddedRequest", src);
        StringAssert.Contains("Class EmbeddedResponse", src);
    }

    [Test]
    public void IncludeTypes_DoesNotReturnReferenceTypes_If_IncludeTypes_NoWildcard_Kotlin()
    {
        var src = (string) appHost.ExecuteService(new TypesKotlin {
            IncludeTypes = ["Dto"]
        });

        StringAssert.DoesNotContain("class EmbeddedResponse", src);
    }

    [Test]
    public void IncludeTypes_ReturnsReferenceTypes_If_IncludeTypes_HasWildcard_Kotlin()
    {
        var src = (string) appHost.ExecuteService(new TypesKotlin {
            IncludeTypes = ["Dto.*"]
        });

        StringAssert.Contains("class DtoResponse", src);
        StringAssert.Contains("class EmbeddedRequest", src);
        StringAssert.Contains("class EmbeddedResponse", src);
    }

    [Test]
    public void IncludeTypes_DoesNotReturnReferenceTypes_If_IncludeTypes_NoWildcard_Java()
    {
        var src = (string) appHost.ExecuteService(new TypesJava {
            IncludeTypes = ["Dto"]
        });

        StringAssert.DoesNotContain("class EmbeddedResponse", src);
    }

    [Test]
    public void IncludeTypes_ReturnsReferenceTypes_If_IncludeTypes_HasWildcard_Java()
    {
        var src = (string) appHost.ExecuteService(new TypesJava {
            IncludeTypes = ["Dto.*"]
        });

        StringAssert.Contains("class DtoResponse", src);
        StringAssert.Contains("class EmbeddedRequest", src);
        StringAssert.Contains("class EmbeddedResponse", src);
    }

    [Test]
    public void IncludeTypes_DoesNotReturnReferenceTypes_If_IncludeTypes_NoWildcard_Swift()
    {
        var src = (string) appHost.ExecuteService(new TypesSwift
        {
            IncludeTypes = ["Dto"]
        });

        StringAssert.DoesNotContain("class EmbeddedResponse", src);
    }

    [Test]
    public void IncludeTypes_ReturnsReferenceTypes_If_IncludeTypes_HasWildcard_Swift()
    {
        var src = (string) appHost.ExecuteService(new TypesSwift
        {
            IncludeTypes = ["Dto.*"]
        });

        StringAssert.Contains("class DtoResponse", src);
        StringAssert.Contains("class EmbeddedRequest", src);
        StringAssert.Contains("class EmbeddedResponse", src);
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
        var src = (string) appHost.ExecuteService(new TypesCSharp
        {
            IncludeTypes = ["DtoRequestWithStructProperty"],
        });

        StringAssert.Contains("class DtoRequestWithStructProperty", src);
        StringAssert.Contains("public virtual string StructType { get; set; }", src);
        StringAssert.Contains("public virtual string? NullableStructType { get; set; }", src);
    }

    [Test]
    public void Custom_ValueTypes_can_be_exported_csharp()
    {
        var src = (string) appHost.ExecuteService(new TypesCSharp
        {
            IncludeTypes = ["DtoRequestWithStructProperty"],
            ExportValueTypes = true,
        });

        StringAssert.Contains("class DtoRequestWithStructProperty", src);
        StringAssert.Contains("public virtual StructType StructType { get; set; }", src);
        StringAssert.Contains("public virtual StructType? NullableStructType { get; set; }", src);
    }

    [Test]
    public void Custom_ValueTypes_can_be_exported_as_different_Type_in_java()
    {
        JavaGenerator.TypeAliases["StructType"] = "JavaStruct";

        var src = (string) appHost.ExecuteService(new TypesJava
        {
            IncludeTypes = ["DtoRequestWithStructProperty"],
            ExportValueTypes = true,
        });
        StringAssert.Contains("class DtoRequestWithStructProperty", src);
        StringAssert.Contains("public JavaStruct getStructType()", src);
        StringAssert.Contains("public JavaStruct getNullableStructType()", src);

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
            DefaultNamespaces = ["ServiceStack"]
        });
        var src = csharp.GetCode(new MetadataTypes {
            Types = [
                new()
                {
                    Name = "TheType",
                    Attributes = [ metaAttr ]
                }
            ]
        }, new BasicRequest(), appHost.TryResolve<INativeTypesMetadata>());
            
        src.Print();
            
        Assert.That(src, Does.Contain(
            "[ValidateRequest(\"HasRole('Accounts')\", ErrorCode=\"ExCode\", Message=\"'Id' Is Required\")]"));
    }

    [Test]
    public void Can_generate_Swift_PocoLookupMap()
    {
        var typeName = "Dictionary`2";
        string[] genericArgs = ["String", "List<Dictionary<String,Poco>>"];

        var gen = new NativeTypes.Swift.SwiftGenerator(new MetadataTypesConfig());
        var type = gen.Type(typeName, genericArgs);
            
        Assert.That(type, Is.EqualTo("[String:[[String:Poco]]]"));
    }

    [Test]
    public void Does_generate_Swift_IntArray()
    {
        var genericArgs = Array.Empty<string>();

        var gen = new NativeTypes.Swift.SwiftGenerator(new MetadataTypesConfig());
        Assert.That(gen.Type("Int32[]", genericArgs), Is.EqualTo("[Int]"));
        Assert.That(gen.Type("Int64[]", genericArgs), Is.EqualTo("[Int]"));
    }

    [Test]
    public void Does_generate_Swift_IntList()
    {
        var gen = new NativeTypes.Swift.SwiftGenerator(new MetadataTypesConfig());
        Assert.That(gen.Type("List`1", ["Int32"]), Is.EqualTo("[Int]"));
        Assert.That(gen.Type("List`1", ["Int64"]), Is.EqualTo("[Int]"));
    }

    [Test]
    public void Does_generate_python_in_correct_order()
    {
        var src = (string) appHost.ExecuteService(new TypesPython());
        src.Print();

        // Ensure the index of the class `Tools` comes before `OpenAiChat`
        var toolsIndex = src.IndexOf("class ToolCall:", StringComparison.Ordinal);
        // Ensure the toolsIndex is the lowest of any other instance of 'ToolCall'
        var firstRef = src.IndexOf("ToolCall", StringComparison.Ordinal);
        Assert.That(toolsIndex, Is.LessThan(firstRef));
    }

    [Test]
    public void Does_generate_typescript_optionals()
    {
        var src = (string) appHost.ExecuteService(new TypesTypeScript { IncludeTypes = ["OptionalTest"] });
        Assert.That(src, Does.Contain("int: number;"));
        Assert.That(src, Does.Contain("nInt?: number;"));
        Assert.That(src, Does.Contain("nRequiredInt: number;"));
        Assert.That(src, Does.Contain("string: string;"));
        Assert.That(src, Does.Contain("nString?: string;"));
        Assert.That(src, Does.Contain("nRequiredString: string;"));
        Assert.That(src, Does.Contain("optionalClass: OptionalClass;"));
        Assert.That(src, Does.Contain("nOptionalClass?: OptionalClass;"));
        Assert.That(src, Does.Contain("nRequiredOptionalClass: OptionalClass;"));
        Assert.That(src, Does.Contain("optionalEnum: OptionalEnum;"));
        Assert.That(src, Does.Contain("nOptionalEnum?: OptionalEnum;"));
        Assert.That(src, Does.Contain("nRequiredOptionalEnum: OptionalEnum;"));
    }

    [Test]
    public void Does_generate_csharp_optionals()
    {
        var src = (string) appHost.ExecuteService(new TypesCSharp { IncludeTypes = ["OptionalTest"] });
        Assert.That(src, Does.Contain("int Int { get; set; }"));
        Assert.That(src, Does.Contain("int? NInt { get; set; }"));
        Assert.That(src, Does.Contain("int NRequiredInt { get; set; }"));
        Assert.That(src, Does.Contain("string String { get; set; }"));
        Assert.That(src, Does.Contain("string? NString { get; set; }"));
        Assert.That(src, Does.Contain("string NRequiredString { get; set; }"));
        Assert.That(src, Does.Contain("OptionalClass OptionalClass { get; set; }"));
        Assert.That(src, Does.Contain("OptionalClass? NOptionalClass { get; set; }"));
        Assert.That(src, Does.Contain("OptionalClass NRequiredOptionalClass { get; set; }"));
        Assert.That(src, Does.Contain("OptionalEnum OptionalEnum { get; set; }"));
        Assert.That(src, Does.Contain("OptionalEnum? NOptionalEnum { get; set; }"));
        Assert.That(src, Does.Contain("OptionalEnum NRequiredOptionalEnum { get; set; }"));
    }

    [Test]
    public void Does_generate_dart_optionals()
    {
        var src = (string) appHost.ExecuteService(new TypesDart { IncludeTypes = ["OptionalTest"] });
        Assert.That(src, Does.Contain("int Int = 0;"));
        Assert.That(src, Does.Contain("int? nInt;"));
        Assert.That(src, Does.Contain("int nRequiredInt = 0;"));
        Assert.That(src, Does.Contain("String string = \"\";"));
        Assert.That(src, Does.Contain("String? nString;"));
        Assert.That(src, Does.Contain("String nRequiredString = \"\";"));
        Assert.That(src, Does.Contain("OptionalClass optionalClass;"));
        Assert.That(src, Does.Contain("OptionalClass? nOptionalClass;"));
        Assert.That(src, Does.Contain("OptionalClass nRequiredOptionalClass;"));
        Assert.That(src, Does.Contain("OptionalEnum optionalEnum;"));
        Assert.That(src, Does.Contain("OptionalEnum? nOptionalEnum;"));
        Assert.That(src, Does.Contain("OptionalEnum nRequiredOptionalEnum;"));
    }

    [Test]
    public void Does_generate_swift_optionals()
    {
        // TODO: Investigate making it more typed
        var src = (string) appHost.ExecuteService(new TypesPython { IncludeTypes = ["OptionalTest"] });
        Assert.That(src, Does.Contain("int_: int"));
        Assert.That(src, Does.Contain("n_int: Optional[int] = None"));
        Assert.That(src, Does.Contain("n_required_int: Optional[int] = None"));
        Assert.That(src, Does.Contain("string: Optional[str] = None"));
        Assert.That(src, Does.Contain("n_string: Optional[str] = None"));
        Assert.That(src, Does.Contain("n_required_string: Optional[str] = None"));
        Assert.That(src, Does.Contain("optional_class: Optional[OptionalClass] = None"));
        Assert.That(src, Does.Contain("n_optional_class: Optional[OptionalClass] = None"));
        Assert.That(src, Does.Contain("n_required_optional_class: Optional[OptionalClass] = None"));
        Assert.That(src, Does.Contain("optional_enum: Optional[OptionalEnum] = None"));
        Assert.That(src, Does.Contain("n_optional_enum: Optional[OptionalEnum] = None"));
        Assert.That(src, Does.Contain("n_required_optional_enum: Optional[OptionalEnum] = None"));
    }
}

public class NativeTypesTestService : Service
{
    public object Any(Dto request) => request;

    public object Any(DtoRequestWithStructProperty request) => request;
        
    public object Post(OpenAiChatCompletion request) => request;
}

public class CollectionTestService : Service
{
    public object Any(AltQueryItems request) => new QueryResponseAlt<Item>();
    public Items Get(GetItems dto) => new();
    public List<Item> Get(GetNakedItems request) => new();


    public object Any(QueryItems request) => new QueryResponse<Item>();
    public void Any(CreateItem request) {}
    public void Any(UpdateItem request) {}
    public void Any(DeleteItem request) {}
}

public record class OptionalClass(int Id);
public enum OptionalEnum { Value1 }

public class OptionalTest : IReturn<OptionalTest>
{
    public int Int { get; set; }
    public int? NInt { get; set; }
    [ValidateNotNull]
    public int? NRequiredInt { get; set; }
    public string String { get; set; }
    public string? NString { get; set; }
    [ValidateNotEmpty]
    public string? NRequiredString { get; set; }
    
    public OptionalClass OptionalClass { get; set; }
    public OptionalClass? NOptionalClass { get; set; }
    [ValidateNotNull]
    public OptionalClass? NRequiredOptionalClass { get; set; }
    
    public OptionalEnum OptionalEnum { get; set; }
    public OptionalEnum? NOptionalEnum { get; set; }
    [ValidateNotNull]
    public OptionalEnum? NRequiredOptionalEnum { get; set; }
}
public class OptionalService : Service
{
    public object Any(OptionalTest request) => request;
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

public class Items
{
    public List<Item> Results { get; set; }
}
public class Item
{
    public string Name { get; set; }
    public string Description { get; set; }
}

public class GetItems : IReturn<Items> { }
public class GetNakedItems : IReturn<List<Item>> { }

public class AltQueryItems : IReturn<QueryResponseAlt<Item>>
{
    public string Name { get; set; }
}
    
public class QueryResponseAlt<T> : IHasResponseStatus, IMeta
{
    public virtual int Offset { get; set; }
    public virtual int Total { get; set; }
    public virtual List<T> Results { get; set; }
    public virtual Dictionary<string, string> Meta { get; set; }
    public virtual ResponseStatus ResponseStatus { get; set; }
}

public class QueryItems : QueryDb<Item> {}
public class CreateItem : ICreateDb<Item>, IReturnVoid {}
public class UpdateItem : IPatchDb<Item>, IReturnVoid {}
public class DeleteItem : IDeleteDb<Item>, IReturnVoid {}
