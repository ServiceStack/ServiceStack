using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.Logging;

namespace ServiceStack.OrmLite.Tests
{
    public class ModelWithRowVersion
    {
        [AutoIncrement]
        public long Id { get; set; }

        public string Text { get; set; }

        public ulong RowVersion { get; set; }
    }

    public class ModelWithRowVersionBase : ModelWithRowVersion
    {
        public string MoreData { get; set; }
    }

    public class ModelWithRowVersionAlias
    {
        [AutoIncrement]
        public long Id { get; set; }

        public string Text { get; set; }

        [Alias("VersionAlias")]
        public ulong RowVersion { get; set; }
    }


    [Alias("TheModelWithAliasedRowVersion")]
    public class ModelWithAliasedRowVersion
    {
        [AutoIncrement]
        public long Id { get; set; }

        public string Text { get; set; }

        [RowVersion]
        [Alias("TheVersion")]
        public long Version { get; set; }
    }

    [Alias("ModelAlias")]
    public class ModelWithAlias
    {
        [Alias("ModelId")]
        [AutoIncrement]
        public int Id { get; set; }
        
        [Alias("IntAlias")]
        public int IntField { get; set; }
    }

    [Schema("Schema")]
    public class ModelWithSchemaAndRowVersionForInnerJoin
    {
        [AutoIncrement]
        public long Id { get; set; }

        public string Text { get; set; }

        public long ModelWithRowVersionId { get; set; }

        public ulong RowVersion { get; set; }
    }

    public class ModelWithOptimisticChildren
    {
        [AutoIncrement]
        public int Id { get; set; }

        public string Text { get; set; }

        [Reference]
        public List<ModelWithRowVersionAndParent> Children { get; set; }
    }

    public class ModelWithRowVersionAndParent
    {
        [AutoIncrement]
        public int Id { get; set; }

        public int ModelWithOptimisticChildrenId { get; set; }

        public string Text { get; set; }

        [RowVersion]
        public long Version { get; set; }
    }

    [TestFixtureOrmLite]
    public class RowVersionTests : OrmLiteProvidersTestBase
    {
        public RowVersionTests(DialectContext context) : base(context) {}

        private IDbConnection db;

        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            LogManager.LogFactory = new ConsoleLogFactory(debugEnabled: true);
            using (var dbConn = OpenDbConnection())
            {
                dbConn.DropAndCreateTable<ModelWithRowVersion>();
                dbConn.DropAndCreateTable<ModelWithRowVersionBase>();
            }
        }

        [SetUp]
        public void SetUp()
        {
            db = OpenDbConnection();
        }

        [TearDown]
        public void TearDown()
        {
            db.Dispose();
        }

        [Test]
        public void Can_create_table_with_RowVersion()
        {
            db.DropAndCreateTable<ModelWithRowVersion>();

            var rowId = db.Insert(new ModelWithRowVersion { Text = "Text" }, selectIdentity:true);

            var row = db.SingleById<ModelWithRowVersion>(rowId);

            row.Text += " Updated";

            db.Update(row);

            var updatedRow = db.SingleById<ModelWithRowVersion>(rowId);

            Assert.That(updatedRow.Text, Is.EqualTo("Text Updated"));
            Assert.That(updatedRow.RowVersion, Is.GreaterThan(0));

            row.Text += " Again";

            //Can't update old record
            Assert.Throws<OptimisticConcurrencyException>(() =>
                db.Update(row));

            //Can update latest version
            updatedRow.Text += " Again";
            db.Update(updatedRow);
        }

        [Test]
        public void Can_create_table_with_RowVersion_Alias()
        {
            db.DropAndCreateTable<ModelWithRowVersionAlias>();

            var rowId = db.Insert(new ModelWithRowVersionAlias { Text = "Text" }, selectIdentity:true);

            var row = db.SingleById<ModelWithRowVersionAlias>(rowId);

            row.Text += " Updated";

            db.Update(row);

            var updatedRow = db.SingleById<ModelWithRowVersionAlias>(rowId);

            Assert.That(updatedRow.Text, Is.EqualTo("Text Updated"));
            Assert.That(updatedRow.RowVersion, Is.GreaterThan(0));

            row.Text += " Again";

            //Can't update old record
            Assert.Throws<OptimisticConcurrencyException>(() =>
                db.Update(row));

            //Can update latest version
            updatedRow.Text += " Again";
            db.Update(updatedRow);
        }

        [Test]
        public void SingleById_retrieves_rowversion()
        {
            var rowId = db.Insert(new ModelWithRowVersion { Text = "One" }, selectIdentity: true);
            TouchRow(rowId);

            var row = db.SingleById<ModelWithRowVersion>(rowId);

            Assert.That(row.RowVersion, Is.Not.EqualTo(0));
        }

        [Test]
        public void Select_retrieves_rowversion()
        {
            var rowId = db.Insert(new ModelWithRowVersion { Text = "OnePointOne" }, selectIdentity: true);
            TouchRow(rowId);

            var rows = db.Select<ModelWithRowVersion>(x => x.Id == rowId);

            Assert.That(rows.Single().RowVersion, Is.Not.EqualTo(0));
        }

        [Test]
        public void SingleById_with_aliases_retrieves_rowversion()
        {
            db.DropAndCreateTable<ModelWithAliasedRowVersion>();

            var row = new ModelWithAliasedRowVersion { Text = "TheOne" };
            db.Save(row);

            var actualRow = db.SingleById<ModelWithAliasedRowVersion>(row.Id);

            Assert.That(actualRow.Version, Is.EqualTo(row.Version));
        }

        [Test]
        public void Can_Save_new_row_and_retrieve_rowversion()
        {
            var row = new ModelWithRowVersion { Text = "First" };

            bool wasInserted = db.Save(row);

            Assert.That(wasInserted, Is.True);
            var actualRow = db.SingleById<ModelWithRowVersion>(row.Id);
            Assert.That(row.RowVersion, Is.EqualTo(actualRow.RowVersion));
        }

        [Test]
        public async Task Can_Save_new_row_and_retrieve_rowversion_Async()
        {
            var row = new ModelWithRowVersion { Text = "First" };

            bool wasInserted = await db.SaveAsync(row);

            Assert.That(wasInserted, Is.True);
            var actualRow = await db.SingleByIdAsync<ModelWithRowVersion>(row.Id);
            Assert.That(row.RowVersion, Is.EqualTo(actualRow.RowVersion));
        }
        
        public class ModelWithAutoGuidAndRowVersion
        {
            [AutoId]
            public Guid Id { get; set; }
            public string Name { get; set; }
            public ulong RowVersion { get; set; }
        }

        [Test]
        public void Can_Save_ModelWithAutoGuidAndRowVersion()
        {
            db.DropAndCreateTable<ModelWithAutoGuidAndRowVersion>();
            var row = new ModelWithAutoGuidAndRowVersion { Name = "A" };
            
            Assert.That(db.Save(row));

            var dbRow = db.SingleById<ModelWithAutoGuidAndRowVersion>(row.Id);
            Assert.That(dbRow.Name, Is.EqualTo(row.Name));

            dbRow.Name = "B";
            db.Save(dbRow);

            dbRow = db.SingleById<ModelWithAutoGuidAndRowVersion>(row.Id);
            Assert.That(dbRow.Name, Is.EqualTo("B"));
        }

        [Test]
        public async Task Can_Save_ModelWithAutoGuidAndRowVersion_Async()
        {
            db.DropAndCreateTable<ModelWithAutoGuidAndRowVersion>();
            var row = new ModelWithAutoGuidAndRowVersion { Name = "A" };
            
            Assert.That(await db.SaveAsync(row));

            var dbRow = await db.SingleByIdAsync<ModelWithAutoGuidAndRowVersion>(row.Id);
            Assert.That(dbRow.Name, Is.EqualTo(row.Name));

            dbRow.Name = "B";
            await db.SaveAsync(dbRow);

            dbRow = await db.SingleByIdAsync<ModelWithAutoGuidAndRowVersion>(row.Id);
            Assert.That(dbRow.Name, Is.EqualTo("B"));
        }

        [Test]
        public void Can_SaveAll_new_rows_and_retrieve_rowversion()
        {
            var rows = new[]
            {
                new ModelWithRowVersion {Text = "Eleventh"},
                new ModelWithRowVersion {Text = "Twelfth"}
            };

            var insertedCount = db.SaveAll(rows);

            Assert.That(insertedCount, Is.EqualTo(2));
            var actualRows = db.SelectByIds<ModelWithRowVersion>(rows.Select(x => x.Id));
            Assert.That(rows[0].RowVersion, Is.EqualTo(actualRows[0].RowVersion));
            Assert.That(rows[1].RowVersion, Is.EqualTo(actualRows[1].RowVersion));
        }

        [Test]
        public void Can_Save_new_row_with_references_and_retrieve_child_rowversions()
        {
            db.DropTable<ModelWithRowVersionAndParent>();
            db.DropAndCreateTable<ModelWithOptimisticChildren>();
            db.CreateTable<ModelWithRowVersionAndParent>();

            var row = new ModelWithOptimisticChildren
            {
                Text = "Twentyfirst",
                Children = new List<ModelWithRowVersionAndParent> {
                    new ModelWithRowVersionAndParent { Text = "Twentysecond" }
                }
            };

            db.Save(row, references: true);

            var actualChildRow = db.SingleById<ModelWithRowVersionAndParent>(row.Children[0].Id);
            Assert.That(row.Children[0].Version, Is.EqualTo(actualChildRow.Version));
        }

        [Test]
        public void Can_update_with_current_rowversion()
        {
            var rowId = db.Insert(new ModelWithRowVersion { Text = "Two" }, selectIdentity: true);
            var row = db.SingleById<ModelWithRowVersion>(rowId);

            row.Text = "Three";
            db.Update(row);

            var actual = db.SingleById<ModelWithRowVersion>(rowId);
            Assert.That(actual.Text, Is.EqualTo("Three"));
            Assert.That(actual.RowVersion, Is.Not.EqualTo(row.RowVersion));
        }

        [Test]
        public void Can_update_with_current_rowversion_base()
        {
            var rowId = db.Insert(new ModelWithRowVersionBase { Text = "Two", MoreData = "Fred" }, selectIdentity: true);
            var row = db.SingleById<ModelWithRowVersionBase>(rowId);

            row.Text = "Three";
            db.Update(row);

            var actual = db.SingleById<ModelWithRowVersionBase>(rowId);
            Assert.That(actual.Text, Is.EqualTo("Three"));
            Assert.That(actual.RowVersion, Is.Not.EqualTo(row.RowVersion));
        }

        [Test]
        public void Can_update_with_current_rowversion_base_ObjectDictionary()
        {
            var rowId = db.Insert(new ModelWithRowVersionBase { Text = "Two", MoreData = "Fred" }, selectIdentity: true);
            var row = db.SingleById<ModelWithRowVersionBase>(rowId);

            row.Text = "Three";
            db.Update<ModelWithRowVersionBase>(row.ToObjectDictionary());

            var actual = db.SingleById<ModelWithRowVersionBase>(rowId);
            Assert.That(actual.Text, Is.EqualTo("Three"));
            Assert.That(actual.RowVersion, Is.Not.EqualTo(row.RowVersion));

            row.Text = "Four";
            Assert.Throws<OptimisticConcurrencyException>(() =>
                db.Update<ModelWithRowVersionBase>(row.ToObjectDictionary()));
        }

        [Test]
        public async Task Can_update_with_current_rowversion_base_ObjectDictionary_Async()
        {
            var rowId = await db.InsertAsync(new ModelWithRowVersionBase { Text = "Two", MoreData = "Fred" }, selectIdentity: true);
            var row = await db.SingleByIdAsync<ModelWithRowVersionBase>(rowId);

            row.Text = "Three";
            await db.UpdateAsync<ModelWithRowVersionBase>(row.ToObjectDictionary());

            var actual = await db.SingleByIdAsync<ModelWithRowVersionBase>(rowId);
            Assert.That(actual.Text, Is.EqualTo("Three"));
            Assert.That(actual.RowVersion, Is.Not.EqualTo(row.RowVersion));

            row.Text = "Four";
            Assert.ThrowsAsync<OptimisticConcurrencyException>(async () =>
                await db.UpdateAsync<ModelWithRowVersionBase>(row.ToObjectDictionary()));
        }

        [Test]
        public void Can_update_with_current_rowversion_base_UpdateOnly_ObjectDictionary()
        {
            var rowId = db.Insert(new ModelWithRowVersionBase { Text = "Two", MoreData = "Fred" }, selectIdentity: true);
            var row = db.SingleById<ModelWithRowVersionBase>(rowId);

            row.Text = "Three";
            db.UpdateOnly<ModelWithRowVersionBase>(row.ToObjectDictionary());

            var actual = db.SingleById<ModelWithRowVersionBase>(rowId);
            Assert.That(actual.Text, Is.EqualTo("Three"));
            Assert.That(actual.RowVersion, Is.Not.EqualTo(row.RowVersion));

            row.Text = "Four";
            Assert.Throws<OptimisticConcurrencyException>(() =>
                db.UpdateOnly<ModelWithRowVersionBase>(row.ToObjectDictionary()));
        }

        [Test]
        public async Task Can_update_with_current_rowversion_base_UpdateOnly_ObjectDictionary_Async()
        {
            var rowId = await db.InsertAsync(new ModelWithRowVersionBase { Text = "Two", MoreData = "Fred" }, selectIdentity: true);
            var row = await db.SingleByIdAsync<ModelWithRowVersionBase>(rowId);

            row.Text = "Three";
            await db.UpdateOnlyAsync<ModelWithRowVersionBase>(row.ToObjectDictionary());

            var actual = await db.SingleByIdAsync<ModelWithRowVersionBase>(rowId);
            Assert.That(actual.Text, Is.EqualTo("Three"));
            Assert.That(actual.RowVersion, Is.Not.EqualTo(row.RowVersion));

            row.Text = "Four";
            Assert.ThrowsAsync<OptimisticConcurrencyException>(async () =>
                await db.UpdateOnlyAsync<ModelWithRowVersionBase>(row.ToObjectDictionary()));
        }

        [Test]
        public void Can_update_multiple_with_current_rowversions()
        {
            var rowIds = new[]
            {
                db.Insert(new ModelWithRowVersion { Text = "Eleven" }, selectIdentity: true),
                db.Insert(new ModelWithRowVersion { Text = "Twelve" }, selectIdentity: true)
            };
            var rows = rowIds
                .Select(id => db.SingleById<ModelWithRowVersion>(id))
                .ToArray();

            rows[0].Text = "Thirteen";
            rows[1].Text = "Fourteen";
            db.UpdateAll(rows);

            var actualRows = rowIds
                .Select(id => db.SingleById<ModelWithRowVersion>(id))
                .ToArray();
            Assert.That(actualRows[0].Text, Is.EqualTo("Thirteen"));
            Assert.That(actualRows[1].Text, Is.EqualTo("Fourteen"));
        }

        [Test]
        public void Can_Save_changed_row_with_current_rowversion_and_retrieve_rowversion()
        {
            var rowId = db.Insert(new ModelWithRowVersion { Text = "Second" }, selectIdentity: true);
            var row = db.SingleById<ModelWithRowVersion>(rowId);

            row.Text = "Third";
            bool wasInserted = db.Save(row);

            Assert.That(wasInserted, Is.False);
            var actualRow = db.SingleById<ModelWithRowVersion>(rowId);
            Assert.That(row.RowVersion, Is.EqualTo(actualRow.RowVersion));
        }

        [Test]
        public void Can_UpdateAll_with_current_rowversions()
        {
            var rowIds = new[]
            {
                db.Insert(new ModelWithRowVersion { Text = "Eleven" }, selectIdentity: true),
                db.Insert(new ModelWithRowVersion { Text = "Twelve" }, selectIdentity: true)
            };
            var rows = db.SelectByIds<ModelWithRowVersion>(rowIds);

            rows[0].Text = "Thirteen";
            rows[1].Text = "Fourteen";
            db.UpdateAll(rows);

            var actualRows = db.SelectByIds<ModelWithRowVersion>(rowIds);
            Assert.That(actualRows[0].Text, Is.EqualTo("Thirteen"));
            Assert.That(actualRows[1].Text, Is.EqualTo("Fourteen"));
        }

        [Test]
        public void Can_SaveAll_changed_rows_with_current_rowversion_and_retrieve_rowversion()
        {
            var rowIds = new[]
            {
                db.Insert(new ModelWithRowVersion { Text = "Thirteenth" }, selectIdentity: true),
                db.Insert(new ModelWithRowVersion { Text = "Fourteenth" }, selectIdentity: true)
            };
            var rows = db.SelectByIds<ModelWithRowVersion>(rowIds);

            rows[0].Text = "Fifteenth";
            rows[1].Text = "Sixteenth";
            var insertedCount = db.SaveAll(rows);

            Assert.That(insertedCount, Is.EqualTo(0));
            var actualRows = db.SelectByIds<ModelWithRowVersion>(rows.Select(x => x.Id));
            Assert.That(actualRows[0].Text, Is.EqualTo("Fifteenth"));
            Assert.That(rows[0].RowVersion, Is.EqualTo(actualRows[0].RowVersion));
            Assert.That(rows[1].RowVersion, Is.EqualTo(actualRows[1].RowVersion));
        }

        [Test]
        public void Can_delete_with_current_rowversion()
        {
            var rowId = db.Insert(new ModelWithRowVersion { Text = "Four" }, selectIdentity: true);
            var row = db.SingleById<ModelWithRowVersion>(rowId);

            db.Delete(row);

            var count = db.Count<ModelWithRowVersion>(m => m.Id == rowId);
            Assert.That(count, Is.EqualTo(0));
        }
        
        [Test]
        public void Can_DeleteById_with_current_rowversion()
        {
            var rowId = db.Insert(new ModelWithRowVersion { Text = "Four" }, selectIdentity: true);
            var row = db.SingleById<ModelWithRowVersion>(rowId);

            db.DeleteById<ModelWithRowVersion>(row.Id, rowVersion:row.RowVersion);

            var count = db.Count<ModelWithRowVersion>(m => m.Id == rowId);
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void Update_with_outdated_rowversion_throws()
        {
            var rowId = db.Insert(new ModelWithRowVersion { Text = "Five" }, selectIdentity: true);
            var row = db.SingleById<ModelWithRowVersion>(rowId);
            TouchRow(rowId);

            row.Text = "Six";
            Assert.Throws<OptimisticConcurrencyException>(() => db.Update(row));

            var actual = db.SingleById<ModelWithRowVersion>(rowId);
            Assert.That(actual.Text, Is.Not.EqualTo("Six"));
        }

        [Test]
        public void Update_with_outdated_rowversionbase_throws()
        {
            var rowId = db.Insert(new ModelWithRowVersionBase { Text = "Five", MoreData = "George" }, selectIdentity: true);
            var row = db.SingleById<ModelWithRowVersionBase>(rowId);
            TouchRowBase(rowId);

            row.Text = "Six";
            Assert.Throws<OptimisticConcurrencyException>(() => db.Update(row));

            var actual = db.SingleById<ModelWithRowVersionBase>(rowId);
            Assert.That(actual.Text, Is.Not.EqualTo("Six"));
        }

        [Test]
        public void Update_with_outdated_rowversion_base_and_explicit_id_check_bypasses_rowversion_check()
        {
            var rowId = db.Insert(new ModelWithRowVersionBase { Text = "Two", MoreData = "Fred" }, selectIdentity: true);
            var row = db.SingleById<ModelWithRowVersionBase>(rowId);
            TouchRowBase(rowId);

            row.Text = "Six";
            db.Update(row, x => x.Id == rowId);

            var actual = db.SingleById<ModelWithRowVersionBase>(rowId);
            Assert.That(actual.Text, Is.EqualTo("Six"));
            Assert.That(actual.RowVersion, Is.Not.EqualTo(row.RowVersion));
        }

        [Test]
        public void Update_with_outdated_rowversion_base_and_explicit_rowversion_check_bypasses_update_with_no_throw()
        {
            var rowId = db.Insert(new ModelWithRowVersionBase { Text = "Two", MoreData = "Fred" }, selectIdentity: true);
            var row = db.SingleById<ModelWithRowVersionBase>(rowId);
            TouchRowBase(rowId);

            row.Text = "Six";
            db.Update(row, x => x.Id == rowId && x.RowVersion == row.RowVersion);

            var actual = db.SingleById<ModelWithRowVersionBase>(rowId);
            Assert.That(actual.Text, Is.EqualTo("Touched"));
            Assert.That(actual.RowVersion, Is.Not.EqualTo(row.RowVersion));
        }

        [Test]
        public void Update_multiple_with_single_outdated_rowversion_throws_and_all_changes_are_rejected()
        {
            var rowIds = new[]
            {
                db.Insert(new ModelWithRowVersion { Text = "Fifteen" }, selectIdentity: true),
                db.Insert(new ModelWithRowVersion { Text = "Sixteen" }, selectIdentity: true)
            };
            var rows = rowIds
                .Select(id => db.SingleById<ModelWithRowVersion>(id))
                .ToArray();
            TouchRow(rowIds[1]);

            rows[0].Text = "Seventeen";
            rows[1].Text = "Eighteen";
            Assert.Throws<OptimisticConcurrencyException>(() => 
                db.UpdateAll(rows));

            var actualRows = rowIds
                .Select(id => db.SingleById<ModelWithRowVersion>(id))
                .ToArray();

            Assert.That(actualRows[0].Text, Is.EqualTo("Fifteen"));
            Assert.That(actualRows[1].Text, Is.Not.EqualTo("Eighteen"));
        }

        [Test]
        public void Save_changed_row_with_outdated_rowversion_throws()
        {
            var rowId = db.Insert(new ModelWithRowVersion { Text = "Fourth" }, selectIdentity: true);
            var row = db.SingleById<ModelWithRowVersion>(rowId);
            TouchRow(rowId);

            row.Text = "Fifth";
            Assert.Throws<OptimisticConcurrencyException>(() => db.Save(row));

            var actualRow = db.SingleById<ModelWithRowVersion>(rowId);
            Assert.That(actualRow.Text, Is.Not.EqualTo("Fourth"));
        }

        [Test]
        public void UpdateAll_with_single_outdated_rowversion_throws_and_all_changes_are_rejected()
        {
            var rowIds = new[]
            {
                db.Insert(new ModelWithRowVersion { Text = "Fifteen" }, selectIdentity: true),
                db.Insert(new ModelWithRowVersion { Text = "Sixteen" }, selectIdentity: true)
            };
            var rows = db.SelectByIds<ModelWithRowVersion>(rowIds);
            TouchRow(rowIds[1]);

            rows[0].Text = "Seventeen";
            rows[1].Text = "Eighteen";
            Assert.Throws<OptimisticConcurrencyException>(() => db.UpdateAll(rows));

            var actualRows = db.SelectByIds<ModelWithRowVersion>(rowIds);
            Assert.That(actualRows[0].Text, Is.EqualTo("Fifteen"));
            Assert.That(actualRows[1].Text, Is.Not.EqualTo("Eighteen"));
        }

        [Test]
        public void SaveAll_with_outdated_rowversion_throws_and_all_changed_are_rejected()
        {
            var rowIds = new[]
            {
                db.Insert(new ModelWithRowVersion { Text = "Seventeenth" }, selectIdentity: true),
                db.Insert(new ModelWithRowVersion { Text = "Eighteenth" }, selectIdentity: true)
            };
            var rows = db.SelectByIds<ModelWithRowVersion>(rowIds);
            TouchRow(rowIds[1]);

            rows[0].Text = "Nineteenth";
            rows[1].Text = "Twentieth";
            Assert.Throws<OptimisticConcurrencyException>(() => db.SaveAll(rows));

            var actualRows = db.SelectByIds<ModelWithRowVersion>(rows.Select(x => x.Id));
            Assert.That(actualRows[0].Text, Is.EqualTo("Seventeenth"));
            Assert.That(actualRows[1].Text, Is.Not.EqualTo("Twentieth"));
        }

        [Test]
        public void Delete_with_outdated_rowversion_throws()
        {
            var rowId = db.Insert(new ModelWithRowVersion { Text = "Seven" }, selectIdentity: true);
            var row = db.SingleById<ModelWithRowVersion>(rowId);
            TouchRow(rowId);

            Assert.Throws<OptimisticConcurrencyException>(() => 
                db.Delete(row));

            var count = db.Count<ModelWithRowVersion>(m => m.Id == rowId);
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public async Task Can_read_from_inner_join_with_schema()
        {
            if ((Dialect & Dialect.AnyMySql) == Dialect) //ERROR table name too long
                return;
        
            db.DropAndCreateTable<ModelWithSchemaAndRowVersionForInnerJoin>();
            var rowVersionModel = new ModelWithRowVersion {
                Text = "test"
            };
            var modelId = await db.InsertAsync(rowVersionModel, selectIdentity: true).ConfigureAwait(false);
            var innerJoinTable = new ModelWithSchemaAndRowVersionForInnerJoin {
                ModelWithRowVersionId = modelId,
                Text = "inner join table"
            };
            var joinId = await db.InsertAsync(innerJoinTable, selectIdentity: true).ConfigureAwait(false);

            var query = db
                .From<ModelWithRowVersion, ModelWithSchemaAndRowVersionForInnerJoin>((x, y) => x.Id == y.ModelWithRowVersionId)
                .Where<ModelWithSchemaAndRowVersionForInnerJoin>(model => model.Id == joinId);

            var result = await db.SingleAsync(query).ConfigureAwait(false);
            Assert.NotNull(result);
        }

        private void TouchRow(long rowId)
        {
            var row = db.SingleById<ModelWithRowVersion>(rowId);
            row.Text = "Touched";
            db.Update(row);
        }

        private void TouchRowBase(long rowId)
        {
            var row = db.SingleById<ModelWithRowVersionBase>(rowId);
            row.Text = "Touched";
            db.Update(row);
        }

        [Schema("Schema")]
        public class SchemaWithRowVersion
        {
            [AutoIncrement]
            public int Id { get; set; }
            public string RandomStringProperty { get; set; }
            public ulong RowVersion { get; set; }
        }

        [Test]
        public void CreateNamedSchemaWithRowVersionClass()
        {
            db.DropAndCreateTable<SchemaWithRowVersion>();
        }
    }
}