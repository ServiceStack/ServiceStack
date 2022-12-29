using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests
{
    public interface IAudit
    {
        DateTime CreatedDate { get; set; }
        DateTime ModifiedDate { get; set; }
        string ModifiedBy { get; set; }
    }

    public class AuditTableA : IAudit
    {
        public AuditTableA()
        {
            this.CreatedDate = this.ModifiedDate = DateTime.UtcNow;
        }

        [AutoIncrement]
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
    }

    public class AuditTableB : IAudit
    {
        [AutoIncrement]
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
    }

    [TestFixtureOrmLite]
    public class OrmLiteFiltersTests : OrmLiteProvidersTestBase
    {
        public OrmLiteFiltersTests(DialectContext context) : base(context) {}

        [Test]
        public void Does_call_Filters_on_insert_and_update()
        {
            var insertDate = new DateTime(2014, 1, 1);
            var updateDate = new DateTime(2015, 1, 1);

            OrmLiteConfig.InsertFilter = (dbCmd, row) =>
            {
                if (row is IAudit auditRow)
                {
                    auditRow.CreatedDate = auditRow.ModifiedDate = insertDate;
                }
            };

            OrmLiteConfig.UpdateFilter = (dbCmd, row) =>
            {
                if (row is IAudit auditRow)
                {
                    auditRow.ModifiedDate = updateDate;
                }
            };

            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<AuditTableA>();
                db.DropAndCreateTable<AuditTableB>();

                var idA = db.Insert(new AuditTableA(), selectIdentity: true);
                var idB = db.Insert(new AuditTableB(), selectIdentity: true);

                var insertRowA = db.SingleById<AuditTableA>(idA);
                var insertRowB = db.SingleById<AuditTableB>(idB);

                Assert.That(insertRowA.CreatedDate, Is.EqualTo(insertDate));
                Assert.That(insertRowA.ModifiedDate, Is.EqualTo(insertDate));

                Assert.That(insertRowB.CreatedDate, Is.EqualTo(insertDate));
                Assert.That(insertRowB.ModifiedDate, Is.EqualTo(insertDate));

                insertRowA.ModifiedBy = "Updated";
                db.Update(insertRowA);

                insertRowA = db.SingleById<AuditTableA>(idA);
                Assert.That(insertRowA.ModifiedDate, Is.EqualTo(updateDate));
            }

            OrmLiteConfig.InsertFilter = OrmLiteConfig.UpdateFilter = null;
        }

        [Test]
        public async Task Does_fire_filters_for_SaveAll()
        {
            var sbInsert = new List<string>(); 
            var sbUpdate = new List<string>();
            OrmLiteConfig.InsertFilter = (cmd, o) => sbInsert.Add(cmd.CommandText);
            OrmLiteConfig.UpdateFilter = (cmd, o) => sbUpdate.Add(cmd.CommandText);

            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<AuditTableA>();
                await db.SaveAllAsync(new[] {
                    new AuditTableA {Id = 1, ModifiedBy = "A1"},
                    new AuditTableA {Id = 2, ModifiedBy = "B1"},
                });
                
                Assert.That(sbInsert.Count, Is.EqualTo(2));

                await db.SaveAllAsync(new[] {
                    new AuditTableA {Id = 1, ModifiedBy = "A2"},
                    new AuditTableA {Id = 2, ModifiedBy = "B2"},
                });
                
                Assert.That(sbUpdate.Count, Is.EqualTo(2));
            }
        }

        [Test]
        public void Does_call_Filters_on_Save()
        {
            var insertDate = new DateTime(2014, 1, 1);
            var updateDate = new DateTime(2015, 1, 1);

            OrmLiteConfig.InsertFilter = (dbCmd, row) =>
            {
                if (row is IAudit auditRow)
                {
                    auditRow.CreatedDate = auditRow.ModifiedDate = insertDate;
                }
            };

            OrmLiteConfig.UpdateFilter = (dbCmd, row) =>
            {
                if (row is IAudit auditRow)
                {
                    auditRow.ModifiedDate = updateDate;
                }
            };

            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<AuditTableA>();
                db.DropAndCreateTable<AuditTableB>();

                var a = new AuditTableA();
                var b = new AuditTableB();
                db.Save(a);
                db.Save(b);

                var insertRowA = db.SingleById<AuditTableA>(a.Id);
                var insertRowB = db.SingleById<AuditTableB>(b.Id);

                Assert.That(insertRowA.CreatedDate, Is.EqualTo(insertDate));
                Assert.That(insertRowA.ModifiedDate, Is.EqualTo(insertDate));

                Assert.That(insertRowB.CreatedDate, Is.EqualTo(insertDate));
                Assert.That(insertRowB.ModifiedDate, Is.EqualTo(insertDate));

                a.ModifiedBy = "Updated";
                db.Save(a);

                a = db.SingleById<AuditTableA>(a.Id);
                Assert.That(a.ModifiedDate, Is.EqualTo(updateDate));
            }

            OrmLiteConfig.InsertFilter = OrmLiteConfig.UpdateFilter = null;
        }

        [Test]
        public void Exceptions_in_filters_prevents_insert_and_update()
        {
            OrmLiteConfig.InsertFilter = OrmLiteConfig.UpdateFilter = (dbCmd, row) =>
            {
                if (row is IAudit auditRow)
                {
                    if (auditRow.ModifiedBy == null)
                        throw new ArgumentNullException("ModifiedBy");
                }
            };

            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<AuditTableA>();
                db.DropAndCreateTable<AuditTableB>();

                try
                {
                    db.Insert(new AuditTableA());
                    Assert.Fail("Should throw");
                }
                catch (ArgumentNullException) { }
                Assert.That(db.Count<AuditTableA>(), Is.EqualTo(0));

                var a = new AuditTableA { ModifiedBy = "Me!" };
                db.Insert(a);

                a.ModifiedBy = null;
                try
                {
                    db.Update(a);
                    Assert.Fail("Should throw");
                }
                catch (ArgumentNullException) { }

                a.ModifiedBy = "Me2!";
                db.Update(a);
            }

            OrmLiteConfig.InsertFilter = OrmLiteConfig.UpdateFilter = null;
        }

        [Test]
        public void Does_call_UpdateFilter_on_anonymous_Type()
        {
            var called = false;
            OrmLiteConfig.UpdateFilter = (dbCmd, row) => { called = true; };

            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<AuditTableA>();
                
                var a = new AuditTableA();
                var id = db.Insert(a, selectIdentity:true);

                db.Update<AuditTableA>(new { ModifiedBy = "Updated" }, where: x => x.Id == id);
                
                Assert.That(db.SingleById<AuditTableA>(id).ModifiedBy, Is.EqualTo("Updated"));
                
                Assert.That(called);
            }

            OrmLiteConfig.UpdateFilter = null;
        }

    }
}