using System.Data;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using System.Collections.Generic;

namespace ServiceStack.OrmLite.Tests.UseCase
{
    [TestFixtureOrmLiteDialects(Dialect.AnySqlServer)]
    public class ArtistTrackSqlExpressions : OrmLiteProvidersTestBase
    {
        public ArtistTrackSqlExpressions(DialectContext context) : base(context) {}

        [Test]
        public void Can_OrderBy_Column_Index()
        {
            var hold = OrmLiteConfig.StripUpperInLike;
            OrmLiteConfig.StripUpperInLike = false;
            using (var db = CreateArtistAndTrackTablesWithData(OpenDbConnection()))
            {
                var q = db.From<Track>()
                    .Where(x => x.Year > 1991)
                    .And(x => x.Name.Contains("A"))
                    .GroupBy(x => x.Year)
                    .OrderByDescending(2)
                    .ThenBy(x => x.Year)
                    .Take(1)
                    .Select(x => new { x.Year, Count = Sql.Count("*") });

                var result = db.Dictionary<int, int>(q);
                Assert.That(result[1993], Is.EqualTo(2));
            }
            OrmLiteConfig.StripUpperInLike = hold;
        }

        [Test]
        public void Can_Order_by_Property_Alias()
        {
            var hold = OrmLiteConfig.StripUpperInLike;
            OrmLiteConfig.StripUpperInLike = false;
            using (var db = CreateArtistAndTrackTablesWithData(OpenDbConnection()))
            {
                var q = db.From<Track>()
                    .Where(x => x.Year > 1991)
                    .And(x => x.Name.Contains("A"))
                    .GroupBy(x => x.Year)
                    .OrderByDescending("Count")
                    .ThenBy(x => x.Year)
                    .Take(1)
                    .Select(x => new { x.Year, Count = Sql.Count("*") });

                var result = db.Dictionary<int, int>(q);
                Assert.That(result[1993], Is.EqualTo(2));
            }
            OrmLiteConfig.StripUpperInLike = hold;
        }

        [Test]
        public void Can_Select_joined_table_with_Alias()
        {
            using (var db = CreateArtistAndTrackTablesWithData(OpenDbConnection()))
            {
                var tracksByYear = db.Dictionary<string, int>(db.From<Track>()
                    .Join<Artist>()
                    .GroupBy<Artist>(x => x.Name)
                    .Select<Artist>(x => new { x.Name, Count = Sql.Count("*") }));

                Assert.That(tracksByYear.Count, Is.EqualTo(4));
                Assert.That(tracksByYear.Map(x => x.Value).Sum(), Is.EqualTo(8));
            }
        }

        [Test]
        public void Can_Count_Distinct()
        {
            using (var db = CreateArtistAndTrackTablesWithData(OpenDbConnection()))
            {
                var differentArtistsCount = db.Scalar<int>(db.From<Track>()
                    .Select(x => Sql.CountDistinct(x.ArtistId)));

                Assert.That(differentArtistsCount, Is.EqualTo(4));
            }
        }
        
        static readonly Artist[] Artists = new [] {
            new Artist {
                Id = 1, Name = "Faith No More",
                Tracks = new List<Track> {
                    new Track { Name = "Everythings Ruined", Album = "Angel Dust", Year = 1992 },
                    new Track { Name = "Ashes to Ashes", Album = "Album of the Year", Year = 1997 },
                }
            },
            new Artist {
                Id = 2, Name = "Live",
                Tracks = new List<Track> {
                    new Track { Name = "Lightning Crashes", Album = "Throwing Copper", Year = 1994 },
                    new Track { Name = "Lakini's Juice", Album = "Secret Samadhi", Year = 1997 },
                }
            },
            new Artist {
                Id = 3, Name = "Nirvana",
                Tracks = new List<Track> {
                    new Track { Name = "Smells Like Teen Spirit", Album = "Nevermind", Year = 1991  },
                    new Track { Name = "Heart-Shaped Box", Album = "In Utero", Year = 1993 },
                }
            },
            new Artist {
                Id = 4, Name = "Pearl Jam",
                Tracks = new List<Track> {
                    new Track { Name = "Alive", Album = "Ten", Year = 1991 },
                    new Track { Name = "Daughter", Album = "Vs", Year = 1993 },
                }
            },
        };

        public IDbConnection CreateArtistAndTrackTablesWithData(IDbConnection db)
        {
            db.DropAndCreateTable<Artist>();
            db.DropAndCreateTable<Track>();
            Artists.Each(x => db.Save(x, references: true));
            return db;
        }
    }
    
    public class Artist
    {
        public int Id { get; set; }
        public string Name { get; set; }

        [Reference]
        public List<Track> Tracks { get; set; }
    }

    public class Track
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
        public int ArtistId { get; set; }
        public string Album { get; set; }
        public int Year { get; set; }
    }
}