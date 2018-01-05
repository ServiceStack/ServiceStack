/* Options:
Date: 2017-06-23 03:04:21
Version: 4.512
Tip: To override a DTO option, remove "//" prefix before updating
BaseUrl: http://localhost:55799

GlobalNamespace: dtos
//MakePartial: True
//MakeVirtual: True
//MakeInternal: False
//MakeDataContractsExtensible: False
//AddReturnMarker: True
//AddDescriptionAsComments: True
//AddDataContractAttributes: False
//AddIndexesToDataMembers: False
//AddGeneratedCodeAttributes: False
//AddResponseStatus: False
//AddImplicitVersion: 
//InitializeCollections: True
//ExportValueTypes: False
//IncludeTypes: 
//ExcludeTypes: 
AddNamespaces: System.Net,Item=dtos.HelloWithNestedInheritance.Item
//AddDefaultXmlNamespace: http://schemas.servicestack.net/types
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack;
using ServiceStack.DataAnnotations;
using System.Net;
using Item=dtos.HelloWithNestedInheritance.Item;
using System.IO;
using dtos;


namespace dtos
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

    [Route("/compress/{Path*}")]
    public partial class CompressFile
    {
        public virtual string Path { get; set; }
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

    [Route("/files/{Path*}")]
    public partial class GetFile
    {
        public virtual string Path { get; set; }
    }

    [Route("/Request1/", "GET")]
    public partial class GetRequest1
        : IReturn<List<ReturnedDto>>, IGet
    {
    }

    [Route("/Request3", "GET")]
    public partial class GetRequest2
        : IReturn<ReturnedDto>, IGet
    {
    }

    [Route("/timestamp", "GET")]
    public partial class GetTimestamp
        : IReturn<TimestampData>
    {
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

    public partial class ObjectDesign
    {
        public virtual int Id { get; set; }
    }

    public partial class ObjectDesignResponse
    {
        public virtual ObjectDesign data { get; set; }
    }

    [Route("/code/object", "GET")]
    public partial class ObjectId
        : IReturn<ObjectDesignResponse>
    {
        public virtual string objectName { get; set; }
    }

    public partial class PgRockstar
        : Rockstar
    {
    }

    [AutoQueryViewer(Description="Use this option to search for Rockstars!", Title="Search for Rockstars")]
    public partial class QueryCustomRockstars
        : QueryDb<Rockstar, CustomRockstar>, IReturn<QueryResponse<CustomRockstar>>
    {
        public virtual int? Age { get; set; }
    }

    public partial class QueryCustomRockstarsFilter
        : QueryDb<Rockstar, CustomRockstar>, IReturn<QueryResponse<CustomRockstar>>
    {
        public virtual int? Age { get; set; }
    }

    [Route("/querydata/rockstars")]
    public partial class QueryDataRockstars
        : QueryData<Rockstar>, IReturn<QueryResponse<Rockstar>>
    {
        public virtual int? Age { get; set; }
    }

    [Route("/query-custom/rockstars")]
    public partial class QueryFieldRockstars
        : QueryDb<Rockstar>, IReturn<QueryResponse<Rockstar>>
    {
        public QueryFieldRockstars()
        {
            FirstNames = new string[]{};
            FirstNameBetween = new string[]{};
            FirstNameContainsMulti = new string[]{};
        }

        public virtual string FirstName { get; set; }
        public virtual string[] FirstNames { get; set; }
        public virtual int? Age { get; set; }
        public virtual string FirstNameCaseInsensitive { get; set; }
        public virtual string FirstNameStartsWith { get; set; }
        public virtual string LastNameEndsWith { get; set; }
        public virtual string[] FirstNameBetween { get; set; }
        public virtual string OrLastName { get; set; }
        public virtual string[] FirstNameContainsMulti { get; set; }
    }

    public partial class QueryFieldRockstarsDynamic
        : QueryDb<Rockstar>, IReturn<QueryResponse<Rockstar>>
    {
        public virtual int? Age { get; set; }
    }

    public partial class QueryGetRockstars
        : QueryDb<Rockstar>, IReturn<QueryResponse<Rockstar>>
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
        : QueryDb<Rockstar>, IReturn<QueryResponse<Rockstar>>
    {
    }

    [Route("/movies")]
    public partial class QueryMovies
        : QueryDb<Movie>, IReturn<QueryResponse<Movie>>
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
        : QueryDb<Rockstar>, IReturn<QueryResponse<Rockstar>>
    {
        public virtual int? Age { get; set; }
        public virtual string FirstName { get; set; }
    }

    public partial class QueryOverridedCustomRockstars
        : QueryDb<Rockstar, CustomRockstar>, IReturn<QueryResponse<CustomRockstar>>
    {
        public virtual int? Age { get; set; }
    }

    public partial class QueryOverridedRockstars
        : QueryDb<Rockstar>, IReturn<QueryResponse<Rockstar>>
    {
        public virtual int? Age { get; set; }
    }

    [Route("/pgsql/pgrockstars")]
    public partial class QueryPostgresPgRockstars
        : QueryDb<PgRockstar>, IReturn<QueryResponse<PgRockstar>>
    {
        public virtual int? Age { get; set; }
    }

    [Route("/pgsql/rockstars")]
    public partial class QueryPostgresRockstars
        : QueryDb<Rockstar>, IReturn<QueryResponse<Rockstar>>
    {
        public virtual int? Age { get; set; }
    }

    [Route("/query/requestlogs")]
    [Route("/query/requestlogs/{Date}")]
    public partial class QueryRequestLogs
        : QueryData<RequestLogEntry>, IReturn<QueryResponse<RequestLogEntry>>
    {
        public virtual DateTime? Date { get; set; }
        public virtual bool ViewErrors { get; set; }
    }

    [Route("/customrockstars")]
    public partial class QueryRockstarAlbums
        : QueryDb<Rockstar, CustomRockstar>, IReturn<QueryResponse<CustomRockstar>>
    {
        public virtual int? Age { get; set; }
        public virtual string RockstarAlbumName { get; set; }
    }

    public partial class QueryRockstarAlbumsImplicit
        : QueryDb<Rockstar, CustomRockstar>, IReturn<QueryResponse<CustomRockstar>>
    {
    }

    public partial class QueryRockstarAlbumsLeftJoin
        : QueryDb<Rockstar, CustomRockstar>, IReturn<QueryResponse<CustomRockstar>>
    {
        public virtual int? Age { get; set; }
        public virtual string AlbumName { get; set; }
    }

    [Route("/query/rockstars")]
    public partial class QueryRockstars
        : QueryDb<Rockstar>, IReturn<QueryResponse<Rockstar>>
    {
        public virtual int? Age { get; set; }
    }

    public partial class QueryRockstarsConventions
        : QueryDb<Rockstar>, IReturn<QueryResponse<Rockstar>>
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
        : QueryDb<Rockstar>, IReturn<QueryResponse<Rockstar>>
    {
        public virtual int? Age { get; set; }
    }

    public partial class QueryRockstarsIFilter
        : QueryDb<Rockstar>, IReturn<QueryResponse<Rockstar>>
    {
        public virtual int? Age { get; set; }
    }

    [Route("/query/rockstar-references")]
    public partial class QueryRockstarsWithReferences
        : QueryDb<RockstarReference>, IReturn<QueryResponse<RockstarReference>>
    {
        public virtual int? Age { get; set; }
    }

    public partial class QueryUnknownRockstars
        : QueryDb<Rockstar>, IReturn<QueryResponse<Rockstar>>
    {
        public virtual int UnknownInt { get; set; }
        public virtual string UnknownProperty { get; set; }
    }

    public partial class ReturnedDto
    {
        public virtual int Id { get; set; }
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
        : QueryDb<Movie>, IReturn<QueryResponse<Movie>>
    {
    }

    public partial class StreamMovies
        : QueryDb<Movie>, IReturn<QueryResponse<Movie>>
    {
        public StreamMovies()
        {
            Ratings = new string[]{};
        }

        public virtual string[] Ratings { get; set; }
    }

    [Route("/test/errorview")]
    public partial class TestErrorView
    {
        public virtual string Id { get; set; }
    }

    [Route("/testexecproc")]
    public partial class TestExecProc
    {
    }

    public partial class TestMiniverView
    {
    }

    public partial class TimestampData
    {
        public virtual long Timestamp { get; set; }
    }

    public partial class TodayErrorLogs
        : QueryData<RequestLogEntry>, IReturn<QueryResponse<RequestLogEntry>>
    {
    }

    public partial class TodayLogs
        : QueryData<RequestLogEntry>, IReturn<QueryResponse<RequestLogEntry>>
    {
    }

    public partial class YesterdayErrorLogs
        : QueryData<RequestLogEntry>, IReturn<QueryResponse<RequestLogEntry>>
    {
    }

    public partial class YesterdayLogs
        : QueryData<RequestLogEntry>, IReturn<QueryResponse<RequestLogEntry>>
    {
    }

    [Route("/alwaysthrows")]
    public partial class AlwaysThrows
        : IReturn<AlwaysThrows>
    {
    }

    [Route("/alwaysthrowsfilterattribute")]
    public partial class AlwaysThrowsFilterAttribute
        : IReturn<AlwaysThrowsFilterAttribute>
    {
    }

    [Route("/alwaysthrowsglobalfilter")]
    public partial class AlwaysThrowsGlobalFilter
        : IReturn<AlwaysThrowsGlobalFilter>
    {
    }

    public partial class AsyncTest
        : IReturn<Echo>
    {
    }

    public partial class CachedEcho
        : IReturn<Echo>
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

    [Route("/dynamically/registered/{Name}")]
    public partial class DynamicallyRegistered
    {
        public virtual string Name { get; set; }
    }

    public partial class Echo
    {
        public virtual string Sentence { get; set; }
    }

    ///<summary>
    ///Echoes a sentence
    ///</summary>
    [Route("/echoes", "POST")]
    [Api(Description="Echoes a sentence")]
    public partial class Echoes
        : IReturn<Echo>
    {
        ///<summary>
        ///The sentence to echo.
        ///</summary>
        [ApiMember(DataType="string", Description="The sentence to echo.", IsRequired=true, Name="Sentence", ParameterType="form")]
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
        : QueryDb<OnlyDefinedInGenericType>, IReturn<QueryResponse<OnlyDefinedInGenericType>>
    {
        public virtual int Id { get; set; }
    }

    public partial class QueryPocoIntoBase
        : QueryDb<OnlyDefinedInGenericTypeFrom, OnlyDefinedInGenericTypeInto>, IReturn<QueryResponse<OnlyDefinedInGenericTypeInto>>
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

    [Route("/return/bytes")]
    public partial class ReturnBytes
        : IReturn<byte[]>
    {
        public ReturnBytes()
        {
            Data = new byte[]{};
        }

        public virtual byte[] Data { get; set; }
    }

    [Route("/return/httpwebresponse")]
    public partial class ReturnHttpWebResponse
        : IReturn<HttpWebResponse>
    {
        public ReturnHttpWebResponse()
        {
            Data = new byte[]{};
        }

        public virtual byte[] Data { get; set; }
    }

    [Route("/return/stream")]
    public partial class ReturnStream
        : IReturn<Stream>
    {
        public ReturnStream()
        {
            Data = new byte[]{};
        }

        public virtual byte[] Data { get; set; }
    }

    [Route("/return/string")]
    public partial class ReturnString
        : IReturn<string>
    {
        public virtual string Data { get; set; }
    }

    public partial class Rockstar
    {
        ///<summary>
        ///Идентификатор
        ///</summary>
        public virtual int Id { get; set; }
        ///<summary>
        ///Фамилия
        ///</summary>
        public virtual string FirstName { get; set; }
        ///<summary>
        ///Имя
        ///</summary>
        public virtual string LastName { get; set; }
        ///<summary>
        ///Возраст
        ///</summary>
        public virtual int? Age { get; set; }
    }

    [Route("/{Version}/userdata", "GET")]
    public partial class SwaggerVersionTest
    {
        public virtual string Version { get; set; }
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

    ///<summary>
    ///AllowedAttributes Description
    ///</summary>
    [Route("/allowed-attributes", "GET")]
    [Api(Description="AllowedAttributes Description")]
    [ApiResponse(400, "Your request was not understood")]
    [DataContract]
    public partial class AllowedAttributes
    {
        [DataMember]
        [Required]
        public virtual int Id { get; set; }

        ///<summary>
        ///Range Description
        ///</summary>
        [DataMember(Name="Aliased")]
        [ApiMember(DataType="double", Description="Range Description", IsRequired=true, ParameterType="path")]
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

    ///<summary>
    ///Description for HelloACodeGenTest
    ///</summary>
    public partial class HelloACodeGenTest
        : IReturn<HelloACodeGenTestResponse>
    {
        public HelloACodeGenTest()
        {
            SecondFields = new List<string>{};
        }

        ///<summary>
        ///Description for FirstField
        ///</summary>
        public virtual int FirstField { get; set; }
        public virtual List<string> SecondFields { get; set; }
    }

    [DataContract]
    public partial class HelloACodeGenTestResponse
    {
        ///<summary>
        ///Description for FirstResult
        ///</summary>
        [DataMember]
        public virtual int FirstResult { get; set; }

        ///<summary>
        ///Description for SecondResult
        ///</summary>
        [DataMember]
        [ApiMember(Description="Description for SecondResult")]
        public virtual int SecondResult { get; set; }
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
    [Api(Description="Multi Line Class")]
    public partial class HelloMultiline
    {
        ///<summary>
        ///Multi Line Property
        ///</summary>
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
        : HelloBase<Item>
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
            NullableByteArray = new Nullable<Byte>[]{};
            NullableByteList = new List<Nullable<Byte>>{};
            NullableDateTimeArray = new Nullable<DateTime>[]{};
            NullableDateTimeList = new List<Nullable<DateTime>>{};
            PocoLookup = new Dictionary<string, List<Poco>>{};
            PocoLookupMap = new Dictionary<string, List<Dictionary<String,Poco>>>{};
        }

        public virtual int[] IntArray { get; set; }
        public virtual List<int> IntList { get; set; }
        public virtual string[] StringArray { get; set; }
        public virtual List<string> StringList { get; set; }
        public virtual Poco[] PocoArray { get; set; }
        public virtual List<Poco> PocoList { get; set; }
        public virtual Nullable<Byte>[] NullableByteArray { get; set; }
        public virtual List<Nullable<Byte>> NullableByteList { get; set; }
        public virtual Nullable<DateTime>[] NullableDateTimeArray { get; set; }
        public virtual List<Nullable<DateTime>> NullableDateTimeList { get; set; }
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
        public virtual KeyValuePair<string, string> KeyValuePair { get; set; }
        public virtual DateTime? NullableDateTime { get; set; }
        public virtual TimeSpan? NullableTimeSpan { get; set; }
        public virtual List<string> StringList { get; set; }
        public virtual string[] StringArray { get; set; }
        public virtual Dictionary<string, string> StringMap { get; set; }
        public virtual Dictionary<int, string> IntStringMap { get; set; }
        public virtual SubType SubType { get; set; }
        public virtual string Point { get; set; }
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
        public HelloInnerTypesResponse()
        {
            InnerList = new List<TypesGroup.InnerTypeItem>{};
        }

        public virtual TypesGroup.InnerType InnerType { get; set; }
        public virtual TypesGroup.InnerEnum InnerEnum { get; set; }
        public virtual List<TypesGroup.InnerTypeItem> InnerList { get; set; }
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

    public partial class HelloStruct
        : IReturn<HelloStruct>
    {
        public virtual string Point { get; set; }
        public virtual string NullablePoint { get; set; }
    }

    public partial class HelloTuple
        : IReturn<HelloTuple>
    {
        public HelloTuple()
        {
            Tuples2 = new List<Tuple<String,Int64>>{};
            Tuples3 = new List<Tuple<String,Int64,Boolean>>{};
        }

        public virtual Tuple<string, long> Tuple2 { get; set; }
        public virtual Tuple<string, long, bool> Tuple3 { get; set; }
        public virtual List<Tuple<String,Int64>> Tuples2 { get; set; }
        public virtual List<Tuple<String,Int64,Boolean>> Tuples3 { get; set; }
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

    public partial class RestrictInternal
        : IReturn<RestrictInternal>
    {
        public virtual int Id { get; set; }
    }

    public partial class RestrictLocalhost
        : IReturn<RestrictLocalhost>
    {
        public virtual int Id { get; set; }
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

        public partial class InnerTypeItem
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

    [Route("/lists", "GET")]
    public partial class GetLists
        : IReturn<GetLists>
    {
        public virtual string Id { get; set; }
    }

    ///<summary>
    ///Api GET Id
    ///</summary>
    [Route("/swaggerexamples/{Id}", "GET")]
    [Api(Description="Api GET Id")]
    public partial class GetSwaggerExample
        : IReturn<GetSwaggerExample>
    {
        public virtual int Id { get; set; }
        public virtual string Get { get; set; }
    }

    ///<summary>
    ///Api GET All
    ///</summary>
    [Route("/swaggerexamples", "GET")]
    [Api(Description="Api GET All")]
    public partial class GetSwaggerExamples
        : IReturn<GetSwaggerExamples>
    {
        public virtual string Get { get; set; }
    }

    [Route("/index")]
    public partial class IndexPage
    {
        public virtual string PathInfo { get; set; }
    }

    public enum MyColor
    {
        Red,
        Green,
        Blue,
    }

    public enum MyEnum
    {
        A,
        B,
        C,
    }

    ///<summary>
    ///Api POST
    ///</summary>
    [Route("/swaggerexamples", "POST")]
    [Api(Description="Api POST")]
    public partial class PostSwaggerExamples
        : IReturn<PostSwaggerExamples>
    {
        public virtual string Post { get; set; }
    }

    ///<summary>
    ///Api PUT Id
    ///</summary>
    [Route("/swaggerexamples/{Id}", "PUT")]
    [Api(Description="Api PUT Id")]
    public partial class PutSwaggerExample
        : IReturn<PutSwaggerExample>
    {
        public virtual int Id { get; set; }
        public virtual string Get { get; set; }
    }

    [Route("/swagger-complex", "POST")]
    public partial class SwaggerComplex
        : IReturn<SwaggerComplexResponse>
    {
        public SwaggerComplex()
        {
            ArrayString = new string[]{};
            ArrayInt = new int[]{};
            ListString = new List<string>{};
            ListInt = new List<int>{};
            DictionaryString = new Dictionary<string, string>{};
        }

        [DataMember]
        [ApiMember]
        public virtual bool IsRequired { get; set; }

        [DataMember]
        [ApiMember(IsRequired=true)]
        public virtual string[] ArrayString { get; set; }

        [DataMember]
        [ApiMember]
        public virtual int[] ArrayInt { get; set; }

        [DataMember]
        [ApiMember]
        public virtual List<string> ListString { get; set; }

        [DataMember]
        [ApiMember]
        public virtual List<int> ListInt { get; set; }

        [DataMember]
        [ApiMember]
        public virtual Dictionary<string, string> DictionaryString { get; set; }
    }

    public partial class SwaggerComplexResponse
    {
        public SwaggerComplexResponse()
        {
            ArrayString = new string[]{};
            ArrayInt = new int[]{};
            ListString = new List<string>{};
            ListInt = new List<int>{};
            DictionaryString = new Dictionary<string, string>{};
        }

        [DataMember]
        [ApiMember]
        public virtual bool IsRequired { get; set; }

        [DataMember]
        [ApiMember(IsRequired=true)]
        public virtual string[] ArrayString { get; set; }

        [DataMember]
        [ApiMember]
        public virtual int[] ArrayInt { get; set; }

        [DataMember]
        [ApiMember]
        public virtual List<string> ListString { get; set; }

        [DataMember]
        [ApiMember]
        public virtual List<int> ListInt { get; set; }

        [DataMember]
        [ApiMember]
        public virtual Dictionary<string, string> DictionaryString { get; set; }
    }

    [Route("/swagger/multiattrtest", "POST")]
    [ApiResponse(400, "Code 1")]
    [ApiResponse(402, "Code 2")]
    [ApiResponse(401, "Code 3")]
    public partial class SwaggerMultiApiResponseTest
        : IReturnVoid
    {
    }

    public partial class SwaggerNestedModel
    {
        ///<summary>
        ///NestedProperty description
        ///</summary>
        [ApiMember(Description="NestedProperty description")]
        public virtual bool NestedProperty { get; set; }
    }

    public partial class SwaggerNestedModel2
    {
        ///<summary>
        ///NestedProperty2 description
        ///</summary>
        [ApiMember(Description="NestedProperty2 description")]
        public virtual bool NestedProperty2 { get; set; }

        ///<summary>
        ///MultipleValues description
        ///</summary>
        [ApiMember(Description="MultipleValues description")]
        public virtual string MultipleValues { get; set; }

        ///<summary>
        ///TestRange description
        ///</summary>
        [ApiMember(Description="TestRange description")]
        public virtual int TestRange { get; set; }
    }

    [Route("/swaggerpost/{Required1}", "GET")]
    [Route("/swaggerpost/{Required1}/{Optional1}", "GET")]
    [Route("/swaggerpost", "POST")]
    public partial class SwaggerPostTest
        : IReturn<HelloResponse>
    {
        [ApiMember(Verb="POST")]
        [ApiMember(ParameterType="path", Route="/swaggerpost/{Required1}", Verb="GET")]
        [ApiMember(ParameterType="path", Route="/swaggerpost/{Required1}/{Optional1}", Verb="GET")]
        public virtual string Required1 { get; set; }

        [ApiMember(Verb="POST")]
        [ApiMember(ParameterType="path", Route="/swaggerpost/{Required1}/{Optional1}", Verb="GET")]
        public virtual string Optional1 { get; set; }
    }

    [Route("/swaggerpost2/{Required1}/{Required2}", "GET")]
    [Route("/swaggerpost2/{Required1}/{Required2}/{Optional1}", "GET")]
    [Route("/swaggerpost2", "POST")]
    public partial class SwaggerPostTest2
        : IReturn<HelloResponse>
    {
        [ApiMember(ParameterType="path", Route="/swaggerpost2/{Required1}/{Required2}", Verb="GET")]
        [ApiMember(ParameterType="path", Route="/swaggerpost2/{Required1}/{Required2}/{Optional1}", Verb="GET")]
        public virtual string Required1 { get; set; }

        [ApiMember(ParameterType="path", Route="/swaggerpost2/{Required1}/{Required2}", Verb="GET")]
        [ApiMember(ParameterType="path", Route="/swaggerpost2/{Required1}/{Required2}/{Optional1}", Verb="GET")]
        public virtual string Required2 { get; set; }

        [ApiMember(ParameterType="path", Route="/swaggerpost2/{Required1}/{Required2}/{Optional1}", Verb="GET")]
        public virtual string Optional1 { get; set; }
    }

    ///<summary>
    ///SwaggerTest Service Description
    ///</summary>
    [Route("/swagger", "GET")]
    [Route("/swagger/{Name}", "GET")]
    [Route("/swagger/{Name}", "POST")]
    [Api(Description="SwaggerTest Service Description")]
    [ApiResponse(400, "Your request was not understood")]
    [ApiResponse(500, "Oops, something broke")]
    [DataContract]
    public partial class SwaggerTest
    {
        public SwaggerTest()
        {
            MyDateBetween = new DateTime[]{};
        }

        ///<summary>
        ///Color Description
        ///</summary>
        [DataMember]
        [ApiMember(DataType="string", Description="Color Description", IsRequired=true, ParameterType="path")]
        public virtual string Name { get; set; }

        [DataMember]
        [ApiMember]
        public virtual MyColor Color { get; set; }

        ///<summary>
        ///Aliased Description
        ///</summary>
        [DataMember(Name="Aliased")]
        [ApiMember(DataType="string", Description="Aliased Description", IsRequired=true)]
        public virtual string Original { get; set; }

        ///<summary>
        ///Not Aliased Description
        ///</summary>
        [DataMember]
        [ApiMember(DataType="string", Description="Not Aliased Description", IsRequired=true)]
        public virtual string NotAliased { get; set; }

        ///<summary>
        ///Format as password
        ///</summary>
        [DataMember]
        [ApiMember(DataType="password", Description="Format as password")]
        public virtual string Password { get; set; }

        [DataMember]
        [ApiMember(AllowMultiple=true)]
        public virtual DateTime[] MyDateBetween { get; set; }

        ///<summary>
        ///Nested model 1
        ///</summary>
        [DataMember]
        [ApiMember(DataType="SwaggerNestedModel", Description="Nested model 1")]
        public virtual SwaggerNestedModel NestedModel1 { get; set; }

        ///<summary>
        ///Nested model 2
        ///</summary>
        [DataMember]
        [ApiMember(DataType="SwaggerNestedModel2", Description="Nested model 2")]
        public virtual SwaggerNestedModel2 NestedModel2 { get; set; }
    }

    [Route("/swaggertest2", "POST")]
    public partial class SwaggerTest2
    {
        [ApiMember]
        public virtual MyEnum MyEnumProperty { get; set; }

        [ApiMember(DataType="string", IsRequired=true, Name="Token", ParameterType="header")]
        public virtual string Token { get; set; }
    }

    [Route("/test/html")]
    public partial class TestHtml
    {
        public virtual string Name { get; set; }
    }
}

