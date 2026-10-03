using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Data;

namespace ServiceStack.OrmLite.Tests.UseCases;

/// <summary>
/// Queries that can read from a read replica, e.g. a PostgreSQL standby, use OpenReadOnlyDbConnection(), which opens
/// the replica when one is registered, otherwise the primary. Two SQLite databases stand in for the primary and its
/// replica, with different rows to tell them apart.
/// </summary>
public class ReadReplicaUseCases
{
    public class ReplicaItem
    {
        public int Id { get; set; }
        public string Database { get; set; }
    }

    private readonly List<string> files = [];
    private readonly List<string> namedConnections = [];

    // A database with a row that says which database it is
    private string CreateDatabase(string name)
    {
        var path = Path.Combine(Path.GetTempPath(), $"replica-{name}-{Guid.NewGuid():N}.sqlite");
        files.Add(path);
        using var db = new OrmLiteConnectionFactory(path, SqliteDialect.Provider, setGlobalDialectProvider: false)
            .OpenDbConnection();
        db.CreateTable<ReplicaItem>();
        db.Insert(new ReplicaItem { Id = 1, Database = name });
        return path;
    }

    private static string DatabaseOf(IDbConnection db) => db.SingleById<ReplicaItem>(1).Database;

    private string RegisterConnection(OrmLiteConnectionFactory dbFactory, string connectionString)
    {
        var name = "replica-test-" + Guid.NewGuid().ToString("N");
        namedConnections.Add(name);
        dbFactory.RegisterConnection(name, connectionString, SqliteDialect.Provider);
        return name;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var name in namedConnections)
            OrmLiteConnectionFactory.NamedConnections.Remove(name);
        namedConnections.Clear();
        foreach (var file in files)
            File.Delete(file);
        files.Clear();
    }

    [Test]
    public async Task Opens_the_read_replica()
    {
        IDbConnectionFactory dbFactory = new OrmLiteConnectionFactory(CreateDatabase("primary"), SqliteDialect.Provider,
            setGlobalDialectProvider: false);
        dbFactory.RegisterReadReplica(CreateDatabase("replica"));

        using (var db = dbFactory.OpenReadOnlyDbConnection())
            Assert.That(DatabaseOf(db), Is.EqualTo("replica"));
        using (var db = await dbFactory.OpenReadOnlyDbConnectionAsync())
            Assert.That(DatabaseOf(db), Is.EqualTo("replica"));

        // Other connections are the primary
        using (var db = dbFactory.OpenDbConnection())
            Assert.That(DatabaseOf(db), Is.EqualTo("primary"));
    }

    [Test]
    public async Task Opens_the_primary_without_a_read_replica()
    {
        IDbConnectionFactory dbFactory = new OrmLiteConnectionFactory(CreateDatabase("primary"), SqliteDialect.Provider,
            setGlobalDialectProvider: false);

        using (var db = dbFactory.OpenReadOnlyDbConnection())
            Assert.That(DatabaseOf(db), Is.EqualTo("primary"));
        using (var db = await dbFactory.OpenReadOnlyDbConnectionAsync())
            Assert.That(DatabaseOf(db), Is.EqualTo("primary"));
    }

    [Test]
    public async Task Named_connections_have_read_replicas_of_their_own()
    {
        var dbFactory = new OrmLiteConnectionFactory(CreateDatabase("primary"), SqliteDialect.Provider,
            setGlobalDialectProvider: false);
        var reporting = RegisterConnection(dbFactory, CreateDatabase("reporting"));
        var archive = RegisterConnection(dbFactory, CreateDatabase("archive"));
        dbFactory.RegisterReadReplica(reporting, CreateDatabase("reporting-replica"));

        // Its connections have the name of the named connection, e.g. for filters that configure each database
        using (var db = dbFactory.OpenReadOnlyDbConnection(reporting))
        {
            Assert.That(DatabaseOf(db), Is.EqualTo("reporting-replica"));
            Assert.That(((OrmLiteConnection)db).NamedConnection, Is.EqualTo(reporting));
        }
        using (var db = await dbFactory.OpenReadOnlyDbConnectionAsync(reporting))
        {
            Assert.That(DatabaseOf(db), Is.EqualTo("reporting-replica"));
            Assert.That(((OrmLiteConnection)db).NamedConnection, Is.EqualTo(reporting));
        }
        using (var db = dbFactory.OpenDbConnection(reporting))
            Assert.That(DatabaseOf(db), Is.EqualTo("reporting"));

        // A named connection without a replica, and the main connection, which doesn't have the named connection's
        using (var db = dbFactory.OpenReadOnlyDbConnection(archive))
            Assert.That(DatabaseOf(db), Is.EqualTo("archive"));
        using (var db = dbFactory.OpenReadOnlyDbConnection())
        {
            Assert.That(DatabaseOf(db), Is.EqualTo("primary"));
            Assert.That(((OrmLiteConnection)db).NamedConnection, Is.Null);
        }

        Assert.Throws<KeyNotFoundException>(() => dbFactory.OpenReadOnlyDbConnection("not-registered"));
        Assert.Throws<KeyNotFoundException>(() => dbFactory.RegisterReadReplica("not-registered", "replica.sqlite"));
    }

    [Test]
    public void Read_replicas_are_registered_with_the_configuration_builder()
    {
        var dbFactory = new OrmLiteConnectionFactory(CreateDatabase("primary"), SqliteDialect.Provider,
            setGlobalDialectProvider: false);
        var name = "replica-test-" + Guid.NewGuid().ToString("N");
        namedConnections.Add(name);

        // e.g. services.AddOrmLite(options => options.UsePostgres(...)).AddReadReplica(...)
        new OrmLiteConfigurationBuilder(dbFactory)
            .AddReadReplica(CreateDatabase("replica"))
            .AddConnection(name, CreateDatabase("reporting"), SqliteDialect.Provider)
            .AddReadReplica(name, CreateDatabase("reporting-replica"));

        using (var db = dbFactory.OpenReadOnlyDbConnection())
            Assert.That(DatabaseOf(db), Is.EqualTo("replica"));
        using (var db = dbFactory.OpenReadOnlyDbConnection(name))
            Assert.That(DatabaseOf(db), Is.EqualTo("reporting-replica"));
    }

    static readonly FilterSet<string> DatabaseFilters = FilterSet.Create<string>(f =>
        f.Filter<ReplicaItem>((x, database) => x.Database == database));

    [Test]
    public void Read_replicas_use_the_primarys_dialect_and_connection_filters()
    {
        var dbFactory = new OrmLiteConnectionFactory(CreateDatabase("primary"), SqliteDialect.Provider,
            setGlobalDialectProvider: false);
        var opened = 0;
        dbFactory.ConnectionFilter = db => { opened++; return db; };
        dbFactory.RegisterReadReplica(CreateDatabase("replica"));

        // The same dialect, so naming strategies, converters and retry policies are the same
        Assert.That(dbFactory.ReadReplica.DialectProvider, Is.SameAs(dbFactory.DialectProvider));

        using var db = dbFactory.OpenReadOnlyDbConnection();
        Assert.That(opened, Is.EqualTo(1));

        // FilterSets are used by the replica's connections the same way
        db.UseFilters(DatabaseFilters.For("primary"));
        Assert.That(db.Select<ReplicaItem>(), Is.Empty);
    }

    [Test]
    public void Read_only_connections_reject_writes_on_SQLite()
    {
        var dbFactory = new OrmLiteConnectionFactory(CreateDatabase("primary"), SqliteDialect.Provider,
            setGlobalDialectProvider: false);

        using (var db = dbFactory.OpenReadOnlyDbConnection())
        {
            Assert.That(DatabaseOf(db), Is.EqualTo("primary"));
            Assert.Catch(() => db.Insert(new ReplicaItem { Id = 2, Database = "written" }));
        }

        // The pooled connection is read-write again
        using (var db = dbFactory.OpenDbConnection())
        {
            db.Insert(new ReplicaItem { Id = 2, Database = "written" });
            Assert.That(db.Count<ReplicaItem>(), Is.EqualTo(2));
        }
    }

    [Test]
    public void Connections_say_if_theyre_read_only_and_to_a_replica()
    {
        var dbFactory = new OrmLiteConnectionFactory(CreateDatabase("primary"), SqliteDialect.Provider,
            setGlobalDialectProvider: false);
        dbFactory.RegisterReadReplica(CreateDatabase("replica"));

        using (var db = (OrmLiteConnection)dbFactory.OpenReadOnlyDbConnection())
        {
            Assert.That(db.IsReadOnly, Is.True);
            Assert.That(db.IsReadReplica, Is.True);
            Assert.That(((OrmLiteConnection)db.WithoutFilters()).IsReadOnly, Is.True);
        }
        using (var db = (OrmLiteConnection)dbFactory.OpenDbConnection())
        {
            Assert.That(db.IsReadOnly, Is.False);
            Assert.That(db.IsReadReplica, Is.False);
        }
    }

    [Test]
    public async Task Falls_back_to_the_primary_when_the_replica_cant_be_opened()
    {
        // A database that doesn't exist, which SQLite would otherwise create
        var missing = $"Data Source={Path.Combine(Path.GetTempPath(), "missing-" + Guid.NewGuid().ToString("N") + ".sqlite")};Mode=ReadOnly";
        var dbFactory = new OrmLiteConnectionFactory(CreateDatabase("primary"), SqliteDialect.Provider,
            setGlobalDialectProvider: false);

        dbFactory.RegisterReadReplica(missing);
        Assert.Catch(() => dbFactory.OpenReadOnlyDbConnection());

        dbFactory.RegisterReadReplica(missing, fallbackToPrimary: true);
        using (var db = (OrmLiteConnection)dbFactory.OpenReadOnlyDbConnection())
        {
            Assert.That(DatabaseOf(db), Is.EqualTo("primary"));
            Assert.That(db.IsReadReplica, Is.False);
            Assert.That(db.IsReadOnly, Is.True); // still can't write
        }
        using (var db = await dbFactory.OpenReadOnlyDbConnectionAsync())
            Assert.That(DatabaseOf(db), Is.EqualTo("primary"));

        // Named connections too
        var reporting = RegisterConnection(dbFactory, CreateDatabase("reporting"));
        dbFactory.RegisterReadReplica(reporting, missing, fallbackToPrimary: true);
        using (var db = dbFactory.OpenReadOnlyDbConnection(reporting))
            Assert.That(DatabaseOf(db), Is.EqualTo("reporting"));
    }

    [Test]
    public void Read_replica_connections_reject_writes()
    {
        // e.g. in development, where the replica is another database that isn't read-only
        var dbFactory = new OrmLiteConnectionFactory(CreateDatabase("primary"), SqliteDialect.Provider,
            setGlobalDialectProvider: false);
        dbFactory.RegisterReadReplica(CreateDatabase("replica"));

        using (var db = dbFactory.OpenReadOnlyDbConnection())
        {
            Assert.That(DatabaseOf(db), Is.EqualTo("replica"));
            Assert.Catch(() => db.Insert(new ReplicaItem { Id = 2, Database = "written" }));
        }
        using (var db = dbFactory.OpenDbConnection())
        {
            db.Insert(new ReplicaItem { Id = 2, Database = "written" });
            Assert.That(db.Count<ReplicaItem>(), Is.EqualTo(2));
        }
    }

    class DiagnosticObserver : IObserver<KeyValuePair<string, object>>
    {
        public readonly List<OrmLiteDiagnosticEvent> Events = [];
        public void OnNext(KeyValuePair<string, object> e)
        {
            if (e.Value is OrmLiteDiagnosticEvent { Command: not null } ormLiteEvent)
                lock (Events) Events.Add(ormLiteEvent);
        }
        public void OnCompleted() {}
        public void OnError(Exception error) {}
    }

    [Test]
    public void Diagnostic_events_say_if_the_command_ran_on_a_read_replica()
    {
        var dbFactory = new OrmLiteConnectionFactory(CreateDatabase("primary"), SqliteDialect.Provider,
            setGlobalDialectProvider: false);
        dbFactory.RegisterReadReplica(CreateDatabase("replica"));

        var observer = new DiagnosticObserver();
        using (Diagnostics.OrmLite.Subscribe(observer))
        {
            using (var db = dbFactory.OpenReadOnlyDbConnection())
                db.Select<ReplicaItem>(x => x.Database == "from-replica");
            using (var db = dbFactory.OpenDbConnection())
                db.Select<ReplicaItem>(x => x.Database == "from-primary");
        }

        List<OrmLiteDiagnosticEvent> events;
        lock (observer.Events) events = observer.Events.ToList();
        var replica = events.Where(x => x.Command.Parameters.Cast<IDbDataParameter>().Any(p => "from-replica".Equals(p.Value))).ToList();
        var primary = events.Where(x => x.Command.Parameters.Cast<IDbDataParameter>().Any(p => "from-primary".Equals(p.Value))).ToList();
        Assert.That(replica, Is.Not.Empty);
        Assert.That(replica.All(x => x.IsReadReplica));
        Assert.That(primary, Is.Not.Empty);
        Assert.That(primary.All(x => !x.IsReadReplica));
    }
}
