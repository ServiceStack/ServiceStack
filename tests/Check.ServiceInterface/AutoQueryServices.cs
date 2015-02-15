using System;
using System.Collections.Generic;
using Check.ServiceModel;
using ServiceStack;
using ServiceStack.DataAnnotations;

namespace Check.ServiceInterface
{
    [Route("/query/rockstars")]
    public class QueryRockstars : QueryBase<Rockstar>
    {
        public int? Age { get; set; }
    }

    public class QueryRockstarsConventions : QueryBase<Rockstar>
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
    public class QueryCustomRockstars : QueryBase<Rockstar, CustomRockstar>
    {
        public int? Age { get; set; }
    }

    [Route("/customrockstars")]
    public class QueryRockstarAlbums : QueryBase<Rockstar, CustomRockstar>, IJoin<Rockstar, RockstarAlbum>
    {
        public int? Age { get; set; }
        public string RockstarAlbumName { get; set; }
    }

    public class QueryRockstarAlbumsImplicit : QueryBase<Rockstar, CustomRockstar>, IJoin<Rockstar, RockstarAlbum>
    {
    }

    public class QueryRockstarAlbumsLeftJoin : QueryBase<Rockstar, CustomRockstar>, ILeftJoin<Rockstar, RockstarAlbum>
    {
        public int? Age { get; set; }
        public string AlbumName { get; set; }
    }


    public class QueryOverridedRockstars : QueryBase<Rockstar>
    {
        public int? Age { get; set; }
    }

    public class QueryOverridedCustomRockstars : QueryBase<Rockstar, CustomRockstar>
    {
        public int? Age { get; set; }
    }

    public class QueryFieldRockstars : QueryBase<Rockstar>
    {
        public string FirstName { get; set; } //default to 'AND FirstName = {Value}'

        public string[] FirstNames { get; set; } //Collections default to 'FirstName IN ({Values})

        [QueryField(Operand = ">=")]
        public int? Age { get; set; }

        [QueryField(Template = "UPPER({Field}) LIKE UPPER({Value})", Field = "FirstName")]
        public string FirstNameCaseInsensitive { get; set; }

        [QueryField(Template = "{Field} LIKE {Value}", Field = "FirstName", ValueFormat = "{0}%")]
        public string FirstNameStartsWith { get; set; }

        [QueryField(Template = "{Field} LIKE {Value}", Field = "LastName", ValueFormat = "%{0}")]
        public string LastNameEndsWith { get; set; }

        [QueryField(Template = "{Field} BETWEEN {Value1} AND {Value2}", Field = "FirstName")]
        public string[] FirstNameBetween { get; set; }

        [QueryField(Term = QueryTerm.Or, Template = "UPPER({Field}) LIKE UPPER({Value})", Field = "LastName")]
        public string OrLastName { get; set; }
    }

    public class QueryFieldRockstarsDynamic : QueryBase<Rockstar>
    {
        public int? Age { get; set; }
    }

    public class QueryRockstarsFilter : QueryBase<Rockstar>
    {
        public int? Age { get; set; }
    }

    public class QueryCustomRockstarsFilter : QueryBase<Rockstar, CustomRockstar>
    {
        public int? Age { get; set; }
    }

    public interface IFilterRockstars { }
    public class QueryRockstarsIFilter : QueryBase<Rockstar>, IFilterRockstars
    {
        public int? Age { get; set; }
    }

    [Query(QueryTerm.Or)]
    [Route("/OrRockstars")]
    public class QueryOrRockstars : QueryBase<Rockstar>
    {
        public int? Age { get; set; }
        public string FirstName { get; set; }
    }

    [Query(QueryTerm.Or)]
    public class QueryGetRockstars : QueryBase<Rockstar>
    {
        public int[] Ids { get; set; }
        public List<int> Ages { get; set; }
        public List<string> FirstNames { get; set; }
        public int[] IdsBetween { get; set; }
    }

    [Query(QueryTerm.Or)]
    public class QueryGetRockstarsDynamic : QueryBase<Rockstar> { }

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
    [Query(QueryTerm.And)] //Default
    public class SearchMovies : QueryBase<Movie> { }

    [Route("/movies")]
    [Query(QueryTerm.Or)]
    public class QueryMovies : QueryBase<Movie>
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

    public class StreamMovies : QueryBase<Movie>
    {
        public string[] Ratings { get; set; }
    }

    public class QueryUnknownRockstars : QueryBase<Rockstar>
    {
        public int UnknownInt { get; set; }
        public string UnknownProperty { get; set; }

    }
    [Route("/query/rockstar-references")]
    public class QueryRockstarsWithReferences : QueryBase<RockstarReference>
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
        public IAutoQuery AutoQuery { get; set; }

        //Override with custom impl
        public QueryResponse<Rockstar> Any(QueryRockstars dto)
        {
            var q = AutoQuery.CreateQuery(dto, Request.GetRequestParams());
            q.Take(1);
            return AutoQuery.Execute(dto, q);
        }
    }
}