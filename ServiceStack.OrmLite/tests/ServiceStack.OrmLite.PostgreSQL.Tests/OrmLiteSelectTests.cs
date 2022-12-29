using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.OrmLite.Tests;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.PostgreSQL.Tests
{
    [TestFixtureOrmLiteDialects(Dialect.AnyPostgreSql)]
    [NonParallelizable]
    public class OrmLiteSelectTests : OrmLiteProvidersTestBase
    {
        public OrmLiteSelectTests(DialectContext context) : base(context) {}

        [Test]
        public void Can_GetById_int_from_ModelWithFieldsOfDifferentTypes_table()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithFieldsOfDifferentTypes>(true);

                var rowIds = new List<int>(new[] { 1, 2, 3 });

                rowIds.ForEach(x => db.Insert(ModelWithFieldsOfDifferentTypes.Create(x)));

                var row = db.SingleById<ModelWithFieldsOfDifferentTypes>(1);

                Assert.That(row.Id, Is.EqualTo(1));
            }
        }

        [Test]
        public void Can_GetById_string_from_ModelWithOnlyStringFields_table()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithOnlyStringFields>(true);

                var rowIds = new List<string>(new[] { "id-1", "id-2", "id-3" });

                rowIds.ForEach(x => db.Insert(ModelWithOnlyStringFields.Create(x)));

                var row = db.SingleById<ModelWithOnlyStringFields>("id-1");

                Assert.That(row.Id, Is.EqualTo("id-1"));
            }
        }

        [Test]
        public void Can_GetByIds_int_from_ModelWithFieldsOfDifferentTypes_table()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithFieldsOfDifferentTypes>(true);

                var rowIds = new List<int>(new[] { 1, 2, 3 });

                rowIds.ForEach(x => db.Insert(ModelWithFieldsOfDifferentTypes.Create(x)));

                var rows = db.SelectByIds<ModelWithFieldsOfDifferentTypes>(rowIds);
                var dbRowIds = rows.ConvertAll(x => x.Id);

                Assert.That(dbRowIds, Is.EquivalentTo(rowIds));
            }
        }

        [Test]
        public void Can_GetByIds_string_from_ModelWithOnlyStringFields_table()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithOnlyStringFields>(true);

                var rowIds = new List<string>(new[] { "id-1", "id-2", "id-3" });

                rowIds.ForEach(x => db.Insert(ModelWithOnlyStringFields.Create(x)));

                var rows = db.SelectByIds<ModelWithOnlyStringFields>(rowIds);
                var dbRowIds = rows.ConvertAll(x => x.Id);

                Assert.That(dbRowIds, Is.EquivalentTo(rowIds));
            }
        }

        [Test]
        public void Can_select_with_filter_from_ModelWithOnlyStringFields_table()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithOnlyStringFields>(true);

                var rowIds = new List<string>(new[] { "id-1", "id-2", "id-3" });

                rowIds.ForEach(x => db.Insert(ModelWithOnlyStringFields.Create(x)));

                var filterRow = ModelWithOnlyStringFields.Create("id-4");
                filterRow.AlbumName = "FilteredName";

                db.Insert(filterRow);

                var rows = db.Select<ModelWithOnlyStringFields>("\"album_name\" = @AlbumName", new { filterRow.AlbumName });
                var dbRowIds = rows.ConvertAll(x => x.Id);

                Assert.That(dbRowIds, Has.Count.EqualTo(1));
                Assert.That(dbRowIds[0], Is.EqualTo(filterRow.Id));
            }
        }

        [Test]
        public void Can_select_scalar_value()
        {
            const int n = 5;

            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithIdAndName>(true);

                n.Times(x => db.Insert(ModelWithIdAndName.Create(x)));

                var count = db.Scalar<long>("SELECT COUNT(*) FROM " + "ModelWithIdAndName".SqlTable(DialectProvider));

                Assert.That(count, Is.EqualTo(n));
            }
        }

        [Test]
        public void Can_loop_each_string_from_ModelWithOnlyStringFields_table()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithOnlyStringFields>(true);

                var rowIds = new List<string>(new[] { "id-1", "id-2", "id-3" });

                rowIds.ForEach(x => db.Insert(ModelWithOnlyStringFields.Create(x)));

                var dbRowIds = new List<string>();
                foreach (var row in db.SelectLazy<ModelWithOnlyStringFields>())
                {
                    dbRowIds.Add(row.Id);
                }

                Assert.That(dbRowIds, Is.EquivalentTo(rowIds));
            }
        }

        [Test]
        public void Can_loop_each_with_filter_from_ModelWithOnlyStringFields_table()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithOnlyStringFields>(true);

                var rowIds = new List<string>(new[] { "id-1", "id-2", "id-3" });

                rowIds.ForEach(x => db.Insert(ModelWithOnlyStringFields.Create(x)));

                var filterRow = ModelWithOnlyStringFields.Create("id-4");
                filterRow.AlbumName = "FilteredName";

                db.Insert(filterRow);
                db.GetLastSql().Print();

                var dbRowIds = new List<string>();
                var rows = db.SelectLazy<ModelWithOnlyStringFields>("\"album_name\" = @AlbumName", new { filterRow.AlbumName });
                foreach (var row in rows)
                {
                    dbRowIds.Add(row.Id);
                }

                Assert.That(dbRowIds, Has.Count.EqualTo(1));
                Assert.That(dbRowIds[0], Is.EqualTo(filterRow.Id));
            }
        }

        [Test]
        public void Can_GetFirstColumn()
        {
            const int n = 5;

            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithIdAndName>(true);

                n.Times(x => db.Insert(ModelWithIdAndName.Create(x)));

                var ids = db.Column<int>("SELECT \"id\" FROM " + "ModelWithIdAndName".SqlTable(DialectProvider));

                Assert.That(ids.Count, Is.EqualTo(n));
            }
        }

        [Test]
        public void Can_GetFirstColumnDistinct()
        {
            const int n = 5;

            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithIdAndName>(true);

                n.Times(x => db.Insert(ModelWithIdAndName.Create(x)));

                var ids = db.ColumnDistinct<int>("SELECT \"id\" FROM " + "ModelWithIdAndName".SqlTable(DialectProvider));

                Assert.That(ids.Count, Is.EqualTo(n));
            }
        }

        [Test]
        public void Can_GetLookup()
        {
            const int n = 5;

            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithIdAndName>(true);

                n.Times(x => {
                    var row = ModelWithIdAndName.Create(x);
                    row.Name = x % 2 == 0 ? "OddGroup" : "EvenGroup";
                    db.Insert(row);
                });

                var lookup = db.Lookup<string, int>("SELECT \"name\", \"id\" FROM " + "ModelWithIdAndName".SqlTable(DialectProvider));

                Assert.That(lookup, Has.Count.EqualTo(2));
                Assert.That(lookup["OddGroup"], Has.Count.EqualTo(3));
                Assert.That(lookup["EvenGroup"], Has.Count.EqualTo(2));
            }
        }

        [Test]
        public void Can_GetDictionary()
        {
            const int n = 5;

            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithIdAndName>(true);

                n.Times(x => db.Insert(ModelWithIdAndName.Create(x)));

                var dictionary = db.Dictionary<int, string>("SELECT \"id\", \"name\" FROM " + "ModelWithIdAndName".SqlTable(DialectProvider));

                Assert.That(dictionary, Has.Count.EqualTo(5));

                //Console.Write(dictionary.Dump());
            }
        }

        [Test]
        public void Can_Select_subset_ModelWithIdAndName_from_ModelWithFieldsOfDifferentTypes_table()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithFieldsOfDifferentTypes>(true);

                var rowIds = new List<int>(new[] { 1, 2, 3 });

                rowIds.ForEach(x => db.Insert(ModelWithFieldsOfDifferentTypes.Create(x)));

                var rows = db.Select<ModelWithIdAndName>("SELECT \"id\", \"name\" FROM "
                    + "ModelWithFieldsOfDifferentTypes".SqlTable(DialectProvider));
                var dbRowIds = rows.ConvertAll(x => x.Id);

                Assert.That(dbRowIds, Is.EquivalentTo(rowIds));
            }
        }

        [Test]
        public void Can_Query_ModelWithFieldsOfDifferentTypes_with_dictionary_parameters()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithFieldsOfDifferentTypes>(true);

                var rowIds = new List<int>(new[] { 1, 2, 3 });

                rowIds.ForEach(x => db.Insert(ModelWithFieldsOfDifferentTypes.Create(x)));

                var rows = db.Select<ModelWithFieldsOfDifferentTypes>("SELECT * FROM {0} where \"id\" = :Id "
                    .Fmt("ModelWithFieldsOfDifferentTypes".SqlTable(DialectProvider)),
                    new Dictionary<string, object> { {"Id", 3} });
                 
                Assert.AreEqual(rows.Count, 1);
                Assert.AreEqual(rows[0].Id, 3);
            }
        }

        [Test]
        public void Can_Select_Into_ModelWithIdAndName_from_ModelWithFieldsOfDifferentTypes_table()
        {
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithFieldsOfDifferentTypes>(true);

                var rowIds = new List<int>(new[] { 1, 2, 3 });

                rowIds.ForEach(x => db.Insert(ModelWithFieldsOfDifferentTypes.Create(x)));

                var rows = db.Select<ModelWithIdAndName>(typeof(ModelWithFieldsOfDifferentTypes));
                var dbRowIds = rows.ConvertAll(x => x.Id);

                Assert.That(dbRowIds, Is.EquivalentTo(rowIds));
            }
        }

        [Test]
        public void Can_Select_In_for_string_value()
        {
            const int n = 5;

            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithIdAndName>(true);

                n.Times(x => db.Insert(ModelWithIdAndName.Create(x)));

                var selectInNames = new[] { "Name1", "Name2" };
                var rows = db.Select<ModelWithIdAndName>("Name IN ({0})".Fmt(selectInNames.SqlInParams(DialectProvider)),
                    new { values = selectInNames.SqlInValues(DialectProvider) });
                Assert.That(rows.Count, Is.EqualTo(selectInNames.Length));

                rows = db.Select<ModelWithIdAndName>("Name IN (@p1, @p2)", new { p1 = "Name1", p2 = "Name2" });
                Assert.That(rows.Count, Is.EqualTo(selectInNames.Length));
            }
        }

        [Test]
        public void Can_Select_With_Subquery()
        {
            const int n = 5;

            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithFieldsOfDifferentTypes>();

                n.Times(x => db.Insert(ModelWithFieldsOfDifferentTypes.Create(n)));

                var sql = @"
                    WITH max_id AS (
                        SELECT 3 AS three)
                    SELECT *
                    FROM {0}
                    WHERE id <= (SELECT three FROM max_id)".Fmt("ModelWithFieldsOfDifferentTypes".SqlTable(DialectProvider));

                var rows = db.SqlList<ModelWithFieldsOfDifferentTypes>(sql);

                Assert.That(rows.Count, Is.EqualTo((3)));
            }
        }
    }
}