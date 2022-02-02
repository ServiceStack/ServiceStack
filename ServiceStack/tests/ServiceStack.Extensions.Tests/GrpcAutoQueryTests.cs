using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Funq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Grpc.Client;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.Logging;
using ServiceStack.OrmLite;
using ServiceStack.Script;
using ServiceStack.Text;
using ServiceStack.Validation;

namespace ServiceStack.Extensions.Tests
{
    [DataContract]
    public class Rockstar
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public string FirstName { get; set; }
        [DataMember(Order = 3)]
        public string LastName { get; set; }
        [DataMember(Order = 4)]
        public int? Age { get; set; }
        [DataMember(Order = 5)]
        public DateTime DateOfBirth { get; set; }
        [DataMember(Order = 6)]
        public DateTime? DateDied { get; set; }
        [DataMember(Order = 7)]
        public LivingStatus LivingStatus { get; set; }
    }
    
    public enum LivingStatus
    {
        Alive,
        Dead
    }
    
    [DataContract]
    public class PagingTest
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public string Name { get; set; }
        [DataMember(Order = 3)]
        public int Value { get; set; }
    }


    [Alias("Rockstar")]
    [NamedConnection("SqlServer")]
    [DataContract]
    public class NamedRockstar : Rockstar { }

    [Route("/query/namedrockstars")]
    [DataContract]
    public class QueryNamedRockstars : QueryDb<NamedRockstar>
    {
        [DataMember(Order = 1)]
        public int? Age { get; set; }
    }

    [Route("/query/rockstars")]
    [DataContract, Id(10), Tag(Keywords.Dynamic)]
    public class QueryRockstars : QueryDb<Rockstar>
    {
        [DataMember(Order = 1)]
        public int? Age { get; set; }
    }

    [Route("/query/rockstaralbums")]
    [DataContract]
    public class QueryRockstarAlbums : QueryDb<RockstarAlbum>
    {
        [DataMember(Order = 1)]
        public int? Id { get; set; }
        [DataMember(Order = 2)]
        public int? RockstarId { get; set; }
        [DataMember(Order = 3)]
        public string Name { get; set; }
        [DataMember(Order = 4)]
        public string Genre { get; set; }
        [DataMember(Order = 5)]
        public int[] IdBetween { get; set; }
    }

    [Route("/query/pagingtest")]
    [DataContract]
    public class QueryPagingTest : QueryDb<PagingTest>
    {
        [DataMember(Order = 1)]
        public int? Id { get; set; }
        [DataMember(Order = 2)]
        public string Name { get; set; }
        [DataMember(Order = 3)]
        public int? Value { get; set; }
    }

    [DataContract]
    public class QueryRockstarsConventions : QueryDb<Rockstar>
    {
        [DataMember(Order = 1)]
        public DateTime? DateOfBirthGreaterThan { get; set; }
        [DataMember(Order = 2)]
        public DateTime? DateDiedLessThan { get; set; }
        [DataMember(Order = 3)]
        public int[] Ids { get; set; }
        [DataMember(Order = 4)]
        public int? AgeOlderThan { get; set; }
        [DataMember(Order = 5)]
        public int? AgeGreaterThanOrEqualTo { get; set; }
        [DataMember(Order = 6)]
        public int? AgeGreaterThan { get; set; }
        [DataMember(Order = 7)]
        public int? GreaterThanAge { get; set; }
        [DataMember(Order = 8)]
        public string FirstNameStartsWith { get; set; }
        [DataMember(Order = 9)]
        public string LastNameEndsWith { get; set; }
        [DataMember(Order = 10)]
        public string LastNameContains { get; set; }
        [DataMember(Order = 11)]
        public string RockstarAlbumNameContains { get; set; }
        [DataMember(Order = 12)]
        public int? RockstarIdAfter { get; set; }
        [DataMember(Order = 13)]
        public int? RockstarIdOnOrAfter { get; set; }
    }

    [DataContract]
    public class QueryCustomRockstars : QueryDb<Rockstar, CustomRockstar>
    {
        [DataMember(Order = 1)]
        public int? Age { get; set; }
    }

    [Route("/customrockstars")]
    [DataContract]
    public class QueryJoinedRockstarAlbums : QueryDb<Rockstar, CustomRockstar>, IJoin<Rockstar, RockstarAlbum>
    {
        [DataMember(Order = 1)]
        public int? Age { get; set; }
        [DataMember(Order = 2)]
        public string RockstarAlbumName { get; set; }
    }

    [DataContract]
    public class QueryRockstarAlbumsImplicit : QueryDb<Rockstar, CustomRockstar>, IJoin<Rockstar, RockstarAlbum>
    {
    }

    [DataContract]
    public class QueryRockstarAlbumsLeftJoin : QueryDb<Rockstar, CustomRockstar>, ILeftJoin<Rockstar, RockstarAlbum>
    {
        [DataMember(Order = 1)]
        public int? Age { get; set; }
        [DataMember(Order = 2)]
        public string AlbumName { get; set; }
        [DataMember(Order = 3)]
        public int? IdNotEqualTo { get; set; }
    }

    [DataContract]
    public class QueryRockstarAlbumsCustomLeftJoin : QueryDb<Rockstar, CustomRockstar>
    {
        [DataMember(Order = 1)]
        public int? Age { get; set; }
        [DataMember(Order = 2)]
        public string AlbumName { get; set; }
        [DataMember(Order = 3)]
        public int? IdNotEqualTo { get; set; }
    }

    [DataContract]
    public class QueryMultiJoinRockstar : QueryDb<Rockstar, CustomRockstar>, 
        IJoin<Rockstar, RockstarAlbum>,
        IJoin<Rockstar, RockstarGenre>
    {
        [DataMember(Order = 1)]
        public int? Age { get; set; }
        [DataMember(Order = 2)]
        public string RockstarAlbumName { get; set; }
        [DataMember(Order = 3)]
        public string RockstarGenreName { get; set; }
    }

    [DataContract]
    public class QueryOverridedRockstars : QueryDb<Rockstar>
    {
        [DataMember(Order = 1)]
        public int? Age { get; set; }
    }

    [DataContract]
    public class QueryOverridedCustomRockstars : QueryDb<Rockstar, CustomRockstar>
    {
        [DataMember(Order = 1)]
        public int? Age { get; set; }
    }

    [DataContract]
    public class QueryCaseInsensitiveOrderBy : QueryDb<Rockstar>
    {
        [DataMember(Order = 1)]
        public int? Age { get; set; }
    }

    [DataContract]
    public class QueryFieldRockstars : QueryDb<Rockstar>
    {
        [DataMember(Order = 1)]
        public string FirstName { get; set; } //default to 'AND FirstName = {Value}'

        [DataMember(Order = 2)]
        public string[] FirstNames { get; set; } //Collections default to 'FirstName IN ({Values})

        [QueryDbField(Operand = ">=")]
        [DataMember(Order = 3)]
        public int? Age { get; set; }

        [QueryDbField(Template = "UPPER({Field}) LIKE UPPER({Value})", Field = "FirstName")]
        [DataMember(Order = 4)]
        public string FirstNameCaseInsensitive { get; set; }

        [QueryDbField(Template = "{Field} LIKE {Value}", Field = "FirstName", ValueFormat = "{0}%")]
        [DataMember(Order = 5)]
        public string FirstNameStartsWith { get; set; }

        [QueryDbField(Template = "{Field} LIKE {Value}", Field = "LastName", ValueFormat = "%{0}")]
        [DataMember(Order = 6)]
        public string LastNameEndsWith { get; set; }

        [QueryDbField(Template = "{Field} BETWEEN {Value1} AND {Value2}", Field = "FirstName")]
        [DataMember(Order = 7)]
        public string[] FirstNameBetween { get; set; }

        [QueryDbField(Term = QueryTerm.Or, Template = "UPPER({Field}) LIKE UPPER({Value})", Field = "LastName")]
        [DataMember(Order = 8)]
        public string OrLastName { get; set; }
    }

    [DataContract]
    public class QueryRockstarAlias : QueryDb<Rockstar, RockstarAlias>,
        IJoin<Rockstar, RockstarAlbum>
    {
        [DataMember(Order = 1)]
        public int? Age { get; set; }
        [DataMember(Order = 2)]
        public string RockstarAlbumName { get; set; }
    }

    [DataContract]
    public class RockstarAlias
    {
        [DataMember(Order = 1)]
        [Alias("Id")]
        public int RockstarId { get; set; }

        [DataMember(Order = 2)]
        public string FirstName { get; set; }

        [DataMember(Order = 3)]
        [Alias("LastName")]
        public string Surname { get; set; }

        [DataMember(Name = "album", Order = 4)]
        public string RockstarAlbumName { get; set; }
    }

    [DataContract]
    public class QueryFieldRockstarsDynamic : QueryDb<Rockstar>
    {
        [DataMember(Order = 1)]
        public int? Age { get; set; }
    }

    [DataContract]
    public class QueryRockstarsFilter : QueryDb<Rockstar>
    {
        [DataMember(Order = 1)]
        public int? Age { get; set; }
    }

    [DataContract]
    public class QueryCustomRockstarsFilter : QueryDb<Rockstar, CustomRockstar>
    {
        [DataMember(Order = 1)]
        public int? Age { get; set; }
    }

    public interface IFilterRockstars { }
    [DataContract]
    public class QueryRockstarsIFilter : QueryDb<Rockstar>, IFilterRockstars
    {
        [DataMember(Order = 1)]
        public int? Age { get; set; }
    }

    [QueryDb(QueryTerm.Or)]
    [Route("/OrRockstars")]
    [DataContract]
    public class QueryOrRockstars : QueryDb<Rockstar>
    {
        [DataMember(Order = 1)]
        public int? Age { get; set; }
        [DataMember(Order = 2)]
        public string FirstName { get; set; }
    }
    
    [DataContract]
    public class QueryRockstarsImplicit : QueryDb<Rockstar> {}

    [Route("/OrRockstarsFields")]
    [DataContract]
    public class QueryOrRockstarsFields : QueryDb<Rockstar>
    {
        [QueryDbField(Term = QueryTerm.Or)]
        [DataMember(Order = 1)]
        public string FirstName { get; set; }

        [QueryDbField(Term = QueryTerm.Or)]
        [DataMember(Order = 2)]
        public string LastName { get; set; }
    }

    [DataContract]
    public class QueryFieldsImplicitConventions : QueryDb<Rockstar>
    {
        [QueryDbField(Term = QueryTerm.Or)]
        [DataMember(Order = 1)]
        public string FirstNameContains { get; set; }

        [QueryDbField(Term = QueryTerm.Or)]
        [DataMember(Order = 2)]
        public string LastNameEndsWith { get; set; }
    }

    [QueryDb(QueryTerm.Or)]
    [DataContract]
    public class QueryGetRockstars : QueryDb<Rockstar>
    {
        [DataMember(Order = 1)]
        public int[] Ids { get; set; }
        [DataMember(Order = 2)]
        public List<int> Ages { get; set; }
        [DataMember(Order = 3)]
        public List<string> FirstNames { get; set; }
        [DataMember(Order = 4)]
        public int[] IdsBetween { get; set; }
    }

    [DataContract]
    public class QueryRockstarFilters : QueryDb<Rockstar>
    {
        [DataMember(Order = 1)]
        public int[] Ids { get; set; }
        [DataMember(Order = 2)]
        public List<int> Ages { get; set; }
        [DataMember(Order = 3)]
        public List<string> FirstNames { get; set; }
        [DataMember(Order = 4)]
        public int[] IdsBetween { get; set; }
    }

    [QueryDb(QueryTerm.Or)]
    [DataContract]
    public class QueryGetRockstarsDynamic : QueryDb<Rockstar> {}

//    [References(typeof(RockstarAlbumGenreGlobalIndex))]
    [DataContract]
    public class RockstarAlbum
    {
        [AutoIncrement]
        [DataMember(Order = 1)]
        public int Id { get; set; }
        [References(typeof(Rockstar))]
        [DataMember(Order = 2)]
        public int RockstarId { get; set; }
        [DataMember(Order = 3)]
        public string Name { get; set; }
        [Index]
        [DataMember(Order = 4)]
        public string Genre { get; set; }
    }

    [DataContract]
    public class RockstarGenre
    {
        [AutoIncrement]
        [DataMember(Order = 1)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public int RockstarId { get; set; }
        [DataMember(Order = 3)]
        public string Name { get; set; }
    }

    [DataContract]
    public class CustomRockstar
    {
        [DataMember(Order = 1)]
        public string FirstName { get; set; }
        [DataMember(Order = 2)]
        public string LastName { get; set; }
        [DataMember(Order = 3)]
        public int? Age { get; set; }
        [DataMember(Order = 4)]
        public string RockstarAlbumName { get; set; }
        [DataMember(Order = 5)]
        public string RockstarGenreName { get; set; }
    }

    [DataContract]
    public class QueryCustomRockstarsSchema : QueryDb<Rockstar, CustomRockstarSchema>
    {
        [DataMember(Order = 1)]
        public int? Age { get; set; }
    }

    [Schema("dbo")]
    [DataContract]
    public class CustomRockstarSchema
    {
        [DataMember(Order = 1)]
        public string FirstName { get; set; }
        [DataMember(Order = 2)]
        public string LastName { get; set; }
        [DataMember(Order = 3)]
        public int? Age { get; set; }
        [DataMember(Order = 4)]
        public string RockstarAlbumName { get; set; }
        [DataMember(Order = 5)]
        public string RockstarGenreName { get; set; }
    }

    [Route("/movies/search")]
    [QueryDb(QueryTerm.And)] //Default
    [DataContract]
    public class SearchMovies : QueryDb<Movie> {}

    [Route("/movies")]
    [QueryDb(QueryTerm.Or)]
    [DataContract]
    public class QueryMovies : QueryDb<Movie>
    {
        [DataMember(Order = 1)]
        public int[] Ids { get; set; }
        [DataMember(Order = 2)]
        public string[] ImdbIds { get; set; }
        [DataMember(Order = 3)]
        public string[] Ratings { get; set; }
    }

//    [References(typeof(MovieTitleIndex))]
    [DataContract]
    public class Movie
    {
        [AutoIncrement]
        [DataMember(Order = 1)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public string ImdbId { get; set; }
        [DataMember(Order = 3)]
        public string Title { get; set; }
        [DataMember(Order = 4)]
        public string Rating { get; set; }
        [DataMember(Order = 5)]
        public decimal Score { get; set; }
        [DataMember(Order = 6)]
        public string Director { get; set; }
        [DataMember(Order = 7)]
        public DateTime ReleaseDate { get; set; }
        [DataMember(Order = 8)]
        public string TagLine { get; set; }
        [DataMember(Order = 9)]
        public List<string> Genres { get; set; }
    }

    [DataContract]
    public class StreamMovies : QueryDb<Movie>
    {
        [DataMember(Order = 1)]
        public string[] Ratings { get; set; }
    }

    [DataContract]
    public class QueryUnknownRockstars : QueryDb<Rockstar>
    {
        [DataMember(Order = 1)]
        public int UnknownInt { get; set; }
        [DataMember(Order = 2)]
        public string UnknownProperty { get; set; }
    }

    [Route("/query/rockstar-references")]
    [DataContract]
    public class QueryRockstarsWithReferences : QueryDb<RockstarReference>
    {
        [DataMember(Order = 1)]
        public int? Age { get; set; }
    }

    [DataContract]
    public class QueryCustomRockstarsReferences : QueryDb<RockstarReference>
    {
        [DataMember(Order = 1)]
        public int? Age { get; set; }
    }

    [Alias("Rockstar")]
    [DataContract]
    public class RockstarReference
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public string FirstName { get; set; }
        [DataMember(Order = 3)]
        public string LastName { get; set; }
        [DataMember(Order = 4)]
        public int? Age { get; set; }

        [Reference]
        [DataMember(Order = 5)]
        public List<RockstarAlbum> Albums { get; set; } 
    }

    [Route("/query/all-fields")]
    [DataContract]
    public class QueryAllFields : QueryDb<AllFields>
    {
        [DataMember(Order = 1)]
        public virtual Guid? Guid { get; set; }
    }

    [DataContract]
    public class AllFields
    {
        [DataMember(Order = 1)]
        public virtual int Id { get; set; }
        [DataMember(Order = 2)]
        public virtual int? NullableId { get; set; }
        [DataMember(Order = 3)]
        public virtual byte Byte { get; set; }
        [DataMember(Order = 4)]
        public virtual short Short { get; set; }
        [DataMember(Order = 5)]
        public virtual int Int { get; set; }
        [DataMember(Order = 6)]
        public virtual long Long { get; set; }
        [DataMember(Order = 7)]
        public virtual ushort UShort { get; set; }
        [DataMember(Order = 8)]
        public virtual uint UInt { get; set; }
        [DataMember(Order = 9)]
        public virtual ulong ULong { get; set; }
        [DataMember(Order = 10)]
        public virtual float Float { get; set; }
        [DataMember(Order = 11)]
        public virtual double Double { get; set; }
        [DataMember(Order = 12)]
        public virtual decimal Decimal { get; set; }
        [DataMember(Order = 13)]
        public virtual string String { get; set; }
        [DataMember(Order = 14)]
        public virtual DateTime DateTime { get; set; }
        [DataMember(Order = 15)]
        public virtual TimeSpan TimeSpan { get; set; }
        [DataMember(Order = 16)]
        public virtual Guid Guid { get; set; }
        [DataMember(Order = 17)]
        public virtual DateTime? NullableDateTime { get; set; }
        [DataMember(Order = 18)]
        public virtual TimeSpan? NullableTimeSpan { get; set; }
        [DataMember(Order = 19)]
        public virtual Guid? NullableGuid { get; set; }
        [DataMember(Order = 20)]
        public HttpStatusCode Enum { get; set; }
        [DataMember(Order = 21)]
        public HttpStatusCode? NullableEnum { get; set; }
    }

    [EnumAsInt]
    public enum SomeEnumAsInt
    {
        Value0 = 0,
        Value1 = 1,
        Value2 = 2,
        Value3 = 3,
    }

    public enum SomeEnum
    {
        // Enum values must be unique globally
        // https://stackoverflow.com/questions/13802844/protobuf-net-into-proto-generates-enum-conflicts
        [ProtoEnum(Name="SomeEnum_Value0")]
        Value0 = 0,
        [ProtoEnum(Name="SomeEnum_Value1")]
        Value1 = 1,
        [ProtoEnum(Name="SomeEnum_Value2")]
        Value2 = 2,
        [ProtoEnum(Name="SomeEnum_Value3")]
        Value3 = 3
    }

    [DataContract]
    public class TypeWithEnum
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public string Name { get; set; }
        [DataMember(Order = 3)]
        public SomeEnum SomeEnum { get; set; }
        [DataMember(Order = 4)]
        public SomeEnumAsInt SomeEnumAsInt { get; set; }
        [DataMember(Order = 5)]
        public SomeEnum? NSomeEnum { get; set; }
        [DataMember(Order = 6)]
        public SomeEnumAsInt? NSomeEnumAsInt { get; set; }
    }

    [Route("/query-enums")]
    [DataContract]
    public class QueryTypeWithEnums : QueryDb<TypeWithEnum> {}

    [DataContract]
    public class Adhoc
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }

        [DataMember(Name = "first_name", Order = 2)]
        public string FirstName { get; set; }

        [DataMember(Order = 3)]
        public string LastName { get; set; }
    }

    [DataContract]
    [Route("/adhoc-rockstars")]
    public class QueryAdhocRockstars : QueryDb<Rockstar>
    {
        [DataMember(Name = "first_name", Order = 1)]
        public string FirstName { get; set; }
    }

    [DataContract]
    [Route("/adhoc")]
    public class QueryAdhoc : QueryDb<Adhoc> {}

    public class AutoQueryService : Service
    {
        public IAutoQueryDb AutoQuery { get; set; }

        //Override with custom impl
        public object Get(QueryOverridedRockstars dto)
        {
            var q = AutoQuery.CreateQuery(dto, Request.GetRequestParams());
            q.Take(1);
            return AutoQuery.Execute(dto, q);
        }

        public object Get(QueryOverridedCustomRockstars dto)
        {
            var q = AutoQuery.CreateQuery(dto, Request.GetRequestParams());
            q.Take(1);
            return AutoQuery.Execute(dto, q);
        }

        public object Get(QueryCaseInsensitiveOrderBy dto)
        {
            var q = AutoQuery.CreateQuery(dto, Request);
            if (q.OrderByExpression != null)
                q.OrderByExpression += " COLLATE NOCASE";

            return AutoQuery.Execute(dto, q);
        }

        public object Get(StreamMovies dto)
        {
            var q = AutoQuery.CreateQuery(dto, Request.GetRequestParams());
            q.Take(2);
            return AutoQuery.Execute(dto, q);
        }

        public object Get(QueryCustomRockstarsReferences request)
        {
            var q = AutoQuery.CreateQuery(request, Request.GetRequestParams());
            var response = new QueryResponse<RockstarReference>
            {
                Offset = q.Offset.GetValueOrDefault(0),
                Results = Db.LoadSelect(q, include:new string[0]),
                Total = (int)Db.Count(q),
            };
            return response;
        }

        public object Get(QueryRockstarAlbumsCustomLeftJoin query)
        {
            var q = AutoQuery.CreateQuery(query, Request)
                .LeftJoin<RockstarAlbum>((r, a) => r.Id == a.RockstarId);
            return AutoQuery.Execute(query, q);
        }
    }

    public interface IChangeDb
    {
        string NamedConnection { get; set; }
        string ConnectionString { get; set; }
        string ProviderName { get; set; }
    }

    [Route("/querychangedb")]
    [DataContract]
    public class QueryChangeDb : QueryDb<Rockstar>, IChangeDb
    {
        [DataMember(Order = 1)]
        public string NamedConnection { get; set; }
        [DataMember(Order = 2)]
        public string ConnectionString { get; set; }
        [DataMember(Order = 3)]
        public string ProviderName { get; set; }
    }

    [Route("/changedb")]
    [DataContract]
    public class ChangeDb : IReturn<ChangeDbResponse>, IChangeDb
    {
        [DataMember(Order = 1)]
        public string NamedConnection { get; set; }
        [DataMember(Order = 2)]
        public string ConnectionString { get; set; }
        [DataMember(Order = 3)]
        public string ProviderName { get; set; }
    }

    [DataContract]
    public class ChangeDbResponse
    {
        [DataMember(Order = 1)]
        public List<Rockstar> Results { get; set; }
    }

    [DataContract]
    public class DynamicDbServices : Service
    {
        public object Get(ChangeDb request)
        {
            return new ChangeDbResponse { Results = Db.Select<Rockstar>() };
        }
    }

    [DataContract]
    public class ChangeConnectionInfo : IReturn<ChangeDbResponse> { }
    [DataContract]
    public class QueryChangeConnectionInfo : QueryDb<Rockstar> { }

    [ConnectionInfo(NamedConnection = AutoQueryAppHost.SqlServerNamedConnection)]
    [DataContract]
    public class NamedConnectionServices : Service
    {
        public IAutoQueryDb AutoQuery { get; set; }

        public object Get(ChangeConnectionInfo request)
        {
            return new ChangeDbResponse { Results = Db.Select<Rockstar>() };
        }

        public object Get(QueryChangeConnectionInfo query)
        {
            return AutoQuery.Execute(query, AutoQuery.CreateQuery(query, Request), Request);
        }
    }

    [Alias(nameof(Rockstar))]
    [DataContract]
    public class CustomSelectRockstar
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public string FirstName { get; set; }
        [DataMember(Order = 3)]
        public string LastName { get; set; }
        [CustomSelect("Age * 2")]
        [DataMember(Order = 4)]
        public int? Age { get; set; }
    }

    [DataContract]
    public class QueryJoinedRockstarAlbumsCustomSelect : QueryDb<CustomSelectRockstar>, 
        IJoin<CustomSelectRockstar, RockstarAlbum>
    {
        [DataMember(Order = 1)]
        public int? Age { get; set; }
        [DataMember(Order = 2)]
        public string RockstarAlbumName { get; set; }
    }

    [DataContract]
    public class CustomSelectRockstarResponse
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }
        [DataMember(Order = 2)]
        public string FirstName { get; set; }
        [DataMember(Order = 3)]
        public int? Age { get; set; }
    }

    [DataContract]
    public class QueryJoinedRockstarAlbumsCustomSelectResponse : QueryDb<CustomSelectRockstar,CustomSelectRockstarResponse>, 
        IJoin<CustomSelectRockstar, RockstarAlbum>
    {
        [DataMember(Order = 1)]
        public int? Age { get; set; }
        [DataMember(Order = 2)]
        public string RockstarAlbumName { get; set; }
    }
    
    public class TestsConfig
    {
        public static readonly int Port = 20000;
        public static readonly string BaseUri = Environment.GetEnvironmentVariable("CI_BASEURI") ?? $"http://localhost:{Port}";
        public static readonly string AbsoluteBaseUri = BaseUri + "/";
        
        public static readonly string HostNameBaseUrl = $"http://DESKTOP-BCS76J0:{Port}/"; //Allow fiddler
        public static readonly string AnyHostBaseUrl = $"http://*:{Port}/"; //Allow capturing by fiddler

        public static readonly string ListeningOn = BaseUri + "/";
        public static readonly string RabbitMQConnString = Environment.GetEnvironmentVariable("CI_RABBITMQ") ?? "localhost";
        public static readonly string SqlServerConnString = Environment.GetEnvironmentVariable("MSSQL_CONNECTION") ?? "Server=localhost;Database=test;User Id=test;Password=test;";
        public static readonly string PostgreSqlConnString = Environment.GetEnvironmentVariable("PGSQL_CONNECTION") ?? "Server=localhost;Port=5432;User Id=test;Password=test;Database=test;Pooling=true;MinPoolSize=0;MaxPoolSize=200";
        public static readonly string DynamoDbServiceURL = Environment.GetEnvironmentVariable("CI_DYNAMODB") ?? "http://localhost:8000";

        public const string AspNetBaseUri = "http://localhost:50000/";
        public const string AspNetServiceStackBaseUri = AspNetBaseUri + "api";

        public static GrpcServiceClient GetInsecureClient()
        {
            GrpcClientFactory.AllowUnencryptedHttp2 = true;
            var client = new GrpcServiceClient(BaseUri);
            return client;
        }
    }

    public static class TestUtils
    {
        public static void AddRequiredConfig(this ScriptContext context)
        {
            context.ScriptMethods.AddRange(new ScriptMethods[] {
                new DbScriptsAsync(),
                new MyValidators(), 
            });
        }
    }

    public class AutoQueryAppHost : AppSelfHostBase
    {
        public AutoQueryAppHost()
            : base("AutoQuery", typeof(AutoQueryService).Assembly) { }

        public static readonly string SqlServerConnString = TestsConfig.SqlServerConnString;
        public const string SqlServerNamedConnection = "SqlServer";
        public const string SqlServerProvider = "SqlServer2012";

        public static string SqliteFileConnString = "~/App_Data/autoquery.sqlite".MapProjectPath();
        
        public Action<AutoQueryAppHost,Container> ConfigureFn { get; set; }

        public override void ConfigureKestrel(KestrelServerOptions options)
        {
            options.ListenLocalhost(TestsConfig.Port, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http2;
            });
        }

        public override void Configure(IServiceCollection services)
        {
            services.AddServiceStackGrpc();
        }
        
        public Action<GrpcFeature> ConfigureGrpc { get; set; }
        
        public override void Configure(Container container)
        {
            var grpcFeature = new GrpcFeature(App);
            ConfigureGrpc?.Invoke(grpcFeature);
            Plugins.Add(grpcFeature);

            var dbFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);
            container.Register<IDbConnectionFactory>(dbFactory);

            dbFactory.RegisterConnection(SqlServerNamedConnection, SqlServerConnString, SqlServer2012Dialect.Provider);
            dbFactory.RegisterDialectProvider(SqlServerProvider, SqlServer2012Dialect.Provider);

            using (var db = dbFactory.OpenDbConnection(SqlServerNamedConnection))
            {
                db.DropTable<RockstarAlbum>();
                db.DropAndCreateTable<NamedRockstar>();

                db.Insert(new NamedRockstar {
                    Id = 1,
                    FirstName = "Microsoft",
                    LastName = "SQL Server",
                    Age = 27,
                    DateOfBirth = new DateTime(1989,1,1),
                    LivingStatus = LivingStatus.Alive,
                });
            }

            using (var db = dbFactory.OpenDbConnectionString(SqliteFileConnString))
            {
                db.DropTable<RockstarAlbum>();
                db.DropAndCreateTable<Rockstar>();
                db.Insert(new Rockstar {
                    Id = 1,
                    FirstName = "Sqlite",
                    LastName = "File DB",
                    Age = 16,
                    DateOfBirth = new DateTime(2000, 8, 1),
                    LivingStatus = LivingStatus.Alive,
                });
            }

            RegisterTypedRequestFilter<IChangeDb>((req, res, dto) =>
                req.Items[Keywords.DbInfo] = dto.ConvertTo<ConnectionInfo>());

            //container.Register<IDbConnectionFactory>(
            //    new OrmLiteConnectionFactory("Server=localhost;Database=test;User Id=test;Password=test;",
            //        SqlServerDialect.Provider));

            //container.Register<IDbConnectionFactory>(
            //    new OrmLiteConnectionFactory("Server=localhost;Database=test;User Id=test;Password=test;",
            //        SqlServer2012Dialect.Provider));

            //container.Register<IDbConnectionFactory>(
            //    new OrmLiteConnectionFactory("Server=localhost;Database=test;UID=root;Password=test",
            //        MySqlDialect.Provider));

            //container.Register<IDbConnectionFactory>(
            //    new OrmLiteConnectionFactory("Server=localhost;Port=5432;User Id=test;Password=test;Database=test;Pooling=true;MinPoolSize=0;MaxPoolSize=200",
            //        PostgreSqlDialect.Provider));

            using (var db = container.Resolve<IDbConnectionFactory>().Open())
            {
                db.DropTable<RockstarAlbum>();
                db.DropTable<Rockstar>();
                db.CreateTable<Rockstar>();
                db.CreateTable<RockstarAlbum>();

                db.DropAndCreateTable<RockstarGenre>();
                db.DropAndCreateTable<Movie>();
                db.DropAndCreateTable<PagingTest>();

                db.InsertAll(SeedRockstars);
                db.InsertAll(SeedAlbums);
                db.InsertAll(SeedGenres);
                db.InsertAll(SeedMovies);
                db.InsertAll(SeedPagingTest);

                db.DropAndCreateTable<AllFields>();
                db.Insert(new AllFields
                {
                    Id = 1,
                    NullableId = 2,
                    Byte = 3,
                    DateTime = new DateTime(2001, 01, 01),
                    NullableDateTime = new DateTime(2002, 02, 02),
                    Decimal = 4,
                    Double = 5.5,
                    Float = 6.6f,
                    Guid = new Guid("3EE6865A-4149-4940-B7A2-F952E0FEFC5E"),
                    NullableGuid = new Guid("7A2FDDD8-4BB0-4735-8230-A6AC79088489"),
                    Long = 7,
                    Short = 8,
                    String = "string",
                    TimeSpan = TimeSpan.FromHours(1),
                    NullableTimeSpan = TimeSpan.FromDays(1),
                    UInt = 9,
                    ULong = 10,
                    UShort = 11,
                    Enum = HttpStatusCode.MethodNotAllowed,
                    NullableEnum = HttpStatusCode.MethodNotAllowed,
                });

                db.DropAndCreateTable<Adhoc>();
                db.InsertAll(SeedRockstars.Map(x => new Adhoc
                {
                    Id = x.Id,
                    FirstName = x.FirstName,
                    LastName = x.LastName
                }));
                
                db.CreateTable<TypeWithEnum>();

                db.Insert(new TypeWithEnum { Id = 1, Name = "Value1", SomeEnum = SomeEnum.Value1, NSomeEnum = SomeEnum.Value1, SomeEnumAsInt = SomeEnumAsInt.Value1, NSomeEnumAsInt = SomeEnumAsInt.Value1 });
                db.Insert(new TypeWithEnum { Id = 2, Name = "Value2", SomeEnum = SomeEnum.Value2, NSomeEnum = SomeEnum.Value2, SomeEnumAsInt = SomeEnumAsInt.Value2, NSomeEnumAsInt = SomeEnumAsInt.Value2 });
                db.Insert(new TypeWithEnum { Id = 3, Name = "Value3", SomeEnum = SomeEnum.Value3, NSomeEnum = SomeEnum.Value3, SomeEnumAsInt = SomeEnumAsInt.Value3, NSomeEnumAsInt = SomeEnumAsInt.Value3 });
            }

            var autoQuery = new AutoQueryFeature
                {
                    MaxLimit = 100,
                    EnableRawSqlFilters = true,
                    ResponseFilters = {
                        ctx => {
                            var executedCmds = new List<Command>();
                            var supportedFns = new Dictionary<string, Func<int, int, int>>(StringComparer.OrdinalIgnoreCase)
                            {
                                {"ADD",      (a,b) => a + b },
                                {"MULTIPLY", (a,b) => a * b },
                                {"DIVIDE",   (a,b) => a / b },
                                {"SUBTRACT", (a,b) => a - b },
                            };
                            foreach (var cmd in ctx.Commands)
                            {
                                if (!supportedFns.TryGetValue(cmd.Name, out var fn)) continue;
                                var label = !cmd.Suffix.IsNullOrWhiteSpace() ? cmd.Suffix.Trim().ToString() : cmd.ToString();
                                ctx.Response.Meta[label] = fn(cmd.Args[0].ParseInt32(), cmd.Args[1].ParseInt32()).ToString();
                                executedCmds.Add(cmd);
                            }
                            ctx.Commands.RemoveAll(executedCmds.Contains);
                        }        
                    }
                }
                .RegisterQueryFilter<QueryRockstarsFilter, Rockstar>((q, dto, req) =>
                    q.And(x => x.LastName.EndsWith("son"))
                )
                .RegisterQueryFilter<QueryCustomRockstarsFilter, Rockstar>((q, dto, req) =>
                    q.And(x => x.LastName.EndsWith("son"))
                )
                .RegisterQueryFilter<IFilterRockstars, Rockstar>((q, dto, req) =>
                    q.And(x => x.LastName.EndsWith("son"))
                );

            Plugins.Add(autoQuery);
            
            ConfigureFn?.Invoke(this,container);
        }

        public static Rockstar[] SeedRockstars = new[] {
            new Rockstar { Id = 1, FirstName = "Jimi", LastName = "Hendrix", Age = 27, LivingStatus = LivingStatus.Dead, DateOfBirth = new DateTime(1942, 11, 27), DateDied = new DateTime(1970, 09, 18), },
            new Rockstar { Id = 2, FirstName = "Jim", LastName = "Morrison", Age = 27, LivingStatus = LivingStatus.Dead, DateOfBirth = new DateTime(1943, 12, 08), DateDied = new DateTime(1971, 07, 03),  },
            new Rockstar { Id = 3, FirstName = "Kurt", LastName = "Cobain", Age = 27, LivingStatus = LivingStatus.Dead, DateOfBirth = new DateTime(1967, 02, 20), DateDied = new DateTime(1994, 04, 05), },
            new Rockstar { Id = 4, FirstName = "Elvis", LastName = "Presley", Age = 42, LivingStatus = LivingStatus.Dead, DateOfBirth = new DateTime(1935, 01, 08), DateDied = new DateTime(1977, 08, 16), },
            new Rockstar { Id = 5, FirstName = "David", LastName = "Grohl", Age = 44, LivingStatus = LivingStatus.Alive, DateOfBirth = new DateTime(1969, 01, 14), },
            new Rockstar { Id = 6, FirstName = "Eddie", LastName = "Vedder", Age = 48, LivingStatus = LivingStatus.Alive, DateOfBirth = new DateTime(1964, 12, 23), },
            new Rockstar { Id = 7, FirstName = "Michael", LastName = "Jackson", Age = 50, LivingStatus = LivingStatus.Dead, DateOfBirth = new DateTime(1958, 08, 29), DateDied = new DateTime(2009, 06, 05), },
        };

        public static RockstarAlbum[] SeedAlbums = new[] {
            new RockstarAlbum { Id = 1, RockstarId = 1, Name = "Electric Ladyland", Genre = "Funk" },
            new RockstarAlbum { Id = 2, RockstarId = 3, Name = "Bleach", Genre = "Grunge" },
            new RockstarAlbum { Id = 3, RockstarId = 3, Name = "Nevermind", Genre = "Grunge" },
            new RockstarAlbum { Id = 4, RockstarId = 3, Name = "In Utero", Genre = "Grunge" },
            new RockstarAlbum { Id = 5, RockstarId = 3, Name = "Incesticide", Genre = "Grunge" },
            new RockstarAlbum { Id = 6, RockstarId = 3, Name = "MTV Unplugged in New York", Genre = "Acoustic" },
            new RockstarAlbum { Id = 7, RockstarId = 5, Name = "Foo Fighters", Genre = "Grunge" },
            new RockstarAlbum { Id = 8, RockstarId = 6, Name = "Into the Wild", Genre = "Folk" },
        };

        public static RockstarGenre[] SeedGenres = new[] {
            new RockstarGenre { RockstarId = 1, Name = "Rock" },    
            new RockstarGenre { RockstarId = 3, Name = "Grunge" },    
            new RockstarGenre { RockstarId = 5, Name = "Alternative Rock" },    
            new RockstarGenre { RockstarId = 6, Name = "Folk Rock" },    
        };

        public static Movie[] SeedMovies = new[] {
            new Movie { ImdbId = "tt0111161", Title = "The Shawshank Redemption", Score = 9.2m, Director = "Frank Darabont", ReleaseDate = new DateTime(1995,2,17), TagLine = "Fear can hold you prisoner. Hope can set you free.", Genres = new List<string>{"Crime","Drama"}, Rating = "R", },
            new Movie { ImdbId = "tt0068646", Title = "The Godfather", Score = 9.2m, Director = "Francis Ford Coppola", ReleaseDate = new DateTime(1972,3,24), TagLine = "An offer you can't refuse.", Genres = new List<string> {"Crime","Drama", "Thriller"}, Rating = "R", },
            new Movie { ImdbId = "tt1375666", Title = "Inception", Score = 9.2m, Director = "Christopher Nolan", ReleaseDate = new DateTime(2010,7,16), TagLine = "Your mind is the scene of the crime", Genres = new List<string>{"Action", "Mystery", "Sci-Fi", "Thriller"}, Rating = "PG-13", },
            new Movie { ImdbId = "tt0071562", Title = "The Godfather: Part II", Score = 9.0m, Director = "Francis Ford Coppola", ReleaseDate = new DateTime(1974,12,20), Genres = new List<string> {"Crime","Drama", "Thriller"}, Rating = "R", },
            new Movie { ImdbId = "tt0060196", Title = "The Good, the Bad and the Ugly", Score = 9.0m, Director = "Sergio Leone", ReleaseDate = new DateTime(1967,12,29), TagLine = "They formed an alliance of hate to steal a fortune in dead man's gold", Genres = new List<string>{"Adventure","Western"}, Rating = "R", },
            new Movie { ImdbId = "tt0114709", Title = "Toy Story", Score = 8.3m, Director = "John Lasseter", ReleaseDate = new DateTime(1995,11,22), TagLine = "A cowboy doll is profoundly threatened and jealous when a new spaceman figure supplants him as top toy in a boy's room.", Genres = new List<string>{"Animation","Adventure","Comedy"}, Rating = "G", },
            new Movie { ImdbId = "tt2294629", Title = "Frozen", Score = 7.8m, Director = "Chris Buck", ReleaseDate = new DateTime(2013,11,27), TagLine = "Fearless optimist Anna teams up with Kristoff in an epic journey, encountering Everest-like conditions, and a hilarious snowman named Olaf", Genres = new List<string>{"Animation","Adventure","Comedy"}, Rating = "PG", },
            new Movie { ImdbId = "tt1453405", Title = "Monsters University", Score = 7.4m, Director = "Dan Scanlon", ReleaseDate = new DateTime(2013,06,21), TagLine = "A look at the relationship between Mike and Sulley during their days at Monsters University -- when they weren't necessarily the best of friends.", Genres = new List<string>{"Animation","Adventure","Comedy"}, Rating = "G", },
            new Movie { ImdbId = "tt0468569", Title = "The Dark Knight", Score = 9.0m, Director = "Christopher Nolan", ReleaseDate = new DateTime(2008,07,18), TagLine = "When Batman, Gordon and Harvey Dent launch an assault on the mob, they let the clown out of the box, the Joker, bent on turning Gotham on itself and bringing any heroes down to his level.", Genres = new List<string>{"Action","Crime","Drama"}, Rating = "PG-13", },
            new Movie { ImdbId = "tt0109830", Title = "Forrest Gump", Score = 8.8m, Director = "Robert Zemeckis", ReleaseDate = new DateTime(1996,07,06), TagLine = "Forrest Gump, while not intelligent, has accidentally been present at many historic moments, but his true love, Jenny Curran, eludes him.", Genres = new List<string>{"Drama","Romance"}, Rating = "PG-13", },
        };

        public static PagingTest[] SeedPagingTest = 250.Times(i => new PagingTest { Id = i, Name = "Name" + i, Value = i % 2 }).ToArray();

        public override void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
        }
    }
            
    public class GrpcAutoQueryTests
    {
        private readonly ServiceStackHost appHost;
        public IServiceClientAsync client;

        private static readonly int TotalRockstars = AutoQueryAppHost.SeedRockstars.Length;
        private static readonly int TotalAlbums = AutoQueryAppHost.SeedAlbums.Length;

        public GrpcAutoQueryTests()
        {
            ConsoleLogFactory.Configure();
            appHost = new AutoQueryAppHost()
                .Init()
                .Start(TestsConfig.ListeningOn);

            GrpcClientFactory.AllowUnencryptedHttp2 = true;
            client = new GrpcServiceClient(TestsConfig.ListeningOn);
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        public List<Rockstar> Rockstars => AutoQueryAppHost.SeedRockstars.ToList();

        public List<PagingTest> PagingTests => AutoQueryAppHost.SeedPagingTest.ToList();
        
        [Test]
        public async Task Can_execute_basic_query()
        {
            var response = await client.GetAsync(new QueryRockstars { Include = "Total" });

            Assert.That(response.Offset, Is.EqualTo(0));
            Assert.That(response.Total, Is.EqualTo(TotalRockstars));
            Assert.That(response.Results.Count, Is.EqualTo(TotalRockstars));
        }
        
        [Test]
        public async Task Can_execute_basic_query_NamedRockstar()
        {
            var response = await client.GetAsync(new QueryNamedRockstars { Include = "Total" });

            Assert.That(response.Offset, Is.EqualTo(0));
            Assert.That(response.Total, Is.EqualTo(1));
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Results[0].LastName, Is.EqualTo("SQL Server"));
        }

        [Test]
        public async Task Can_execute_overridden_basic_query()
        {
            var response = await client.GetAsync(new QueryOverridedRockstars { Include = "Total" });

            Assert.That(response.Offset, Is.EqualTo(0));
            Assert.That(response.Total, Is.EqualTo(TotalRockstars));
            Assert.That(response.Results.Count, Is.EqualTo(1));
        }
        
        [Test]
        public async Task Can_execute_overridden_basic_query_with_case_insensitive_orderBy()
        {
            var response = await client.GetAsync(new QueryCaseInsensitiveOrderBy { Age = 27, OrderBy = "FirstName" });

            Assert.That(response.Results.Count, Is.EqualTo(3));
        }

        [Test]
        public async Task Can_execute_AdhocRockstars_query()
        {
            var request = new QueryAdhocRockstars { FirstName = "Jimi", Include = "Total" };

            Assert.That(request.ToGetUrl(), Is.EqualTo("/adhoc-rockstars?first_name=Jimi&include=Total"));

            var response = await client.GetAsync(request);

            Assert.That(response.Offset, Is.EqualTo(0));
            Assert.That(response.Total, Is.EqualTo(1));
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Results[0].FirstName, Is.EqualTo(request.FirstName));
        }

        [Test]
        public async Task Can_execute_explicit_equality_condition_on_overridden_CustomRockstar()
        {
            var response = await client.GetAsync(new QueryOverridedCustomRockstars { Age = 27, Include = "Total" });

            Assert.That(response.Total, Is.EqualTo(3));
            Assert.That(response.Results.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task Can_execute_basic_query_with_limits()
        {
            var response = await client.GetAsync(new QueryRockstars { Skip = 2, Include = "Total" });
            Assert.That(response.Offset, Is.EqualTo(2));
            Assert.That(response.Total, Is.EqualTo(TotalRockstars));
            Assert.That(response.Results.Count, Is.EqualTo(TotalRockstars - 2));

            response = await client.GetAsync(new QueryRockstars { Take = 2, Include = "Total" });
            Assert.That(response.Offset, Is.EqualTo(0));
            Assert.That(response.Total, Is.EqualTo(TotalRockstars));
            Assert.That(response.Results.Count, Is.EqualTo(2));

            response = await client.GetAsync(new QueryRockstars { Skip = 2, Take = 2, Include = "Total" });
            Assert.That(response.Offset, Is.EqualTo(2));
            Assert.That(response.Total, Is.EqualTo(TotalRockstars));
            Assert.That(response.Results.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task Can_execute_explicit_equality_condition()
        {
            var response = await client.GetAsync(new QueryRockstars { Age = 27, Include = "Total" });

            Assert.That(response.Total, Is.EqualTo(3));
            Assert.That(response.Results.Count, Is.EqualTo(3));
        }

        [Test]
        public async Task Can_execute_explicit_equality_condition_implicitly()
        {
            var client = new GrpcServiceClient(TestsConfig.ListeningOn) {
                RequestFilter = ctx => {
                    ctx.RequestHeaders.Add("query.Age", "27");
                    ctx.RequestHeaders.Add("query.Include", "Total");
                }
            };
            var response = await client.GetAsync(new QueryRockstarsImplicit());

            Assert.That(response.Total, Is.EqualTo(3));
            Assert.That(response.Results.Count, Is.EqualTo(3));
        }

        [Test]
        public async Task Can_execute_explicit_equality_condition_on_CustomRockstar()
        {
            var response = await client.GetAsync(new QueryCustomRockstars { Age = 27, Include = "Total" });

            Assert.That(response.Total, Is.EqualTo(3));
            Assert.That(response.Results.Count, Is.EqualTo(3));
        }

        [Test]
        public async Task Can_execute_explicit_equality_condition_on_CustomRockstarSchema()
        {
            var response = await client.GetAsync(new QueryCustomRockstarsSchema { Age = 27, Include = "Total" });

            response.PrintDump();

            Assert.That(response.Total, Is.EqualTo(3));
            Assert.That(response.Results.Count, Is.EqualTo(3));
            Assert.That(response.Results[0].FirstName, Is.Not.Null);
            Assert.That(response.Results[0].LastName, Is.Not.Null);
            Assert.That(response.Results[0].Age, Is.EqualTo(27));
        }

        [Test]
        public async Task Can_execute_query_with_JOIN_on_RockstarAlbums()
        {
            var response = await client.GetAsync(new QueryJoinedRockstarAlbums { Include = "Total" });
            Assert.That(response.Total, Is.EqualTo(TotalAlbums));
            Assert.That(response.Results.Count, Is.EqualTo(TotalAlbums));
            var albumNames = response.Results.Select(x => x.RockstarAlbumName);
            Assert.That(albumNames, Is.EquivalentTo(new[] {
                "Electric Ladyland", "Bleach", "Nevermind", "In Utero", "Incesticide",
                "MTV Unplugged in New York", "Foo Fighters", "Into the Wild",
            }));

            response = await client.GetAsync(new QueryJoinedRockstarAlbums { Age = 27, Include = "Total" });
            Assert.That(response.Total, Is.EqualTo(6));
            Assert.That(response.Results.Count, Is.EqualTo(6));
            albumNames = response.Results.Select(x => x.RockstarAlbumName);
            Assert.That(albumNames, Is.EquivalentTo(new[] {
                "Electric Ladyland", "Bleach", "Nevermind", "In Utero", "Incesticide",
                "MTV Unplugged in New York",
            }));

            response = await client.GetAsync(new QueryJoinedRockstarAlbums { RockstarAlbumName = "Nevermind", Include = "Total" });
            Assert.That(response.Total, Is.EqualTo(1));
            Assert.That(response.Results.Count, Is.EqualTo(1));
            albumNames = response.Results.Select(x => x.RockstarAlbumName);
            Assert.That(albumNames, Is.EquivalentTo(new[] { "Nevermind" }));
        }

        [Test]
        public async Task Can_execute_query_with_JOIN_on_RockstarAlbums_and_CustomSelectRockstar()
        {
            var response = await client.GetAsync(new QueryJoinedRockstarAlbumsCustomSelect { Include = "Total" });
            Assert.That(response.Total, Is.EqualTo(TotalAlbums));
            Assert.That(response.Results.Count, Is.EqualTo(TotalAlbums));
            var ages = response.Results.Select(x => x.Age);
            Assert.That(ages.Contains(27 * 2));
            
            var customRes = await client.GetAsync(new QueryJoinedRockstarAlbumsCustomSelectResponse { Include = "Total" });
            Assert.That(customRes.Total, Is.EqualTo(TotalAlbums));
            Assert.That(customRes.Results.Count, Is.EqualTo(TotalAlbums));
            ages = customRes.Results.Select(x => x.Age);
            Assert.That(ages.Contains(27 * 2));
        }
        
        [Test]
        public async Task Can_execute_query_with_multiple_JOINs_on_Rockstar_Albums_and_Genres()
        {
            var response = await client.GetAsync(new QueryMultiJoinRockstar { Include = "Total" });
            Assert.That(response.Total, Is.EqualTo(TotalAlbums));
            Assert.That(response.Results.Count, Is.EqualTo(TotalAlbums));
            var albumNames = response.Results.Select(x => x.RockstarAlbumName);
            Assert.That(albumNames, Is.EquivalentTo(new[] {
                "Electric Ladyland", "Bleach", "Nevermind", "In Utero", "Incesticide",
                "MTV Unplugged in New York", "Foo Fighters", "Into the Wild",
            }));

            var genreNames = response.Results.Select(x => x.RockstarGenreName).Distinct();
            Assert.That(genreNames, Is.EquivalentTo(new[] {
                "Rock", "Grunge", "Alternative Rock", "Folk Rock"
            }));

            response = await client.GetAsync(new QueryMultiJoinRockstar { RockstarAlbumName = "Nevermind", Include = "Total" });
            Assert.That(response.Total, Is.EqualTo(1));
            Assert.That(response.Results.Count, Is.EqualTo(1));
            albumNames = response.Results.Select(x => x.RockstarAlbumName);
            Assert.That(albumNames, Is.EquivalentTo(new[] { "Nevermind" }));

            response = await client.GetAsync(new QueryMultiJoinRockstar { RockstarGenreName = "Folk Rock", Include = "Total" });
            Assert.That(response.Total, Is.EqualTo(1));
            Assert.That(response.Results.Count, Is.EqualTo(1));
            albumNames = response.Results.Select(x => x.RockstarGenreName);
            Assert.That(albumNames, Is.EquivalentTo(new[] { "Folk Rock" }));
        }

        [Test]
        public async Task Can_execute_query_with_LEFTJOIN_on_RockstarAlbums()
        {
            var response = await client.GetAsync(new QueryRockstarAlbumsLeftJoin { IdNotEqualTo = 3, Include = "Total" });
            Assert.That(response.Total, Is.EqualTo(TotalRockstars - 1));
            Assert.That(response.Results.Count, Is.EqualTo(TotalRockstars - 1));
            var albumNames = response.Results.Where(x => x.RockstarAlbumName != null).Select(x => x.RockstarAlbumName);
            Assert.That(albumNames, Is.EquivalentTo(new[] {
                "Electric Ladyland", "Foo Fighters", "Into the Wild"
            }));
        }
    
        [Test]
        public async Task Can_execute_query_with_custom_LEFTJOIN_on_RockstarAlbums()
        {
            var response = await client.GetAsync(new QueryRockstarAlbumsCustomLeftJoin { IdNotEqualTo = 3, Include = "Total" });
            Assert.That(response.Total, Is.EqualTo(TotalRockstars - 1));
            Assert.That(response.Results.Count, Is.EqualTo(TotalRockstars - 1));
            var albumNames = response.Results.Where(x => x.RockstarAlbumName != null).Select(x => x.RockstarAlbumName);
            Assert.That(albumNames, Is.EquivalentTo(new[] {
                "Electric Ladyland", "Foo Fighters", "Into the Wild"
            }));
        }

        [Test]
        public async Task Can_execute_custom_QueryFields()
        {
            QueryResponse<Rockstar> response;
            response = await client.GetAsync(new QueryFieldRockstars { FirstName = "Jim" });
            Assert.That(response.Results.Count, Is.EqualTo(1));

            response = await client.GetAsync(new QueryFieldRockstars { FirstNames = new[] { "Jim","Kurt" } });
            Assert.That(response.Results.Count, Is.EqualTo(2));

            response = await client.GetAsync(new QueryFieldRockstars { FirstNameCaseInsensitive = "jim" });
            Assert.That(response.Results.Count, Is.EqualTo(1));

            response = await client.GetAsync(new QueryFieldRockstars { FirstNameStartsWith = "Jim" });
            Assert.That(response.Results.Count, Is.EqualTo(2));

            response = await client.GetAsync(new QueryFieldRockstars { LastNameEndsWith = "son" });
            Assert.That(response.Results.Count, Is.EqualTo(2));

            response = await client.GetAsync(new QueryFieldRockstars { FirstNameBetween = new[] {"A","F"} });
            Assert.That(response.Results.Count, Is.EqualTo(3));

            response = await client.GetAsync(new QueryFieldRockstars
            {
                LastNameEndsWith = "son",
                OrLastName = "Hendrix"
            });
            Assert.That(response.Results.Count, Is.EqualTo(3));

            response = await client.GetAsync(new QueryFieldRockstars
            {
                FirstNameStartsWith = "Jim",
                OrLastName = "Presley"
            });
            Assert.That(response.Results.Count, Is.EqualTo(3));

            response = await client.GetAsync(new QueryFieldRockstars { Age = 42 });
            Assert.That(response.Results.Count, Is.EqualTo(4));
        }

        [Test]
        public async Task Can_execute_combination_of_QueryFields()
        {
            QueryResponse<Rockstar> response;

            response = await client.GetAsync(new QueryFieldRockstars
            {
                FirstNameStartsWith = "Jim",
                LastNameEndsWith = "son",
            });
            Assert.That(response.Results.Count, Is.EqualTo(1));

            response = await client.GetAsync(new QueryFieldRockstars
            {
                FirstNameStartsWith = "Jim",
                OrLastName = "Cobain",
            });
            Assert.That(response.Results.Count, Is.EqualTo(3));
        }

        [Test]
        public async Task Does_escape_values()
        {
            QueryResponse<Rockstar> response;

            response = await client.GetAsync(new QueryFieldRockstars
            {
                FirstNameStartsWith = "Jim'\"",
            });
            Assert.That(response.Results?.Count ?? 0, Is.EqualTo(0));
        }

        [Test]
        public async Task Does_use_custom_model_to_select_columns()
        {
            var response = await client.GetAsync(new QueryRockstarAlias { RockstarAlbumName = "Nevermind" });

            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Results[0].RockstarId, Is.EqualTo(3));
            Assert.That(response.Results[0].FirstName, Is.EqualTo("Kurt"));
            Assert.That(response.Results[0].RockstarAlbumName, Is.EqualTo("Nevermind"));
        }

        [Test]
        public async Task Does_allow_adding_attributes_dynamically()
        {
            typeof(QueryFieldRockstarsDynamic)
                .GetProperty("Age")
                .AddAttributes(new QueryDbFieldAttribute { Operand = ">=" });

            var response = await client.GetAsync(new QueryFieldRockstarsDynamic { Age = 42 });
            Assert.That(response.Results.Count, Is.EqualTo(4));
        }

        [Test]
        public async Task Does_execute_typed_QueryFilters()
        {
            // QueryFilter appends additional: x => x.LastName.EndsWith("son")
            var response = await client.GetAsync(new QueryRockstarsFilter { Age = 27 });
            Assert.That(response.Results.Count, Is.EqualTo(1));

            var custom = await client.GetAsync(new QueryCustomRockstarsFilter { Age = 27 });
            Assert.That(custom.Results.Count, Is.EqualTo(1));

            response = await client.GetAsync(new QueryRockstarsIFilter { Age = 27 });
            Assert.That(response.Results.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task Can_execute_OR_QueryFilters()
        {
            var response = await client.GetAsync(new QueryOrRockstars { Age = 42, FirstName = "Jim" });
            Assert.That(response.Results.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task Does_retain_implicit_convention_when_not_overriding_template_or_ValueFormat()
        {
            var response = await client.GetAsync(new QueryFieldsImplicitConventions { FirstNameContains = "im" });
            Assert.That(response.Results.Count, Is.EqualTo(2));

            response = await client.GetAsync(new QueryFieldsImplicitConventions { LastNameEndsWith = "son" });
            Assert.That(response.Results.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task Can_execute_OR_QueryFilters_Fields()
        {
            var response = await client.GetAsync(new QueryOrRockstarsFields
            {
                FirstName = "Jim",
                LastName = "Vedder",
            });
            Assert.That(response.Results.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task Can_execute_Explicit_conventions()
        {
            var response = await client.GetAsync(new QueryRockstarsConventions { Ids = new[] {1, 2, 3} });
            Assert.That(response.Results.Count, Is.EqualTo(3));

            response = await client.GetAsync(new QueryRockstarsConventions { AgeOlderThan = 42 });
            Assert.That(response.Results.Count, Is.EqualTo(3));

            response = await client.GetAsync(new QueryRockstarsConventions { AgeGreaterThanOrEqualTo = 42 });
            Assert.That(response.Results.Count, Is.EqualTo(4));

            response = await client.GetAsync(new QueryRockstarsConventions { AgeGreaterThan = 42 });
            Assert.That(response.Results.Count, Is.EqualTo(3));
            response = await client.GetAsync(new QueryRockstarsConventions { GreaterThanAge = 42 });
            Assert.That(response.Results.Count, Is.EqualTo(3));

            response = await client.GetAsync(new QueryRockstarsConventions { FirstNameStartsWith = "Jim" });
            Assert.That(response.Results.Count, Is.EqualTo(2));
            response = await client.GetAsync(new QueryRockstarsConventions { LastNameEndsWith = "son" });
            Assert.That(response.Results.Count, Is.EqualTo(2));
            response = await client.GetAsync(new QueryRockstarsConventions { LastNameContains = "e" });
            Assert.That(response.Results.Count, Is.EqualTo(3));

            response = await client.GetAsync(new QueryRockstarsConventions { DateOfBirthGreaterThan = new DateTime(1960, 01, 01) });
            Assert.That(response.Results.Count, Is.EqualTo(3));
            response = await client.GetAsync(new QueryRockstarsConventions { DateDiedLessThan = new DateTime(1980, 01, 01) });
            Assert.That(response.Results.Count, Is.EqualTo(3));
        }

        [Test]
        public async Task Can_execute_In_OR_Queries()
        {
            QueryResponse<Rockstar> response;
            response = await client.GetAsync(new QueryGetRockstars());
            Assert.That(response.Results?.Count ?? 0, Is.EqualTo(0));

            response = await client.GetAsync(new QueryGetRockstars { Ids = new[] { 1, 2, 3 } });
            Assert.That(response.Results.Count, Is.EqualTo(3));

            response = await client.GetAsync(new QueryGetRockstars { Ages = new[] { 42, 44 }.ToList() });
            Assert.That(response.Results.Count, Is.EqualTo(2));

            response = await client.GetAsync(new QueryGetRockstars { FirstNames = new[] { "Jim", "Kurt" }.ToList() });
            Assert.That(response.Results.Count, Is.EqualTo(2));

            response = await client.GetAsync(new QueryGetRockstars { IdsBetween = new[] { 1, 3 } });
            Assert.That(response.Results.Count, Is.EqualTo(3));
        }

        [Test]
        public async Task Does_ignore_empty_collection_filters_by_default()
        {
            QueryResponse<Rockstar> response;
            response = await client.GetAsync(new QueryRockstarFilters());
            Assert.That(response.Results.Count, Is.EqualTo(AutoQueryAppHost.SeedRockstars.Length));

            response = await client.GetAsync(new QueryRockstarFilters
            {
                Ids = new int[] {},
                Ages = new List<int>(),
                FirstNames = new List<string>(),
                IdsBetween = new int[] {},               
            });
            Assert.That(response.Results.Count, Is.EqualTo(AutoQueryAppHost.SeedRockstars.Length));
        }

        [Test]
        public async Task Can_query_Movie_Ratings()
        {
            var response = await client.GetAsync(new QueryMovies { Ratings = new[] {"G","PG-13"} });
            Assert.That(response.Results.Count, Is.EqualTo(5));

            response = await client.GetAsync(new QueryMovies {
                Ids = new[] { 1, 2 },
                ImdbIds = new[] { "tt0071562", "tt0060196" },
                Ratings = new[] { "G", "PG-13" }
            });
            Assert.That(response.Results.Count, Is.EqualTo(9));
        }

        [Test]
        public async Task Does_implicitly_OrderBy_PrimaryKey_when_limits_is_specified()
        {
            var movies = await client.GetAsync(new SearchMovies { Take = 100 });
            var ids = movies.Results.Map(x => x.Id);
            var orderedIds = ids.OrderBy(x => x);
            Assert.That(ids, Is.EqualTo(orderedIds));

            var rockstars = await client.GetAsync(new SearchMovies { Take = 100 });
            ids = rockstars.Results.Map(x => x.Id);
            orderedIds = ids.OrderBy(x => x);
            Assert.That(ids, Is.EqualTo(orderedIds));
        }

        [Test]
        public async Task Can_OrderBy_queries()
        {
            var movies = await client.GetAsync(new SearchMovies { Take = 100, OrderBy = "ImdbId" });
            var ids = movies.Results.Map(x => x.ImdbId);
            var orderedIds = ids.OrderBy(x => x).ToList();
            Assert.That(ids, Is.EqualTo(orderedIds));

            movies = await client.GetAsync(new SearchMovies { Take = 100, OrderBy = "Rating,ImdbId" });
            ids = movies.Results.Map(x => x.ImdbId);
            orderedIds = movies.Results.OrderBy(x => x.Rating).ThenBy(x => x.ImdbId).Map(x => x.ImdbId);
            Assert.That(ids, Is.EqualTo(orderedIds));

            movies = await client.GetAsync(new SearchMovies { Take = 100, OrderByDesc = "ImdbId" });
            ids = movies.Results.Map(x => x.ImdbId);
            orderedIds = ids.OrderByDescending(x => x).ToList();
            Assert.That(ids, Is.EqualTo(orderedIds));

            movies = await client.GetAsync(new SearchMovies { Take = 100, OrderByDesc = "Rating,ImdbId" });
            ids = movies.Results.Map(x => x.ImdbId);
            orderedIds = movies.Results.OrderByDescending(x => x.Rating)
                .ThenByDescending(x => x.ImdbId).Map(x => x.ImdbId);
            Assert.That(ids, Is.EqualTo(orderedIds));

            movies = await client.GetAsync(new SearchMovies { Take = 100, OrderBy = "Rating,-ImdbId" });
            ids = movies.Results.Map(x => x.ImdbId);
            orderedIds = movies.Results.OrderBy(x => x.Rating)
                .ThenByDescending(x => x.ImdbId).Map(x => x.ImdbId);
            Assert.That(ids, Is.EqualTo(orderedIds));

            movies = await client.GetAsync(new SearchMovies { Take = 100, OrderByDesc = "Rating,-ImdbId" });
            ids = movies.Results.Map(x => x.ImdbId);
            orderedIds = movies.Results.OrderByDescending(x => x.Rating)
                .ThenBy(x => x.ImdbId).Map(x => x.ImdbId);
            Assert.That(ids, Is.EqualTo(orderedIds));
        }

        [Test]
        public async Task Does_not_query_Ignored_properties()
        {
            var response = await client.GetAsync(new QueryUnknownRockstars {
                UnknownProperty = "Foo",
                UnknownInt = 1,
                Include = "Total"
            });

            Assert.That(response.Offset, Is.EqualTo(0));
            Assert.That(response.Total, Is.EqualTo(TotalRockstars));
            Assert.That(response.Results.Count, Is.EqualTo(TotalRockstars));
        }

        [Test]
        public async Task Can_Query_Rockstars_with_References()
        {
            var response = await client.GetAsync(new QueryRockstarsWithReferences {
                Age = 27
            });
         
            Assert.That(response.Results.Count, Is.EqualTo(3));

            var jimi = response.Results.First(x => x.FirstName == "Jimi");
            Assert.That(jimi.Albums.Count, Is.EqualTo(1));
            Assert.That(jimi.Albums[0].Name, Is.EqualTo("Electric Ladyland"));

            var jim = response.Results.First(x => x.FirstName == "Jim");
            Assert.That(jim.Albums, Is.Null);

            var kurt = response.Results.First(x => x.FirstName == "Kurt");
            Assert.That(kurt.Albums.Count, Is.EqualTo(5));

            response = await client.GetAsync(new QueryRockstarsWithReferences
            {
                Age = 27,
                Fields = "Id,FirstName,Age"
            });
            Assert.That(response.Results.Count, Is.EqualTo(3));
            Assert.That(response.Results.All(x => x.Id > 0));
            Assert.That(response.Results.All(x => x.LastName == null));
            Assert.That(response.Results.All(x => x.Albums == null));

            response = await client.GetAsync(new QueryRockstarsWithReferences
            {
                Age = 27,
                Fields = "Id,FirstName,Age,Albums"
            });
            Assert.That(response.Results.Where(x => x.FirstName != "Jim").All(x => x.Albums != null));
        }

        [Test]
        public async Task Can_Query_RockstarReference_without_References()
        {
            var response = await client.GetAsync(new QueryCustomRockstarsReferences
            {
                Age = 27
            });
            Assert.That(response.Results.Count, Is.EqualTo(3));
            Assert.That(response.Results.All(x => x.Albums == null));
        }

        [Test]
        public async Task Can_Query_AllFields_Guid()
        {
            var guid = new Guid("3EE6865A-4149-4940-B7A2-F952E0FEFC5E");
            var response = await client.GetAsync(new QueryAllFields {
                Guid = guid
            });

            Assert.That(response.Results.Count, Is.EqualTo(1));

            Assert.That(response.Results[0].Guid, Is.EqualTo(guid));
        }

        [Test]
        public async Task Does_populate_Total()
        {
            var response = await client.GetAsync(new QueryRockstars { Include = "Total" });
            Assert.That(response.Total, Is.EqualTo(Rockstars.Count));
            Assert.That(response.Meta, Is.Null);

            response = await client.GetAsync(new QueryRockstars { Include = "COUNT" });
            Assert.That(response.Total, Is.EqualTo(Rockstars.Count));

            response = await client.GetAsync(new QueryRockstars { Include = "COUNT(*)" });
            Assert.That(response.Total, Is.EqualTo(Rockstars.Count));

            response = await client.GetAsync(new QueryRockstars { Include = "COUNT(DISTINCT LivingStatus), Total" });
            Assert.That(response.Total, Is.EqualTo(Rockstars.Count));

            response = await client.GetAsync(new QueryRockstars { Include = "Count(*), Min(Age), Max(Age), Sum(Id)" });
            Assert.That(response.Total, Is.EqualTo(Rockstars.Count));

            response = await client.GetAsync(new QueryRockstars { Age = 27, Include = "Count(*), Min(Age), Max(Age), Sum(Id)" });
            Assert.That(response.Total, Is.EqualTo(Rockstars.Count(x => x.Age == 27)));
        }

        [Test]
        public async Task Can_Include_Aggregates_in_AutoQuery()
        {
            var response = await client.GetAsync(new QueryRockstars { Include = "COUNT" });
            Assert.That(response.Meta["COUNT(*)"], Is.EqualTo(Rockstars.Count.ToString()));

            response = await client.GetAsync(new QueryRockstars { Include = "COUNT(*)" });
            Assert.That(response.Meta["COUNT(*)"], Is.EqualTo(Rockstars.Count.ToString()));

            response = await client.GetAsync(new QueryRockstars { Include = "COUNT(DISTINCT LivingStatus)" });
            Assert.That(response.Meta["COUNT(DISTINCT LivingStatus)"], Is.EqualTo("2"));

            response = await client.GetAsync(new QueryRockstars { Include = "MIN(Age)" });
            Assert.That(response.Meta["MIN(Age)"], Is.EqualTo(Rockstars.Map(x => x.Age).Min().ToString()));

            response = await client.GetAsync(new QueryRockstars { Include = "Count(*), Min(Age), Max(Age), Sum(Id), Avg(Age)", OrderBy = "Id" });
            Assert.That(response.Meta["Count(*)"], Is.EqualTo(Rockstars.Count.ToString()));
            Assert.That(response.Meta["Min(Age)"], Is.EqualTo(Rockstars.Map(x => x.Age).Min().ToString()));
            Assert.That(response.Meta["Max(Age)"], Is.EqualTo(Rockstars.Map(x => x.Age).Max().ToString()));
            Assert.That(response.Meta["Sum(Id)"], Is.EqualTo(Rockstars.Map(x => x.Id).Sum().ToString()));
            Assert.That(double.Parse(response.Meta["Avg(Age)"]), Is.EqualTo(Rockstars.Average(x => x.Age)).Within(1d));
            //Not supported by Sqlite
            //Assert.That(response.Meta["First(Id)"], Is.EqualTo(Rockstars.First().Id.ToString()));
            //Assert.That(response.Meta["Last(Id)"], Is.EqualTo(Rockstars.Last().Id.ToString()));

            response = await client.GetAsync(new QueryRockstars { Age = 27, Include = "Count(*), Min(Age), Max(Age), Sum(Id), Avg(Age)", OrderBy = "Id" });
            var rockstars27 = Rockstars.Where(x => x.Age == 27).ToList();
            Assert.That(response.Meta["Count(*)"], Is.EqualTo(rockstars27.Count.ToString()));
            Assert.That(response.Meta["Min(Age)"], Is.EqualTo(rockstars27.Map(x => x.Age).Min().ToString()));
            Assert.That(response.Meta["Max(Age)"], Is.EqualTo(rockstars27.Map(x => x.Age).Max().ToString()));
            Assert.That(response.Meta["Sum(Id)"], Is.EqualTo(rockstars27.Map(x => x.Id).Sum().ToString()));
            Assert.That(double.Parse(response.Meta["Avg(Age)"]), Is.EqualTo(rockstars27.Average(x => x.Age)).Within(1d));
            //Not supported by Sqlite
            //Assert.That(response.Meta["First(Id)"], Is.EqualTo(rockstars27.First().Id.ToString()));
            //Assert.That(response.Meta["Last(Id)"], Is.EqualTo(rockstars27.Last().Id.ToString()));
        }

        [Test]
        public async Task Does_ignore_unknown_aggregate_commands()
        {
            var response = await client.GetAsync(new QueryRockstars { Include = "FOO(1), Total" });
            Assert.That(response.Total, Is.EqualTo(Rockstars.Count));
            Assert.That(response.Meta, Is.Null);

            response = await client.GetAsync(new QueryRockstars { Include = "FOO(1), Min(Age), Bar('a') alias, Count(*), Baz(1,'foo')" });
            Assert.That(response.Total, Is.EqualTo(Rockstars.Count));
            Assert.That(response.Meta["Min(Age)"], Is.EqualTo(Rockstars.Map(x => x.Age).Min().ToString()));
            Assert.That(response.Meta["Count(*)"], Is.EqualTo(Rockstars.Count.ToString()));
        }

        [Test]
        public async Task Can_Include_Aggregates_in_AutoQuery_with_Aliases()
        {
            var response = await client.GetAsync(new QueryRockstars { Include = "COUNT(*) count" });
            Assert.That(response.Meta["count"], Is.EqualTo(Rockstars.Count.ToString()));

            response = await client.GetAsync(new QueryRockstars { Include = "COUNT(DISTINCT LivingStatus) as uniquestatus" });
            Assert.That(response.Meta["uniquestatus"], Is.EqualTo("2"));

            response = await client.GetAsync(new QueryRockstars { Include = "MIN(Age) minage" });
            Assert.That(response.Meta["minage"], Is.EqualTo(Rockstars.Map(x => x.Age).Min().ToString()));

            response = await client.GetAsync(new QueryRockstars { Include = "Count(*) count, Min(Age) min, Max(Age) max, Sum(Id) sum" });
            Assert.That(response.Meta["count"], Is.EqualTo(Rockstars.Count.ToString()));
            Assert.That(response.Meta["min"], Is.EqualTo(Rockstars.Map(x => x.Age).Min().ToString()));
            Assert.That(response.Meta["max"], Is.EqualTo(Rockstars.Map(x => x.Age).Max().ToString()));
            Assert.That(response.Meta["sum"], Is.EqualTo(Rockstars.Map(x => x.Id).Sum().ToString()));
        }

        [Test]
        public async Task Can_execute_custom_aggregate_functions()
        {
            var response = await client.GetAsync(new QueryRockstars {
                Include = "ADD(6,2), Multiply(6,2) SixTimesTwo, Subtract(6,2), divide(6,2) TheDivide"
            });
            Assert.That(response.Meta["ADD(6,2)"], Is.EqualTo("8"));
            Assert.That(response.Meta["SixTimesTwo"], Is.EqualTo("12"));
            Assert.That(response.Meta["Subtract(6,2)"], Is.EqualTo("4"));
            Assert.That(response.Meta["TheDivide"], Is.EqualTo("3"));
        }

        [Test]
        public async Task Sending_empty_ChangeDb_returns_default_info()
        {
            var response = await client.GetAsync(new ChangeDb());
            Assert.That(response.Results.Count, Is.EqualTo(TotalRockstars));

            var aqResponse = await client.GetAsync(new QueryChangeDb());
            Assert.That(aqResponse.Results.Count, Is.EqualTo(TotalRockstars));
        }

        [Test]
        public async Task Can_ChangeDb_with_Named_Connection()
        {
            var response = await client.GetAsync(new ChangeDb { NamedConnection = AutoQueryAppHost.SqlServerNamedConnection });
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Results[0].FirstName, Is.EqualTo("Microsoft"));

            var aqResponse = await client.GetAsync(new QueryChangeDb { NamedConnection = AutoQueryAppHost.SqlServerNamedConnection });
            Assert.That(aqResponse.Results.Count, Is.EqualTo(1));
            Assert.That(aqResponse.Results[0].FirstName, Is.EqualTo("Microsoft"));
        }

        [Test]
        public async Task Can_ChangeDb_with_ConnectionString()
        {
            var response = await client.GetAsync(new ChangeDb { ConnectionString = AutoQueryAppHost.SqliteFileConnString });
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Results[0].FirstName, Is.EqualTo("Sqlite"));

            var aqResponse = await client.GetAsync(new QueryChangeDb { ConnectionString = AutoQueryAppHost.SqliteFileConnString });
            Assert.That(aqResponse.Results.Count, Is.EqualTo(1));
            Assert.That(aqResponse.Results[0].FirstName, Is.EqualTo("Sqlite"));
        }

        [Test]
        public async Task Can_ChangeDb_with_ConnectionString_and_Provider()
        {
            var response = await client.GetAsync(new ChangeDb
            {
                ConnectionString = AutoQueryAppHost.SqlServerConnString,
                ProviderName = AutoQueryAppHost.SqlServerProvider,
            });
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Results[0].FirstName, Is.EqualTo("Microsoft"));

            var aqResponse = await client.GetAsync(new QueryChangeDb
            {
                ConnectionString = AutoQueryAppHost.SqlServerConnString,
                ProviderName = AutoQueryAppHost.SqlServerProvider,
            });
            Assert.That(aqResponse.Results.Count, Is.EqualTo(1));
            Assert.That(aqResponse.Results[0].FirstName, Is.EqualTo("Microsoft"));
        }

        [Test]
        public async Task Can_Change_Named_Connection_with_ConnectionInfoAttribute()
        {
            var response = await client.GetAsync(new ChangeConnectionInfo());
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Results[0].FirstName, Is.EqualTo("Microsoft"));

            var aqResponse = await client.GetAsync(new QueryChangeConnectionInfo());
            Assert.That(aqResponse.Results.Count, Is.EqualTo(1));
            Assert.That(aqResponse.Results[0].FirstName, Is.EqualTo("Microsoft"));
        }

        [Test]
        public async Task Does_return_MaxLimit_results()
        {
            QueryResponse<PagingTest> response;
            response = await client.GetAsync(new QueryPagingTest { Include = "Total" });
            Assert.That(response.Results.Count, Is.EqualTo(100));
            Assert.That(response.Total, Is.EqualTo(PagingTests.Count));

            response = await client.GetAsync(new QueryPagingTest { Skip = 200, Include = "Total" });
            Assert.That(response.Results.Count, Is.EqualTo(PagingTests.Skip(200).Count()));
            Assert.That(response.Total, Is.EqualTo(PagingTests.Count));

            response = await client.GetAsync(new QueryPagingTest { Value = 1, Include = "Total" });
            Assert.That(response.Results.Count, Is.EqualTo(100));
            Assert.That(response.Total, Is.EqualTo(PagingTests.Count(x => x.Value == 1)));
        }

        [Test]
        public async Task Can_query_on_ForeignKey_and_Index()
        {
            QueryResponse<RockstarAlbum> response;
            response = await client.GetAsync(new QueryRockstarAlbums { RockstarId = 3, Include = "Total" }); //Hash
            Assert.That(response.Results.Count, Is.EqualTo(5));
            Assert.That(response.Total, Is.EqualTo(5));

            response = await client.GetAsync(new QueryRockstarAlbums { RockstarId = 3, Id = 3, Include = "Total" }); //Hash + Range
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Total, Is.EqualTo(1));
            Assert.That(response.Results[0].Name, Is.EqualTo("Nevermind"));

            //Hash + Range BETWEEN
            response = await client.GetAsync(new QueryRockstarAlbums
            {
                RockstarId = 3,
                IdBetween = new[] { 2, 3 },
                Include = "Total"
            });
            Assert.That(response.Results.Count, Is.EqualTo(2));
            Assert.That(response.Total, Is.EqualTo(2));

            //Hash + Range BETWEEN + Filter
            response = await client.GetAsync(new QueryRockstarAlbums
            {
                RockstarId = 3,
                IdBetween = new[] { 2, 3 },
                Name = "Nevermind",
                Include = "Total"
            });
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Total, Is.EqualTo(1));
            Assert.That(response.Results[0].Id, Is.EqualTo(3));

            //Hash + LocalSecondaryIndex
            response = await client.GetAsync(new QueryRockstarAlbums { RockstarId = 3, Genre = "Grunge", Include = "Total" });
            Assert.That(response.Results.Count, Is.EqualTo(4));
            Assert.That(response.Total, Is.EqualTo(4));

            response.PrintDump();
        }
    }
}