﻿/* Options:
Version: 1
BaseUrl: http://localhost:55799

ServerVersion: 1
MakePartial: True
MakeVirtual: True
MakeDataContractsExtensible: False
AddReturnMarker: True
AddDescriptionAsComments: True
AddDataContractAttributes: False
AddIndexesToDataMembers: False
AddResponseStatus: False
AddImplicitVersion: 
InitializeCollections: True
AddDefaultXmlNamespace: http://schemas.servicestack.net/types
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack;
using ServiceStack.DataAnnotations;
using Check.ServiceModel;
using Check.ServiceModel.Operations;
using Check.ServiceModel.Types;
using Check.ServiceInterface;


namespace Check.ServiceInterface
{

    [Route("/api/acsprofiles/{profileId}")]
    [Route("/api/acsprofiles", "POST,PUT,PATCH,DELETE")]
    public partial class ACSProfile
        : IReturn<acsprofileResponse>
    {
        public virtual string profileId { get; set; }
        [StringLength(20)]
        [Required]
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
    }

    public partial class acsprofileResponse
    {
        public virtual string profileId { get; set; }
    }

    [Route("/anontype")]
    public partial class AnonType
    {
    }

    [Route("/changerequest/{Id}")]
    public partial class ChangeRequest
        : IReturn<ChangeRequest>
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
        public virtual string FirstName { get; set; }
        public virtual string LastName { get; set; }
        public virtual int? Age { get; set; }
        public virtual string RockstarAlbumName { get; set; }
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
}

namespace Check.ServiceModel
{

    public partial class AsyncTest
        : IReturn<Echo>
    {
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

    public partial class Rockstar
    {
        public virtual int Id { get; set; }
        public virtual string FirstName { get; set; }
        public virtual string LastName { get; set; }
        public virtual int? Age { get; set; }
    }

    [Route("/throwhttperror/{Status}")]
    public partial class ThrowHttpError
    {
        public virtual int Status { get; set; }
        public virtual string Message { get; set; }
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
        [Required]
        [Default(5)]
        public virtual int Id { get; set; }

        [DataMember(Name="Aliased")]
        [ApiMember(Description="Range Description", ParameterType="path", DataType="double", IsRequired=true)]
        public virtual double Range { get; set; }

        [StringLength(20)]
        [Meta("Foo", "Bar")]
        [References(typeof(Check.ServiceModel.Operations.Hello))]
        public virtual string Name { get; set; }
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

    [Route("/hello/{Name}")]
    public partial class Hello
        : IReturn<Hello>
    {
        public virtual string Name { get; set; }
    }

    public partial class HelloAllTypes
        : IReturn<HelloAllTypes>
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

    public partial class HelloResponse
    {
        public virtual string Result { get; set; }
    }

    public partial class HelloString
    {
        public virtual string Name { get; set; }
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
        : IReturn<HelloWithDataContract>
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
        : IReturn<HelloWithDescription>
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
        : HelloBase, IReturn<HelloWithInheritance>
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
        public virtual NestedClass NestedClassProp { get; set; }

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
        : IReturn<HelloWithRoute>
    {
        public virtual string Name { get; set; }
    }

    public partial class HelloWithRouteResponse
    {
        public virtual string Result { get; set; }
    }

    public partial class HelloWithType
        : IReturn<HelloWithType>
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
        }

        public virtual int[] IntArray { get; set; }
        public virtual List<int> IntList { get; set; }
        public virtual string[] StringArray { get; set; }
        public virtual List<string> StringList { get; set; }
        public virtual Poco[] PocoArray { get; set; }
        public virtual List<Poco> PocoList { get; set; }
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


