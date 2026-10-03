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
        Assert.That(() => ModelDefinition<NotAVector>.Definition, Throws.TypeOf<NotSupportedException>()
            .With.Message.Contains("float[] and ReadOnlyMemory<float>"));
    }

    public class NotAVector
    {
        public int Id { get; set; }
        [Vector(3)]
        public string Embedding { get; set; }
    }

    // Classes that vectors are read into need [Vector] too
    public class PassageVector
    {
        public int Id { get; set; }
        [Vector(3)]
        public float[] Embedding { get; set; }
    }

    public class PassageWithTitle
    {
        public string Text { get; set; }
        [Vector(3)]
        public float[] Embedding { get; set; }
        public string Title { get; set; }
    }

    [Test]
    public void Select_vectors_in_a_custom_select()
    {
        using var db = OpenVectorDb();

        var rows = db.Select<PassageVector>(db.From<Passage>().OrderBy(x => x.Id).Select(x => new { x.Id, x.Embedding }));
        Assert.That(rows[0].Embedding, Is.EqualTo(new[] { 0.9f, 0f, 0.1f }));

        // With the columns of joined tables
        var q = db.From<Passage>()
            .Join<Book>((p, b) => p.BookId == b.Id)
            .Where(x => x.Text == "Smaug guards his hoard")
            .Select<Passage, Book>((p, b) => new { p.Text, p.Embedding, b.Title });
        var withTitle = db.Single<PassageWithTitle>(q);
        Assert.That(withTitle.Title, Is.EqualTo("The Hobbit"));
        Assert.That(withTitle.Embedding, Is.EqualTo(new[] { 0.9f, 0f, 0.1f }));

        // The joined table's vector in a query whose rows are its own
        var joined = db.Select<PassageVector>(db.From<Book>()
            .Join<Passage>((b, p) => b.Id == p.BookId)
            .Where<Passage>(p => p.Text == "The pale blue dot"));
        Assert.That(joined.Single().Embedding, Is.EqualTo(new[] { 0f, 0.9f, 0.1f }));
    }

    public class MemoryPassage
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Text { get; set; }

        // e.g. Embedding<float>.Vector of Microsoft.Extensions.AI
        [Vector(3)]
        public ReadOnlyMemory<float> Embedding { get; set; }

        [Vector(3)]
        public ReadOnlyMemory<float>? Summary { get; set; }
    }

    [Test]
    public void Vectors_can_be_ReadOnlyMemory()
    {
        using var db = OpenVectorDb();
        db.DropAndCreateTable<MemoryPassage>();

        ReadOnlyMemory<float> dragons = new[] { 0.9f, 0f, 0.1f };
        db.Insert(new MemoryPassage { Text = "Smaug guards his hoard", Embedding = dragons, Summary = dragons });
        db.Insert(new MemoryPassage { Text = "The pale blue dot", Embedding = new[] { 0f, 0.9f, 0.1f } });
        // Saved without a vector, which is NULL
        db.Insert(new MemoryPassage { Text = "Not embedded yet" });

        var nearest = db.Select(db.From<MemoryPassage>()
            .Where(x => x.Text != "Not embedded yet")
            .OrderBy(x => Sql.CosineDistance(x.Embedding, new ReadOnlyMemory<float>(AboutSpace)))
            .Take(1));
        Assert.That(nearest[0].Text, Is.EqualTo("The pale blue dot"));
        Assert.That(nearest[0].Embedding.ToArray(), Is.EqualTo(new[] { 0f, 0.9f, 0.1f }));
        Assert.That(nearest[0].Summary, Is.Null);

        // float[] vectors can be compared with them too
        var smaug = db.Single(db.From<MemoryPassage>()
            .Where(x => x.Text != "Not embedded yet")
            .OrderBy(x => Sql.CosineDistance(x.Embedding, AboutDragons)));
        Assert.That(smaug.Summary!.Value.ToArray(), Is.EqualTo(dragons.ToArray()));
        Assert.That(db.Single<MemoryPassage>(x => x.Text == "Not embedded yet").Embedding.IsEmpty);
    }

    public class TunedPassage
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Text { get; set; }

        [Vector(3, M = 8, EfConstruction = 32), Index, Required]
        public float[] Embedding { get; set; }
    }

    public class MPassage
    {
        [AutoIncrement]
        public int Id { get; set; }

        [Vector(3, M = 8), Index, Required]
        public float[] Embedding { get; set; }
    }

    public class ListedPassage
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Text { get; set; }

        [Vector(3, IndexType = VectorIndexType.IvfFlat, Lists = 2), Index, Required]
        public float[] Embedding { get; set; }
    }

    bool IsMariaDb(IDbConnection db) =>
        Dialect.AnyMySql.HasFlag(Dialect) && db.Scalar<string>("SELECT VERSION()").Contains("MariaDB");

    [Test]
    public void Create_vector_indexes_with_options()
    {
        using var db = OpenVectorDb();

        if (IsMariaDb(db))
        {
            // MariaDB's vector indexes only have M
            Assert.That(() => DialectProvider.ToCreateIndexStatements(typeof(TunedPassage)), Throws.TypeOf<NotSupportedException>());
            Assert.That(() => DialectProvider.ToCreateIndexStatements(typeof(ListedPassage)), Throws.TypeOf<NotSupportedException>());
            Assert.That(DialectProvider.ToCreateIndexStatements(typeof(MPassage)).Single(), Does.Contain(" M=8 DISTANCE=cosine"));
            db.DropAndCreateTable<MPassage>();
            return;
        }

        var tuned = DialectProvider.ToCreateIndexStatements(typeof(TunedPassage));
        if (Dialect.AnyPostgreSql.HasFlag(Dialect))
            Assert.That(tuned.Single(), Does.Contain("USING hnsw").And.Contain("WITH (m = 8, ef_construction = 32)"));
        else
            Assert.That(tuned, Is.Empty);

        db.DropAndCreateTable<TunedPassage>();
        db.InsertAll(new[] {
            new TunedPassage { Text = "Smaug guards his hoard", Embedding = [0.9f, 0f, 0.1f] },
            new TunedPassage { Text = "The pale blue dot", Embedding = [0f, 0.9f, 0.1f] },
        });
        Assert.That(db.Single(db.From<TunedPassage>().OrderBy(x => Sql.CosineDistance(x.Embedding, AboutSpace)).Take(1)).Text,
            Is.EqualTo("The pale blue dot"));

        if (!Dialect.AnyPostgreSql.HasFlag(Dialect))
            return;
        Assert.That(DialectProvider.ToCreateIndexStatements(typeof(ListedPassage)).Single(),
            Does.Contain("USING ivfflat").And.Contain("WITH (lists = 2)"));
        db.DropAndCreateTable<ListedPassage>();
        db.Insert(new ListedPassage { Text = "Smaug guards his hoard", Embedding = [0.9f, 0f, 0.1f] });
        Assert.That(db.Select<ListedPassage>().Count, Is.EqualTo(1));
    }

    [Test]
    public void Set_how_vector_indexes_are_searched()
    {
        using var db = OpenVectorDb();
        db.SetVectorSearch(new() { EfSearch = 100, Probes = 4, IterativeScan = true });

        if (Dialect.AnyPostgreSql.HasFlag(Dialect))
        {
            Assert.That(db.Scalar<string>("SHOW hnsw.ef_search"), Is.EqualTo("100"));
            Assert.That(db.Scalar<string>("SHOW ivfflat.probes"), Is.EqualTo("4"));
            Assert.That(db.Scalar<string>("SHOW hnsw.iterative_scan"), Is.EqualTo("strict_order"));
        }
        else if (IsMariaDb(db))
        {
            Assert.That(db.Scalar<int>("SELECT @@mhnsw_ef_search"), Is.EqualTo(100));
        }

        // Queries are run as before
        Assert.That(db.Single(db.From<Passage>().OrderBy(x => Sql.CosineDistance(x.Embedding, AboutSpace)).Take(1)).Text,
            Is.EqualTo("The pale blue dot"));
    }

    public class HalfPassage
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Text { get; set; }

        [Vector(3, Precision = VectorPrecision.Half), Index, Required]
        public float[] Embedding { get; set; }
    }

    [Test]
    public void Store_vectors_in_half_precision()
    {
        using var db = OpenVectorDb();

        if (!Dialect.AnyPostgreSql.HasFlag(Dialect) && !Dialect.AnySqlServer.HasFlag(Dialect))
        {
            Assert.That(() => db.DropAndCreateTable<HalfPassage>(), Throws.TypeOf<NotSupportedException>());
            return;
        }

        try
        {
            db.DropAndCreateTable<HalfPassage>();
        }
        catch (Exception e) when (Dialect.AnySqlServer.HasFlag(Dialect))
        {
            Assert.Ignore("Half-precision vectors need SQL Server 2025's PREVIEW_FEATURES: " + e.Message);
        }

        if (Dialect.AnyPostgreSql.HasFlag(Dialect))
            Assert.That(DialectProvider.ToCreateIndexStatements(typeof(HalfPassage)).Single(), Does.Contain("halfvec_cosine_ops"));

        db.InsertAll(new[] {
            new HalfPassage { Text = "Smaug guards his hoard", Embedding = [0.9f, 0f, 0.1f] },
            new HalfPassage { Text = "The pale blue dot", Embedding = [0f, 0.9f, 0.1f] },
        });
        var nearest = db.Single(db.From<HalfPassage>().OrderBy(x => Sql.CosineDistance(x.Embedding, AboutSpace)).Take(1));
        Assert.That(nearest.Text, Is.EqualTo("The pale blue dot"));
        // Read back with the precision they were stored with
        Assert.That(nearest.Embedding[1], Is.EqualTo(0.9f).Within(0.001f));
    }
}
