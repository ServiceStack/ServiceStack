using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Issues
{
    [Alias("UV_Child")]
    public partial class UV_Child
    {
        public int? KindergartenId { get; set; }
        public string KindergartenName { get; set; }
        public int? ClassId { get; set; }
        public string ClassName { get; set; }
        [Required]
        public int ChildId { get; set; }
        [Required]
        public string ChildName { get; set; }
        public int? ParentId { get; set; }
        public string DisplayName { get; set; }
        public string UserName { get; set; }
        public string PhoneNumber { get; set; }
        public string ImageName { get; set; }
    }

    public class ChildResponse
    {
        public int? KindergartenId { get; set; }
        public string KindergartenName { get; set; }
        public int? ClassId { get; set; }
        public string ClassName { get; set; }
        public int ChildId { get; set; }
        public string ChildName { get; set; }
        public int? ParentId { get; set; }
        public string DisplayName { get; set; }
        public string UserName { get; set; }
        public string PhoneNumber { get; set; }
        public string ImageName { get; set; }
    }

    [TestFixtureOrmLite]
    public class CreateTableAliasTest : OrmLiteProvidersTestBase
    {
        public CreateTableAliasTest(DialectContext context) : base(context) {}

        [Test]
        public void Can_Create_and_Select_from_table()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<UV_Child>();

                db.GetLastSql().Print();

                var results = db.LoadSelect<ChildResponse, UV_Child>(
                    db.From<UV_Child>()
                       .Where(x => x.ClassId == 112));

                db.GetLastSql().Print();

                results.PrintDump();
            }
        }
    }
}