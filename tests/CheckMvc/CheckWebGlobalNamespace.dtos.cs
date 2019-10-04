/* Options:
Date: 2019-10-04 18:16:41
Version: 5.70
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
//AddNamespaces: 
//AddDefaultXmlNamespace: http://schemas.servicestack.net/types
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack;
using ServiceStack.DataAnnotations;
using System.IO;
using dtos;


namespace dtos
{

    [Route("/api/acsprofiles", "POST,PUT,PATCH,DELETE")]
    [Route("/api/acsprofiles/{profileId}")]
    [References(typeof(Check.ServiceInterface.acsprofileResponse))]
    [Serializable]
    public partial class ACSProfile
        : IReturn<acsprofileResponse>, IHasVersion, IHasSessionId
    {
        public virtual string profileId { get; set; }
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

    [Serializable]
    public partial class acsprofileResponse
    {
        public virtual string profileId { get; set; }
    }

    [Route("/anontype")]
    [Serializable]
    public partial class AnonType
    {
    }

    [Serializable]
    public partial class ArrayElementInDictionary
    {
        public virtual int Id { get; set; }
    }

    [Serializable]
    public partial class BatchThrows
        : IReturn<BatchThrowsResponse>
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
    }

    [Serializable]
    public partial class BatchThrowsAsync
        : IReturn<BatchThrowsResponse>
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
    }

    [Serializable]
    public partial class BatchThrowsResponse
    {
        public virtual string Result { get; set; }
        public virtual ResponseStatus ResponseStatus { get; set; }
    }

    [Route("/changerequest/{Id}")]
    [Serializable]
    public partial class ChangeRequest
        : IReturn<ChangeRequestResponse>
    {
        public virtual string Id { get; set; }
    }

    [Serializable]
    public partial class ChangeRequestResponse
    {
        public virtual string ContentType { get; set; }
        public virtual string Header { get; set; }
        public virtual string QueryString { get; set; }
        public virtual string Form { get; set; }
        public virtual ResponseStatus ResponseStatus { get; set; }
    }

    [Route("/compress/{Path*}")]
    [Serializable]
    public partial class CompressFile
    {
        public virtual string Path { get; set; }
    }

    [Route("/jwt")]
    [Serializable]
    public partial class CreateJwt
        : AuthUserSession, IReturn<CreateJwtResponse>, IMeta
    {
        public virtual DateTime? JwtExpiry { get; set; }
    }

    [Serializable]
    public partial class CreateJwtResponse
    {
        public virtual string Token { get; set; }
        public virtual ResponseStatus ResponseStatus { get; set; }
    }

    [Route("/jwt-refresh")]
    [Serializable]
    public partial class CreateRefreshJwt
        : IReturn<CreateRefreshJwtResponse>
    {
        public virtual string UserAuthId { get; set; }
        public virtual DateTime? JwtExpiry { get; set; }
    }

    [Serializable]
    public partial class CreateRefreshJwtResponse
    {
        public virtual string Token { get; set; }
        public virtual ResponseStatus ResponseStatus { get; set; }
    }

    [Serializable]
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

    [Serializable]
    public partial class CustomUserSession
        : AuthUserSession, IMeta
    {
        [DataMember]
        public virtual string CustomName { get; set; }

        [DataMember]
        public virtual string CustomInfo { get; set; }
    }

    [Serializable]
    public partial class DiscoverTypes
        : IReturn<DiscoverTypes>
    {
        public DiscoverTypes()
        {
            ElementInDictionary = new Dictionary<string, ArrayElementInDictionary[]>{};
        }

        public virtual Dictionary<string, ArrayElementInDictionary[]> ElementInDictionary { get; set; }
    }

    [Serializable]
    public partial class FallbackRoute
    {
        public virtual string PathInfo { get; set; }
    }

    [Route("/files/{Path*}")]
    [Serializable]
    public partial class GetFile
    {
        public virtual string Path { get; set; }
    }

    [Route("/Request1/", "GET")]
    [Serializable]
    public partial class GetRequest1
        : IReturn<List<ReturnedDto>>, IGet
    {
    }

    [Route("/Request3", "GET")]
    [Serializable]
    public partial class GetRequest2
        : IReturn<ReturnedDto>, IGet
    {
    }

    [Route("/timestamp", "GET")]
    [Serializable]
    public partial class GetTimestamp
        : IReturn<TimestampData>
    {
    }

    [Serializable]
    public partial class GetUserSession
        : IReturn<CustomUserSession>
    {
    }

    public partial interface IFilterRockstars
    {
    }

    [Route("/info/{Id}")]
    [Serializable]
    public partial class Info
    {
        public virtual string Id { get; set; }
    }

    [Route("/Routing/LeadPost.aspx")]
    [Serializable]
    public partial class LegacyLeadPost
    {
        public virtual string LeadType { get; set; }
        public virtual int MyId { get; set; }
    }

    [Route("/matchroute/html")]
    [Serializable]
    public partial class MatchesHtml
        : IReturn<MatchesHtml>
    {
        public virtual string Name { get; set; }
    }

    [Route("/matchregex/{Id}")]
    [Serializable]
    public partial class MatchesId
    {
        public virtual int Id { get; set; }
    }

    [Route("/matchroute/json")]
    [Serializable]
    public partial class MatchesJson
        : IReturn<MatchesJson>
    {
        public virtual string Name { get; set; }
    }

    [Route("/matchlast/{Id}")]
    [Serializable]
    public partial class MatchesLastInt
    {
        public virtual int Id { get; set; }
    }

    [Route("/matchlast/{Slug}")]
    [Serializable]
    public partial class MatchesNotLastInt
    {
        public virtual string Slug { get; set; }
    }

    [Route("/matchregex/{Slug}")]
    [Serializable]
    public partial class MatchesSlug
    {
        public virtual string Slug { get; set; }
    }

    [Serializable]
    public partial class MetadataRequest
        : IReturn<AutoQueryMetadataResponse>
    {
        public virtual MetadataType MetadataType { get; set; }
    }

    [Serializable]
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
    [Serializable]
    public partial class NamedConnection
    {
        public virtual string EmailAddresses { get; set; }
    }

    [Serializable]
    public partial class NativeTypesTestService
    {

        [Serializable]
        public partial class HelloInService
        {
            public virtual string Name { get; set; }
        }
    }

    [Serializable]
    public partial class NoRepeat
        : IReturn<NoRepeatResponse>
    {
        public virtual Guid Id { get; set; }
    }

    [Serializable]
    public partial class NoRepeatResponse
    {
        public virtual Guid Id { get; set; }
    }

    [Serializable]
    public partial class ObjectDesign
    {
        public virtual int Id { get; set; }
    }

    [Serializable]
    public partial class ObjectDesignResponse
    {
        public virtual ObjectDesign data { get; set; }
    }

    [Route("/code/object", "GET")]
    [Serializable]
    public partial class ObjectId
        : IReturn<ObjectDesignResponse>
    {
        public virtual string objectName { get; set; }
    }

    [Serializable]
    public partial class PgRockstar
        : Rockstar
    {
    }

    [AutoQueryViewer(Description="Use this option to search for Rockstars!", Title="Search for Rockstars")]
    [Serializable]
    public partial class QueryCustomRockstars
        : QueryDb<Rockstar, CustomRockstar>, IReturn<QueryResponse<CustomRockstar>>, IMeta
    {
        public virtual int? Age { get; set; }
    }

    [Serializable]
    public partial class QueryCustomRockstarsFilter
        : QueryDb<Rockstar, CustomRockstar>, IReturn<QueryResponse<CustomRockstar>>, IMeta
    {
        public virtual int? Age { get; set; }
    }

    [Route("/querydata/rockstars")]
    [Serializable]
    public partial class QueryDataRockstars
        : QueryData<Rockstar>, IReturn<QueryResponse<Rockstar>>, IMeta
    {
        public virtual int? Age { get; set; }
    }

    [Route("/query-custom/rockstars")]
    [Serializable]
    public partial class QueryFieldRockstars
        : QueryDb<Rockstar>, IReturn<QueryResponse<Rockstar>>, IMeta
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

    [Serializable]
    public partial class QueryFieldRockstarsDynamic
        : QueryDb<Rockstar>, IReturn<QueryResponse<Rockstar>>, IMeta
    {
        public virtual int? Age { get; set; }
    }

    [Serializable]
    public partial class QueryGetRockstars
        : QueryDb<Rockstar>, IReturn<QueryResponse<Rockstar>>, IMeta
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

    [Serializable]
    public partial class QueryGetRockstarsDynamic
        : QueryDb<Rockstar>, IReturn<QueryResponse<Rockstar>>, IMeta
    {
    }

    [Route("/movies")]
    [Serializable]
    public partial class QueryMovies
        : QueryDb<Movie>, IReturn<QueryResponse<Movie>>, IMeta
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
    [Serializable]
    public partial class QueryOrRockstars
        : QueryDb<Rockstar>, IReturn<QueryResponse<Rockstar>>, IMeta
    {
        public virtual int? Age { get; set; }
        public virtual string FirstName { get; set; }
    }

    [Serializable]
    public partial class QueryOverridedCustomRockstars
        : QueryDb<Rockstar, CustomRockstar>, IReturn<QueryResponse<CustomRockstar>>, IMeta
    {
        public virtual int? Age { get; set; }
    }

    [Serializable]
    public partial class QueryOverridedRockstars
        : QueryDb<Rockstar>, IReturn<QueryResponse<Rockstar>>, IMeta
    {
        public virtual int? Age { get; set; }
    }

    [Route("/pgsql/pgrockstars")]
    [Serializable]
    public partial class QueryPostgresPgRockstars
        : QueryDb<PgRockstar>, IReturn<QueryResponse<PgRockstar>>, IMeta
    {
        public virtual int? Age { get; set; }
    }

    [Route("/pgsql/rockstars")]
    [Serializable]
    public partial class QueryPostgresRockstars
        : QueryDb<Rockstar>, IReturn<QueryResponse<Rockstar>>, IMeta
    {
        public virtual int? Age { get; set; }
    }

    [Route("/query/requestlogs")]
    [Route("/query/requestlogs/{Date}")]
    [Serializable]
    public partial class QueryRequestLogs
        : QueryData<RequestLogEntry>, IReturn<QueryResponse<RequestLogEntry>>, IMeta
    {
        public virtual DateTime? Date { get; set; }
        public virtual bool ViewErrors { get; set; }
    }

    [Route("/customrockstars")]
    [Serializable]
    public partial class QueryRockstarAlbums
        : QueryDb<Rockstar, CustomRockstar>, IReturn<QueryResponse<CustomRockstar>>, IMeta
    {
        public virtual int? Age { get; set; }
        public virtual string RockstarAlbumName { get; set; }
    }

    [Serializable]
    public partial class QueryRockstarAlbumsImplicit
        : QueryDb<Rockstar, CustomRockstar>, IReturn<QueryResponse<CustomRockstar>>, IMeta
    {
    }

    [Serializable]
    public partial class QueryRockstarAlbumsLeftJoin
        : QueryDb<Rockstar, CustomRockstar>, IReturn<QueryResponse<CustomRockstar>>, IMeta
    {
        public virtual int? Age { get; set; }
        public virtual string AlbumName { get; set; }
    }

    [Route("/query/rockstars")]
    [Serializable]
    public partial class QueryRockstars
        : QueryDb<Rockstar>, IReturn<QueryResponse<Rockstar>>, IMeta
    {
        public virtual int? Age { get; set; }
    }

    [Route("/query/rockstars/cached")]
    [Serializable]
    public partial class QueryRockstarsCached
        : QueryDb<Rockstar>, IReturn<QueryResponse<Rockstar>>, IMeta
    {
        public virtual int? Age { get; set; }
    }

    [Serializable]
    public partial class QueryRockstarsConventions
        : QueryDb<Rockstar>, IReturn<QueryResponse<Rockstar>>, IMeta
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

    [Serializable]
    public partial class QueryRockstarsFilter
        : QueryDb<Rockstar>, IReturn<QueryResponse<Rockstar>>, IMeta
    {
        public virtual int? Age { get; set; }
    }

    [Serializable]
    public partial class QueryRockstarsIFilter
        : QueryDb<Rockstar>, IReturn<QueryResponse<Rockstar>>, IMeta, IFilterRockstars
    {
        public virtual int? Age { get; set; }
    }

    [Route("/query/rockstar-references")]
    [Serializable]
    public partial class QueryRockstarsWithReferences
        : QueryDb<RockstarReference>, IReturn<QueryResponse<RockstarReference>>, IMeta
    {
        public virtual int? Age { get; set; }
    }

    [Serializable]
    public partial class QueryUnknownRockstars
        : QueryDb<Rockstar>, IReturn<QueryResponse<Rockstar>>, IMeta
    {
        public virtual int UnknownInt { get; set; }
        public virtual string UnknownProperty { get; set; }
    }

    [Serializable]
    public partial class ReturnedDto
    {
        public virtual int Id { get; set; }
    }

    [Serializable]
    public partial class RockstarAlbum
    {
        public virtual int Id { get; set; }
        public virtual int RockstarId { get; set; }
        public virtual string Name { get; set; }
    }

    [Serializable]
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
    [Serializable]
    public partial class SearchMovies
        : QueryDb<Movie>, IReturn<QueryResponse<Movie>>, IMeta
    {
    }

    [Serializable]
    public partial class StreamMovies
        : QueryDb<Movie>, IReturn<QueryResponse<Movie>>, IMeta
    {
        public StreamMovies()
        {
            Ratings = new string[]{};
        }

        public virtual string[] Ratings { get; set; }
    }

    [Route("/test/errorview")]
    [Serializable]
    public partial class TestErrorView
    {
        public virtual string Id { get; set; }
    }

    [Route("/testexecproc")]
    [Serializable]
    public partial class TestExecProc
    {
    }

    [Serializable]
    public partial class TestMiniverView
    {
    }

    [Serializable]
    public partial class TimestampData
    {
        public virtual long Timestamp { get; set; }
    }

    [Serializable]
    public partial class TodayErrorLogs
        : QueryData<RequestLogEntry>, IReturn<QueryResponse<RequestLogEntry>>, IMeta
    {
    }

    [AutoQueryViewer(Name="Today\'s Logs", Title="Logs from Today")]
    [Serializable]
    public partial class TodayLogs
        : QueryData<RequestLogEntry>, IReturn<QueryResponse<RequestLogEntry>>, IMeta
    {
    }

    [Serializable]
    public partial class YesterdayErrorLogs
        : QueryData<RequestLogEntry>, IReturn<QueryResponse<RequestLogEntry>>, IMeta
    {
    }

    [Serializable]
    public partial class YesterdayLogs
        : QueryData<RequestLogEntry>, IReturn<QueryResponse<RequestLogEntry>>, IMeta
    {
    }

    [Route("/alwaysthrows")]
    [Serializable]
    public partial class AlwaysThrows
        : IReturn<AlwaysThrows>
    {
    }

    [Route("/alwaysthrowsfilterattribute")]
    [Serializable]
    public partial class AlwaysThrowsFilterAttribute
        : IReturn<AlwaysThrowsFilterAttribute>
    {
    }

    [Route("/alwaysthrowsglobalfilter")]
    [Serializable]
    public partial class AlwaysThrowsGlobalFilter
        : IReturn<AlwaysThrowsGlobalFilter>
    {
    }

    [Serializable]
    public partial class AsyncTest
        : IReturn<Echo>
    {
    }

    [Serializable]
    public partial class CachedEcho
        : IReturn<Echo>
    {
        public virtual bool Reload { get; set; }
        public virtual string Sentence { get; set; }
    }

    [Serializable]
    public partial class CustomFieldHttpError
        : IReturn<CustomFieldHttpErrorResponse>
    {
    }

    [Serializable]
    public partial class CustomFieldHttpErrorResponse
    {
        public virtual string Custom { get; set; }
        public virtual ResponseStatus ResponseStatus { get; set; }
    }

    [Serializable]
    public partial class CustomHttpError
        : IReturn<CustomHttpErrorResponse>
    {
        public virtual int StatusCode { get; set; }
        public virtual string StatusDescription { get; set; }
    }

    [Serializable]
    public partial class CustomHttpErrorResponse
    {
        public virtual string Custom { get; set; }
        public virtual ResponseStatus ResponseStatus { get; set; }
    }

    [Route("/dynamically/registered/{Name}")]
    [Serializable]
    public partial class DynamicallyRegistered
    {
        public virtual string Name { get; set; }
    }

    [Serializable]
    public partial class Echo
        : IEcho
    {
        public virtual string Sentence { get; set; }
    }

    ///<summary>
    ///Echoes a sentence
    ///</summary>
    [Route("/echoes", "POST")]
    [Api(Description="Echoes a sentence")]
    [Serializable]
    public partial class Echoes
        : IReturn<Echo>
    {
        ///<summary>
        ///The sentence to echo.
        ///</summary>
        [ApiMember(DataType="string", Description="The sentence to echo.", IsRequired=true, Name="Sentence", ParameterType="form")]
        public virtual string Sentence { get; set; }
    }

    [Serializable]
    public partial class ExcludeMetadataProperty
    {
        public virtual int Id { get; set; }
    }

    [Route("/example", "GET")]
    [DataContract]
    [Serializable]
    public partial class GetExample
        : IReturn<GetExampleResponse>
    {
    }

    [DataContract]
    [Serializable]
    public partial class GetExampleResponse
    {
        [DataMember(Order=1)]
        public virtual ResponseStatus ResponseStatus { get; set; }

        [DataMember(Order=2)]
        [ApiMember]
        public virtual MenuExample MenuExample1 { get; set; }
    }

    public partial interface IEcho
    {
        string Sentence { get; set; }
    }

    [DataContract]
    [Serializable]
    public partial class MenuExample
    {
        [DataMember(Order=1)]
        [ApiMember]
        public virtual MenuItemExample MenuItemExample1 { get; set; }
    }

    [Serializable]
    public partial class MenuItemExample
    {
        [DataMember(Order=1)]
        [ApiMember]
        public virtual string Name1 { get; set; }

        public virtual MenuItemExampleItem MenuItemExampleItem { get; set; }
    }

    [Serializable]
    public partial class MenuItemExampleItem
    {
        [DataMember(Order=1)]
        [ApiMember]
        public virtual string Name1 { get; set; }
    }

    [Serializable]
    public partial class MetadataTest
        : IReturn<MetadataTestResponse>
    {
        public virtual int Id { get; set; }
    }

    [Serializable]
    public partial class MetadataTestChild
    {
        public MetadataTestChild()
        {
            Results = new List<MetadataTestNestedChild>{};
        }

        public virtual string Name { get; set; }
        public virtual List<MetadataTestNestedChild> Results { get; set; }
    }

    [Serializable]
    public partial class MetadataTestNestedChild
    {
        public virtual string Name { get; set; }
    }

    [Serializable]
    public partial class MetadataTestResponse
    {
        public MetadataTestResponse()
        {
            Results = new List<MetadataTestChild>{};
        }

        public virtual int Id { get; set; }
        public virtual List<MetadataTestChild> Results { get; set; }
    }

    [Serializable]
    public partial class OnlyDefinedInGenericType
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
    }

    [Serializable]
    public partial class OnlyDefinedInGenericTypeFrom
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
    }

    [Serializable]
    public partial class OnlyDefinedInGenericTypeInto
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
    }

    [Serializable]
    public partial class QueryPocoBase
        : QueryDb<OnlyDefinedInGenericType>, IReturn<QueryResponse<OnlyDefinedInGenericType>>, IMeta
    {
        public virtual int Id { get; set; }
    }

    [Serializable]
    public partial class QueryPocoIntoBase
        : QueryDb<OnlyDefinedInGenericTypeFrom, OnlyDefinedInGenericTypeInto>, IReturn<QueryResponse<OnlyDefinedInGenericTypeInto>>, IMeta
    {
        public virtual int Id { get; set; }
    }

    [Serializable]
    public partial class RecursiveNode
        : IReturn<RecursiveNode>
    {
        public RecursiveNode()
        {
            Children = new RecursiveNode[]{};
        }

        public virtual int Id { get; set; }
        public virtual string Text { get; set; }
        public virtual RecursiveNode[] Children { get; set; }
    }

    [Route("/return404")]
    [Serializable]
    public partial class Return404
    {
    }

    [Route("/return404result")]
    [Serializable]
    public partial class Return404Result
    {
    }

    [Route("/return/bytes")]
    [Serializable]
    public partial class ReturnBytes
        : IReturn<byte[]>
    {
        public ReturnBytes()
        {
            Data = new byte[]{};
        }

        public virtual byte[] Data { get; set; }
    }

    [Route("/return/stream")]
    [Serializable]
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
    [Serializable]
    public partial class ReturnString
        : IReturn<string>
    {
        public virtual string Data { get; set; }
    }

    [Serializable]
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

    [Route("/swagger/range")]
    [Serializable]
    public partial class SwaggerRangeTest
    {
        public virtual string IntRange { get; set; }
        public virtual string DoubleRange { get; set; }
    }

    [Route("/{Version}/userdata", "GET")]
    [Serializable]
    public partial class SwaggerVersionTest
    {
        public virtual string Version { get; set; }
    }

    [Serializable]
    public partial class TestAttributeExport
        : IReturn<TestAttributeExport>
    {
        public virtual int UnitMeasKey { get; set; }
    }

    [Route("/throw404")]
    [Route("/throw404/{Message}")]
    [Serializable]
    public partial class Throw404
    {
        public virtual string Message { get; set; }
    }

    [Route("/throwhttperror/{Status}")]
    [Serializable]
    public partial class ThrowHttpError
        : IReturn<ThrowHttpErrorResponse>
    {
        public virtual int Status { get; set; }
        public virtual string Message { get; set; }
    }

    [Serializable]
    public partial class ThrowHttpErrorResponse
    {
    }

    [Route("/throw/{Type}")]
    [Serializable]
    public partial class ThrowType
        : IReturn<ThrowTypeResponse>
    {
        public virtual string Type { get; set; }
        public virtual string Message { get; set; }
    }

    [Serializable]
    public partial class ThrowTypeResponse
    {
        public virtual ResponseStatus ResponseStatus { get; set; }
    }

    [Route("/throwvalidation")]
    [Serializable]
    public partial class ThrowValidation
        : IReturn<ThrowValidationResponse>
    {
        public virtual int Age { get; set; }
        public virtual string Required { get; set; }
        public virtual string Email { get; set; }
    }

    [Serializable]
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
    [ApiResponse(Description="Your request was not understood", StatusCode=400)]
    [DataContract]
    [Serializable]
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

    [Serializable]
    public partial class ArrayResult
    {
        public virtual string Result { get; set; }
    }

    public enum EnumAsInt
    {
        Value1 = 1000,
        Value2 = 2000,
        Value3 = 3000,
    }

    [Flags]
    public enum EnumFlags
    {
        Value0 = 0,
        [EnumMember(Value="Value 1")]
        Value1 = 1,
        Value2 = 2,
        Value3 = 4,
        Value123 = 7,
    }

    public enum EnumStyle
    {
        lower,
        UPPER,
        PascalCase,
        camelCase,
        camelUPPER,
        PascalUPPER,
    }

    public enum EnumStyleMembers
    {
        [EnumMember(Value="lower")]
        Lower,
        [EnumMember(Value="UPPER")]
        Upper,
        PascalCase,
        [EnumMember(Value="camelCase")]
        CamelCase,
        [EnumMember(Value="camelUPPER")]
        CamelUpper,
        [EnumMember(Value="PascalUPPER")]
        PascalUpper,
    }

    public enum EnumType
    {
        Value1,
        Value2,
        Value3,
    }

    [Flags]
    public enum EnumTypeFlags
    {
        Value1 = 0,
        Value2 = 1,
        Value3 = 2,
    }

    public enum EnumWithValues
    {
        None,
        [EnumMember(Value="Member 1")]
        Value1,
        Value2,
    }

    [Route("/hello")]
    [Route("/hello/{Name}")]
    [Serializable]
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
    [Serializable]
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
    [Serializable]
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

    [Serializable]
    public partial class HelloAllTypes
        : IReturn<HelloAllTypesResponse>
    {
        public virtual string Name { get; set; }
        public virtual AllTypes AllTypes { get; set; }
        public virtual AllCollectionTypes AllCollectionTypes { get; set; }
    }

    [Serializable]
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
    [Serializable]
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
    [Serializable]
    public partial class HelloAnnotatedResponse
    {
        [DataMember]
        public virtual string Result { get; set; }
    }

    [Serializable]
    public partial class HelloArray
        : IReturn<ArrayResult[]>
    {
        public HelloArray()
        {
            Names = new List<string>{};
        }

        public virtual List<string> Names { get; set; }
    }

    ///<summary>
    ///Multi Line Class
    ///</summary>
    [Api(Description="Multi \r\nLine \r\nClass")]
    [Serializable]
    public partial class HelloAttributeStringTest
    {
        ///<summary>
        ///Multi Line Property
        ///</summary>
        [ApiMember(Description="Multi \r\nLine \r\nProperty")]
        public virtual string Overflow { get; set; }

        ///<summary>
        ///Some \ escaped 	  chars
        ///</summary>
        [ApiMember(Description="Some \\ escaped \t \n chars")]
        public virtual string EscapedChars { get; set; }
    }

    [Serializable]
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

    [Serializable]
    public partial class HelloExisting
        : IReturn<HelloExistingResponse>
    {
        public HelloExisting()
        {
            Names = new List<string>{};
        }

        public virtual List<string> Names { get; set; }
    }

    [Serializable]
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

    [Serializable]
    public partial class HelloList
        : IReturn<List<ListResult>>
    {
        public HelloList()
        {
            Names = new List<string>{};
        }

        public virtual List<string> Names { get; set; }
    }

    [Serializable]
    public partial class HelloResponse
    {
        public virtual string Result { get; set; }
    }

    [Serializable]
    public partial class HelloReturnList
        : IReturn<List<OnlyInReturnListArg>>
    {
        public HelloReturnList()
        {
            Names = new List<string>{};
        }

        public virtual List<string> Names { get; set; }
    }

    [Serializable]
    public partial class HelloString
        : IReturn<string>
    {
        public virtual string Name { get; set; }
    }

    [Serializable]
    public partial class HelloVoid
        : IReturnVoid
    {
        public virtual string Name { get; set; }
    }

    [Serializable]
    public partial class HelloWithAlternateReturnResponse
        : HelloWithReturnResponse
    {
        public virtual string AltResult { get; set; }
    }

    [DataContract]
    [Serializable]
    public partial class HelloWithDataContract
        : IReturn<HelloWithDataContractResponse>
    {
        [DataMember(Name="name", Order=1, IsRequired=true, EmitDefaultValue=false)]
        public virtual string Name { get; set; }

        [DataMember(Name="id", Order=2, EmitDefaultValue=false)]
        public virtual int Id { get; set; }
    }

    [DataContract]
    [Serializable]
    public partial class HelloWithDataContractResponse
    {
        [DataMember(Name="result", Order=1, IsRequired=true, EmitDefaultValue=false)]
        public virtual string Result { get; set; }
    }

    ///<summary>
    ///Description on HelloWithDescription type
    ///</summary>
    [Serializable]
    public partial class HelloWithDescription
        : IReturn<HelloWithDescriptionResponse>
    {
        public virtual string Name { get; set; }
    }

    ///<summary>
    ///Description on HelloWithDescriptionResponse type
    ///</summary>
    [Serializable]
    public partial class HelloWithDescriptionResponse
    {
        public virtual string Result { get; set; }
    }

    [Serializable]
    public partial class HelloWithEnum
    {
        public virtual EnumType EnumProp { get; set; }
        public virtual EnumTypeFlags EnumTypeFlags { get; set; }
        public virtual EnumWithValues EnumWithValues { get; set; }
        public virtual EnumType? NullableEnumProp { get; set; }
        public virtual EnumFlags EnumFlags { get; set; }
        public virtual EnumAsInt EnumAsInt { get; set; }
        public virtual EnumStyle EnumStyle { get; set; }
        public virtual EnumStyleMembers EnumStyleMembers { get; set; }
    }

    [Serializable]
    public partial class HelloWithEnumList
    {
        public HelloWithEnumList()
        {
            EnumProp = new List<EnumType>{};
            EnumWithValues = new List<EnumWithValues>{};
            NullableEnumProp = new List<Nullable<EnumType>>{};
            EnumFlags = new List<EnumFlags>{};
            EnumStyle = new List<EnumStyle>{};
        }

        public virtual List<EnumType> EnumProp { get; set; }
        public virtual List<EnumWithValues> EnumWithValues { get; set; }
        public virtual List<Nullable<EnumType>> NullableEnumProp { get; set; }
        public virtual List<EnumFlags> EnumFlags { get; set; }
        public virtual List<EnumStyle> EnumStyle { get; set; }
    }

    [Serializable]
    public partial class HelloWithEnumMap
    {
        public HelloWithEnumMap()
        {
            EnumProp = new Dictionary<EnumType, EnumType>{};
            EnumWithValues = new Dictionary<EnumWithValues, EnumWithValues>{};
            NullableEnumProp = new Dictionary<Nullable<EnumType>, Nullable<EnumType>>{};
            EnumFlags = new Dictionary<EnumFlags, EnumFlags>{};
            EnumStyle = new Dictionary<EnumStyle, EnumStyle>{};
        }

        public virtual Dictionary<EnumType, EnumType> EnumProp { get; set; }
        public virtual Dictionary<EnumWithValues, EnumWithValues> EnumWithValues { get; set; }
        public virtual Dictionary<Nullable<EnumType>, Nullable<EnumType>> NullableEnumProp { get; set; }
        public virtual Dictionary<EnumFlags, EnumFlags> EnumFlags { get; set; }
        public virtual Dictionary<EnumStyle, EnumStyle> EnumStyle { get; set; }
    }

    [Serializable]
    public partial class HelloWithGenericInheritance
        : HelloBase<Poco>
    {
        public virtual string Result { get; set; }
    }

    [Serializable]
    public partial class HelloWithGenericInheritance2
        : HelloBase<Hello>
    {
        public virtual string Result { get; set; }
    }

    [Serializable]
    public partial class HelloWithInheritance
        : HelloBase, IReturn<HelloWithInheritanceResponse>
    {
        public virtual string Name { get; set; }
    }

    [Serializable]
    public partial class HelloWithInheritanceResponse
        : HelloResponseBase
    {
        public virtual string Result { get; set; }
    }

    [Serializable]
    public partial class HelloWithListInheritance
        : List<InheritedItem>
    {
    }

    [Serializable]
    public partial class HelloWithNestedClass
        : IReturn<HelloResponse>
    {
        public virtual string Name { get; set; }
        public virtual HelloWithNestedClass.NestedClass NestedClassProp { get; set; }

        [Serializable]
        public partial class NestedClass
        {
            public virtual string Value { get; set; }
        }
    }

    [Serializable]
    public partial class HelloWithNestedInheritance
        : HelloBase<HelloWithNestedInheritance.Item>
    {

        [Serializable]
        public partial class Item
        {
            public virtual string Value { get; set; }
        }
    }

    [Serializable]
    public partial class HelloWithReturn
        : IReturn<HelloWithAlternateReturnResponse>
    {
        public virtual string Name { get; set; }
    }

    [Route("/helloroute")]
    [Serializable]
    public partial class HelloWithRoute
        : IReturn<HelloWithRouteResponse>
    {
        public virtual string Name { get; set; }
    }

    [Serializable]
    public partial class HelloWithRouteResponse
    {
        public virtual string Result { get; set; }
    }

    [Serializable]
    public partial class HelloWithType
        : IReturn<HelloWithTypeResponse>
    {
        public virtual string Name { get; set; }
    }

    [Serializable]
    public partial class HelloWithTypeResponse
    {
        public virtual HelloType Result { get; set; }
    }

    [Serializable]
    public partial class InheritedItem
    {
        public virtual string Name { get; set; }
    }

    [Serializable]
    public partial class ListResult
    {
        public virtual string Result { get; set; }
    }

    [Serializable]
    public partial class OnlyInReturnListArg
    {
        public virtual string Result { get; set; }
    }

    [Serializable]
    public partial class RestrictedAttributes
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual Hello Hello { get; set; }
    }

    [Serializable]
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

    [Serializable]
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

    [Serializable]
    public partial class EmptyClass
    {
    }

    [Serializable]
    public partial class EnumRequest
        : IReturn<EnumResponse>, IPut
    {
        public virtual ScopeType Operator { get; set; }
    }

    [Serializable]
    public partial class EnumResponse
    {
        public virtual ScopeType Operator { get; set; }
    }

    [Serializable]
    public partial class ExcludeTest1
        : IReturn<ExcludeTestNested>
    {
    }

    [Serializable]
    public partial class ExcludeTest2
        : IReturn<string>
    {
        public virtual ExcludeTestNested ExcludeTestNested { get; set; }
    }

    [Serializable]
    public partial class ExcludeTestNested
    {
        public virtual int Id { get; set; }
    }

    [Serializable]
    public partial class HelloAuthenticated
        : IReturn<HelloAuthenticatedResponse>, IHasSessionId
    {
        public virtual string SessionId { get; set; }
        public virtual int Version { get; set; }
    }

    [Serializable]
    public partial class HelloAuthenticatedResponse
    {
        public virtual int Version { get; set; }
        public virtual string SessionId { get; set; }
        public virtual string UserName { get; set; }
        public virtual string Email { get; set; }
        public virtual bool IsAuthenticated { get; set; }
        public virtual ResponseStatus ResponseStatus { get; set; }
    }

    [Serializable]
    public partial class HelloBase
    {
        public virtual int Id { get; set; }
    }

    [Serializable]
    public partial class HelloBuiltin
    {
        public virtual DayOfWeek DayOfWeek { get; set; }
        public virtual ShortDays ShortDays { get; set; }
    }

    [Serializable]
    public partial class HelloDelete
        : IReturn<HelloVerbResponse>, IDelete
    {
        public virtual int Id { get; set; }
    }

    [Serializable]
    public partial class HelloDictionary
        : IReturn<Dictionary<string, string>>
    {
        public virtual string Key { get; set; }
        public virtual string Value { get; set; }
    }

    [Serializable]
    public partial class HelloGet
        : IReturn<HelloVerbResponse>, IGet
    {
        public virtual int Id { get; set; }
    }

    [Serializable]
    public partial class HelloImplementsInterface
        : IReturn<HelloImplementsInterface>, ImplementsPoco
    {
        public virtual string Name { get; set; }
    }

    [Serializable]
    public partial class HelloInnerTypes
        : IReturn<HelloInnerTypesResponse>
    {
    }

    [Serializable]
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

    [Serializable]
    public partial class HelloInterface
    {
        public virtual IPoco Poco { get; set; }
        public virtual IEmptyInterface EmptyInterface { get; set; }
        public virtual EmptyClass EmptyClass { get; set; }
        public virtual string Value { get; set; }
    }

    [Serializable]
    public partial class HelloPatch
        : IReturn<HelloVerbResponse>, IPatch
    {
        public virtual int Id { get; set; }
    }

    [Serializable]
    public partial class HelloPost
        : HelloBase, IReturn<HelloVerbResponse>, IPost
    {
    }

    [Serializable]
    public partial class HelloPut
        : IReturn<HelloVerbResponse>, IPut
    {
        public virtual int Id { get; set; }
    }

    [Serializable]
    public partial class HelloReserved
    {
        public virtual string Class { get; set; }
        public virtual string Type { get; set; }
        public virtual string extension { get; set; }
    }

    [Serializable]
    public partial class HelloResponseBase
    {
        public virtual int RefId { get; set; }
    }

    [Serializable]
    public partial class HelloReturnVoid
        : IReturnVoid
    {
        public virtual int Id { get; set; }
    }

    [Serializable]
    public partial class HelloSession
        : IReturn<HelloSessionResponse>
    {
    }

    [Serializable]
    public partial class HelloSessionResponse
    {
        public virtual AuthUserSession Result { get; set; }
    }

    [Serializable]
    public partial class HelloStruct
        : IReturn<HelloStruct>
    {
        public virtual string Point { get; set; }
        public virtual string NullablePoint { get; set; }
    }

    [Serializable]
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

    [Serializable]
    public partial class HelloType
    {
        public virtual string Result { get; set; }
    }

    [Serializable]
    public partial class HelloVerbResponse
    {
        public virtual string Result { get; set; }
    }

    [Serializable]
    public partial class HelloWithReturnResponse
    {
        public virtual string Result { get; set; }
    }

    public partial interface IEmptyInterface
    {
    }

    public partial interface ImplementsPoco
    {
        string Name { get; set; }
    }

    public partial interface IPoco
    {
        string Name { get; set; }
    }

    [Serializable]
    public partial class Poco
    {
        public virtual string Name { get; set; }
    }

    [DataContract]
    [Serializable]
    public partial class QueryResponseTemplate<T>
        : IMeta
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

    [Serializable]
    public partial class QueryTemplate
        : IReturn<QueryResponseTemplate<Poco>>
    {
    }

    [Serializable]
    public partial class Request1
        : IReturn<Request1Response>
    {
        public virtual TypeA Test { get; set; }
    }

    [Serializable]
    public partial class Request1Response
    {
        public virtual TypeA Test { get; set; }
    }

    [Serializable]
    public partial class Request2
        : IReturn<Request2Response>
    {
        public virtual TypeA Test { get; set; }
    }

    [Serializable]
    public partial class Request2Response
    {
        public virtual TypeA Test { get; set; }
    }

    [Serializable]
    public partial class RestrictInternal
        : IReturn<RestrictInternal>
    {
        public virtual int Id { get; set; }
    }

    [Serializable]
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

    [DataContract]
    public enum ShortDays
    {
        [EnumMember(Value="MON")]
        Monday,
        [EnumMember(Value="TUE")]
        Tuesday,
        [EnumMember(Value="WED")]
        Wednesday,
        [EnumMember(Value="THU")]
        Thursday,
        [EnumMember(Value="FRI")]
        Friday,
        [EnumMember(Value="SAT")]
        Saturday,
        [EnumMember(Value="SUN")]
        Sunday,
    }

    [Serializable]
    public partial class SubType
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
    }

    [Serializable]
    public partial class TypeA
    {
        public TypeA()
        {
            Bar = new List<TypeB>{};
        }

        public virtual List<TypeB> Bar { get; set; }
    }

    [Serializable]
    public partial class TypeB
    {
        public virtual string Foo { get; set; }
    }

    [Serializable]
    public partial class TypesGroup
    {

        [Serializable]
        public partial class InnerType
        {
            public virtual long Id { get; set; }
            public virtual string Name { get; set; }
        }

        [Serializable]
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

    [Route("/surveys/{surveyId}/sendouts/{sendoutId}/respondents", "POST")]
    [Serializable]
    public partial class AddRespondentRequest
        : IReturn<AddRespondentResponse>
    {
        public AddRespondentRequest()
        {
            backgroundData = new Dictionary<int, string>{};
        }

        ///<summary>
        ///Remarks: SurveyId of requested Survey.
        ///</summary>
        [ApiMember(DataType="integer", Description="Remarks: SurveyId of requested Survey.", Format="int32", IsRequired=true, Name="surveyId", ParameterType="path")]
        public virtual int surveyId { get; set; }

        ///<summary>
        ///Remarks: SendoutId of Sendout to which a respondent is added.
        ///</summary>
        [ApiMember(DataType="integer", Description="Remarks: SendoutId of Sendout to which a respondent is added.", Format="int32", IsRequired=true, Name="sendoutId", ParameterType="path")]
        public virtual int sendoutId { get; set; }

        ///<summary>
        ///Remarks: Valid email address, SMS recipient or login identifier.
        ///</summary>
        [ApiMember(DataType="string", Description="Remarks: Valid email address, SMS recipient or login identifier.", Name="contactDetails", ParameterType="query")]
        public virtual string contactDetails { get; set; }

        ///<summary>
        ///Remarks: Indicates whether Netigate should send the survey link to the respondent, or if you distribute it yourself.
        ///</summary>
        [ApiMember(DataType="boolean", Description="Remarks: Indicates whether Netigate should send the survey link to the respondent, or if you distribute it yourself.", Name="sendMail", ParameterType="query")]
        public virtual bool sendMail { get; set; }

        ///<summary>
        ///Remarks: Key = BGDataLabelId, Value = respondent's background data (not empty or null)
        ///</summary>
        [ApiMember(Description="Remarks: Key = BGDataLabelId, Value = respondent\'s background data (not empty or null)", Name="backgroundData", ParameterType="query")]
        public virtual Dictionary<int, string> backgroundData { get; set; }
    }

    [Serializable]
    public partial class AddRespondentResponse
    {
        public virtual int RespondentId { get; set; }
        public virtual string Email { get; set; }
        public virtual string Password { get; set; }
        public virtual string SurveyURL { get; set; }
    }

    [Route("/defaultview/action")]
    [Serializable]
    public partial class DefaultViewActionAttr
    {
    }

    [Route("/defaultview/class")]
    [Serializable]
    public partial class DefaultViewAttr
    {
    }

    [Route("/gzip/{FileName}")]
    [Serializable]
    public partial class DownloadGzipFile
        : IReturn<byte[]>
    {
        public virtual string FileName { get; set; }
    }

    [Route("/lists", "GET")]
    [Serializable]
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
    [Serializable]
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
    [Serializable]
    public partial class GetSwaggerExamples
        : IReturn<GetSwaggerExamples>
    {
        public virtual string Get { get; set; }
    }

    [Route("/httpresult-dto")]
    [Serializable]
    public partial class HttpResultDto
        : IReturn<HttpResultDto>
    {
        public virtual string Name { get; set; }
    }

    [Route("/index")]
    [Serializable]
    public partial class IndexPage
    {
        public virtual string PathInfo { get; set; }
    }

    [Serializable]
    public partial class InProcRequest1
    {
    }

    [Serializable]
    public partial class InProcRequest2
    {
    }

    [Route("/match/{Language*}")]
    [Serializable]
    public partial class MatchLang
        : IReturn<HelloResponse>
    {
        public virtual string Language { get; set; }
    }

    [Route("/match/{Language}/{Name*}")]
    [Serializable]
    public partial class MatchName
        : IReturn<HelloResponse>
    {
        public virtual string Language { get; set; }
        public virtual string Name { get; set; }
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

    [Route("/plain-dto")]
    [Serializable]
    public partial class PlainDto
        : IReturn<PlainDto>
    {
        public virtual string Name { get; set; }
    }

    ///<summary>
    ///Api POST
    ///</summary>
    [Route("/swaggerexamples", "POST")]
    [Api(Description="Api POST")]
    [Serializable]
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
    [Serializable]
    public partial class PutSwaggerExample
        : IReturn<PutSwaggerExample>
    {
        public virtual int Id { get; set; }
        public virtual string Get { get; set; }
    }

    [Route("/query/alltypes")]
    [Serializable]
    public partial class QueryAllTypes
        : QueryDb<AllTypes>, IReturn<QueryResponse<AllTypes>>, IMeta
    {
    }

    [Route("/reqlogstest/{Name}")]
    [Serializable]
    public partial class RequestLogsTest
        : IReturn<string>
    {
        public virtual string Name { get; set; }
    }

    [Route("/return/text")]
    [Serializable]
    public partial class ReturnText
    {
        public virtual string Text { get; set; }
    }

    [Route("/set-cache")]
    [Serializable]
    public partial class SetCache
        : IReturn<SetCache>
    {
        public virtual string ETag { get; set; }
        public virtual TimeSpan? Age { get; set; }
        public virtual TimeSpan? MaxAge { get; set; }
        public virtual DateTime? Expires { get; set; }
        public virtual DateTime? LastModified { get; set; }
        public virtual CacheControl? CacheControl { get; set; }
    }

    [Route("/swagger-complex", "POST")]
    [Serializable]
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

    [Serializable]
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

    [Route("/swagger/model")]
    [Serializable]
    public partial class SwaggerModel
        : IReturn<SwaggerModel>
    {
        public virtual int Int { get; set; }
        public virtual string String { get; set; }
        public virtual DateTime DateTime { get; set; }
        public virtual DateTimeOffset DateTimeOffset { get; set; }
        public virtual TimeSpan TimeSpan { get; set; }
    }

    [Route("/swagger/multiattrtest", "POST")]
    [ApiResponse(Description="Code 1", StatusCode=400)]
    [ApiResponse(Description="Code 2", StatusCode=402)]
    [ApiResponse(Description="Code 3", StatusCode=401)]
    [Serializable]
    public partial class SwaggerMultiApiResponseTest
        : IReturnVoid
    {
    }

    [Serializable]
    public partial class SwaggerNestedModel
    {
        ///<summary>
        ///NestedProperty description
        ///</summary>
        [ApiMember(Description="NestedProperty description")]
        public virtual bool NestedProperty { get; set; }
    }

    [Serializable]
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
    [Serializable]
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
    [Serializable]
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
    [ApiResponse(Description="Your request was not understood", StatusCode=400)]
    [ApiResponse(Description="Oops, something broke", StatusCode=500)]
    [DataContract]
    [Serializable]
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
    [Serializable]
    public partial class SwaggerTest2
    {
        [ApiMember]
        public virtual MyEnum MyEnumProperty { get; set; }

        [ApiMember(DataType="string", IsRequired=true, Name="Token", ParameterType="header")]
        public virtual string Token { get; set; }
    }

    [Route("/test/html")]
    [Serializable]
    public partial class TestHtml
        : IReturn<TestHtml>
    {
        public virtual string Name { get; set; }
    }

    [Route("/test/html2")]
    [Serializable]
    public partial class TestHtml2
    {
        public virtual string Name { get; set; }
    }

    [Route("/restrict/mq")]
    [Serializable]
    public partial class TestMqRestriction
        : IReturn<TestMqRestriction>
    {
        public virtual string Name { get; set; }
    }

    [Route("/views/request")]
    [Serializable]
    public partial class ViewRequest
        : IReturn<ViewResponse>
    {
        public virtual string Name { get; set; }
    }

    [Serializable]
    public partial class ViewResponse
    {
        public virtual string Result { get; set; }
    }
}

