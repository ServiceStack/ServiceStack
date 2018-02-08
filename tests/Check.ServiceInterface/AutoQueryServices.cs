using System;
using System.Collections.Generic;
using Check.ServiceModel;
using ServiceStack;
using ServiceStack.DataAnnotations;

namespace Check.ServiceInterface
{
    [Route("/querydata/rockstars")]
    public class QueryDataRockstars : QueryData<Rockstar>
    {
        public int? Age { get; set; }
    }

    [Route("/query/rockstars")]
    public class QueryRockstars : QueryDb<Rockstar>
    {
        public int? Age { get; set; }
    }

    [Route("/query/rockstars/cached")]
    public class QueryRockstarsCached : QueryDb<Rockstar>
    {
        public int? Age { get; set; }
    }

    [ConnectionInfo(NamedConnection = "pgsql")]
    [Route("/pgsql/rockstars")]
    public class QueryPostgresRockstars : QueryDb<Rockstar>
    {
        public int? Age { get; set; }
    }

    [NamedConnection("pgsql")]
    public class PgRockstar : Rockstar {}

    [Route("/pgsql/pgrockstars")]
    public class QueryPostgresPgRockstars : QueryDb<PgRockstar>
    {
        public int? Age { get; set; }
    }

    public class QueryRockstarsConventions : QueryDb<Rockstar>
    {
        public int[] Ids { get; set; }
        public int? AgeOlderThan { get; set; }
        public int? AgeGreaterThanOrEqualTo { get; set; }
        public int? AgeGreaterThan { get; set; }
        public int? GreaterThanAge { get; set; }
        public string FirstNameStartsWith { get; set; }
        public string LastNameEndsWith { get; set; }
        public string LastNameContains { get; set; }
        public string RockstarAlbumNameContains { get; set; }
        public int? RockstarIdAfter { get; set; }
        public int? RockstarIdOnOrAfter { get; set; }
    }

    [AutoQueryViewer(Title = "Search for Rockstars", Description = "Use this option to search for Rockstars!")]
    public class QueryCustomRockstars : QueryDb<Rockstar, CustomRockstar>
    {
        public int? Age { get; set; }
    }

    [Route("/customrockstars")]
    public class QueryRockstarAlbums : QueryDb<Rockstar, CustomRockstar>, IJoin<Rockstar, RockstarAlbum>
    {
        public int? Age { get; set; }
        public string RockstarAlbumName { get; set; }
    }

    public class QueryRockstarAlbumsImplicit : QueryDb<Rockstar, CustomRockstar>, IJoin<Rockstar, RockstarAlbum>
    {
    }

    public class QueryRockstarAlbumsLeftJoin : QueryDb<Rockstar, CustomRockstar>, ILeftJoin<Rockstar, RockstarAlbum>
    {
        public int? Age { get; set; }
        public string AlbumName { get; set; }
    }


    public class QueryOverridedRockstars : QueryDb<Rockstar>
    {
        public int? Age { get; set; }
    }

    public class QueryOverridedCustomRockstars : QueryDb<Rockstar, CustomRockstar>
    {
        public int? Age { get; set; }
    }

    [Route("/query-custom/rockstars")]
    public class QueryFieldRockstars : QueryDb<Rockstar>
    {
        public string FirstName { get; set; } //default to 'AND FirstName = {Value}'

        public string[] FirstNames { get; set; } //Collections default to 'FirstName IN ({Values})

        [QueryDbField(Operand = ">=")]
        public int? Age { get; set; }

        [QueryDbField(Template = "UPPER({Field}) LIKE UPPER({Value})", Field = "FirstName")]
        public string FirstNameCaseInsensitive { get; set; }

        [QueryDbField(Template = "{Field} LIKE {Value}", Field = "FirstName", ValueFormat = "{0}%")]
        public string FirstNameStartsWith { get; set; }

        [QueryDbField(Template = "{Field} LIKE {Value}", Field = "LastName", ValueFormat = "%{0}")]
        public string LastNameEndsWith { get; set; }

        [QueryDbField(Template = "{Field} BETWEEN {Value1} AND {Value2}", Field = "FirstName")]
        public string[] FirstNameBetween { get; set; }

        [QueryDbField(Term = QueryTerm.Or, Template = "UPPER({Field}) LIKE UPPER({Value})", Field = "LastName")]
        public string OrLastName { get; set; }

        [QueryDbField(Template = "{Field} LIKE {Value1} OR {Field} LIKE {Value2}", Field = "FirstName", ValueFormat = "%{0}%")]
        public string[] FirstNameContainsMulti { get; set; }
    }

    public class QueryFieldRockstarsDynamic : QueryDb<Rockstar>
    {
        public int? Age { get; set; }
    }

    public class QueryRockstarsFilter : QueryDb<Rockstar>
    {
        public int? Age { get; set; }
    }

    public class QueryCustomRockstarsFilter : QueryDb<Rockstar, CustomRockstar>
    {
        public int? Age { get; set; }
    }

    public interface IFilterRockstars { }
    public class QueryRockstarsIFilter : QueryDb<Rockstar>, IFilterRockstars
    {
        public int? Age { get; set; }
    }

    [QueryDb(QueryTerm.Or)]
    [Route("/OrRockstars")]
    public class QueryOrRockstars : QueryDb<Rockstar>
    {
        public int? Age { get; set; }
        public string FirstName { get; set; }
    }

    [QueryDb(QueryTerm.Or)]
    public class QueryGetRockstars : QueryDb<Rockstar>
    {
        public int[] Ids { get; set; }
        public List<int> Ages { get; set; }
        public List<string> FirstNames { get; set; }
        public int[] IdsBetween { get; set; }
    }

    [QueryDb(QueryTerm.Or)]
    public class QueryGetRockstarsDynamic : QueryDb<Rockstar> { }

    public class RockstarAlbum
    {
        [AutoIncrement]
        public int Id { get; set; }
        public int RockstarId { get; set; }
        public string Name { get; set; }
    }

    public class CustomRockstar
    {
        [AutoQueryViewerField(Title = "Name")]
        public string FirstName { get; set; }

        [AutoQueryViewerField(HideInSummary = true)]
        public string LastName { get; set; }
        public int? Age { get; set; }

        [AutoQueryViewerField(Title = "Album")]
        public string RockstarAlbumName { get; set; }

        [AutoQueryViewerField(Title = "Genre")]
        public string RockstarGenreName { get; set; }
    }

    [Route("/movies/search")]
    [QueryDb(QueryTerm.And)] //Default
    public class SearchMovies : QueryDb<Movie> { }

    [Route("/movies")]
    [QueryDb(QueryTerm.Or)]
    public class QueryMovies : QueryDb<Movie>
    {
        public int[] Ids { get; set; }
        public string[] ImdbIds { get; set; }
        public string[] Ratings { get; set; }
    }

    public class Movie
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string ImdbId { get; set; }
        public string Title { get; set; }
        public string Rating { get; set; }
        public decimal Score { get; set; }
        public string Director { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string TagLine { get; set; }
        public List<string> Genres { get; set; }
    }

    public class StreamMovies : QueryDb<Movie>
    {
        public string[] Ratings { get; set; }
    }

    public class QueryUnknownRockstars : QueryDb<Rockstar>
    {
        public int UnknownInt { get; set; }
        public string UnknownProperty { get; set; }

    }
    [Route("/query/rockstar-references")]
    public class QueryRockstarsWithReferences : QueryDb<RockstarReference>
    {
        public int? Age { get; set; }
    }

    [Alias("Rockstar")]
    public class RockstarReference
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Age { get; set; }

        [Reference]
        public List<RockstarAlbum> Albums { get; set; }
    }

    public class AutoQueryService : Service
    {
        public IAutoQueryDb AutoQuery { get; set; }

        //Override with custom impl
        public QueryResponse<Rockstar> Any(QueryRockstars dto)
        {
            var q = AutoQuery.CreateQuery(dto, Request.GetRequestParams(), Request);
            //q.Take(1);
            return AutoQuery.Execute(dto, q);
        }
    }

    [CacheResponse(Duration = 3600)]
    public class AutoQueryCachedServices : Service
    {
        public IAutoQueryDb AutoQuery { get; set; }

        public QueryResponse<Rockstar> Any(QueryRockstarsCached request) =>
            AutoQuery.Execute(request, AutoQuery.CreateQuery(request, Request.GetRequestParams()));
    }
}