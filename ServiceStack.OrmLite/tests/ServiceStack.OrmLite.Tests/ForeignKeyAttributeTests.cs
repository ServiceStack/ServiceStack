using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests
{
    [TestFixtureOrmLite]
    public class ForeignKeyAttributeTests : OrmLiteProvidersTestBase
    {
        public ForeignKeyAttributeTests(DialectContext context) : base(context) {}

        [OneTimeSetUp]
        public void Setup()
        {
            using var dbConn = OpenDbConnection();
            dbConn.DropTable<TypeWithOnDeleteAndUpdateCascade>();
            dbConn.DropTable<TypeWithOnDeleteSetNull>();
            dbConn.DropTable<TypeWithOnDeleteSetDefault>();
            dbConn.DropTable<TypeWithOnDeleteRestrict>();
            dbConn.DropTable<TypeWithOnDeleteNoAction>();
            dbConn.DropTable<TypeWithOnDeleteCascade>();
            dbConn.DropTable<TypeWithSimpleForeignKey>();
            dbConn.DropTable<ReferencedType>();

            dbConn.CreateTable<ReferencedType>();
        }

        [Test]
        public void CanCreateSimpleForeignKey()
        {
            using var dbConn = OpenDbConnection();
            dbConn.DropAndCreateTable<TypeWithSimpleForeignKey>();
        }

        [Test]
        public void CanCreateForeignWithOnDeleteCascade()
        {
            using var dbConn = OpenDbConnection();
            dbConn.DropAndCreateTable<TypeWithOnDeleteCascade>();
        }

        [Test]
        [IgnoreDialect(Dialect.Sqlite, "no support for cascade deletes")]
        public void CascadesOnDelete()
        {
            // TODO: group tests around db features
            Setup();
            using var db = OpenDbConnection();
            db.CreateTableIfNotExists<TypeWithOnDeleteCascade>();
            db.Save(new ReferencedType { Id = 1 });
            db.Save(new TypeWithOnDeleteCascade { RefId = 1 });

            Assert.AreEqual(1, db.Select<ReferencedType>().Count);
            Assert.AreEqual(1, db.Select<TypeWithOnDeleteCascade>().Count);

            db.Delete<ReferencedType>(r => r.Id == 1);

            Assert.AreEqual(0, db.Select<ReferencedType>().Count);
            Assert.AreEqual(0, db.Select<TypeWithOnDeleteCascade>().Count);
        }

        [Test]
        public void CanCreateForeignWithOnDeleteCascadeAndOnUpdateCascade()
        {
            using var dbConn = OpenDbConnection();
            dbConn.DropAndCreateTable<TypeWithOnDeleteAndUpdateCascade>();
        }

        [Test]
        [IgnoreDialect(Tests.Dialect.Sqlite, "Not supported in sqlite?")]
        public void CanCreateForeignWithOnDeleteNoAction()
        {
            using var dbConn = OpenDbConnection();
            dbConn.DropAndCreateTable<TypeWithOnDeleteNoAction>();
        }

        [Test]
        public void CanCreateForeignWithOnDeleteRestrict()
        {
            using var dbConn = OpenDbConnection();
            dbConn.DropAndCreateTable<TypeWithOnDeleteRestrict>();
        }

        [Test]
        public void CanCreateForeignWithOnDeleteSetDefault()
        {
            // ignoring Not supported in InnoDB: http://stackoverflow.com/a/1498015/85785
            if (DialectProvider == MySqlDialect.Provider)
            {
                Assert.Ignore("MySql FK's not supported, skipping");
                return;
            }

            using var dbConn = OpenDbConnection();
            dbConn.DropAndCreateTable<TypeWithOnDeleteSetDefault>();
        }

        [Test]
        public void CanCreateForeignWithOnDeleteSetNull()
        {
            using var dbConn = OpenDbConnection();
            dbConn.DropAndCreateTable<TypeWithOnDeleteSetNull>();
        }

        [Test]
        public void Can_Skip_creating_ForeignKeys()
        {
            OrmLiteConfig.SkipForeignKeys = true;
            Setup();

            using (var db = OpenDbConnection())
            {
                db.CreateTableIfNotExists<TypeWithOnDeleteCascade>();
                db.Save(new ReferencedType { Id = 1 });
                db.Save(new TypeWithOnDeleteCascade { RefId = 1 });

                db.Delete<ReferencedType>(r => r.Id == 1);

                Assert.That(db.Select<ReferencedType>().Count, Is.EqualTo(0));
                Assert.That(db.Select<TypeWithOnDeleteCascade>().Count, Is.EqualTo(1));
            }

            OrmLiteConfig.SkipForeignKeys = false;
        }
    }

    public class ReferencedType
    {
        public int Id { get; set; }
    }


    public class TypeWithSimpleForeignKey
    {
        [AutoIncrement]
        public int Id { get; set; }
        [References(typeof(ReferencedType))]
        public int RefId { get; set; }
    }

    public class TypeWithOnDeleteCascade
    {
        [AutoIncrement]
        public int Id { get; set; }

        [ForeignKey(typeof(ReferencedType), OnDelete = "CASCADE")]
        public int? RefId { get; set; }
    }

    public class TypeWithOnDeleteAndUpdateCascade
    {
        [AutoIncrement]
        public int Id { get; set; }

        [ForeignKey(typeof(ReferencedType), OnDelete = "CASCADE", OnUpdate = "CASCADE")]
        public int? RefId { get; set; }
    }

    public class TypeWithOnDeleteNoAction
    {
        [AutoIncrement]
        public int Id { get; set; }

        [ForeignKey(typeof(ReferencedType), OnDelete = "NO ACTION")]
        public int? RefId { get; set; }
    }

    public class TypeWithOnDeleteRestrict
    {
        [AutoIncrement]
        public int Id { get; set; }

        [ForeignKey(typeof(ReferencedType), OnDelete = "RESTRICT")]
        public int? RefId { get; set; }
    }

    public class TypeWithOnDeleteSetDefault
    {
        [AutoIncrement]
        public int Id { get; set; }

        [Default(typeof(int), "17")]
        [ForeignKey(typeof(ReferencedType), OnDelete = "SET DEFAULT")]
        public int RefId { get; set; }
    }

    public class TypeWithOnDeleteSetNull
    {
        [AutoIncrement]
        public int Id { get; set; }

        [ForeignKey(typeof(ReferencedType), OnDelete = "SET NULL")]
        public int? RefId { get; set; }
    }
}
