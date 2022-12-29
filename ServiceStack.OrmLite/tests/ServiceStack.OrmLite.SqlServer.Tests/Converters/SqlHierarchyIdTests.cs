using System.Linq;
using Microsoft.SqlServer.Types;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.SqlServerTests.Converters
{
    [TestFixture]
    public class SqlHierarchyIdTests : SqlServer2012ConvertersOrmLiteTestBase
    {
        public string ColumnDefinition { get; set; }

        [OneTimeSetUp]
        public new void TestFixtureSetUp()
        {
            base.TestFixtureSetUp();

            var converter = new SqlServer.Converters.SqlServerHierarchyIdTypeConverter();
            ColumnDefinition = converter.ColumnDefinition;
        }

        [Test]
        public void Can_insert_and_retrieve_HierarchyId()
        {
            Db.DropAndCreateTable<HierarchyIdTable>();

            var stringValue = "/1/1/3/";  // 0x5ADE
            var hierarchyId = SqlHierarchyId.Parse(stringValue);

            Db.Insert(new HierarchyIdTable() {
                TreeId = hierarchyId,
                NullTreeId = SqlHierarchyId.Null,
                StringTreeId = stringValue });

            var result = Db.Select<HierarchyIdTable>().FirstOrDefault();
            Assert.AreEqual(null, result.NullTreeId);
            Assert.AreEqual(hierarchyId, result.TreeId);
            Assert.AreEqual(stringValue, result.StringTreeId);

            var parent = Db.Scalar<SqlHierarchyId>(
                Db.From<HierarchyIdTable>().Select("TreeId.GetAncestor(1)"));
            var str = parent.ToString();
            Assert.AreEqual("/1/1/", str);
        }

        [Test]
        public void Can_convert_to_and_from_HierarchyId_and_string_with_anon_typed_insert()
        {
            Db.DropAndCreateTable<HierarchyIdTable>();

            var dialect = Db.GetDialectProvider();
            var tableName = dialect.GetQuotedTableName(ModelDefinition<HierarchyIdTable>.Definition);

            var stringValue = "/2/3/6/";
            var treeId = SqlHierarchyId.Parse(stringValue);

            var sql = $"INSERT {tableName} (TreeId, StringTreeId, NullTreeId) VALUES (@TreeId, @StringTreeId, @NullTreeId);";
            Db.ExecuteSql(sql, new { TreeId = treeId, StringTreeId = stringValue, NullTreeId = SqlHierarchyId.Null });

            var result = Db.Select<FlippedHierarchyIdTable>().FirstOrDefault();
            Assert.AreEqual(stringValue, result.StringTreeId);
            Assert.AreEqual(treeId, result.TreeId);
            Assert.AreEqual(null, result.NullTreeId);
        }

        [Test]
        public void Can_convert_to_and_from_HierarchyId_and_string_with_strong_typed_insert()
        {
            Db.DropAndCreateTable<HierarchyIdTable>();

            var stringValue = "/5/4/1/";
            var treeId = SqlHierarchyId.Parse(stringValue);

            Db.Insert(new HierarchyIdTable() { TreeId = treeId, StringTreeId = stringValue, NullTreeId = SqlHierarchyId.Null });

            var result = Db.Select<FlippedHierarchyIdTable>().FirstOrDefault();
            Assert.AreEqual(stringValue, result.StringTreeId);
            Assert.AreEqual(treeId, result.TreeId);
            Assert.AreEqual(null, result.NullTreeId);
        }

        [Test]
        public void Can_convert_hierarchyid_to_quoted_string()
        {
            var converter = new SqlServer.Converters.SqlServerHierarchyIdTypeConverter();

            string stringValue = null;
            var hierarchyId = SqlHierarchyId.Parse(stringValue); // NULL
            var str = converter.ToQuotedString(typeof(SqlHierarchyId), hierarchyId);

            Assert.AreEqual($"CAST(null AS {ColumnDefinition})", str);

            stringValue = "/1/1/3/";
            hierarchyId = SqlHierarchyId.Parse(stringValue); // 0x5ADE
            str = converter.ToQuotedString(typeof(SqlHierarchyId), hierarchyId);

            Assert.AreEqual($"CAST('{stringValue}' AS {ColumnDefinition})", str);
        }
    }

    [Alias("HierarchyIdTable")]
    public class HierarchyIdTable
    {
        [AutoIncrement]
        public long Id { get; set; }

        public SqlHierarchyId TreeId { get; set; }        

        public string StringTreeId { get; set; }

        public SqlHierarchyId? NullTreeId { get; set; }
    }

    [Alias("HierarchyIdTable")]
    public class FlippedHierarchyIdTable
    {
        [AutoIncrement]
        public long Id { get; set; }

        [Alias("StringTreeId")]
        public SqlHierarchyId TreeId { get; set; }

        [Alias("TreeId")]
        public string StringTreeId { get; set; }

        public string NullTreeId { get; set; }
    }
}
