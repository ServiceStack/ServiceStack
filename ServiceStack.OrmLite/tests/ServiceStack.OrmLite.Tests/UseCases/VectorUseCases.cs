using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.UseCases;

/// <summary>
/// [Vector] columns store the embeddings of an AI model, so the rows most similar to a piece of text can be found
/// in the same query as typed filters and joins. They need an RDBMS with vector support: PostgreSQL with the
/// pgvector extension, SQL Server 2025, MariaDB 11.7+ or MySQL 9, and SQLite with the sqlite-vec extension.
/// Real embeddings have hundreds of dimensions, these use 3 to keep them readable.
/// </summary>
[TestFixtureOrmLite]
public class VectorUseCases(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    public class Passage
    {
        [AutoIncrement]
        public int Id { get; set; }
        public int BookId { get; set; }
        public string Text { get; set; }

        [Vector(3)]
        public float[] Embedding { get; set; }
    }

    public class IndexedPassage
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Text { get; set; }

        // A vector index is created where the RDBMS has one, for the distance queries order by
        [Vector(3, Distance = VectorDistance.Cosine), Index, Required]
        public float[] Embedding { get; set; }
    }

    public class PassageMatch
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public double Distance { get; set; }
    }

    // Embeddings where each dimension is how much a passage is about: dragons, space and history
    static readonly float[] AboutDragons = [1f, 0f, 0f];
    static readonly float[] AboutSpace = [0f, 1f, 0f];

    IDbConnection OpenVectorDb()
    {
        var db = OpenDbConnection();
        try
        {
            if (Dialect == Dialect.Sqlite)
                LoadSqliteVec(db);
            else if (Dialect.AnyPostgreSql.HasFlag(Dialect))
                db.ExecuteSql("CREATE EXTENSION IF NOT EXISTS vector");

            db.DropAndCreateTable<Passage>();
        }
        catch (Exception e)
        {
            db.Dispose();
            Assert.Ignore($"Vector support isn't enabled in this {Dialect} database: {e.Message}");
        }

        Bookstore.Seed(db);
        var books = db.Dictionary<string, int>(db.From<Book>().Select(x => new { x.Title, x.Id }));
        db.InsertAll(new[] {
            new Passage { BookId = books["The Hobbit"], Text = "Smaug guards his hoard", Embedding = [0.9f, 0f, 0.1f] },
            new Passage { BookId = books["The Hobbit"], Text = "The dwarves reclaim Erebor", Embedding = [0.6f, 0f, 0.4f] },
            new Passage { BookId = books["Dune"], Text = "Spice is mined on Arrakis", Embedding = [0.1f, 0.8f, 0.1f] },
            new Passage { BookId = books["Cosmos"], Text = "The pale blue dot", Embedding = [0f, 0.9f, 0.1f] },
            new Passage { BookId = books["SPQR"], Text = "The founding of Rome", Embedding = [0f, 0.1f, 0.9f] },
        });
        return db;
    }

    // The sqlite-vec NuGet package has the extension for each platform, which is loaded into each connection
    static void LoadSqliteVec(IDbConnection db)
    {
        var os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win"
            : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx" : "linux";
        var rid = $"{os}-{RuntimeInformation.ProcessArchitecture.ToString().ToLower()}";
        var path = Directory.GetFiles(AppContext.BaseDirectory, "vec0.*", SearchOption.AllDirectories)
            .FirstOrDefault(x => x.Contains(rid))
            ?? throw new FileNotFoundException($"sqlite-vec extension for {rid} was not found");

        var conn = db.ToDbConnection();
        var type = conn.GetType();
        // System.Data.SQLite needs extensions to be enabled first, Microsoft.Data.Sqlite doesn't
        type.GetMethod("EnableExtensions", [typeof(bool)])?.Invoke(conn, [true]);
        var load = type.GetMethod("LoadExtension", [typeof(string)]);
        if (load != null)
            load.Invoke(conn, [path]);
        else
            type.GetMethod("LoadExtension", [typeof(string), typeof(string)])!.Invoke(conn, [path, null]);
    }

    [Test]
    public void Find_the_most_similar_rows()
    {
        using var db = OpenVectorDb();

        // The embedding of what's being searched for, e.g. of a user's question
        var nearest = db.Select(db.From<Passage>()
            .OrderBy(x => Sql.CosineDistance(x.Embedding, AboutDragons))
            .Take(2));

        Assert.That(nearest.Map(x => x.Text), Is.EqualTo(new[] { "Smaug guards his hoard", "The dwarves reclaim Erebor" }));

        // Vectors are read back as they were saved
        Assert.That(nearest[0].Embedding, Is.EqualTo(new[] { 0.9f, 0f, 0.1f }));
    }

    [Test]
    public void Combine_with_filters_and_joins()
    {
        using var db = OpenVectorDb();

        // The passages about space, of the books that are in stock and aren't fiction
        var q = db.From<Passage>()
            .Join<Book>((p, b) => p.BookId == b.Id)
            .Where<Book>(b => b.Available && b.Genre != Genre.Fiction)
            .OrderBy(x => Sql.CosineDistance(x.Embedding, AboutSpace))
            .Take(2);

        Assert.That(db.Select(q).Map(x => x.Text), Is.EqualTo(new[] { "The pale blue dot", "The founding of Rome" }));
    }

    [Test]
    public void Return_the_distance_and_leave_out_weak_matches()
    {
        using var db = OpenVectorDb();

        var matches = db.Select<PassageMatch>(db.From<Passage>()
            .Where(x => Sql.CosineDistance(x.Embedding, AboutSpace) < 0.5)
            .OrderBy(x => Sql.CosineDistance(x.Embedding, AboutSpace))
            .Select(x => new {
                x.Id,
                x.Text,
                Distance = Sql.CosineDistance(x.Embedding, AboutSpace),
            }));

        Assert.That(matches.Map(x => x.Text), Is.EqualTo(new[] { "The pale blue dot", "Spice is mined on Arrakis" }));

        // The same distance as it's calculated in memory
        Assert.That(matches[0].Distance, Is.EqualTo(Sql.CosineDistance([0f, 0.9f, 0.1f], AboutSpace)).Within(0.0001));
        Assert.That(matches[0].Distance, Is.LessThan(matches[1].Distance));
    }

    [Test]
    public void Measure_the_straight_line_distance()
    {
        using var db = OpenVectorDb();

        var q = db.From<Passage>()
            .OrderBy(x => Sql.L2Distance(x.Embedding, AboutDragons))
            .Select(x => new { x.Id, x.Text, Distance = Sql.L2Distance(x.Embedding, AboutDragons) });
        var matches = db.Select<PassageMatch>(q);

        Assert.That(matches[0].Text, Is.EqualTo("Smaug guards his hoard"));
        Assert.That(matches[0].Distance, Is.EqualTo(Sql.L2Distance([0.9f, 0f, 0.1f], AboutDragons)).Within(0.0001));
    }

    [Test]
    public void Save_update_and_clear_vectors()
    {
        using var db = OpenVectorDb();

        var passage = new Passage { BookId = 1, Text = "Not embedded yet" };
        passage.Id = (int)db.Insert(passage, selectIdentity: true);
        Assert.That(db.SingleById<Passage>(passage.Id).Embedding, Is.Null);

        passage.Embedding = [0.5f, 0.25f, 0.125f];
        db.Update(passage);
        Assert.That(db.SingleById<Passage>(passage.Id).Embedding, Is.EqualTo(new[] { 0.5f, 0.25f, 0.125f }));

        float[] changed = [0.1f, 0.2f, 0.3f];
        db.UpdateOnly(() => new Passage { Embedding = changed }, where: x => x.Id == passage.Id);
        Assert.That(db.SingleById<Passage>(passage.Id).Embedding, Is.EqualTo(changed));

        db.Save(new Passage { Id = passage.Id, BookId = 1, Text = "Saved", Embedding = [1f, 2f, 3f] });
        Assert.That(db.SingleById<Passage>(passage.Id).Embedding, Is.EqualTo(new[] { 1f, 2f, 3f }));

        // Rows without a vector are left out when comparing them
        db.UpdateOnly(() => new Passage { Embedding = null }, where: x => x.Id == passage.Id);
        var nearest = db.Select(db.From<Passage>()
            .Where(x => x.Embedding != null)
            .OrderBy(x => Sql.CosineDistance(x.Embedding, AboutDragons)));
        Assert.That(nearest.Count, Is.EqualTo(5));
        Assert.That(nearest.Map(x => x.Id), Does.Not.Contain(passage.Id));
    }

    [Test]
    public async Task Find_the_most_similar_rows_async()
    {
        using var db = OpenVectorDb();

        await db.InsertAsync(new Passage { BookId = 1, Text = "Here be dragons", Embedding = [1f, 0f, 0f] });

        var nearest = await db.SelectAsync(db.From<Passage>()
            .OrderBy(x => Sql.CosineDistance(x.Embedding, AboutDragons))
            .Take(1));

        Assert.That(nearest[0].Text, Is.EqualTo("Here be dragons"));
        Assert.That(nearest[0].Embedding, Is.EqualTo(AboutDragons));
    }

    [Test]
    public void Create_a_vector_index()
    {
        using var db = OpenVectorDb();
        db.DropAndCreateTable<IndexedPassage>();

        var indexes = DialectProvider.ToCreateIndexStatements(typeof(IndexedPassage));
        if (Dialect.AnyPostgreSql.HasFlag(Dialect))
            Assert.That(indexes.Single(), Does.Contain("USING hnsw").And.Contain("vector_cosine_ops"));
        else if (Dialect.AnyMySql.HasFlag(Dialect) && db.Scalar<string>("SELECT VERSION()").Contains("MariaDB"))
            Assert.That(indexes.Single(), Does.StartWith("CREATE VECTOR INDEX").And.Contain("DISTANCE=cosine"));
        else
            Assert.That(indexes, Is.Empty); // every row is compared

        db.InsertAll(new[] {
            new IndexedPassage { Text = "Smaug guards his hoard", Embedding = [0.9f, 0f, 0.1f] },
            new IndexedPassage { Text = "The pale blue dot", Embedding = [0f, 0.9f, 0.1f] },
        });

        var nearest = db.Select(db.From<IndexedPassage>()
            .OrderBy(x => Sql.CosineDistance(x.Embedding, AboutSpace))
            .Take(1));
        Assert.That(nearest[0].Text, Is.EqualTo("The pale blue dot"));
    }

    [Test]
    public void Compare_normalized_vectors_by_their_inner_product()
    {
        using var db = OpenVectorDb();

        var isMariaDb = Dialect.AnyMySql.HasFlag(Dialect) && db.Scalar<string>("SELECT VERSION()").Contains("MariaDB");
        if (Dialect == Dialect.Sqlite || isMariaDb)
        {
            // sqlite-vec and MariaDB only have the cosine and L2 distances
            Assert.That(() => db.From<Passage>().OrderBy(x => Sql.NegativeInnerProduct(x.Embedding, AboutDragons)),
                Throws.TypeOf<NotSupportedException>());
            return;
        }

        var q = db.From<Passage>().OrderBy(x => Sql.NegativeInnerProduct(x.Embedding, AboutDragons)).Take(1);
        Assert.That(db.Select(q)[0].Text, Is.EqualTo("Smaug guards his hoard"));
    }

    [Test]
    public void Vector_columns_are_float_arrays()
    {
        Assert.That(() => ModelDefinition<NotAVector>.Definition, Throws.TypeOf<NotSupportedException>());
    }

    public class NotAVector
    {
        public int Id { get; set; }
        [Vector(3)]
        public string Embedding { get; set; }
    }
}
