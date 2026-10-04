using System;
using System.Data;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.UseCases;

/// <summary>
/// SQLite can't alter a column, its default, foreign keys or constraints, so db.RebuildTable&lt;T&gt;() creates the
/// table again from its model and copies its rows, which ApplySchemaDiff() and the migrations of ToMigration() use.
/// </summary>
[TestFixtureOrmLite]
public class RebuildTableUseCases(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    public class RebuildCustomer
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public static class Before
    {
        public class RebuildOrder
        {
            [AutoIncrement]
            public int Id { get; set; }

            [Required, StringLength(20)]
            public string Reference { get; set; }

            public int Qty { get; set; }

            public int? CustomerId { get; set; }
        }
    }

    public class RebuildOrder
    {
        [AutoIncrement]
        public int Id { get; set; }

        [StringLength(50)] // larger and allows nulls
        public string Reference { get; set; }

        [Default(1), CheckConstraint("Qty > 0")] // a default and a check
        public int Qty { get; set; }

        [ForeignKey(typeof(RebuildCustomer))] // a foreign key
        public int? CustomerId { get; set; }
    }

    // Deletes its lines when its order is deleted, which a rebuilt table mustn't do
    public class RebuildLine
    {
        [AutoIncrement]
        public int Id { get; set; }

        [ForeignKey(typeof(RebuildOrder), OnDelete = "CASCADE")]
        public int OrderId { get; set; }
    }

    IDbConnection OpenSeededDb(int qty = 2)
    {
        var db = OpenDbConnection();
        db.ExecuteSql("PRAGMA foreign_keys=OFF");
        db.DropTable<RebuildLine>();
        db.DropTable<RebuildOrder>();
        db.DropAndCreateTable<RebuildCustomer>();
        db.CreateTable<Before.RebuildOrder>();
        db.CreateTable<RebuildLine>();
        db.ExecuteSql("PRAGMA foreign_keys=ON");

        db.Insert(new RebuildCustomer { Name = "Customer" });
        db.Insert(new Before.RebuildOrder { Reference = "a", Qty = qty, CustomerId = 1 });
        db.Insert(new Before.RebuildOrder { Reference = "b", Qty = qty, CustomerId = 1 });
        db.Insert(new Before.RebuildOrder { Reference = "c", Qty = qty, CustomerId = 1 });
        db.DeleteById<Before.RebuildOrder>(3); // the next id is still 4
        db.Insert(new RebuildLine { OrderId = 1 });
        db.Insert(new RebuildLine { OrderId = 2 });
        db.ExecuteSql("CREATE TRIGGER trg_rebuild_order AFTER INSERT ON RebuildOrder " +
                      "BEGIN UPDATE RebuildOrder SET Reference = upper(new.Reference) WHERE Id = new.Id; END");
        return db;
    }

    void AssertRebuilt(IDbConnection db)
    {
        Assert.That(db.GetSchemaDiff<RebuildOrder>().Changes, Is.Empty, db.GetSchemaDiff<RebuildOrder>().ToString());

        // Its rows, the rows that reference it and its triggers are kept
        Assert.That(db.Select<RebuildOrder>().Map(x => x.Reference), Is.EquivalentTo(new[] { "a", "b" }));
        Assert.That(db.Count<RebuildLine>(), Is.EqualTo(2));
        Assert.That(db.Scalar<long>("PRAGMA foreign_keys"), Is.EqualTo(1));

        // The ids of deleted rows aren't reused, the new default is used and the trigger runs
        var id = db.Insert(new RebuildOrder { Reference = "d", CustomerId = 1, Qty = 1 }, selectIdentity: true);
        Assert.That(id, Is.EqualTo(4));
        Assert.That(db.SingleById<RebuildOrder>(4).Reference, Is.EqualTo("D"));
        db.InsertOnly(() => new RebuildOrder { Reference = "e" });
        Assert.That(db.Single<RebuildOrder>(x => x.Reference == "E").Qty, Is.EqualTo(1));

        // The new check and foreign key are enforced
        // Insert() leaves out a [Default] property with its type's default, so Qty = 0 is inserted with SQL
        Assert.Throws(Is.InstanceOf<Exception>(), () => db.ExecuteSql("INSERT INTO RebuildOrder (Reference, Qty) VALUES ('f', 0)"));
        Assert.Throws(Is.InstanceOf<Exception>(), () => db.Insert(new RebuildOrder { Reference = "g", Qty = 1, CustomerId = 9 }));
    }

    [Test]
    public void SQLite_tables_are_rebuilt_to_make_the_changes_SQLite_cant_alter()
    {
        if (DialectProvider.Kind != DbKind.Sqlite)
            Assert.Ignore("Only SQLite needs its tables to be rebuilt");
        using var db = OpenSeededDb();

        var diff = db.GetSchemaDiff<RebuildOrder>();
        var rebuild = diff.Changes.Single(x => x.Type == SchemaChangeType.RebuildTable);
        var rebuilt = diff.Changes.Where(x => x.IsRebuilt).ToList();
        Assert.That(rebuilt.Map(x => x.Type), Is.EquivalentTo(new[] {
            SchemaChangeType.AlterColumn, SchemaChangeType.AlterDefault, SchemaChangeType.AddConstraint,
            SchemaChangeType.AddForeignKey }), diff.ToString());
        Assert.That(rebuilt.All(x => x.Sql == null));
        // The new check and foreign key can fail with the rows of the table
        Assert.That(rebuild.IsDestructive, Is.True);
        Assert.That(rebuild.Sql, Does.Contain("ALTER TABLE \"RebuildOrder_ormlite_rebuild\" RENAME TO \"RebuildOrder\""));
        Assert.That(diff.ToString(), Does.Contain("~ rebuild table to change Reference, the default of Qty"));
        Assert.That(diff.ToString(), Does.Contain("(by rebuilding the table)"));

        var source = diff.ToMigration("Migration1012");
        Assert.That(source, Does.Contain("Db.RebuildTable<RebuildOrder>();"));
        // The migration's model has all the properties the table is created from
        Assert.That(source, Does.Contain("[ForeignKey(typeof(ServiceStack.OrmLite.Tests.UseCases.RebuildTableUseCases.RebuildCustomer))]"));

        Assert.That(db.ApplySchemaDiff(diff), Does.Not.Contain(rebuild));
        // Outside a transaction, foreign keys aren't enforced while it's rebuilt, so its lines aren't deleted
        Assert.That(db.ApplySchemaDiff(diff, allowDestructive: true), Does.Contain(rebuild));
        AssertRebuilt(db);
    }

    [Test]
    public void SQLite_tables_are_rebuilt_in_a_migrations_transaction_when_foreign_keys_are_not_enforced()
    {
        if (DialectProvider.Kind != DbKind.Sqlite)
            Assert.Ignore("Only SQLite needs its tables to be rebuilt");
        using var db = OpenSeededDb();
        db.ExecuteSql("PRAGMA foreign_keys=OFF");

        using (var trans = db.OpenTransaction())
        {
            db.RebuildTable<RebuildOrder>();
            trans.Commit();
        }
        db.ExecuteSql("PRAGMA foreign_keys=ON");
        AssertRebuilt(db);
    }

    [Test]
    public void Tables_that_are_referenced_are_not_rebuilt_in_a_transaction_while_foreign_keys_are_enforced()
    {
        if (DialectProvider.Kind != DbKind.Sqlite)
            Assert.Ignore("Only SQLite needs its tables to be rebuilt");
        using var db = OpenSeededDb();

        using (var trans = db.OpenTransaction())
        {
            var ex = Assert.Throws<InvalidOperationException>(() => db.RebuildTable<RebuildOrder>());
            Assert.That(ex.Message, Does.Contain("RebuildLine"));
        }
        Assert.That(db.Count<RebuildLine>(), Is.EqualTo(2));
        Assert.That(db.GetSchemaDiff<RebuildOrder>().Changes.Any(x => x.Type == SchemaChangeType.RebuildTable));
    }

    [Test]
    public void A_table_is_not_changed_when_its_rows_break_the_rebuilt_table()
    {
        if (DialectProvider.Kind != DbKind.Sqlite)
            Assert.Ignore("Only SQLite needs its tables to be rebuilt");
        using var db = OpenSeededDb(qty: 0); // breaks Qty > 0

        Assert.Throws(Is.InstanceOf<Exception>(), () => db.RebuildTable<RebuildOrder>());

        Assert.That(db.Count<Before.RebuildOrder>(), Is.EqualTo(2));
        Assert.That(db.Count<RebuildLine>(), Is.EqualTo(2));
        Assert.That(db.Scalar<long>("PRAGMA foreign_keys"), Is.EqualTo(1));
        Assert.That(db.GetSchemaDiff<RebuildOrder>().Changes.Any(x => x.Type == SchemaChangeType.RebuildTable));
    }

    [Test]
    public void Other_databases_alter_their_tables()
    {
        if (DialectProvider.Kind == DbKind.Sqlite)
            Assert.Ignore("SQLite rebuilds its tables");
        using var db = OpenDbConnection();
        Assert.Throws<NotSupportedException>(() => db.RebuildTable<RebuildOrder>());
    }
}
