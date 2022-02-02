using System.Runtime.Serialization;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Issues
{
    [Alias("makemodel")]
    public class MakeModel
    {
        [Alias("id"), PrimaryKey]
        public int Id { get; set; }

        [Alias("manufacturer")]
        public string Manufacturer { get; set; }

        [Alias("model")]
        public string Model { get; set; }

        [References(typeof(PerformanceCategory))]
        [Alias("performance_category_id")]
        public int PerformanceCategoryId { get; set; }

        public ulong RowVersion { get; set; }
    }

    [Alias("performance_category")]
    public class PerformanceCategory
    {
        [Alias("id"), PrimaryKey]
        public int Id { get; set; }

        [Alias("category")]
        public string Category { get; set; }

        [Alias("description")]
        public string Description { get; set; }
    }

    [DataContract]
    public class VehicleDto : DataTransferObject
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "manufacturer")]
        public string Manufacturer { get; set; }

        [DataMember(Name = "model")]
        public string Model { get; set; }

        [DataMember(Name = "performance_category_id")]
        public string PerformanceCategoryId { get; set; }

        [DataMember(Name = "category")]
        public string Category { get; set; }

        [DataMember(Name = "row-version")]
        public ulong RowVersion { get; set; }
    }

    public class DataTransferObject { }

    [TestFixtureOrmLite]
    public class RowVersionJoinIssue : OrmLiteProvidersTestBase
    {
        public RowVersionJoinIssue(DialectContext context) : base(context) {}

        [Test]
        public void Can_join_on_table_with_RowVersion()
        {
            using (var db = OpenDbConnection())
            {
                var id = 1;

                db.DropTable<MakeModel>();
                db.DropTable<PerformanceCategory>();
                db.CreateTable<PerformanceCategory>();
                db.CreateTable<MakeModel>();

                db.Insert(new PerformanceCategory { Id = 1, Category = "category" });
                db.Insert(new MakeModel { Id = 1, Manufacturer = "manufacturer", Model = "model", PerformanceCategoryId = 1 });

                var row = db.Single<VehicleDto>(db.From<MakeModel, PerformanceCategory>().Where(v => v.Id == id));

                db.GetLastSql().Print();

                Assert.That(row.Id, Is.EqualTo("1"));
                Assert.That(row.Category, Is.EqualTo("category"));
            }
        }

    }
}