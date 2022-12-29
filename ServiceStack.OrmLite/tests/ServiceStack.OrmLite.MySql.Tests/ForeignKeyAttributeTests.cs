using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.MySql.Tests
{
    [TestFixture]
    public class ForeignKeyAttributeTests : OrmLiteTestBase
    {
        public ForeignKeyAttributeTests()
        {
            using (var db = OpenDbConnection())
            {
                db.DropTable<TypeWithOnDeleteAndUpdateCascade>();
                db.DropTable<TypeWithOnDeleteSetNull>();
                db.DropTable<TypeWithOnDeleteSetDefault>();
                db.DropTable<TypeWithOnDeleteRestrict>();
                db.DropTable<TypeWithOnDeleteNoAction>();
                db.DropTable<TypeWithOnDeleteCascade>();
                db.DropTable<TypeWithSimpleForeignKey>();
                db.DropTable<ReferencedType>();

                db.CreateTable<ReferencedType>();
            }
        }

        [Test]
        public void CanCreateSimpleForeignKey()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<TypeWithSimpleForeignKey>(true);
            }
        }

        [Test]
        public void CanCreateForeignWithOnDeleteCascade()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<TypeWithOnDeleteCascade>(true);
            }
        }

        [Test]
        public void CascadesOnDelete()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<TypeWithOnDeleteCascade>(true);

                db.Save(new ReferencedType { Id = 1 });
                db.Save(new TypeWithOnDeleteCascade { RefId = 1 });

                Assert.AreEqual(1, db.Select<ReferencedType>().Count);
                Assert.AreEqual(1, db.Select<TypeWithOnDeleteCascade>().Count);

                db.Delete<ReferencedType>(r => r.Id == 1);

                Assert.AreEqual(0, db.Select<ReferencedType>().Count);
                Assert.AreEqual(0, db.Select<TypeWithOnDeleteCascade>().Count);
            }
        }

        [Test]
        public void CanCreateForeignWithOnDeleteCascadeAndOnUpdateCascade()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<TypeWithOnDeleteAndUpdateCascade>(true);
            }
        }

        [Test]
        public void CanCreateForeignWithOnDeleteNoAction()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<TypeWithOnDeleteNoAction>(true);
            }
        }

        [Test]
        public void CanCreateForeignWithOnDeleteRestrict()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<TypeWithOnDeleteRestrict>(true);
            }
        }

        [NUnit.Framework.Ignore("Not supported in MySQL")]
        [Test]
        public void CanCreateForeignWithOnDeleteSetDefault()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<TypeWithOnDeleteSetDefault>(true);
            }
        }

        [Test]
        public void CanCreateForeignWithOnDeleteSetNull()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<TypeWithOnDeleteSetNull>(true);
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