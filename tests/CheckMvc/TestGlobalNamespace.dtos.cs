/* Options:
Date: 2015-11-23 18:41:03
Version: 4.00
BaseUrl: http://test.servicestack.net

GlobalNamespace: testdtos
//MakePartial: True
//MakeVirtual: True
//MakeDataContractsExtensible: False
//AddReturnMarker: True
//AddDescriptionAsComments: True
//AddDataContractAttributes: False
//AddIndexesToDataMembers: False
//AddGeneratedCodeAttributes: False
//AddResponseStatus: False
//AddImplicitVersion: 
//InitializeCollections: True
//IncludeTypes: 
//ExcludeTypes: 
//AddDefaultXmlNamespace: http://schemas.servicestack.net/types
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack;
using ServiceStack.DataAnnotations;
using testdtos;


namespace testdtos
{

    public enum ExternalEnum
    {
        Foo,
        Bar,
        Baz,
    }

    public enum ExternalEnum2
    {
        Uno,
        Due,
        Tre,
    }

    public enum ExternalEnum3
    {
        Un,
        Deux,
        Trois,
    }

    public partial class ExternalOperation
        : IReturn<ExternalOperationResponse>
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual ExternalEnum ExternalEnum { get; set; }
    }

    public partial class ExternalOperation2
        : IReturn<ExternalOperation2Response>
    {
        public virtual int Id { get; set; }
    }

    public partial class ExternalOperation2Response
    {
        public virtual ExternalType ExternalType { get; set; }
    }

    public partial class ExternalOperation3
        : IReturn<ExternalReturnTypeResponse>
    {
        public virtual int Id { get; set; }
    }

    public partial class ExternalOperation4
    {
        public virtual int Id { get; set; }
    }

    public partial class ExternalOperationResponse
    {
        public virtual string Result { get; set; }
    }

    public partial class ExternalReturnTypeResponse
    {
        public virtual ExternalEnum3 ExternalEnum3 { get; set; }
    }

    public partial class ExternalType
    {
        public virtual ExternalEnum2 ExternalEnum2 { get; set; }
    }

    public partial class Account
    {
        public virtual string Name { get; set; }
    }

    public partial class CustomUserSession
        : AuthUserSession
    {
        [DataMember]
        public virtual string CustomName { get; set; }

        [DataMember]
        public virtual string CustomInfo { get; set; }
    }

    [Route("/image-draw/{Name}")]
    public partial class DrawImage
    {
        public virtual string Name { get; set; }
        public virtual string Format { get; set; }
        public virtual int? Width { get; set; }
        public virtual int? Height { get; set; }
        public virtual int? FontSize { get; set; }
        public virtual string Foreground { get; set; }
        public virtual string Background { get; set; }
    }

    public partial class GetAccount
        : IReturn<Account>
    {
        public virtual string Account { get; set; }
    }

    public partial class GetProject
        : IReturn<Project>
    {
        public virtual string Account { get; set; }
        public virtual string Project { get; set; }
    }

    [Route("/session")]
    public partial class GetSession
        : IReturn<GetSessionResponse>
    {
    }

    public partial class GetSessionResponse
    {
        public virtual CustomUserSession Result { get; set; }
        public virtual UnAuthInfo UnAuthInfo { get; set; }
        public virtual ResponseStatus ResponseStatus { get; set; }
    }

    [Route("/image-bytes")]
    public partial class ImageAsBytes
    {
        public virtual string Format { get; set; }
    }

    [Route("/image-custom")]
    public partial class ImageAsCustomResult
    {
        public virtual string Format { get; set; }
    }

    [Route("/image-file")]
    public partial class ImageAsFile
    {
        public virtual string Format { get; set; }
    }

    [Route("/image-redirect")]
    public partial class ImageAsRedirect
    {
        public virtual string Format { get; set; }
    }

    [Route("/image-stream")]
    public partial class ImageAsStream
    {
        public virtual string Format { get; set; }
    }

    [Route("/image-response")]
    public partial class ImageWriteToResponse
    {
        public virtual string Format { get; set; }
    }

    [Route("/ping")]
    public partial class Ping
        : IReturn<PingResponse>
    {
    }

    public partial class PingResponse
    {
        public PingResponse()
        {
            Responses = new Dictionary<string, ResponseStatus>{};
        }

        public virtual Dictionary<string, ResponseStatus> Responses { get; set; }
        public virtual ResponseStatus ResponseStatus { get; set; }
    }

    public partial class PingService
    {

        [Route("/reset-connections")]
        public partial class ResetConnections
        {
        }
    }

    public partial class Project
    {
        public virtual string Account { get; set; }
        public virtual string Name { get; set; }
    }

    [Route("/{Path*}")]
    public partial class RootPathRoutes
    {
        public virtual string Path { get; set; }
    }

    public partial class SendDefault
        : IReturn<SendVerbResponse>
    {
        public virtual int Id { get; set; }
    }

    public partial class SendGet
        : IReturn<SendVerbResponse>, IGet
    {
        public virtual int Id { get; set; }
    }

    public partial class SendPost
        : IReturn<SendVerbResponse>, IPost
    {
        public virtual int Id { get; set; }
    }

    public partial class SendPut
        : IReturn<SendVerbResponse>, IPut
    {
        public virtual int Id { get; set; }
    }

    [Route("/sendrestget/{Id}", "GET")]
    public partial class SendRestGet
        : IReturn<SendVerbResponse>, IGet
    {
        public virtual int Id { get; set; }
    }

    public partial class SendVerbResponse
    {
        public virtual int Id { get; set; }
        public virtual string PathInfo { get; set; }
        public virtual string RequestMethod { get; set; }
    }

    [Route("/null-response")]
    public partial class TestNullResponse
    {
    }

    [Route("/void-response")]
    public partial class TestVoidResponse
    {
    }

    [Route("/textfile-test")]
    public partial class TextFileTest
    {
        public virtual bool AsAttachment { get; set; }
    }

    public partial class UnAuthInfo
    {
        public virtual string CustomInfo { get; set; }
    }

    [Route("/session/edit/{CustomName}")]
    public partial class UpdateSession
        : IReturn<GetSessionResponse>
    {
        public virtual string CustomName { get; set; }
    }

    [Route("/wait/{ForMs}")]
    public partial class Wait
        : IReturn<Wait>
    {
        public virtual int ForMs { get; set; }
    }

    ///<summary>
    ///AllowedAttributes Description
    ///</summary>
    [Route("/allowed-attributes", "GET")]
    [Api("AllowedAttributes Description")]
    [ApiResponse(400, "Your request was not understood")]
    [DataContract]
    public partial class AllowedAttributes
    {
        [DataMember(Name="Aliased")]
        [ApiMember(ParameterType="path", Description="Range Description", DataType="double", IsRequired=true)]
        public virtual double Range { get; set; }
    }

    public partial class ArrayResult
    {
        public virtual string Result { get; set; }
    }

    public partial class CustomHttpError
        : IReturn<CustomHttpErrorResponse>
    {
        public virtual int StatusCode { get; set; }
        public virtual string StatusDescription { get; set; }
    }

    public partial class CustomHttpErrorResponse
    {
        public virtual string Custom { get; set; }
        public virtual ResponseStatus ResponseStatus { get; set; }
    }

    public partial class EmptyClass
    {
    }

    [Flags]
    public enum EnumFlags
    {
        Value1 = 1,
        Value2 = 2,
        Value3 = 4,
    }

    public partial class EnumRequest
        : IReturn<EnumResponse>, IPut
    {
        public virtual ScopeType Operator { get; set; }
    }

    public partial class EnumResponse
    {
        public virtual ScopeType Operator { get; set; }
    }

    public enum EnumType
    {
        Value1,
        Value2,
    }

    [Route("/example", "GET")]
    [DataContract]
    public partial class GetExample
        : IReturn<GetExampleResponse>
    {
    }

    [DataContract]
    public partial class GetExampleResponse
    {
        [DataMember(Order=1)]
        public virtual ResponseStatus ResponseStatus { get; set; }

        [DataMember(Order=2)]
        [ApiMember]
        public virtual MenuExample MenuExample1 { get; set; }
    }

    [Route("/randomids")]
    public partial class GetRandomIds
        : IReturn<GetRandomIdsResponse>
    {
        public virtual int? Take { get; set; }
    }

    public partial class GetRandomIdsResponse
    {
        public GetRandomIdsResponse()
        {
            Results = new List<string>{};
        }

        public virtual List<string> Results { get; set; }
    }

    [Route("/hello")]
    [Route("/hello/{Name}")]
    public partial class Hello
        : IReturn<HelloResponse>
    {
        [Required]
        public virtual string Name { get; set; }

        public virtual string Title { get; set; }
    }

    [Route("/all-types")]
    public partial class HelloAllTypes
        : IReturn<HelloAllTypesResponse>
    {
        public virtual string Name { get; set; }
        public virtual AllTypes AllTypes { get; set; }
        public virtual AllCollectionTypes AllCollectionTypes { get; set; }
    }

    public partial class HelloAllTypesResponse
    {
        public virtual string Result { get; set; }
        public virtual AllTypes AllTypes { get; set; }
        public virtual AllCollectionTypes AllCollectionTypes { get; set; }
    }

    public partial class HelloArray
        : IReturn<ArrayResult[]>
    {
        public HelloArray()
        {
            Names = new List<string>{};
        }

        public virtual List<string> Names { get; set; }
    }

    public partial class HelloBase<T>
    {
        public HelloBase()
        {
            Items = new List<T>{};
            Counts = new List<int>{};
        }

        public virtual List<T> Items { get; set; }
        public virtual List<int> Counts { get; set; }
    }

    public partial class HelloBuiltin
    {
        public virtual DayOfWeek DayOfWeek { get; set; }
    }

    public partial class HelloDateTime
        : IReturn<HelloDateTime>
    {
        public virtual DateTime DateTime { get; set; }
    }

    public partial class HelloDelete
        : IReturn<HelloVerbResponse>, IDelete
    {
        public virtual int Id { get; set; }
    }

    public partial class HelloExternal
    {
        public virtual string Name { get; set; }
    }

    public partial class HelloGet
        : IReturn<HelloVerbResponse>, IGet
    {
        public virtual int Id { get; set; }
    }

    public partial class HelloInnerTypes
        : IReturn<HelloInnerTypesResponse>
    {
    }

    public partial class HelloInnerTypesResponse
    {
        public virtual TypesGroup.InnerType InnerType { get; set; }
        public virtual TypesGroup.InnerEnum InnerEnum { get; set; }
    }

    public partial class HelloInterface
    {
        public virtual IPoco Poco { get; set; }
        public virtual IEmptyInterface EmptyInterface { get; set; }
        public virtual EmptyClass EmptyClass { get; set; }
    }

    public partial class HelloList
        : IReturn<List<ListResult>>
    {
        public HelloList()
        {
            Names = new List<string>{};
        }

        public virtual List<string> Names { get; set; }
    }

    public partial class HelloPatch
        : IReturn<HelloVerbResponse>, IPatch
    {
        public virtual int Id { get; set; }
    }

    public partial class HelloPost
        : HelloBase, IReturn<HelloVerbResponse>, IPost
    {
    }

    public partial class HelloPut
        : IReturn<HelloVerbResponse>, IPut
    {
        public virtual int Id { get; set; }
    }

    public partial class HelloResponse
    {
        public virtual string Result { get; set; }
    }

    public partial class HelloReturnVoid
        : IReturnVoid
    {
        public virtual int Id { get; set; }
    }

    public partial class HelloString
        : IReturn<string>
    {
        public virtual string Name { get; set; }
    }

    public partial class HelloVerbResponse
    {
        public virtual string Result { get; set; }
    }

    public partial class HelloVoid
    {
        public virtual string Name { get; set; }
    }

    public partial class HelloWithAlternateReturnResponse
        : HelloWithReturnResponse
    {
        public virtual string AltResult { get; set; }
    }

    [DataContract]
    public partial class HelloWithDataContract
        : IReturn<HelloWithDataContractResponse>
    {
        [DataMember(Name="name", Order=1, IsRequired=true, EmitDefaultValue=false)]
        public virtual string Name { get; set; }

        [DataMember(Name="id", Order=2, EmitDefaultValue=false)]
        public virtual int Id { get; set; }
    }

    [DataContract]
    public partial class HelloWithDataContractResponse
    {
        [DataMember(Name="result", Order=1, IsRequired=true, EmitDefaultValue=false)]
        public virtual string Result { get; set; }
    }

    ///<summary>
    ///Description on HelloWithDescription type
    ///</summary>
    public partial class HelloWithDescription
        : IReturn<HelloWithDescriptionResponse>
    {
        public virtual string Name { get; set; }
    }

    ///<summary>
    ///Description on HelloWithDescriptionResponse type
    ///</summary>
    public partial class HelloWithDescriptionResponse
    {
        public virtual string Result { get; set; }
    }

    public partial class HelloWithEnum
    {
        public virtual EnumType EnumProp { get; set; }
        public virtual EnumType? NullableEnumProp { get; set; }
        public virtual EnumFlags EnumFlags { get; set; }
    }

    public partial class HelloWithGenericInheritance
        : HelloBase<Poco>
    {
        public virtual string Result { get; set; }
    }

    public partial class HelloWithGenericInheritance2
        : HelloBase<Hello>
    {
        public virtual string Result { get; set; }
    }

    public partial class HelloWithInheritance
        : HelloBase, IReturn<HelloWithInheritanceResponse>
    {
        public virtual string Name { get; set; }
    }

    public partial class HelloWithInheritanceResponse
        : HelloResponseBase
    {
        public virtual string Result { get; set; }
    }

    public partial class HelloWithNestedClass
        : IReturn<HelloResponse>
    {
        public virtual string Name { get; set; }
        public virtual HelloWithNestedClass.NestedClass NestedClassProp { get; set; }

        public partial class NestedClass
        {
            public virtual string Value { get; set; }
        }
    }

    public partial class HelloWithNestedInheritance
        : HelloBase<HelloWithNestedInheritance.Item>
    {

        public partial class Item
        {
            public virtual string Value { get; set; }
        }
    }

    public partial class HelloWithReturn
        : IReturn<HelloWithAlternateReturnResponse>
    {
        public virtual string Name { get; set; }
    }

    [Route("/helloroute")]
    public partial class HelloWithRoute
        : IReturn<HelloWithRouteResponse>
    {
        public virtual string Name { get; set; }
    }

    public partial class HelloWithRouteResponse
    {
        public virtual string Result { get; set; }
    }

    public partial class HelloWithType
        : IReturn<HelloWithTypeResponse>
    {
        public virtual string Name { get; set; }
    }

    public partial class HelloWithTypeResponse
    {
        public virtual HelloType Result { get; set; }
    }

    public partial interface IEmptyInterface
    {
    }

    public partial interface IPoco
    {
        string Name { get; set; }
    }

    public partial class ListResult
    {
        public virtual string Result { get; set; }
    }

    [DataContract]
    public partial class MenuExample
    {
        [DataMember(Order=1)]
        [ApiMember]
        public virtual MenuItemExample MenuItemExample1 { get; set; }
    }

    public partial class MenuItemExample
    {
        [DataMember(Order=1)]
        [ApiMember]
        public virtual string Name1 { get; set; }

        public virtual MenuItemExampleItem MenuItemExampleItem { get; set; }
    }

    public partial class MenuItemExampleItem
    {
        [DataMember(Order=1)]
        [ApiMember]
        public virtual string Name1 { get; set; }
    }

    [Route("/metadatatest")]
    public partial class MetadataTest
        : IReturn<MetadataTestResponse>
    {
        public virtual int Id { get; set; }
    }

    [Route("/metadatatest-array")]
    public partial class MetadataTestArray
        : IReturn<MetadataTestChild[]>
    {
        public virtual int Id { get; set; }
    }

    public partial class MetadataTestChild
    {
        public MetadataTestChild()
        {
            Results = new List<MetadataTestNestedChild>{};
        }

        public virtual string Name { get; set; }
        public virtual List<MetadataTestNestedChild> Results { get; set; }
    }

    public partial class MetadataTestNestedChild
    {
        public virtual string Name { get; set; }
    }

    public partial class MetadataTestResponse
    {
        public MetadataTestResponse()
        {
            Results = new List<MetadataTestChild>{};
        }

        public virtual int Id { get; set; }
        public virtual List<MetadataTestChild> Results { get; set; }
    }

    public partial class OnlyDefinedInGenericType
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
    }

    public partial class OnlyDefinedInGenericTypeFrom
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
    }

    public partial class OnlyDefinedInGenericTypeInto
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
    }

    public partial class QueryPocoBase
        : QueryBase<OnlyDefinedInGenericType>, IReturn<QueryResponse<OnlyDefinedInGenericType>>
    {
        public virtual int Id { get; set; }
    }

    public partial class QueryPocoIntoBase
        : QueryBase<OnlyDefinedInGenericTypeFrom, OnlyDefinedInGenericTypeInto>, IReturn<QueryResponse<OnlyDefinedInGenericTypeInto>>
    {
        public virtual int Id { get; set; }
    }

    [Route("/rockstars")]
    public partial class QueryRockstars
        : QueryBase<Rockstar>, IReturn<QueryResponse<Rockstar>>
    {
    }

    [Route("/requires-role")]
    public partial class RequiresRole
        : IReturn<RequiresRoleResponse>
    {
    }

    public partial class RequiresRoleResponse
    {
        public virtual string Result { get; set; }
        public virtual ResponseStatus ResponseStatus { get; set; }
    }

    public partial class Rockstar
    {
        public virtual int Id { get; set; }
        public virtual string FirstName { get; set; }
        public virtual string LastName { get; set; }
        public virtual int? Age { get; set; }
    }

    [DataContract]
    public enum ScopeType
    {
        Global = 1,
        Sale = 2,
    }

    [Route("/throw404")]
    [Route("/throw404/{Message}")]
    public partial class Throw404
    {
        public virtual string Message { get; set; }
    }

    [Route("/throwbusinesserror")]
    public partial class ThrowBusinessError
        : IReturn<ThrowBusinessErrorResponse>
    {
    }

    public partial class ThrowBusinessErrorResponse
    {
        public virtual ResponseStatus ResponseStatus { get; set; }
    }

    [Route("/throwcustom400")]
    [Route("/throwcustom400/{Message}")]
    public partial class ThrowCustom400
    {
        public virtual string Message { get; set; }
    }

    [Route("/throwhttperror/{Status}")]
    public partial class ThrowHttpError
    {
        public virtual int? Status { get; set; }
        public virtual string Message { get; set; }
    }

    [Route("/throw/{Type}")]
    public partial class ThrowType
        : IReturn<ThrowTypeResponse>
    {
        public virtual string Type { get; set; }
        public virtual string Message { get; set; }
    }

    public partial class ThrowTypeResponse
    {
        public virtual ResponseStatus ResponseStatus { get; set; }
    }

    [Route("/throwvalidation")]
    public partial class ThrowValidation
        : IReturn<ThrowValidationResponse>
    {
        public virtual int Age { get; set; }
        public virtual string Required { get; set; }
        public virtual string Email { get; set; }
    }

    public partial class ThrowValidationResponse
    {
        public virtual int Age { get; set; }
        public virtual string Required { get; set; }
        public virtual string Email { get; set; }
        public virtual ResponseStatus ResponseStatus { get; set; }
    }

    public partial class TypesGroup
    {

        public partial class InnerType
        {
            public virtual long Id { get; set; }
            public virtual string Name { get; set; }
        }

        public enum InnerEnum
        {
            Foo,
            Bar,
            Baz,
        }
    }

    public partial class AllCollectionTypes
    {
        public AllCollectionTypes()
        {
            IntArray = new int[]{};
            IntList = new List<int>{};
            StringArray = new string[]{};
            StringList = new List<string>{};
            PocoArray = new Poco[]{};
            PocoList = new List<Poco>{};
            PocoLookup = new Dictionary<string, List<Poco>>{};
            PocoLookupMap = new Dictionary<string, List<Dictionary<String,Poco>>>{};
        }

        public virtual int[] IntArray { get; set; }
        public virtual List<int> IntList { get; set; }
        public virtual string[] StringArray { get; set; }
        public virtual List<string> StringList { get; set; }
        public virtual Poco[] PocoArray { get; set; }
        public virtual List<Poco> PocoList { get; set; }
        public virtual Dictionary<string, List<Poco>> PocoLookup { get; set; }
        public virtual Dictionary<string, List<Dictionary<String,Poco>>> PocoLookupMap { get; set; }
    }

    public partial class AllTypes
    {
        public AllTypes()
        {
            StringList = new List<string>{};
            StringArray = new string[]{};
            StringMap = new Dictionary<string, string>{};
            IntStringMap = new Dictionary<int, string>{};
        }

        public virtual int Id { get; set; }
        public virtual int? NullableId { get; set; }
        public virtual byte Byte { get; set; }
        public virtual short Short { get; set; }
        public virtual int Int { get; set; }
        public virtual long Long { get; set; }
        public virtual ushort UShort { get; set; }
        public virtual uint UInt { get; set; }
        public virtual ulong ULong { get; set; }
        public virtual float Float { get; set; }
        public virtual double Double { get; set; }
        public virtual decimal Decimal { get; set; }
        public virtual string String { get; set; }
        public virtual DateTime DateTime { get; set; }
        public virtual TimeSpan TimeSpan { get; set; }
        public virtual DateTimeOffset DateTimeOffset { get; set; }
        public virtual Guid Guid { get; set; }
        public virtual Char Char { get; set; }
        public virtual DateTime? NullableDateTime { get; set; }
        public virtual TimeSpan? NullableTimeSpan { get; set; }
        public virtual List<string> StringList { get; set; }
        public virtual string[] StringArray { get; set; }
        public virtual Dictionary<string, string> StringMap { get; set; }
        public virtual Dictionary<int, string> IntStringMap { get; set; }
        public virtual SubType SubType { get; set; }
    }

    public partial class HelloBase
    {
        public virtual int Id { get; set; }
    }

    public partial class HelloResponseBase
    {
        public virtual int RefId { get; set; }
    }

    public partial class HelloType
    {
        public virtual string Result { get; set; }
    }

    public partial class HelloWithReturnResponse
    {
        public virtual string Result { get; set; }
    }

    public partial class Poco
    {
        public virtual string Name { get; set; }
    }

    public partial class SubType
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
    }
}

