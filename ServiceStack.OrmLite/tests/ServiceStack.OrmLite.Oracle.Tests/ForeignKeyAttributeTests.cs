using System;
using System.Data;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests
{
    [TestFixture]
    public class ForeignKeyAttributeTests : OrmLiteTestBase
    {
        [OneTimeSetUp]
        public void Setup()
        {
            using (var dbConn = OpenDbConnection())
            {
                DropTables(dbConn);

                dbConn.DropAndCreateTable<ReferencedType>();
            }
        }

        private void DropTables(IDbConnection dbConnection)
        {
            if (dbConnection.TableExists("TWODUC")) dbConnection.DropTable<TypeWithOnDeleteAndUpdateCascade>();
            if (dbConnection.TableExists("TWODSN")) dbConnection.DropTable<TypeWithOnDeleteSetNull>();
            if (dbConnection.TableExists("TWODDF")) dbConnection.DropTable<TypeWithOnDeleteSetDefault>();
            if (dbConnection.TableExists("TWODNR")) dbConnection.DropTable<TypeWithOnDeleteRestrict>();
            if (dbConnection.TableExists("TWODNA")) dbConnection.DropTable<TypeWithOnDeleteNoAction>();
            if (dbConnection.TableExists("TWODC")) dbConnection.DropTable<TypeWithOnDeleteCascade>();
            if (dbConnection.TableExists("TWSKF")) dbConnection.DropTable<TypeWithSimpleForeignKey>();
            if (dbConnection.TableExists("TWONFKI")) dbConnection.DropTable<TypeWithNoForeignKeyInitially>();
        }

        [Test]
        public void CanCreateSimpleForeignKey()
        {
            using (var dbConnection = OpenDbConnection())
            {
                dbConnection.DropAndCreateTable<TypeWithSimpleForeignKey>();
            }
        }

        [Test]
        public void ForeignWithOnDeleteCascadeCreatesOk()
        {
            using (var dbConn = OpenDbConnection())
            {
                dbConn.DropAndCreateTable<TypeWithOnDeleteCascade>();
            }
        }

        [Test]
        public void ForeignWithOnDeleteCascadeWorks()
        {
            using (var dbConnection = OpenDbConnection())
            {
                dbConnection.DropAndCreateTable<TypeWithOnDeleteCascade>();

                dbConnection.Save(new ReferencedType { Id = 1 });
                dbConnection.Save(new TypeWithOnDeleteCascade { RefId = 1 });

                Assert.AreEqual(1, dbConnection.Select<ReferencedType>().Count);
                Assert.AreEqual(1, dbConnection.Select<TypeWithOnDeleteCascade>().Count);

                dbConnection.Delete<ReferencedType>(r => r.Id == 1);

                Assert.AreEqual(0, dbConnection.Select<ReferencedType>().Count);
                Assert.AreEqual(0, dbConnection.Select<TypeWithOnDeleteCascade>().Count);
            }
        }

        [Test]
        public void ForeignWithOnDeleteCascadeAndOnUpdateCascadeCreatesOk()
        {
            using (var dbConn = OpenDbConnection())
            {
                dbConn.DropAndCreateTable<TypeWithOnDeleteAndUpdateCascade>();
            }
        }

        [Test]
        public void ForeignWithOnDeleteNoActionCreatesOk()
        {
            using (var dbConn = OpenDbConnection())
            {
                dbConn.DropAndCreateTable<TypeWithOnDeleteNoAction>();
            }
        }

        [Test]
        public void ForeignWithOnDeleteNoActionThrowsException()
        {
            using (var dbConnection = OpenDbConnection())
            {
                dbConnection.CreateTableIfNotExists<TypeWithOnDeleteNoAction>();

                dbConnection.Save(new ReferencedType { Id = 1 });
                dbConnection.Save(new TypeWithOnDeleteNoAction { RefId = 1 });

                Assert.AreEqual(1, dbConnection.Select<ReferencedType>().Count);
                Assert.AreEqual(1, dbConnection.Select<TypeWithOnDeleteNoAction>().Count);

                // Do not want to require reference to dll with exception definition so use catch
                Assert.Catch<Exception>(() => dbConnection.Delete<ReferencedType>(r => r.Id == 1));
            }
        }

        [Test]
        public void ForeignWithOnDeleteRestrictCreatesOk()
        {
            using (var dbConn = OpenDbConnection())
            {
                dbConn.DropAndCreateTable<TypeWithOnDeleteRestrict>();
            }
        }

        [Test]
        public void ForeignWithOnDeleteRestrictThrowsException()
        {
            using (var dbConnection = OpenDbConnection())
            {
                dbConnection.CreateTableIfNotExists<TypeWithOnDeleteRestrict>();

                dbConnection.Save(new ReferencedType { Id = 1 });
                dbConnection.Save(new TypeWithOnDeleteRestrict { RefId = 1 });

                Assert.AreEqual(1, dbConnection.Select<ReferencedType>().Count);
                Assert.AreEqual(1, dbConnection.Select<TypeWithOnDeleteRestrict>().Count);

                // Do not want to require reference to dll with exception definition so use catch
                Assert.Catch<Exception>(() => dbConnection.Delete<ReferencedType>(r => r.Id == 1));
            }
        }

        [Test]
        public void ForeignWithOnDeleteSetDefaultCreatesOk()
        {
            using (var dbConn = OpenDbConnection())
            {
                dbConn.DropAndCreateTable<TypeWithOnDeleteSetDefault>();
            }
        }

        [Test]
        public void ForeignWithOnDeleteSetDefaultThrowsException()
        {
            using (var dbConnection = OpenDbConnection())
            {
                dbConnection.CreateTableIfNotExists<TypeWithOnDeleteSetDefault>();

                dbConnection.Save(new ReferencedType { Id = 1 });
                dbConnection.Save(new TypeWithOnDeleteSetDefault { RefId = 1 });

                Assert.AreEqual(1, dbConnection.Select<ReferencedType>().Count);
                Assert.AreEqual(1, dbConnection.Select<TypeWithOnDeleteSetDefault>().Count);

                // Do not want to require reference to dll with exception definition so use catch
                Assert.Catch<Exception>(() => dbConnection.Delete<ReferencedType>(r => r.Id == 1));
            }
        }

        [Test]
        public void ForeignWithOnDeleteSetNullCreatesOk()
        {
            using (var dbConn = OpenDbConnection())
            {
                dbConn.DropAndCreateTable<TypeWithOnDeleteSetNull>();
            }
        }

        [Test]
        public void ForeignWithOnDeleteSetNullWorks()
        {
            using (var dbConnection = OpenDbConnection())
            {
                DropTables(dbConnection);

                dbConnection.CreateTableIfNotExists<TypeWithOnDeleteSetNull>();

                dbConnection.Save(new ReferencedType { Id = 1 });
                dbConnection.Save(new TypeWithOnDeleteSetNull { RefId = 1 });

                Assert.AreEqual(1, dbConnection.Select<ReferencedType>().Count);
                Assert.AreEqual(1, dbConnection.Select<TypeWithOnDeleteSetNull>().Count);

                dbConnection.Delete<ReferencedType>(r => r.Id == 1);

                Assert.AreEqual(0, dbConnection.Select<ReferencedType>().Count);
                var row = dbConnection.Select<TypeWithOnDeleteSetNull>().First();
                Assert.That(row.RefId, Is.Null);
            }
        }

        [Test, NUnit.Framework.Ignore("Base implementation does not allow provider override so cannot work in Oracle")]
        public void CanDropForeignKey()
        {
            using (var dbConnection = OpenDbConnection())
            {
                dbConnection.DropAndCreateTable<TypeWithOnDeleteNoAction>();
                dbConnection.DropForeignKey<TypeWithOnDeleteNoAction>("FK_DNA");
            }
        }

        [Test]
        public void CanAddForeignKey()
        {
            using var dbConnection = OpenDbConnection();
            dbConnection.DropAndCreateTable<TypeWithNoForeignKeyInitially>();
            dbConnection.AddForeignKey<TypeWithNoForeignKeyInitially, ReferencedType>(
                field: t => t.RefId, 
                foreignField: tr => tr.Id,
                onUpdate: OnFkOption.NoAction, 
                onDelete: OnFkOption.Cascade, 
                "FK_ADDED");
        }
    }

    public class ReferencedType
    {
        public int Id { get; set; }
    }

    [Alias("TWSKF")]
    public class TypeWithSimpleForeignKey
    {
        [AutoIncrement]
        public int Id { get; set; }
        [References(typeof(ReferencedType))]
        public int RefId { get; set; }
    }

    [Alias("TWODC")]
    public class TypeWithOnDeleteCascade
    {
        [AutoIncrement]
        public int Id { get; set; }

        [ForeignKey(typeof(ReferencedType), OnDelete = "CASCADE", ForeignKeyName = "FK_DC")]
        public int? RefId { get; set; }
    }

    [Alias("TWODUC")]
    public class TypeWithOnDeleteAndUpdateCascade
    {
        [AutoIncrement]
        public int Id { get; set; }

        [ForeignKey(typeof(ReferencedType), OnDelete = "CASCADE", OnUpdate = "CASCADE", ForeignKeyName = "FK_DC_UC")]
        public int? RefId { get; set; }
    }

    [Alias("TWODNA")]
    public class TypeWithOnDeleteNoAction
    {
        [AutoIncrement]
        public int Id { get; set; }

        [ForeignKey(typeof(ReferencedType), OnDelete = "NO ACTION", ForeignKeyName = "FK_DNA")]
        public int? RefId { get; set; }
    }

    [Alias("TWODNR")]
    public class TypeWithOnDeleteRestrict
    {
        [AutoIncrement]
        public int Id { get; set; }

        [ForeignKey(typeof(ReferencedType), OnDelete = "RESTRICT", ForeignKeyName = "FK_DR")]
        public int? RefId { get; set; }
    }

    [Alias("TWODDF")]
    public class TypeWithOnDeleteSetDefault
    {
        [AutoIncrement]
        public int Id { get; set; }

        [Default(typeof(int), "17")]
        [ForeignKey(typeof(ReferencedType), OnDelete = "SET DEFAULT", ForeignKeyName = "FK_DDF")]
        public int RefId { get; set; }
    }

    [Alias("TWODSN")]
    public class TypeWithOnDeleteSetNull
    {
        [AutoIncrement]
        public int Id { get; set; }

        [ForeignKey(typeof(ReferencedType), OnDelete = "SET NULL", ForeignKeyName = "FK_SN")]
        public int? RefId { get; set; }
    }

    [Alias("TWONFKI")]
    public class TypeWithNoForeignKeyInitially
    {
        [AutoIncrement]
        public int Id { get; set; }

        public int? RefId { get; set; }
    }
}