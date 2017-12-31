using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Threading;
using Funq;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;
using ServiceStack.Text;
using TestsConfig = ServiceStack.WebHost.Endpoints.Tests.Config;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class AutoQueryAppHost : AppSelfHostBase
    {
        public AutoQueryAppHost()
            : base("AutoQuery", typeof(AutoQueryService).Assembly) { }

        public static readonly string SqlServerConnString = TestsConfig.SqlServerConnString;
        public const string SqlServerNamedConnection = "SqlServer";
        public const string SqlServerProvider = "SqlServer2012";

        public static string SqliteFileConnString = "~/App_Data/autoquery.sqlite".MapProjectPath();

        public override void Configure(Container container)
        {
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
                                Func<int, int, int> fn;
                                if (!supportedFns.TryGetValue(cmd.Name.ToString(), out fn)) continue;
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
    }

    [Alias("Rockstar")]
    [NamedConnection("SqlServer")]
    public class NamedRockstar : Rockstar { }

    [Route("/query/namedrockstars")]
    public class QueryNamedRockstars : QueryDb<NamedRockstar>
    {
        public int? Age { get; set; }
    }

    [Route("/query/rockstars")]
    public class QueryRockstars : QueryDb<Rockstar>
    {
        public int? Age { get; set; }
        //public LivingStatus? LivingStatus { get; set; }
    }

    [Route("/query/rockstaralbums")]
    public class QueryRockstarAlbums : QueryDb<RockstarAlbum>
    {
        public int? Id { get; set; }
        public int? RockstarId { get; set; }
        public string Name { get; set; }
        public string Genre { get; set; }
        public int[] IdBetween { get; set; }
    }

    [Route("/query/pagingtest")]
    public class QueryPagingTest : QueryDb<PagingTest>
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public int? Value { get; set; }
    }

    public class QueryRockstarsConventions : QueryDb<Rockstar>
    {
        public DateTime? DateOfBirthGreaterThan { get; set; }
        public DateTime? DateDiedLessThan { get; set; }
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

    public class QueryCustomRockstars : QueryDb<Rockstar, CustomRockstar>
    {
        public int? Age { get; set; }
    }

    [Route("/customrockstars")]
    public class QueryJoinedRockstarAlbums : QueryDb<Rockstar, CustomRockstar>, IJoin<Rockstar, RockstarAlbum>
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
        public int? IdNotEqualTo { get; set; }
    }

    public class QueryRockstarAlbumsCustomLeftJoin : QueryDb<Rockstar, CustomRockstar>
    {
        public int? Age { get; set; }
        public string AlbumName { get; set; }
        public int? IdNotEqualTo { get; set; }
    }

    public class QueryMultiJoinRockstar : QueryDb<Rockstar, CustomRockstar>, 
        IJoin<Rockstar, RockstarAlbum>,
        IJoin<Rockstar, RockstarGenre>
    {
        public int? Age { get; set; }
        public string RockstarAlbumName { get; set; }
        public string RockstarGenreName { get; set; }
    }

    public class QueryOverridedRockstars : QueryDb<Rockstar>
    {
        public int? Age { get; set; }
    }

    public class QueryOverridedCustomRockstars : QueryDb<Rockstar, CustomRockstar>
    {
        public int? Age { get; set; }
    }

    public class QueryCaseInsensitiveOrderBy : QueryDb<Rockstar>
    {
        public int? Age { get; set; }
    }

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
    }

    public class QueryRockstarAlias : QueryDb<Rockstar, RockstarAlias>,
        IJoin<Rockstar, RockstarAlbum>
    {
        public int? Age { get; set; }
        public string RockstarAlbumName { get; set; }
    }

    [DataContract]
    public class RockstarAlias
    {
        [DataMember]
        [Alias("Id")]
        public int RockstarId { get; set; }

        [DataMember]
        public string FirstName { get; set; }

        [DataMember]
        [Alias("LastName")]
        public string Surname { get; set; }

        [DataMember(Name = "album")]
        public string RockstarAlbumName { get; set; }
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

    [Route("/OrRockstarsFields")]
    public class QueryOrRockstarsFields : QueryDb<Rockstar>
    {
        [QueryDbField(Term = QueryTerm.Or)]
        public string FirstName { get; set; }

        [QueryDbField(Term = QueryTerm.Or)]
        public string LastName { get; set; }
    }

    public class QueryFieldsImplicitConventions : QueryDb<Rockstar>
    {
        [QueryDbField(Term = QueryTerm.Or)]
        public string FirstNameContains { get; set; }

        [QueryDbField(Term = QueryTerm.Or)]
        public string LastNameEndsWith { get; set; }
    }

    [QueryDb(QueryTerm.Or)]
    public class QueryGetRockstars : QueryDb<Rockstar>
    {
        public int[] Ids { get; set; }
        public List<int> Ages { get; set; }
        public List<string> FirstNames { get; set; }
        public int[] IdsBetween { get; set; }
    }

    public class QueryRockstarFilters : QueryDb<Rockstar>
    {
        public int[] Ids { get; set; }
        public List<int> Ages { get; set; }
        public List<string> FirstNames { get; set; }
        public int[] IdsBetween { get; set; }
    }

    [QueryDb(QueryTerm.Or)]
    public class QueryGetRockstarsDynamic : QueryDb<Rockstar> {}

    [References(typeof(RockstarAlbumGenreGlobalIndex))]
    public class RockstarAlbum
    {
        [AutoIncrement]
        public int Id { get; set; }
        [References(typeof(Rockstar))]
        public int RockstarId { get; set; }
        public string Name { get; set; }
        [Index]
        public string Genre { get; set; }
    }

    public class RockstarGenre
    {
        [AutoIncrement]
        public int Id { get; set; }
        public int RockstarId { get; set; }
        public string Name { get; set; }
    }

    public class CustomRockstar
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Age { get; set; }
        public string RockstarAlbumName { get; set; }
        public string RockstarGenreName { get; set; }
    }

    public class QueryCustomRockstarsSchema : QueryDb<Rockstar, CustomRockstarSchema>
    {
        public int? Age { get; set; }
    }

    [Schema("dbo")]
    public class CustomRockstarSchema
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Age { get; set; }
        public string RockstarAlbumName { get; set; }
        public string RockstarGenreName { get; set; }
    }

    [Route("/movies/search")]
    [QueryDb(QueryTerm.And)] //Default
    public class SearchMovies : QueryDb<Movie> {}

    [Route("/movies")]
    [QueryDb(QueryTerm.Or)]
    public class QueryMovies : QueryDb<Movie>
    {
        public int[] Ids { get; set; }
        public string[] ImdbIds { get; set; }
        public string[] Ratings { get; set; }
    }

    [References(typeof(MovieTitleIndex))]
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

    public class QueryCustomRockstarsReferences : QueryDb<RockstarReference>
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

    [Route("/query/all-fields")]
    public class QueryAllFields : QueryDb<AllFields>
    {
        public virtual Guid? Guid { get; set; }
    }

    public class AllFields
    {
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
        public virtual Guid Guid { get; set; }
        public virtual DateTime? NullableDateTime { get; set; }
        public virtual TimeSpan? NullableTimeSpan { get; set; }
        public virtual Guid? NullableGuid { get; set; }
        public HttpStatusCode Enum { get; set; }
        public HttpStatusCode? NullableEnum { get; set; }
    }

    [EnumAsInt]
    public enum SomeEnumAsInt
    {
        Value1 = 1,
        Value2 = 2,
        Value3 = 3,
    }

    public enum SomeEnum
    {
        Value1 = 1,
        Value2 = 2,
        Value3 = 3
    }

    public class TypeWithEnum
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public SomeEnum SomeEnum { get; set; }
        public SomeEnumAsInt SomeEnumAsInt { get; set; }
        public SomeEnum? NSomeEnum { get; set; }
        public SomeEnumAsInt? NSomeEnumAsInt { get; set; }
    }

    [Route("/query-enums")]
    public class QueryTypeWithEnums : QueryDb<TypeWithEnum> {}

    [DataContract]
    public class Adhoc
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember(Name = "first_name")]
        public string FirstName { get; set; }

        [DataMember]
        public string LastName { get; set; }
    }

    [DataContract]
    [Route("/adhoc-rockstars")]
    public class QueryAdhocRockstars : QueryDb<Rockstar>
    {
        [DataMember(Name = "first_name")]
        public string FirstName { get; set; }
    }

    [DataContract]
    [Route("/adhoc")]
    public class QueryAdhoc : QueryDb<Adhoc> {}

    public class AutoQueryService : Service
    {
        public IAutoQueryDb AutoQuery { get; set; }

        //Override with custom impl
        public object Any(QueryOverridedRockstars dto)
        {
            var q = AutoQuery.CreateQuery(dto, Request.GetRequestParams());
            q.Take(1);
            return AutoQuery.Execute(dto, q);
        }

        public object Any(QueryOverridedCustomRockstars dto)
        {
            var q = AutoQuery.CreateQuery(dto, Request.GetRequestParams());
            q.Take(1);
            return AutoQuery.Execute(dto, q);
        }

        public object Any(QueryCaseInsensitiveOrderBy dto)
        {
            var q = AutoQuery.CreateQuery(dto, Request);
            if (q.OrderByExpression != null)
                q.OrderByExpression += " COLLATE NOCASE";

            return AutoQuery.Execute(dto, q);
        }

        public object Any(StreamMovies dto)
        {
            var q = AutoQuery.CreateQuery(dto, Request.GetRequestParams());
            q.Take(2);
            return AutoQuery.Execute(dto, q);
        }

        public object Any(QueryCustomRockstarsReferences request)
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

        public object Any(QueryRockstarAlbumsCustomLeftJoin query)
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
    public class QueryChangeDb : QueryDb<Rockstar>, IChangeDb
    {
        public string NamedConnection { get; set; }
        public string ConnectionString { get; set; }
        public string ProviderName { get; set; }
    }

    [Route("/changedb")]
    public class ChangeDb : IReturn<ChangeDbResponse>, IChangeDb
    {
        public string NamedConnection { get; set; }
        public string ConnectionString { get; set; }
        public string ProviderName { get; set; }
    }

    public class ChangeDbResponse
    {
        public List<Rockstar> Results { get; set; }
    }

    public class DynamicDbServices : Service
    {
        public object Any(ChangeDb request)
        {
            return new ChangeDbResponse { Results = Db.Select<Rockstar>() };
        }
    }

    public class ChangeConnectionInfo : IReturn<ChangeDbResponse> { }
    public class QueryChangeConnectionInfo : QueryDb<Rockstar> { }

    [ConnectionInfo(NamedConnection = AutoQueryAppHost.SqlServerNamedConnection)]
    public class NamedConnectionServices : Service
    {
        public IAutoQueryDb AutoQuery { get; set; }

        public object Any(ChangeConnectionInfo request)
        {
            return new ChangeDbResponse { Results = Db.Select<Rockstar>() };
        }

        public object Any(QueryChangeConnectionInfo query)
        {
            return AutoQuery.Execute(query, AutoQuery.CreateQuery(query, Request));
        }
    }

    [TestFixture]
    public class AutoQueryTests
    {
        private readonly ServiceStackHost appHost;
        public IServiceClient client;

        private static readonly int TotalRockstars = AutoQueryAppHost.SeedRockstars.Length;
        private static readonly int TotalAlbums = AutoQueryAppHost.SeedAlbums.Length;

        public AutoQueryTests()
        {
            appHost = new AutoQueryAppHost()
                .Init()
                .Start(Config.ListeningOn);

            client = new JsonServiceClient(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        public List<Rockstar> Rockstars
        {
            get { return AutoQueryAppHost.SeedRockstars.ToList(); }
        }

        public List<PagingTest> PagingTests
        {
            get { return AutoQueryAppHost.SeedPagingTest.ToList(); }
        }

//        [NUnit.Framework.Ignore("Debug Run"), Test]
        public void RunFor10Mins()
        {
#if NET45
            Process.Start(Config.ListeningOn);
#endif
            Thread.Sleep(TimeSpan.FromMinutes(10));
        }

        [Test]
        public void Can_execute_basic_query()
        {
            var response = client.Get(new QueryRockstars { Include = "Total" });

            Assert.That(response.Offset, Is.EqualTo(0));
            Assert.That(response.Total, Is.EqualTo(TotalRockstars));
            Assert.That(response.Results.Count, Is.EqualTo(TotalRockstars));
        }

        [Test]
        public void Can_execute_basic_query_NamedRockstar()
        {
            var response = client.Get(new QueryNamedRockstars { Include = "Total" });

            Assert.That(response.Offset, Is.EqualTo(0));
            Assert.That(response.Total, Is.EqualTo(1));
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Results[0].LastName, Is.EqualTo("SQL Server"));
        }

        [Test]
        public void Can_execute_overridden_basic_query()
        {
            var response = client.Get(new QueryOverridedRockstars { Include = "Total" });

            Assert.That(response.Offset, Is.EqualTo(0));
            Assert.That(response.Total, Is.EqualTo(TotalRockstars));
            Assert.That(response.Results.Count, Is.EqualTo(1));
        }

        [Test]
        public void Can_execute_overridden_basic_query_with_case_insensitive_orderBy()
        {
            var response = client.Get(new QueryCaseInsensitiveOrderBy { Age = 27, OrderBy = "FirstName" });

            Assert.That(response.Results.Count, Is.EqualTo(3));
        }

        [Test]
        public void Can_execute_AdhocRockstars_query()
        {
            var request = new QueryAdhocRockstars { FirstName = "Jimi", Include = "Total" };

            Assert.That(request.ToGetUrl(), Is.EqualTo("/adhoc-rockstars?first_name=Jimi&include=Total"));

            var response = client.Get(request);

            Assert.That(response.Offset, Is.EqualTo(0));
            Assert.That(response.Total, Is.EqualTo(1));
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Results[0].FirstName, Is.EqualTo(request.FirstName));
        }

        [Test]
        public void Can_execute_Adhoc_query_alias()
        {
            var response = Config.ListeningOn.CombineWith("adhoc")
                .AddQueryParam("first_name", "Jimi")
                .GetJsonFromUrl()
                .FromJson<QueryResponse<Adhoc>>();

            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Results[0].FirstName, Is.EqualTo("Jimi"));
        }

        [Test]
        public void Can_execute_Adhoc_query_convention()
        {
            var response = Config.ListeningOn.CombineWith("adhoc")
                .AddQueryParam("last_name", "Hendrix")
                .GetJsonFromUrl()
                .FromJson<QueryResponse<Adhoc>>();
            Assert.That(response.Results.Count, Is.EqualTo(7));

            JsConfig.EmitLowercaseUnderscoreNames = true;
            response = Config.ListeningOn.CombineWith("adhoc")
                .AddQueryParam("last_name", "Hendrix")
                .GetJsonFromUrl()
                .FromJson<QueryResponse<Adhoc>>();
            JsConfig.Reset();

            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Results[0].FirstName, Is.EqualTo("Jimi"));
        }

        [Test]
        public void Can_execute_explicit_equality_condition_on_overridden_CustomRockstar()
        {
            var response = client.Get(new QueryOverridedCustomRockstars { Age = 27, Include = "Total" });

            Assert.That(response.Total, Is.EqualTo(3));
            Assert.That(response.Results.Count, Is.EqualTo(1));
        }

        [Test]
        public void Can_execute_basic_query_with_limits()
        {
            var response = client.Get(new QueryRockstars { Skip = 2, Include = "Total" });
            Assert.That(response.Offset, Is.EqualTo(2));
            Assert.That(response.Total, Is.EqualTo(TotalRockstars));
            Assert.That(response.Results.Count, Is.EqualTo(TotalRockstars - 2));

            response = client.Get(new QueryRockstars { Take = 2, Include = "Total" });
            Assert.That(response.Offset, Is.EqualTo(0));
            Assert.That(response.Total, Is.EqualTo(TotalRockstars));
            Assert.That(response.Results.Count, Is.EqualTo(2));

            response = client.Get(new QueryRockstars { Skip = 2, Take = 2, Include = "Total" });
            Assert.That(response.Offset, Is.EqualTo(2));
            Assert.That(response.Total, Is.EqualTo(TotalRockstars));
            Assert.That(response.Results.Count, Is.EqualTo(2));
        }

        [Test]
        public void Can_execute_explicit_equality_condition()
        {
            var response = client.Get(new QueryRockstars { Age = 27, Include = "Total" });

            Assert.That(response.Total, Is.EqualTo(3));
            Assert.That(response.Results.Count, Is.EqualTo(3));
        }

        [Test]
        public void Can_execute_explicit_equality_condition_on_CustomRockstar()
        {
            var response = client.Get(new QueryCustomRockstars { Age = 27, Include = "Total" });

            Assert.That(response.Total, Is.EqualTo(3));
            Assert.That(response.Results.Count, Is.EqualTo(3));
        }

        [Test]
        public void Can_execute_explicit_equality_condition_on_CustomRockstarSchema()
        {
            var response = client.Get(new QueryCustomRockstarsSchema { Age = 27, Include = "Total" });

            response.PrintDump();

            Assert.That(response.Total, Is.EqualTo(3));
            Assert.That(response.Results.Count, Is.EqualTo(3));
            Assert.That(response.Results[0].FirstName, Is.Not.Null);
            Assert.That(response.Results[0].LastName, Is.Not.Null);
            Assert.That(response.Results[0].Age, Is.EqualTo(27));
        }

        [Test]
        public void Can_execute_implicit_equality_condition()
        {
            var response = Config.ListeningOn.CombineWith("json/reply/QueryRockstars")
                .AddQueryParam("FirstName", "Jim")
                .AddQueryParam("LivingStatus", "Dead")
                .AddQueryParam("Include", "Total")
                .GetJsonFromUrl()
                .FromJson<QueryResponse<Rockstar>>();

            Assert.That(response.Total, Is.EqualTo(1));
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Results[0].LastName, Is.EqualTo("Morrison"));
        }

        [Test]
        public void Can_execute_multiple_conditions_with_same_param_name()
        {
            var response = Config.ListeningOn.CombineWith("json/reply/QueryRockstars")
                .AddQueryParam("FirstName", "Jim")
                .AddQueryParam("FirstName", "Jim")
                .AddQueryParam("Include", "Total")
                .GetJsonFromUrl()
                .FromJson<QueryResponse<Rockstar>>();

            Assert.That(response.Total, Is.EqualTo(1));
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Results[0].LastName, Is.EqualTo("Morrison"));

            response = Config.ListeningOn.CombineWith("json/reply/QueryRockstars")
                .AddQueryParam("FirstNameStartsWith", "Jim")
                .AddQueryParam("FirstNameStartsWith", "Jimi")
                .AddQueryParam("Include", "Total")
                .GetJsonFromUrl()
                .FromJson<QueryResponse<Rockstar>>();

            Assert.That(response.Total, Is.EqualTo(1));
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Results[0].LastName, Is.EqualTo("Hendrix"));
        }

        [Test]
        public void Can_execute_implicit_IsNull_condition()
        {
            var response = Config.ListeningOn.CombineWith("json/reply/QueryRockstars?DateDied=&Include=Total")
                .GetJsonFromUrl()
                .FromJson<QueryResponse<Rockstar>>();

            Assert.That(response.Total, Is.EqualTo(2));
            Assert.That(response.Results.Count, Is.EqualTo(2));
        }

        [Test]
        public void Can_execute_query_with_JOIN_on_RockstarAlbums()
        {
            var response = client.Get(new QueryJoinedRockstarAlbums { Include = "Total" });
            Assert.That(response.Total, Is.EqualTo(TotalAlbums));
            Assert.That(response.Results.Count, Is.EqualTo(TotalAlbums));
            var albumNames = response.Results.Select(x => x.RockstarAlbumName);
            Assert.That(albumNames, Is.EquivalentTo(new[] {
                "Electric Ladyland", "Bleach", "Nevermind", "In Utero", "Incesticide",
                "MTV Unplugged in New York", "Foo Fighters", "Into the Wild",
            }));

            response = client.Get(new QueryJoinedRockstarAlbums { Age = 27, Include = "Total" });
            Assert.That(response.Total, Is.EqualTo(6));
            Assert.That(response.Results.Count, Is.EqualTo(6));
            albumNames = response.Results.Select(x => x.RockstarAlbumName);
            Assert.That(albumNames, Is.EquivalentTo(new[] {
                "Electric Ladyland", "Bleach", "Nevermind", "In Utero", "Incesticide",
                "MTV Unplugged in New York",
            }));

            response = client.Get(new QueryJoinedRockstarAlbums { RockstarAlbumName = "Nevermind", Include = "Total" });
            Assert.That(response.Total, Is.EqualTo(1));
            Assert.That(response.Results.Count, Is.EqualTo(1));
            albumNames = response.Results.Select(x => x.RockstarAlbumName);
            Assert.That(albumNames, Is.EquivalentTo(new[] { "Nevermind" }));
        }

        [Test]
        public void Can_execute_query_with_multiple_JOINs_on_Rockstar_Albums_and_Genres()
        {
            var response = client.Get(new QueryMultiJoinRockstar { Include = "Total" });
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

            response = client.Get(new QueryMultiJoinRockstar { RockstarAlbumName = "Nevermind", Include = "Total" });
            Assert.That(response.Total, Is.EqualTo(1));
            Assert.That(response.Results.Count, Is.EqualTo(1));
            albumNames = response.Results.Select(x => x.RockstarAlbumName);
            Assert.That(albumNames, Is.EquivalentTo(new[] { "Nevermind" }));

            response = client.Get(new QueryMultiJoinRockstar { RockstarGenreName = "Folk Rock", Include = "Total" });
            Assert.That(response.Total, Is.EqualTo(1));
            Assert.That(response.Results.Count, Is.EqualTo(1));
            albumNames = response.Results.Select(x => x.RockstarGenreName);
            Assert.That(albumNames, Is.EquivalentTo(new[] { "Folk Rock" }));
        }

        [Test]
        public void Can_execute_IMPLICIT_query_with_JOIN_on_RockstarAlbums()
        {
            var response = Config.ListeningOn.CombineWith("json/reply/QueryRockstarAlbumsImplicit")
                .AddQueryParam("Age", "27")
                .AddQueryParam("Include", "Total")
                .GetJsonFromUrl()
                .FromJson<QueryResponse<CustomRockstar>>();
            Assert.That(response.Total, Is.EqualTo(6));
            Assert.That(response.Results.Count, Is.EqualTo(6));
            var albumNames = response.Results.Select(x => x.RockstarAlbumName);
            Assert.That(albumNames, Is.EquivalentTo(new[] {
                "Electric Ladyland", "Bleach", "Nevermind", "In Utero", "Incesticide",
                "MTV Unplugged in New York"
            }));

            response = Config.ListeningOn.CombineWith("json/reply/QueryRockstarAlbumsImplicit")
                .AddQueryParam("RockstarAlbumName", "Nevermind")
                .AddQueryParam("Include", "Total")
                .GetJsonFromUrl()
                .FromJson<QueryResponse<CustomRockstar>>();
            Assert.That(response.Total, Is.EqualTo(1));
            Assert.That(response.Results.Count, Is.EqualTo(1));
            albumNames = response.Results.Select(x => x.RockstarAlbumName);
            Assert.That(albumNames, Is.EquivalentTo(new[] { "Nevermind" }));
        }

        [Test]
        public void Can_execute_query_with_LEFTJOIN_on_RockstarAlbums()
        {
            var response = client.Get(new QueryRockstarAlbumsLeftJoin { IdNotEqualTo = 3, Include = "Total" });
            Assert.That(response.Total, Is.EqualTo(TotalRockstars - 1));
            Assert.That(response.Results.Count, Is.EqualTo(TotalRockstars - 1));
            var albumNames = response.Results.Where(x => x.RockstarAlbumName != null).Select(x => x.RockstarAlbumName);
            Assert.That(albumNames, Is.EquivalentTo(new[] {
                "Electric Ladyland", "Foo Fighters", "Into the Wild"
            }));
        }

        [Test]
        public void Can_execute_query_with_custom_LEFTJOIN_on_RockstarAlbums()
        {
            var response = client.Get(new QueryRockstarAlbumsCustomLeftJoin { IdNotEqualTo = 3, Include = "Total" });
            Assert.That(response.Total, Is.EqualTo(TotalRockstars - 1));
            Assert.That(response.Results.Count, Is.EqualTo(TotalRockstars - 1));
            var albumNames = response.Results.Where(x => x.RockstarAlbumName != null).Select(x => x.RockstarAlbumName);
            Assert.That(albumNames, Is.EquivalentTo(new[] {
                "Electric Ladyland", "Foo Fighters", "Into the Wild"
            }));
        }

        [Test]
        public void Can_execute_custom_QueryFields()
        {
            QueryResponse<Rockstar> response;
            response = client.Get(new QueryFieldRockstars { FirstName = "Jim" });
            Assert.That(response.Results.Count, Is.EqualTo(1));

            response = client.Get(new QueryFieldRockstars { FirstNames = new[] { "Jim","Kurt" } });
            Assert.That(response.Results.Count, Is.EqualTo(2));

            response = client.Get(new QueryFieldRockstars { FirstNameCaseInsensitive = "jim" });
            Assert.That(response.Results.Count, Is.EqualTo(1));

            response = client.Get(new QueryFieldRockstars { FirstNameStartsWith = "Jim" });
            Assert.That(response.Results.Count, Is.EqualTo(2));

            response = client.Get(new QueryFieldRockstars { LastNameEndsWith = "son" });
            Assert.That(response.Results.Count, Is.EqualTo(2));

            response = client.Get(new QueryFieldRockstars { FirstNameBetween = new[] {"A","F"} });
            Assert.That(response.Results.Count, Is.EqualTo(3));

            response = client.Get(new QueryFieldRockstars
            {
                LastNameEndsWith = "son",
                OrLastName = "Hendrix"
            });
            Assert.That(response.Results.Count, Is.EqualTo(3));

            response = client.Get(new QueryFieldRockstars
            {
                FirstNameStartsWith = "Jim",
                OrLastName = "Presley"
            });
            Assert.That(response.Results.Count, Is.EqualTo(3));

            response = client.Get(new QueryFieldRockstars { Age = 42 });
            Assert.That(response.Results.Count, Is.EqualTo(4));
        }

        [Test]
        public void Can_execute_combination_of_QueryFields()
        {
            QueryResponse<Rockstar> response;

            response = client.Get(new QueryFieldRockstars
            {
                FirstNameStartsWith = "Jim",
                LastNameEndsWith = "son",
            });
            Assert.That(response.Results.Count, Is.EqualTo(1));

            response = client.Get(new QueryFieldRockstars
            {
                FirstNameStartsWith = "Jim",
                OrLastName = "Cobain",
            });
            Assert.That(response.Results.Count, Is.EqualTo(3));
        }

        [Test]
        public void Does_escape_values()
        {
            QueryResponse<Rockstar> response;

            response = client.Get(new QueryFieldRockstars
            {
                FirstNameStartsWith = "Jim'\"",
            });
            Assert.That(response.Results.Count, Is.EqualTo(0));
        }

        [Test]
        public void Does_use_custom_model_to_select_columns()
        {
            var response = client.Get(new QueryRockstarAlias { RockstarAlbumName = "Nevermind" });

            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Results[0].RockstarId, Is.EqualTo(3));
            Assert.That(response.Results[0].FirstName, Is.EqualTo("Kurt"));
            Assert.That(response.Results[0].RockstarAlbumName, Is.EqualTo("Nevermind"));
        }

        [Test]
        public void Does_allow_adding_attributes_dynamically()
        {
            typeof(QueryFieldRockstarsDynamic)
                .GetProperty("Age")
                .AddAttributes(new QueryDbFieldAttribute { Operand = ">=" });

            var response = client.Get(new QueryFieldRockstarsDynamic { Age = 42 });
            Assert.That(response.Results.Count, Is.EqualTo(4));
        }

        [Test]
        public void Does_execute_typed_QueryFilters()
        {
            // QueryFilter appends additional: x => x.LastName.EndsWith("son")
            var response = client.Get(new QueryRockstarsFilter { Age = 27 });
            Assert.That(response.Results.Count, Is.EqualTo(1));

            var custom = client.Get(new QueryCustomRockstarsFilter { Age = 27 });
            Assert.That(custom.Results.Count, Is.EqualTo(1));

            response = client.Get(new QueryRockstarsIFilter { Age = 27 });
            Assert.That(response.Results.Count, Is.EqualTo(1));
        }

        [Test]
        public void Can_execute_OR_QueryFilters()
        {
            var response = client.Get(new QueryOrRockstars { Age = 42, FirstName = "Jim" });
            Assert.That(response.Results.Count, Is.EqualTo(2));

            response = Config.ListeningOn.CombineWith("OrRockstars")
                .AddQueryParam("Age", "27")
                .AddQueryParam("FirstName", "Kurt")
                .AddQueryParam("LastName", "Hendrix")
                .GetJsonFromUrl()
                .FromJson<QueryResponse<Rockstar>>();
            Assert.That(response.Results.Count, Is.EqualTo(3));
        }

        [Test]
        public void Does_retain_implicit_convention_when_not_overriding_template_or_ValueFormat()
        {
            var response = client.Get(new QueryFieldsImplicitConventions { FirstNameContains = "im" });
            Assert.That(response.Results.Count, Is.EqualTo(2));

            response = client.Get(new QueryFieldsImplicitConventions { LastNameEndsWith = "son" });
            Assert.That(response.Results.Count, Is.EqualTo(2));
        }

        [Test]
        public void Can_execute_OR_QueryFilters_Fields()
        {
            var response = client.Get(new QueryOrRockstarsFields
            {
                FirstName = "Jim",
                LastName = "Vedder",
            });
            Assert.That(response.Results.Count, Is.EqualTo(2));

            response = Config.ListeningOn.CombineWith("OrRockstarsFields")
                .AddQueryParam("FirstName", "Kurt")
                .AddQueryParam("LastName", "Hendrix")
                .GetJsonFromUrl()
                .FromJson<QueryResponse<Rockstar>>();
            Assert.That(response.Results.Count, Is.EqualTo(2));
        }

        [Test]
        public void Can_execute_implicit_conventions()
        {
            var baseUrl = Config.ListeningOn.CombineWith("json/reply/QueryRockstars");

            var response = baseUrl.AddQueryParam("AgeOlderThan", 42).AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(3));

            response = baseUrl.AddQueryParam("AgeGreaterThanOrEqualTo", 42).AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(4));

            response = baseUrl.AddQueryParam("AgeGreaterThan", 42).AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(3));
            response = baseUrl.AddQueryParam("GreaterThanAge", 42).AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(3));
            response = baseUrl.AddQueryParam("AgeNotEqualTo", 27).AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(4));

            response = baseUrl.AddQueryParam(">Age", 42).AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(4));
            response = baseUrl.AddQueryParam("Age>", 42).AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(3));
            response = baseUrl.AddQueryParam("<Age", 42).AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(3));
            response = baseUrl.AddQueryParam("Age<", 42).AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(4));
            response = baseUrl.AddQueryParam("Age!", "27").AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(4));

            response = baseUrl.AddQueryParam("FirstNameStartsWith", "jim").AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(2));
            response = baseUrl.AddQueryParam("LastNameEndsWith", "son").AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(2));
            response = baseUrl.AddQueryParam("LastNameContains", "e").AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(3));
        }

        [Test]
        public void Can_execute_implicit_conventions_on_JOIN()
        {
            var baseUrl = Config.ListeningOn.CombineWith("json/reply/QueryJoinedRockstarAlbums");

            var response = baseUrl.AddQueryParam("RockstarAlbumNameContains", "n").AsJsonInto<CustomRockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(6));

            response = baseUrl.AddQueryParam(">RockstarId", "3").AsJsonInto<CustomRockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(7));
            response = baseUrl.AddQueryParam("RockstarId>", "3").AsJsonInto<CustomRockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(2));
        }

        [Test]
        public void Can_execute_Explicit_conventions()
        {
            var response = client.Get(new QueryRockstarsConventions { Ids = new[] {1, 2, 3} });
            Assert.That(response.Results.Count, Is.EqualTo(3));

            response = client.Get(new QueryRockstarsConventions { AgeOlderThan = 42 });
            Assert.That(response.Results.Count, Is.EqualTo(3));

            response = client.Get(new QueryRockstarsConventions { AgeGreaterThanOrEqualTo = 42 });
            Assert.That(response.Results.Count, Is.EqualTo(4));

            response = client.Get(new QueryRockstarsConventions { AgeGreaterThan = 42 });
            Assert.That(response.Results.Count, Is.EqualTo(3));
            response = client.Get(new QueryRockstarsConventions { GreaterThanAge = 42 });
            Assert.That(response.Results.Count, Is.EqualTo(3));

            response = client.Get(new QueryRockstarsConventions { FirstNameStartsWith = "Jim" });
            Assert.That(response.Results.Count, Is.EqualTo(2));
            response = client.Get(new QueryRockstarsConventions { LastNameEndsWith = "son" });
            Assert.That(response.Results.Count, Is.EqualTo(2));
            response = client.Get(new QueryRockstarsConventions { LastNameContains = "e" });
            Assert.That(response.Results.Count, Is.EqualTo(3));

            response = client.Get(new QueryRockstarsConventions { DateOfBirthGreaterThan = new DateTime(1960, 01, 01) });
            Assert.That(response.Results.Count, Is.EqualTo(3));
            response = client.Get(new QueryRockstarsConventions { DateDiedLessThan = new DateTime(1980, 01, 01) });
            Assert.That(response.Results.Count, Is.EqualTo(3));
        }

        [Test]
        public void Can_execute_where_SqlFilter()
        {
            var baseUrl = Config.ListeningOn.CombineWith("json/reply/QueryRockstars");

            var response = baseUrl.AddQueryParam("_where", "Age > 42").AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(3));
            response = baseUrl.AddQueryParam("_where", "Age >= 42").AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(4));

            response = baseUrl.AddQueryParam("_where", "FirstName".SqlColumn() + " LIKE 'Jim%'").AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(2));
            response = baseUrl.AddQueryParam("_where", "LastName".SqlColumn() + " LIKE '%son'").AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(2));
            response = baseUrl.AddQueryParam("_where", "LastName".SqlColumn() + " LIKE '%e%'").AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(3));

            response = baseUrl
                .AddQueryParam("_select", "r.*")
                .AddQueryParam("_from", "{0} r INNER JOIN {1} a ON r.{2} = a.{3}".Fmt(
                    "Rockstar".SqlTable(), "RockstarAlbum".SqlTable(), 
                    "Id".SqlColumn(),      "RockstarId".SqlColumn()))
                .AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(TotalAlbums));

            response = baseUrl
                .AddQueryParam("_select", "FirstName".SqlColumn())
                .AddQueryParam("_where", "LastName".SqlColumn() + " = 'Cobain'")
                .AsJsonInto<Rockstar>();
            var row = response.Results[0];
            Assert.That(row.Id, Is.EqualTo(default(int)));
            Assert.That(row.FirstName, Is.EqualTo("Kurt"));
            Assert.That(row.LastName, Is.Null);
            Assert.That(row.Age, Is.Null);
        }

        [Test]
        public void Can_execute_In_OR_Queries()
        {
            QueryResponse<Rockstar> response;
            response = client.Get(new QueryGetRockstars());
            Assert.That(response.Results.Count, Is.EqualTo(0));

            response = client.Get(new QueryGetRockstars { Ids = new[] { 1, 2, 3 } });
            Assert.That(response.Results.Count, Is.EqualTo(3));

            response = client.Get(new QueryGetRockstars { Ages = new[] { 42, 44 }.ToList() });
            Assert.That(response.Results.Count, Is.EqualTo(2));

            response = client.Get(new QueryGetRockstars { FirstNames = new[] { "Jim", "Kurt" }.ToList() });
            Assert.That(response.Results.Count, Is.EqualTo(2));

            response = client.Get(new QueryGetRockstars { IdsBetween = new[] { 1, 3 } });
            Assert.That(response.Results.Count, Is.EqualTo(3));
        }

        [Test]
        public void Does_ignore_empty_collection_filters_by_default()
        {
            QueryResponse<Rockstar> response;
            response = client.Get(new QueryRockstarFilters());
            Assert.That(response.Results.Count, Is.EqualTo(AutoQueryAppHost.SeedRockstars.Length));

            response = client.Get(new QueryRockstarFilters
            {
                Ids = new int[] {},
                Ages = new List<int>(),
                FirstNames = new List<string>(),
                IdsBetween = new int[] {},               
            });
            Assert.That(response.Results.Count, Is.EqualTo(AutoQueryAppHost.SeedRockstars.Length));
        }

        [Test]
        public void Can_execute_In_OR_Queries_with_implicit_conventions()
        {
            var baseUrl = Config.ListeningOn.CombineWith("json/reply/QueryGetRockstarsDynamic");

            QueryResponse<Rockstar> response;
            response = baseUrl.AddQueryParam("Ids", "1,2,3").AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(3));

            response = baseUrl.AddQueryParam("Ages", "42, 44").AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(2));

            response = baseUrl.AddQueryParam("FirstNames", "Jim,Kurt").AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(2));

            response = baseUrl.AddQueryParam("IdsBetween", "1,3").AsJsonInto<Rockstar>();
            Assert.That(response.Results.Count, Is.EqualTo(3));
        }

        [Test]
        public void Can_query_Movie_Ratings()
        {
            var response = client.Get(new QueryMovies { Ratings = new[] {"G","PG-13"} });
            Assert.That(response.Results.Count, Is.EqualTo(5));

            var url = Config.ListeningOn + "movies?ratings=G,PG-13";
            response = url.AsJsonInto<Movie>();
            Assert.That(response.Results.Count, Is.EqualTo(5));

            response = client.Get(new QueryMovies {
                Ids = new[] { 1, 2 },
                ImdbIds = new[] { "tt0071562", "tt0060196" },
                Ratings = new[] { "G", "PG-13" }
            });
            Assert.That(response.Results.Count, Is.EqualTo(9));

            url = Config.ListeningOn + "movies?ratings=G,PG-13&ids=1,2&imdbIds=tt0071562,tt0060196";
            response = url.AsJsonInto<Movie>();
            Assert.That(response.Results.Count, Is.EqualTo(9));
        }

        [Test]
        public void Can_StreamMovies()
        {
            var results = client.GetLazy(new StreamMovies()).ToList();
            Assert.That(results.Count, Is.EqualTo(10));

            results = client.GetLazy(new StreamMovies { Ratings = new[]{"G","PG-13"} }).ToList();
            Assert.That(results.Count, Is.EqualTo(5));
        }

        [Test]
        public void Does_implicitly_OrderBy_PrimaryKey_when_limits_is_specified()
        {
            var movies = client.Get(new SearchMovies { Take = 100 });
            var ids = movies.Results.Map(x => x.Id);
            var orderedIds = ids.OrderBy(x => x);
            Assert.That(ids, Is.EqualTo(orderedIds));

            var rockstars = client.Get(new SearchMovies { Take = 100 });
            ids = rockstars.Results.Map(x => x.Id);
            orderedIds = ids.OrderBy(x => x);
            Assert.That(ids, Is.EqualTo(orderedIds));
        }

        [Test]
        public void Can_OrderBy_queries()
        {
            var movies = client.Get(new SearchMovies { Take = 100, OrderBy = "ImdbId" });
            var ids = movies.Results.Map(x => x.ImdbId);
            var orderedIds = ids.OrderBy(x => x).ToList();
            Assert.That(ids, Is.EqualTo(orderedIds));

            movies = client.Get(new SearchMovies { Take = 100, OrderBy = "Rating,ImdbId" });
            ids = movies.Results.Map(x => x.ImdbId);
            orderedIds = movies.Results.OrderBy(x => x.Rating).ThenBy(x => x.ImdbId).Map(x => x.ImdbId);
            Assert.That(ids, Is.EqualTo(orderedIds));

            movies = client.Get(new SearchMovies { Take = 100, OrderByDesc = "ImdbId" });
            ids = movies.Results.Map(x => x.ImdbId);
            orderedIds = ids.OrderByDescending(x => x).ToList();
            Assert.That(ids, Is.EqualTo(orderedIds));

            movies = client.Get(new SearchMovies { Take = 100, OrderByDesc = "Rating,ImdbId" });
            ids = movies.Results.Map(x => x.ImdbId);
            orderedIds = movies.Results.OrderByDescending(x => x.Rating)
                .ThenByDescending(x => x.ImdbId).Map(x => x.ImdbId);
            Assert.That(ids, Is.EqualTo(orderedIds));

            movies = client.Get(new SearchMovies { Take = 100, OrderBy = "Rating,-ImdbId" });
            ids = movies.Results.Map(x => x.ImdbId);
            orderedIds = movies.Results.OrderBy(x => x.Rating)
                .ThenByDescending(x => x.ImdbId).Map(x => x.ImdbId);
            Assert.That(ids, Is.EqualTo(orderedIds));

            movies = client.Get(new SearchMovies { Take = 100, OrderByDesc = "Rating,-ImdbId" });
            ids = movies.Results.Map(x => x.ImdbId);
            orderedIds = movies.Results.OrderByDescending(x => x.Rating)
                .ThenBy(x => x.ImdbId).Map(x => x.ImdbId);
            Assert.That(ids, Is.EqualTo(orderedIds));

            var url = Config.ListeningOn + "movies/search?take=100&orderBy=Rating,ImdbId";
            movies = url.AsJsonInto<Movie>();
            ids = movies.Results.Map(x => x.ImdbId);
            orderedIds = movies.Results.OrderBy(x => x.Rating).ThenBy(x => x.ImdbId).Map(x => x.ImdbId);
            Assert.That(ids, Is.EqualTo(orderedIds));

            url = Config.ListeningOn + "movies/search?take=100&orderByDesc=Rating,ImdbId";
            movies = url.AsJsonInto<Movie>();
            ids = movies.Results.Map(x => x.ImdbId);
            orderedIds = movies.Results.OrderByDescending(x => x.Rating)
                .ThenByDescending(x => x.ImdbId).Map(x => x.ImdbId);
            Assert.That(ids, Is.EqualTo(orderedIds));
        }

        [Test]
        public void Can_consume_as_CSV()
        {
            var url = Config.ListeningOn + "movies/search.csv?ratings=G,PG-13";
            var csv = url.GetStringFromUrl();
            var headers = csv.SplitOnFirst('\n')[0].Trim();
            Assert.That(headers, Is.EqualTo("Id,ImdbId,Title,Rating,Score,Director,ReleaseDate,TagLine,Genres"));
            csv.Print();

            url = Config.ListeningOn + "query/rockstars.csv?Age=27";
            csv = url.GetStringFromUrl();
            headers = csv.SplitOnFirst('\n')[0].Trim();
            Assert.That(headers, Is.EqualTo("Id,FirstName,LastName,Age,DateOfBirth,DateDied,LivingStatus"));
            csv.Print();

            url = Config.ListeningOn + "customrockstars.csv";
            csv = url.GetStringFromUrl();
            headers = csv.SplitOnFirst('\n')[0].Trim();
            Assert.That(headers, Is.EqualTo("FirstName,LastName,Age,RockstarAlbumName,RockstarGenreName"));
            csv.Print();
        }

        [Test]
        public void Does_not_query_Ignored_properties()
        {
            var response = client.Get(new QueryUnknownRockstars {
                UnknownProperty = "Foo",
                UnknownInt = 1,
                Include = "Total"
            });

            Assert.That(response.Offset, Is.EqualTo(0));
            Assert.That(response.Total, Is.EqualTo(TotalRockstars));
            Assert.That(response.Results.Count, Is.EqualTo(TotalRockstars));
        }

        [Test]
        public void Can_Query_Rockstars_with_References()
        {
            var response = client.Get(new QueryRockstarsWithReferences {
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

            response = client.Get(new QueryRockstarsWithReferences
            {
                Age = 27,
                Fields = "Id,FirstName,Age"
            });
            Assert.That(response.Results.Count, Is.EqualTo(3));
            Assert.That(response.Results.All(x => x.Id > 0));
            Assert.That(response.Results.All(x => x.LastName == null));
            Assert.That(response.Results.All(x => x.Albums == null));

            response = client.Get(new QueryRockstarsWithReferences
            {
                Age = 27,
                Fields = "Id,FirstName,Age,Albums"
            });
            Assert.That(response.Results.Where(x => x.FirstName != "Jim").All(x => x.Albums != null));
        }

        [Test]
        public void Can_Query_RockstarReference_without_References()
        {
            var response = client.Get(new QueryCustomRockstarsReferences
            {
                Age = 27
            });
            Assert.That(response.Results.Count, Is.EqualTo(3));
            Assert.That(response.Results.All(x => x.Albums == null));
        }

        [Test]
        public void Can_Query_AllFields_Guid()
        {
            var guid = new Guid("3EE6865A-4149-4940-B7A2-F952E0FEFC5E");
            var response = client.Get(new QueryAllFields {
                Guid = guid
            });

            Assert.That(response.Results.Count, Is.EqualTo(1));

            Assert.That(response.Results[0].Guid, Is.EqualTo(guid));
        }

        [Test]
        public void Does_populate_Total()
        {
            var response = client.Get(new QueryRockstars { Include = "Total" });
            Assert.That(response.Total, Is.EqualTo(Rockstars.Count));
            Assert.That(response.Meta, Is.Null);

            response = client.Get(new QueryRockstars { Include = "COUNT" });
            Assert.That(response.Total, Is.EqualTo(Rockstars.Count));

            response = client.Get(new QueryRockstars { Include = "COUNT(*)" });
            Assert.That(response.Total, Is.EqualTo(Rockstars.Count));

            response = client.Get(new QueryRockstars { Include = "COUNT(DISTINCT LivingStatus), Total" });
            Assert.That(response.Total, Is.EqualTo(Rockstars.Count));

            response = client.Get(new QueryRockstars { Include = "Count(*), Min(Age), Max(Age), Sum(Id)" });
            Assert.That(response.Total, Is.EqualTo(Rockstars.Count));

            response = client.Get(new QueryRockstars { Age = 27, Include = "Count(*), Min(Age), Max(Age), Sum(Id)" });
            Assert.That(response.Total, Is.EqualTo(Rockstars.Count(x => x.Age == 27)));
        }

        [Test]
        public void Can_Include_Aggregates_in_AutoQuery()
        {
            var response = client.Get(new QueryRockstars { Include = "COUNT" });
            Assert.That(response.Meta["COUNT(*)"], Is.EqualTo(Rockstars.Count.ToString()));

            response = client.Get(new QueryRockstars { Include = "COUNT(*)" });
            Assert.That(response.Meta["COUNT(*)"], Is.EqualTo(Rockstars.Count.ToString()));

            response = client.Get(new QueryRockstars { Include = "COUNT(DISTINCT LivingStatus)" });
            Assert.That(response.Meta["COUNT(DISTINCT LivingStatus)"], Is.EqualTo("2"));

            response = client.Get(new QueryRockstars { Include = "MIN(Age)" });
            Assert.That(response.Meta["MIN(Age)"], Is.EqualTo(Rockstars.Map(x => x.Age).Min().ToString()));

            response = client.Get(new QueryRockstars { Include = "Count(*), Min(Age), Max(Age), Sum(Id), Avg(Age)", OrderBy = "Id" });
            Assert.That(response.Meta["Count(*)"], Is.EqualTo(Rockstars.Count.ToString()));
            Assert.That(response.Meta["Min(Age)"], Is.EqualTo(Rockstars.Map(x => x.Age).Min().ToString()));
            Assert.That(response.Meta["Max(Age)"], Is.EqualTo(Rockstars.Map(x => x.Age).Max().ToString()));
            Assert.That(response.Meta["Sum(Id)"], Is.EqualTo(Rockstars.Map(x => x.Id).Sum().ToString()));
            Assert.That(double.Parse(response.Meta["Avg(Age)"]), Is.EqualTo(Rockstars.Average(x => x.Age)).Within(1d));
            //Not supported by Sqlite
            //Assert.That(response.Meta["First(Id)"], Is.EqualTo(Rockstars.First().Id.ToString()));
            //Assert.That(response.Meta["Last(Id)"], Is.EqualTo(Rockstars.Last().Id.ToString()));

            response = client.Get(new QueryRockstars { Age = 27, Include = "Count(*), Min(Age), Max(Age), Sum(Id), Avg(Age)", OrderBy = "Id" });
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
        public void Does_ignore_unknown_aggregate_commands()
        {
            var response = client.Get(new QueryRockstars { Include = "FOO(1), Total" });
            Assert.That(response.Total, Is.EqualTo(Rockstars.Count));
            Assert.That(response.Meta, Is.Null);

            response = client.Get(new QueryRockstars { Include = "FOO(1), Min(Age), Bar('a') alias, Count(*), Baz(1,'foo')" });
            Assert.That(response.Total, Is.EqualTo(Rockstars.Count));
            Assert.That(response.Meta["Min(Age)"], Is.EqualTo(Rockstars.Map(x => x.Age).Min().ToString()));
            Assert.That(response.Meta["Count(*)"], Is.EqualTo(Rockstars.Count.ToString()));
        }

        [Test]
        public void Can_Include_Aggregates_in_AutoQuery_with_Aliases()
        {
            var response = client.Get(new QueryRockstars { Include = "COUNT(*) count" });
            Assert.That(response.Meta["count"], Is.EqualTo(Rockstars.Count.ToString()));

            response = client.Get(new QueryRockstars { Include = "COUNT(DISTINCT LivingStatus) as uniquestatus" });
            Assert.That(response.Meta["uniquestatus"], Is.EqualTo("2"));

            response = client.Get(new QueryRockstars { Include = "MIN(Age) minage" });
            Assert.That(response.Meta["minage"], Is.EqualTo(Rockstars.Map(x => x.Age).Min().ToString()));

            response = client.Get(new QueryRockstars { Include = "Count(*) count, Min(Age) min, Max(Age) max, Sum(Id) sum" });
            Assert.That(response.Meta["count"], Is.EqualTo(Rockstars.Count.ToString()));
            Assert.That(response.Meta["min"], Is.EqualTo(Rockstars.Map(x => x.Age).Min().ToString()));
            Assert.That(response.Meta["max"], Is.EqualTo(Rockstars.Map(x => x.Age).Max().ToString()));
            Assert.That(response.Meta["sum"], Is.EqualTo(Rockstars.Map(x => x.Id).Sum().ToString()));
        }

        [Test]
        public void Can_execute_custom_aggregate_functions()
        {
            var response = client.Get(new QueryRockstars {
                Include = "ADD(6,2), Multiply(6,2) SixTimesTwo, Subtract(6,2), divide(6,2) TheDivide"
            });
            Assert.That(response.Meta["ADD(6,2)"], Is.EqualTo("8"));
            Assert.That(response.Meta["SixTimesTwo"], Is.EqualTo("12"));
            Assert.That(response.Meta["Subtract(6,2)"], Is.EqualTo("4"));
            Assert.That(response.Meta["TheDivide"], Is.EqualTo("3"));
        }

        [Test]
        public void Sending_empty_ChangeDb_returns_default_info()
        {
            var response = client.Get(new ChangeDb());
            Assert.That(response.Results.Count, Is.EqualTo(TotalRockstars));

            var aqResponse = client.Get(new QueryChangeDb());
            Assert.That(aqResponse.Results.Count, Is.EqualTo(TotalRockstars));
        }

        [Test]
        public void Can_ChangeDb_with_Named_Connection()
        {
            var response = client.Get(new ChangeDb { NamedConnection = AutoQueryAppHost.SqlServerNamedConnection });
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Results[0].FirstName, Is.EqualTo("Microsoft"));

            var aqResponse = client.Get(new QueryChangeDb { NamedConnection = AutoQueryAppHost.SqlServerNamedConnection });
            Assert.That(aqResponse.Results.Count, Is.EqualTo(1));
            Assert.That(aqResponse.Results[0].FirstName, Is.EqualTo("Microsoft"));
        }

        [Test]
        public void Can_ChangeDb_with_ConnectionString()
        {
            var response = client.Get(new ChangeDb { ConnectionString = AutoQueryAppHost.SqliteFileConnString });
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Results[0].FirstName, Is.EqualTo("Sqlite"));

            var aqResponse = client.Get(new QueryChangeDb { ConnectionString = AutoQueryAppHost.SqliteFileConnString });
            Assert.That(aqResponse.Results.Count, Is.EqualTo(1));
            Assert.That(aqResponse.Results[0].FirstName, Is.EqualTo("Sqlite"));
        }

        [Test]
        public void Can_ChangeDb_with_ConnectionString_and_Provider()
        {
            var response = client.Get(new ChangeDb
            {
                ConnectionString = AutoQueryAppHost.SqlServerConnString,
                ProviderName = AutoQueryAppHost.SqlServerProvider,
            });
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Results[0].FirstName, Is.EqualTo("Microsoft"));

            var aqResponse = client.Get(new QueryChangeDb
            {
                ConnectionString = AutoQueryAppHost.SqlServerConnString,
                ProviderName = AutoQueryAppHost.SqlServerProvider,
            });
            Assert.That(aqResponse.Results.Count, Is.EqualTo(1));
            Assert.That(aqResponse.Results[0].FirstName, Is.EqualTo("Microsoft"));
        }

        [Test]
        public void Can_Change_Named_Connection_with_ConnectionInfoAttribute()
        {
            var response = client.Get(new ChangeConnectionInfo());
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Results[0].FirstName, Is.EqualTo("Microsoft"));

            var aqResponse = client.Get(new QueryChangeConnectionInfo());
            Assert.That(aqResponse.Results.Count, Is.EqualTo(1));
            Assert.That(aqResponse.Results[0].FirstName, Is.EqualTo("Microsoft"));
        }

        [Test]
        public void Can_select_partial_list_of_fields()
        {
            var response = Config.ListeningOn.CombineWith("json/reply/QueryRockstars")
                .AddQueryParam("Age", "27")
                .AddQueryParam("Fields", "Id,FirstName,Age")
                .GetJsonFromUrl()
                .FromJson<QueryResponse<Rockstar>>();

            response.PrintDump();

            Assert.That(response.Results.All(x => x.Id > 0));
            Assert.That(response.Results.All(x => x.FirstName != null));
            Assert.That(response.Results.All(x => x.LastName == null));
            Assert.That(response.Results.Any(x => x.Age > 0));
            Assert.That(response.Results.All(x => x.DateDied == null));
            Assert.That(response.Results.All(x => x.DateOfBirth == default(DateTime).ToLocalTime()));
        }

        [Test]
        public void Can_select_partial_list_of_fields_DISTINCT()
        {
            var response = Config.ListeningOn.CombineWith("json/reply/QueryRockstars")
                .AddQueryParam("Fields", "DISTINCT Age")
                .GetJsonFromUrl()
                .FromJson<QueryResponse<Rockstar>>();

            response.PrintDump();

            Assert.That(response.Results.Any(x => x.Age > 0));
            Assert.That(response.Results.Count, Is.EqualTo(response.Results.Select(x => x.Age).ToHashSet().Count));
            Assert.That(response.Results.All(x => x.Id == 0));
            Assert.That(response.Results.All(x => x.FirstName == null));
            Assert.That(response.Results.All(x => x.LastName == null));
            Assert.That(response.Results.All(x => x.DateDied == null));
            Assert.That(response.Results.All(x => x.DateOfBirth == default(DateTime).ToLocalTime()));
        }

        [Test]
        public void Can_select_partial_list_of_fields_case_insensitive()
        {
            var response = Config.ListeningOn.CombineWith("json/reply/QueryRockstars")
                .AddQueryParam("Age", "27")
                .AddQueryParam("Fields", "id,firstname,age")
                .GetJsonFromUrl()
                .FromJson<QueryResponse<Rockstar>>();

            response.PrintDump();

            Assert.That(response.Results.All(x => x.Id > 0));
            Assert.That(response.Results.All(x => x.FirstName != null));
            Assert.That(response.Results.All(x => x.LastName == null));
            Assert.That(response.Results.Any(x => x.Age > 0));
            Assert.That(response.Results.All(x => x.DateDied == null));
            Assert.That(response.Results.All(x => x.DateOfBirth == default(DateTime).ToLocalTime()));
        }

        [Test]
        public void Can_select_partial_list_of_fields_from_joined_table()
        {
            var response = Config.ListeningOn.CombineWith("json/reply/QueryJoinedRockstarAlbums")
                .AddQueryParam("Age", "27")
                .AddQueryParam("fields", "FirstName,Age,RockstarAlbumName")
                .GetJsonFromUrl()
                .FromJson<QueryResponse<CustomRockstar>>();

            Assert.That(response.Results.All(x => x.FirstName != null));
            Assert.That(response.Results.All(x => x.LastName == null));
            Assert.That(response.Results.All(x => x.Age > 0));
            Assert.That(response.Results.All(x => x.RockstarAlbumName != null));
        }

        [Test]
        public void Can_select_partial_list_of_fields_from_joined_table_case_insensitive()
        {
            var response = Config.ListeningOn.CombineWith("json/reply/QueryJoinedRockstarAlbums")
                .AddQueryParam("Age", "27")
                .AddQueryParam("fields", "firstname,age,rockstaralbumname")
                .GetJsonFromUrl()
                .FromJson<QueryResponse<CustomRockstar>>();

            Assert.That(response.Results.All(x => x.FirstName != null));
            Assert.That(response.Results.All(x => x.LastName == null));
            Assert.That(response.Results.All(x => x.Age > 0));
            Assert.That(response.Results.All(x => x.RockstarAlbumName != null));
        }

        [Test]
        public void Does_return_MaxLimit_results()
        {
            QueryResponse<PagingTest> response;
            response = client.Get(new QueryPagingTest { Include = "Total" });
            Assert.That(response.Results.Count, Is.EqualTo(100));
            Assert.That(response.Total, Is.EqualTo(PagingTests.Count));

            response = client.Get(new QueryPagingTest { Skip = 200, Include = "Total" });
            Assert.That(response.Results.Count, Is.EqualTo(PagingTests.Skip(200).Count()));
            Assert.That(response.Total, Is.EqualTo(PagingTests.Count));

            response = client.Get(new QueryPagingTest { Value = 1, Include = "Total" });
            Assert.That(response.Results.Count, Is.EqualTo(100));
            Assert.That(response.Total, Is.EqualTo(PagingTests.Count(x => x.Value == 1)));
        }

        [Test]
        public void Can_query_on_ForeignKey_and_Index()
        {
            QueryResponse<RockstarAlbum> response;
            response = client.Get(new QueryRockstarAlbums { RockstarId = 3, Include = "Total" }); //Hash
            Assert.That(response.Results.Count, Is.EqualTo(5));
            Assert.That(response.Total, Is.EqualTo(5));

            response = client.Get(new QueryRockstarAlbums { RockstarId = 3, Id = 3, Include = "Total" }); //Hash + Range
            Assert.That(response.Results.Count, Is.EqualTo(1));
            Assert.That(response.Total, Is.EqualTo(1));
            Assert.That(response.Results[0].Name, Is.EqualTo("Nevermind"));

            //Hash + Range BETWEEN
            response = client.Get(new QueryRockstarAlbums
            {
                RockstarId = 3,
                IdBetween = new[] { 2, 3 },
                Include = "Total"
            });
            Assert.That(response.Results.Count, Is.EqualTo(2));
            Assert.That(response.Total, Is.EqualTo(2));

            //Hash + Range BETWEEN + Filter
            response = client.Get(new QueryRockstarAlbums
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
            response = client.Get(new QueryRockstarAlbums { RockstarId = 3, Genre = "Grunge", Include = "Total" });
            Assert.That(response.Results.Count, Is.EqualTo(4));
            Assert.That(response.Total, Is.EqualTo(4));

            response.PrintDump();
        }

        [Test]
        public void Can_use_implicit_query_on_enums_on_all_fields()
        {
            var allFieldsResponse = Config.ListeningOn.CombineWith("query", "all-fields")
                .AddQueryParam("EnumContains", "MethodNotAllowed")
                .GetJsonFromUrl()
                .FromJson<QueryResponse<AllFields>>();

            Assert.That(allFieldsResponse.Results[0].Enum, Is.EqualTo(HttpStatusCode.MethodNotAllowed));
        }

        [Test]
        public void Can_use_implicit_query_to_query_equals_on_int_enums()
        {
            var response = Config.ListeningOn.CombineWith("query-enums")
                .AddQueryParam("SomeEnumAsInt", "Value2")
                .GetJsonFromUrl()
                .FromJson<QueryResponse<TypeWithEnum>>();

            Assert.That(response.Results[0].SomeEnumAsInt, Is.EqualTo(SomeEnumAsInt.Value2));
            
            response = Config.ListeningOn.CombineWith("query-enums")
                .AddQueryParam("NSomeEnumAsInt", "Value2")
                .GetJsonFromUrl()
                .FromJson<QueryResponse<TypeWithEnum>>();

            Assert.That(response.Results[0].NSomeEnumAsInt, Is.EqualTo(SomeEnumAsInt.Value2));
        }

        [Test]
        public void Can_use_implicit_query_to_query_contains_on_string_enums()
        {
            var response = Config.ListeningOn.CombineWith("query-enums")
                .AddQueryParam("SomeEnumContains", "Value2")
                .GetJsonFromUrl()
                .FromJson<QueryResponse<TypeWithEnum>>();

            Assert.That(response.Results[0].SomeEnum, Is.EqualTo(SomeEnum.Value2));
            
            response = Config.ListeningOn.CombineWith("query-enums")
                .AddQueryParam("NSomeEnumContains", "Value2")
                .GetJsonFromUrl()
                .FromJson<QueryResponse<TypeWithEnum>>();

            Assert.That(response.Results[0].NSomeEnum, Is.EqualTo(SomeEnum.Value2));
        }
    }

    public static class AutoQueryExtensions
    {
        public static QueryResponse<T> AsJsonInto<T>(this string url)
        {
            return url.GetJsonFromUrl()
                .FromJson<QueryResponse<T>>();
        }
    }
}

