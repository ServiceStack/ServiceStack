using System;
using System.Globalization;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Tests;

namespace ServiceStack.OrmLite.PostgreSQL.Tests
{
    [TestFixtureOrmLiteDialects(Dialect.AnyPostgreSql)]
    public class OrmLiteInsertTests : OrmLiteProvidersTestBase
    {
        public OrmLiteInsertTests(DialectContext context) : base(context) {}

        [Test]
        public void Can_insert_into_ModelWithFieldsOfDifferentTypes_table()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithFieldsOfDifferentTypes>(true);

                var row = ModelWithFieldsOfDifferentTypes.Create(1);

                db.Insert(row);
            }
        }

        [Test]
        public void Can_insert_and_select_from_ModelWithFieldsOfDifferentTypes_table()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithFieldsOfDifferentTypes>(true);

                var row = ModelWithFieldsOfDifferentTypes.Create(1);

                db.Insert(row);

                var rows = db.Select<ModelWithFieldsOfDifferentTypes>();

                Assert.That(rows, Has.Count.EqualTo(1));

                ModelWithFieldsOfDifferentTypes.AssertIsEqual(rows[0], row);
            }
        }

        [Test]
        public void Can_insert_and_select_from_ModelWithFieldsOfNullableTypes_table()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithFieldsOfNullableTypes>(true);

                var row = ModelWithFieldsOfNullableTypes.Create(1);

                db.Insert(row);

                var rows = db.Select<ModelWithFieldsOfNullableTypes>();

                Assert.That(rows, Has.Count.EqualTo(1));

                ModelWithFieldsOfNullableTypes.AssertIsEqual(rows[0], row);
            }
        }

        [Test]
        public void Can_insert_and_select_from_ModelWithFieldsOfDifferentAndNullableTypes_table()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithFieldsOfDifferentAndNullableTypes>(true);

                var row = ModelWithFieldsOfDifferentAndNullableTypes.Create(1);

                db.Insert(row);

                var rows = db.Select<ModelWithFieldsOfDifferentAndNullableTypes>();

                Assert.That(rows, Has.Count.EqualTo(1));

                ModelWithFieldsOfDifferentAndNullableTypes.AssertIsEqual(rows[0], row);
            }
        }

        [Test]
        public void Can_insert_table_with_null_fields()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithIdAndName>(true);

                var row = ModelWithIdAndName.Create(1);
                row.Name = null;

                db.Insert(row);

                var rows = db.Select<ModelWithIdAndName>();

                Assert.That(rows, Has.Count.EqualTo(1));

                ModelWithIdAndName.AssertIsEqual(rows[0], row);
            }
        }

        [Test]
        public void Can_retrieve_LastInsertId_from_inserted_table()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithIdAndName1>(true);

                var row1 = new ModelWithIdAndName1() { Name = "A", Id = 4 };
                var row2 = new ModelWithIdAndName1() { Name = "B", Id = 5 };

                var row1LastInsertId = db.Insert(row1, selectIdentity: true);
                Assert.That(db.GetLastSql(), Does.Match("\\) RETURNING \"?[Ii]d"));

                var row2LastInsertId = db.Insert(row2, selectIdentity: true);
                Assert.That(db.GetLastSql(), Does.Match("\\) RETURNING \"?[Ii]d"));

                var insertedRow1 = db.SingleById<ModelWithIdAndName1>(row1LastInsertId);
                var insertedRow2 = db.SingleById<ModelWithIdAndName1>(row2LastInsertId);

                Assert.That(insertedRow1.Name, Is.EqualTo(row1.Name));
                Assert.That(insertedRow2.Name, Is.EqualTo(row2.Name));
            }
        }

        [Test]
        public void Can_retrieve_LastInsertId_from_inserted_table_with_LastVal()
        {
            PostgreSQL.PostgreSqlDialectProvider.Instance.UseReturningForLastInsertId = false;
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<ModelWithIdAndName>();

                var row1 = ModelWithIdAndName.Create(5);
                var row2 = ModelWithIdAndName.Create(6);

                var row1LastInsertId = db.Insert(row1, selectIdentity: true);
                Assert.That(db.GetLastSql(), Does.EndWith("; SELECT LASTVAL()"));

                var row2LastInsertId = db.Insert(row2, selectIdentity: true);
                Assert.That(db.GetLastSql(), Does.EndWith("; SELECT LASTVAL()"));

                var insertedRow1 = db.SingleById<ModelWithIdAndName>(row1LastInsertId);
                var insertedRow2 = db.SingleById<ModelWithIdAndName>(row2LastInsertId);

                Assert.That(insertedRow1.Name, Is.EqualTo(row1.Name));
                Assert.That(insertedRow2.Name, Is.EqualTo(row2.Name));
            }
            PostgreSQL.PostgreSqlDialectProvider.Instance.UseReturningForLastInsertId = true;
        }

        [Test]
        public void Can_insert_single_quote()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithIdAndName1>(true);

                var row1 = new ModelWithIdAndName1() { Name = @"'", Id = 55};
                
                db.Insert(row1);
                var row1LastInsertId = db.LastInsertId();

                var insertedRow1 = db.SingleById<ModelWithIdAndName1>(row1LastInsertId);
             
                Assert.That(insertedRow1.Name, Is.EqualTo(row1.Name));				 
            }
        }

        [Test]
        public void Can_insert_TaskQueue_table()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<TaskQueue>(true);

                var row = TaskQueue.Create(1);

                db.Insert(row);

                var rows = db.Select<TaskQueue>();

                Assert.That(rows, Has.Count.EqualTo(1));

                //Update the auto-increment id
                row.Id = rows[0].Id;

                TaskQueue.AssertIsEqual(rows[0], row);
            }
        }
    }

    class ModelWithIdAndName1
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
    }

    [TestFixtureOrmLiteDialects(Dialect.AnyPostgreSql), SetUICulture("en-US"), SetCulture("en-US")]
    public class PostgreSQLUpdateTests : OrmLiteProvidersTestBase
    {
        public PostgreSQLUpdateTests(DialectContext context) : base(context) {}

        [Test]
        public void Can_insert_datetimeoffsets_regardless_of_current_culture()
        {
            // datetimeoffset's default .ToString depends on culture, ensure we use one with MDY
//#if NETCORE
//            var previousCulture = CultureInfo.CurrentCulture;
//            CultureInfo.CurrentCulture = new CultureInfo("en-US");
//#else            
//            var previousCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
//            System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
//#endif
            try
            {
                using (var db = OpenDbConnection())
                {
                    // and set datestyle to DMY, crashing the insert when we're formatting it as the default on en-US
                    db.ExecuteNonQuery("SET datestyle TO \"ISO, DMY\"");
                    db.CreateTable<ModelWithDateTimeOffset>(true);

                    var date = new DateTimeOffset(2010, 11, 29, 1, 2, 3, new TimeSpan(0));
                    var row = new ModelWithDateTimeOffset
                    {
                        Id = 1,
                        Value = date
                    };

                    db.Insert(row);
                    db.Update<ModelWithDateTimeOffset>(new { Value = date.AddDays(30) }, r => r.Id == 1);

                    var rows = db.Select<ModelWithDateTimeOffset>();

                    Assert.That(rows, Has.Count.EqualTo(1));
                    Assert.That(rows[0].Value, Is.EqualTo(date.AddDays(30)));
                }
            }
            finally
            {
//#if NETCORE
//                CultureInfo.CurrentCulture = previousCulture;
//#else
//                System.Threading.Thread.CurrentThread.CurrentCulture = previousCulture;
//#endif
            }
        }

    }

    class ModelWithDateTimeOffset
    {
        [AutoIncrement]
        public int Id { get; set; }
        public DateTimeOffset Value { get; set; }
    }

}