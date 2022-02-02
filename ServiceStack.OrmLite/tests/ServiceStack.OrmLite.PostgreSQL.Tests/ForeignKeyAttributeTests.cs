using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Tests;

namespace ServiceStack.OrmLite.PostgreSQL.Tests
{
    [TestFixtureOrmLiteDialects(Dialect.AnyPostgreSql)]
    public class ForeignKeyAttributeTests : OrmLiteProvidersTestBase
    {
        public ForeignKeyAttributeTests(DialectContext context) : base(context) {}

        [OneTimeSetUp]
        public void Setup()
        {
            DropTables();

            using (var dbConn = OpenDbConnection())
            {
                dbConn.CreateTable<ReferencedType>(true);
            }
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            DropTables();
        }

        private void DropTables()
        {
            using (var dbConn = OpenDbConnection())
            {
                dbConn.DropTable<TypeWithOnDeleteAndUpdateCascade>();
                dbConn.DropTable<TypeWithOnDeleteSetNull>();
                dbConn.DropTable<TypeWithOnDeleteSetDefault>();
                dbConn.DropTable<TypeWithOnDeleteRestrict>();
                dbConn.DropTable<TypeWithOnDeleteNoAction>();
                dbConn.DropTable<TypeWithOnDeleteCascade>();
                dbConn.DropTable<TypeWithSimpleForeignKey>();
                dbConn.DropTable<ReferencedType>();
            }
        }

        [Test]
        public void CanCreateSimpleForeignKey()
        {
            using (var dbConn = OpenDbConnection())
            {
                dbConn.CreateTable<TypeWithSimpleForeignKey>(true);
            }
        }

        [Test]
        public void CanCreateForeignWithOnDeleteCascade()
        {
            using (var dbConn = OpenDbConnection())
            {
                dbConn.CreateTable<TypeWithOnDeleteCascade>(true);
            }
        }

        [Test]
        public void CascadesOnDelete()
        {
            using (var dbConn = OpenDbConnection())
            {
                dbConn.CreateTable<TypeWithOnDeleteCascade>(true);

                dbConn.Save(new ReferencedType { Id = 1 });
                dbConn.Save(new TypeWithOnDeleteCascade { RefId = 1 });

                Assert.AreEqual(1, dbConn.Select<ReferencedType>().Count);
                Assert.AreEqual(1, dbConn.Select<TypeWithOnDeleteCascade>().Count);

                dbConn.Delete<ReferencedType>(r => r.Id == 1);

                Assert.AreEqual(0, dbConn.Select<ReferencedType>().Count);
                Assert.AreEqual(0, dbConn.Select<TypeWithOnDeleteCascade>().Count);
            }
        }

        [Test]
        public void CanCreateForeignWithOnDeleteCascadeAndOnUpdateCascade()
        {
            using (var dbConn = OpenDbConnection())
            {
                dbConn.CreateTable<TypeWithOnDeleteAndUpdateCascade>(true);
            }
        }

        [Test]
        public void CanCreateForeignWithOnDeleteNoAction()
        {
            using (var dbConn = OpenDbConnection())
            {
                dbConn.CreateTable<TypeWithOnDeleteNoAction>(true);
            }
        }

        [Test]
        public void CanCreateForeignWithOnDeleteRestrict()
        {
            using (var dbConn = OpenDbConnection())
            {
                dbConn.CreateTable<TypeWithOnDeleteRestrict>(true);
            }
        }

        [Test]
        public void CanCreateForeignWithOnDeleteSetDefault()
        {
            using (var dbConn = OpenDbConnection())
            {
                dbConn.CreateTable<TypeWithOnDeleteSetDefault>(true);
            }
        }

        [Test]
        public void CanCreateForeignWithOnDeleteSetNull()
        {
            using (var dbConn = OpenDbConnection())
            {
                dbConn.CreateTable<TypeWithOnDeleteSetNull>(true);
            }
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