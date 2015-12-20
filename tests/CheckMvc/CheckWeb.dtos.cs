/* Options:
Date: 2015-11-23 10:41:20
Version: 4.00
BaseUrl: http://localhost:55799

//GlobalNamespace: 
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
using Check.ServiceModel;
using Check.ServiceInterface;
using Check.ServiceModel.Operations;
using Check.ServiceModel.Types;


namespace Check.ServiceInterface
{

    [Route("/api/acsprofiles", "POST,PUT,PATCH,DELETE")]
    [Route("/api/acsprofiles/{profileId}")]
    public partial class ACSProfile
        : IReturn<acsprofileResponse>
    {
        public virtual string profileId { get; set; }
        [Required]
        [StringLength(20)]
        public virtual string shortName { get; set; }

        [StringLength(60)]
        public virtual string longName { get; set; }

        [StringLength(20)]
        public virtual string regionId { get; set; }

        [StringLength(20)]
        public virtual string groupId { get; set; }

        [StringLength(12)]
        public virtual string deviceID { get; set; }

        public virtual DateTime lastUpdated { get; set; }
        public virtual bool enabled { get; set; }
        public virtual int Version { get; set; }
        public virtual string SessionId { get; set; }
    }

    public partial class acsprofileResponse
    {
        public virtual string profileId { get; set; }
    }

    [Route("/anontype")]
    public partial class AnonType
    {
    }

    public partial class BatchThrows
        : IReturn<BatchThrowsResponse>
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
    }

    public partial class BatchThrowsAsync
        : IReturn<BatchThrowsResponse>
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
    }

    public partial class BatchThrowsResponse
    {
        public virtual string Result { get; set; }
        public virtual ResponseStatus ResponseStatus { get; set; }
    }

    [Route("/changerequest/{Id}")]
    public partial class ChangeRequest
        : IReturn<ChangeRequestResponse>
    {
        public virtual string Id { get; set; }
    }

    public partial class ChangeRequestResponse
    {
        public virtual string ContentType { get; set; }
        public virtual string Header { get; set; }
        public virtual string QueryString { get; set; }
        public virtual string Form { get; set; }
        public virtual ResponseStatus ResponseStatus { get; set; }
    }

    public partial class CustomRockstar
    {
        [AutoQueryViewerField(Title="Name")]
        public virtual string FirstName { get; set; }

        [AutoQueryViewerField(HideInSummary=true)]
        public virtual string LastName { get; set; }

        public virtual int? Age { get; set; }
        [AutoQueryViewerField(Title="Album")]
        public virtual string RockstarAlbumName { get; set; }

        [AutoQueryViewerField(Title="Genre")]
        public virtual string RockstarGenreName { get; set; }
    }

    public partial class CustomUserSession
        : AuthUserSession
    {
        [DataMember]
        public virtual string CustomName { get; set; }

        [DataMember]
        public virtual string CustomInfo { get; set; }
    }

    [Route("{PathInfo*}")]
    public partial class FallbackRoute
    {
        public virtual string PathInfo { get; set; }
    }

    public partial class GetUserSession
        : IReturn<CustomUserSession>
    {
    }

    [Route("/info/{Id}")]
    public partial class Info
    {
        public virtual string Id { get; set; }
    }

    [Route("/Routing/LeadPost.aspx")]
    public partial class LegacyLeadPost
    {
        public virtual string LeadType { get; set; }
        public virtual int MyId { get; set; }
    }

    public partial class MetadataRequest
        : IReturn<AutoQueryMetadataResponse>
    {
        public virtual MetadataType MetadataType { get; set; }
    }

    public partial class Movie
    {
        public Movie()
        {
            Genres = new List<string>{};
        }

        public virtual int Id { get; set; }
        public virtual string ImdbId { get; set; }
        public virtual string Title { get; set; }
        public virtual string Rating { get; set; }
        public virtual decimal Score { get; set; }
        public virtual string Director { get; set; }
        public virtual DateTime ReleaseDate { get; set; }
        public virtual string TagLine { get; set; }
        public virtual List<string> Genres { get; set; }
    }

    [Route("/namedconnection")]
    public partial class NamedConnection
    {
        public virtual string EmailAddresses { get; set; }
    }

    public partial class NativeTypesTestService
    {

        public partial class HelloInService
        {
            public virtual string Name { get; set; }
        }
    }

    public partial class NoRepeat
        : IReturn<NoRepeatResponse>
    {
        public virtual Guid Id { get; set; }
    }

    public partial class NoRepeatResponse
    {
        public virtual Guid Id { get; set; }
    }

    [AutoQueryViewer(Title="Search for Rockstars", Description="Use this option to search for Rockstars!")]
    public partial class QueryCustomRockstars
        : QueryBase<Rockstar, CustomRockstar>, IReturn<QueryResponse<CustomRockstar>>
    {
        public virtual int? Age { get; set; }
    }

    public partial class QueryCustomRockstarsFilter
        : QueryBase<Rockstar, CustomRockstar>, IReturn<QueryResponse<CustomRockstar>>
    {
        public virtual int? Age { get; set; }
    }

    public partial class QueryFieldRockstars
        : QueryBase<Rockstar>, IReturn<QueryResponse<Rockstar>>
    {
        public QueryFieldRockstars()
        {
            FirstNames = new string[]{};
            FirstNameBetween = new string[]{};
        }

        public virtual string FirstName { get; set; }
        public virtual string[] FirstNames { get; set; }
        public virtual int? Age { get; set; }
        public virtual string FirstNameCaseInsensitive { get; set; }
        public virtual string FirstNameStartsWith { get; set; }
        public virtual string LastNameEndsWith { get; set; }
        public virtual string[] FirstNameBetween { get; set; }
        public virtual string OrLastName { get; set; }
    }

    public partial class QueryFieldRockstarsDynamic
        : QueryBase<Rockstar>, IReturn<QueryResponse<Rockstar>>
    {
        public virtual int? Age { get; set; }
    }

    public partial class QueryGetRockstars
        : QueryBase<Rockstar>, IReturn<QueryResponse<Rockstar>>
    {
        public QueryGetRockstars()
        {
            Ids = new int[]{};
            Ages = new List<int>{};
            FirstNames = new List<string>{};
            IdsBetween = new int[]{};
        }

        public virtual int[] Ids { get; set; }
        public virtual List<int> Ages { get; set; }
        public virtual List<string> FirstNames { get; set; }
        public virtual int[] IdsBetween { get; set; }
    }

    public partial class QueryGetRockstarsDynamic
        : QueryBase<Rockstar>, IReturn<QueryResponse<Rockstar>>
    {
    }

    [Route("/movies")]
    public partial class QueryMovies
        : QueryBase<Movie>, IReturn<QueryResponse<Movie>>
    {
        public QueryMovies()
        {
            Ids = new int[]{};
            ImdbIds = new string[]{};
            Ratings = new string[]{};
        }

        public virtual int[] Ids { get; set; }
        public virtual string[] ImdbIds { get; set; }
        public virtual string[] Ratings { get; set; }
    }

    [Route("/OrRockstars")]
    public partial class QueryOrRockstars
        : QueryBase<Rockstar>, IReturn<QueryResponse<Rockstar>>
    {
        public virtual int? Age { get; set; }
        public virtual string FirstName { get; set; }
    }

    public partial class QueryOverridedCustomRockstars
        : QueryBase<Rockstar, CustomRockstar>, IReturn<QueryResponse<CustomRockstar>>
    {
        public virtual int? Age { get; set; }
    }

    public partial class QueryOverridedRockstars
        : QueryBase<Rockstar>, IReturn<QueryResponse<Rockstar>>
    {
        public virtual int? Age { get; set; }
    }

    [Route("/customrockstars")]
    public partial class QueryRockstarAlbums
        : QueryBase<Rockstar, CustomRockstar>, IReturn<QueryResponse<CustomRockstar>>
    {
        public virtual int? Age { get; set; }
        public virtual string RockstarAlbumName { get; set; }
    }

    public partial class QueryRockstarAlbumsImplicit
        : QueryBase<Rockstar, CustomRockstar>, IReturn<QueryResponse<CustomRockstar>>
    {
    }

    public partial class QueryRockstarAlbumsLeftJoin
        : QueryBase<Rockstar, CustomRockstar>, IReturn<QueryResponse<CustomRockstar>>
    {
        public virtual int? Age { get; set; }
        public virtual string AlbumName { get; set; }
    }

    [Route("/query/rockstars")]
    public partial class QueryRockstars
        : QueryBase<Rockstar>, IReturn<QueryResponse<Rockstar>>
    {
        public virtual int? Age { get; set; }
    }

    public partial class QueryRockstarsConventions
        : QueryBase<Rockstar>, IReturn<QueryResponse<Rockstar>>
    {
        public QueryRockstarsConventions()
        {
            Ids = new int[]{};
        }

        public virtual int[] Ids { get; set; }
        public virtual int? AgeOlderThan { get; set; }
        public virtual int? AgeGreaterThanOrEqualTo { get; set; }
        public virtual int? AgeGreaterThan { get; set; }
        public virtual int? GreaterThanAge { get; set; }
        public virtual string FirstNameStartsWith { get; set; }
        public virtual string LastNameEndsWith { get; set; }
        public virtual string LastNameContains { get; set; }
        public virtual string RockstarAlbumNameContains { get; set; }
        public virtual int? RockstarIdAfter { get; set; }
        public virtual int? RockstarIdOnOrAfter { get; set; }
    }

    public partial class QueryRockstarsFilter
        : QueryBase<Rockstar>, IReturn<QueryResponse<Rockstar>>
    {
        public virtual int? Age { get; set; }
    }

    public partial class QueryRockstarsIFilter
        : QueryBase<Rockstar>, IReturn<QueryResponse<Rockstar>>
    {
        public virtual int? Age { get; set; }
    }

    [Route("/query/rockstar-references")]
    public partial class QueryRockstarsWithReferences
        : QueryBase<RockstarReference>, IReturn<QueryResponse<RockstarReference>>
    {
        public virtual int? Age { get; set; }
    }

    public partial class QueryUnknownRockstars
        : QueryBase<Rockstar>, IReturn<QueryResponse<Rockstar>>
    {
        public virtual int UnknownInt { get; set; }
        public virtual string UnknownProperty { get; set; }
    }

    public partial class RockstarAlbum
    {
        public virtual int Id { get; set; }
        public virtual int RockstarId { get; set; }
        public virtual string Name { get; set; }
    }

    public partial class RockstarReference
    {
        public RockstarReference()
        {
            Albums = new List<RockstarAlbum>{};
        }

        public virtual int Id { get; set; }
        public virtual string FirstName { get; set; }
        public virtual string LastName { get; set; }
        public virtual int? Age { get; set; }
        public virtual List<RockstarAlbum> Albums { get; set; }
    }

    [Route("/movies/search")]
    public partial class SearchMovies
        : QueryBase<Movie>, IReturn<QueryResponse<Movie>>
    {
    }

    public partial class StreamMovies
        : QueryBase<Movie>, IReturn<QueryResponse<Movie>>
    {
        public StreamMovies()
        {
            Ratings = new string[]{};
        }

        public virtual string[] Ratings { get; set; }
    }

    [Route("/testexecproc")]
    public partial class TestExecProc
    {
    }

    public partial class TestMiniverView
    {
    }
}

namespace Check.ServiceModel
{

    public partial class AsyncTest
        : IReturn<Echo>
    {
    }

    public partial class CachedEcho
    {
        public virtual bool Reload { get; set; }
        public virtual string Sentence { get; set; }
    }

    public partial class CustomFieldHttpError
        : IReturn<CustomFieldHttpErrorResponse>
    {
    }

    public partial class CustomFieldHttpErrorResponse
    {
        public virtual string Custom { get; set; }
        public virtual ResponseStatus ResponseStatus { get; set; }
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

    public partial class Echo
    {
        public virtual string Sentence { get; set; }
    }

    ///<summary>
    ///Echoes a sentence
    ///</summary>
    [Route("/echoes", "POST")]
    [Api("Echoes a sentence")]
    public partial class Echoes
        : IReturn<Echo>
    {
        [ApiMember(Description="The sentence to echo.", ParameterType="form", DataType="string", IsRequired=true, Name="Sentence")]
        public virtual string Sentence { get; set; }
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

    public partial class Issue221Base<T>
    {
        public virtual T Id { get; set; }
    }

    public partial class Issue221Long
        : Issue221Base<long>
    {
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

    public partial class MetadataTest
        : IReturn<MetadataTestResponse>
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

    [Route("/return404")]
    public partial class Return404
    {
    }

    [Route("/return404result")]
    public partial class Return404Result
    {
    }

    public partial class Rockstar
    {
        public virtual int Id { get; set; }
        public virtual string FirstName { get; set; }
        public virtual string LastName { get; set; }
        public virtual int? Age { get; set; }
    }

    [Route("/throw404")]
    [Route("/throw404/{Message}")]
    public partial class Throw404
    {
        public virtual string Message { get; set; }
    }

    [Route("/throwhttperror/{Status}")]
    public partial class ThrowHttpError
        : IReturn<ThrowHttpErrorResponse>
    {
        public virtual int Status { get; set; }
        public virtual string Message { get; set; }
    }

    public partial class ThrowHttpErrorResponse
    {
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
}

namespace Check.ServiceModel.Operations
{

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
        [ApiMember(Description="Range Description", ParameterType="path", DataType="double", IsRequired=true)]
        public virtual double Range { get; set; }
    }

    public partial class ArrayResult
    {
        public virtual string Result { get; set; }
    }

    [Flags]
    public enum EnumFlags
    {
        Value1 = 1,
        Value2 = 2,
        Value3 = 4,
    }

    public enum EnumType
    {
        Value1,
        Value2,
    }

    public enum EnumWithValues
    {
        Value1 = 1,
        Value2 = 2,
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

    ///<summary>
    ///Description on HelloAll type
    ///</summary>
    [DataContract]
    public partial class HelloAnnotated
        : IReturn<HelloAnnotatedResponse>
    {
        [DataMember]
        public virtual string Name { get; set; }
    }

    ///<summary>
    ///Description on HelloAllResponse type
    ///</summary>
    [DataContract]
    public partial class HelloAnnotatedResponse
    {
        [DataMember]
        public virtual string Result { get; set; }
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

    public partial class HelloExisting
        : IReturn<HelloExistingResponse>
    {
        public HelloExisting()
        {
            Names = new List<string>{};
        }

        public virtual List<string> Names { get; set; }
    }

    public partial class HelloExistingResponse
    {
        public HelloExistingResponse()
        {
            ArrayResults = new ArrayResult[]{};
            ListResults = new List<ListResult>{};
        }

        public virtual HelloList HelloList { get; set; }
        public virtual HelloArray HelloArray { get; set; }
        public virtual ArrayResult[] ArrayResults { get; set; }
        public virtual List<ListResult> ListResults { get; set; }
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

    ///<summary>
    ///Multi Line Class
    ///</summary>
    [Api("Multi Line Class")]
    public partial class HelloMultiline
    {
        [ApiMember(Description="Multi Line Property")]
        public virtual string Overflow { get; set; }
    }

    public partial class HelloResponse
    {
        public virtual string Result { get; set; }
    }

    public partial class HelloReturnList
        : IReturn<List<OnlyInReturnListArg>>
    {
        public HelloReturnList()
        {
            Names = new List<string>{};
        }

        public virtual List<string> Names { get; set; }
    }

    public partial class HelloString
        : IReturn<string>
    {
        public virtual string Name { get; set; }
    }

    public partial class HelloVoid
        : IReturnVoid
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
        public virtual EnumWithValues EnumWithValues { get; set; }
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

    public partial class HelloWithListInheritance
        : List<InheritedItem>
    {
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

    public partial class InheritedItem
    {
        public virtual string Name { get; set; }
    }

    public partial class ListResult
    {
        public virtual string Result { get; set; }
    }

    public partial class OnlyInReturnListArg
    {
        public virtual string Result { get; set; }
    }

    public partial class RestrictedAttributes
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual Hello Hello { get; set; }
    }
}

namespace Check.ServiceModel.Types
{

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
        : IReturn<AllTypes>
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
        [DataMember(Name="aliasedName")]
        public virtual string OriginalName { get; set; }
    }

    public partial class EmptyClass
    {
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

    public partial class ExcludeTest1
        : IReturn<ExcludeTestNested>
    {
    }

    public partial class ExcludeTest2
        : IReturn<string>
    {
        public virtual ExcludeTestNested ExcludeTestNested { get; set; }
    }

    public partial class ExcludeTestNested
    {
        public virtual int Id { get; set; }
    }

    public partial class HelloBase
    {
        public virtual int Id { get; set; }
    }

    public partial class HelloBuiltin
    {
        public virtual DayOfWeek DayOfWeek { get; set; }
    }

    public partial class HelloDelete
        : IReturn<HelloVerbResponse>, IDelete
    {
        public virtual int Id { get; set; }
    }

    public partial class HelloDictionary
        : IReturn<Dictionary<string, string>>
    {
        public virtual string Key { get; set; }
        public virtual string Value { get; set; }
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
        public virtual string Value { get; set; }
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

    public partial class HelloReserved
    {
        public virtual string Class { get; set; }
        public virtual string Type { get; set; }
        public virtual string extension { get; set; }
    }

    public partial class HelloResponseBase
    {
        public virtual int RefId { get; set; }
    }

    public partial class HelloReturnVoid
        : IReturnVoid
    {
        public virtual int Id { get; set; }
    }

    public partial class HelloSession
        : IReturn<HelloSessionResponse>
    {
    }

    public partial class HelloSessionResponse
    {
        public virtual AuthUserSession Result { get; set; }
    }

    public partial class HelloType
    {
        public virtual string Result { get; set; }
    }

    public partial class HelloVerbResponse
    {
        public virtual string Result { get; set; }
    }

    public partial class HelloWithReturnResponse
    {
        public virtual string Result { get; set; }
    }

    public partial interface IEmptyInterface
    {
    }

    public partial interface IPoco
    {
        string Name { get; set; }
    }

    public partial class Poco
    {
        public virtual string Name { get; set; }
    }

    [DataContract]
    public partial class QueryResponseTemplate<T>
    {
        public QueryResponseTemplate()
        {
            Results = new List<T>{};
            Meta = new Dictionary<string, string>{};
        }

        [DataMember(Order=1)]
        public virtual int Offset { get; set; }

        [DataMember(Order=2)]
        public virtual int Total { get; set; }

        [DataMember(Order=3)]
        public virtual List<T> Results { get; set; }

        [DataMember(Order=4)]
        public virtual Dictionary<string, string> Meta { get; set; }

        [DataMember(Order=5)]
        public virtual ResponseStatus ResponseStatus { get; set; }
    }

    public partial class QueryTemplate
        : IReturn<QueryResponseTemplate<Poco>>
    {
    }

    public partial class Request1
        : IReturn<Request1Response>
    {
        public virtual TypeA Test { get; set; }
    }

    public partial class Request1Response
    {
        public virtual TypeA Test { get; set; }
    }

    public partial class Request2
        : IReturn<Request2Response>
    {
        public virtual TypeA Test { get; set; }
    }

    public partial class Request2Response
    {
        public virtual TypeA Test { get; set; }
    }

    [DataContract]
    public enum ScopeType
    {
        Global = 1,
        Sale = 2,
    }

    public partial class SubType
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
    }

    public partial class TypeA
    {
        public TypeA()
        {
            Bar = new List<TypeB>{};
        }

        public virtual List<TypeB> Bar { get; set; }
    }

    public partial class TypeB
    {
        public virtual string Foo { get; set; }
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
}

