using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Model;

namespace ServiceStack.OrmLite.Tests.Async.Issues
{
    public class ComputeTest : IHasId<int>
    {
        [PrimaryKey]
        [Alias("EmployeeId")]
        [AutoIncrement]
        [Index(Unique = true)]
        public int Id { get; set; }

        [Required]
        public string FirstName { get; set; }
        public string LastName { get; set; }

        [Required]
        [Index(true)]
        public string Username { get; set; }

        public string Password { get; set; }

        [Compute]
        public string FullName { get; set; }
    }

    public class TestExpression
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string AccountName { get; set; }
        public bool IsActive { get; set; }
    }

    [TestFixtureOrmLiteDialects(Dialect.AnySqlServer)]
    public class SqlServerComputedColumnIssue : OrmLiteProvidersTestBase
    {
        public SqlServerComputedColumnIssue(DialectContext context) : base(context) {}
        
        private ComputeTest CreateTableAndGetRow()
        {
            using (var db = OpenDbConnection())
            {
                db.DropTable<ComputeTest>();
                db.ExecuteSql(@"
CREATE TABLE [dbo].[ComputeTest](
    [EmployeeId] [int] IDENTITY(1,1) NOT NULL,
    [FullName]  AS (concat(ltrim(rtrim([FirstName])),' ',ltrim(rtrim([LastName])))) PERSISTED NOT NULL,
    [FirstName] [nvarchar](55) NOT NULL,
    [LastName] [nvarchar](55) NULL,
    [Username] [nvarchar](55) NOT NULL,
    [Password] [nvarchar](55) NULL
 CONSTRAINT [PK_Employee] PRIMARY KEY CLUSTERED 
(
    [EmployeeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 80) ON [PRIMARY]
) ON [PRIMARY]");

                var item = new ComputeTest
                {
                    FirstName = "FirstName",
                    LastName = "LastName",
                    Username = "Username",
                    Password = "Password",
                    FullName = "Should be ignored",
                };
                return item;
            }
        }

        [Test]
        public void Can_Insert_and_Update_table_with_Computed_Column()
        {
            using (var db = OpenDbConnection())
            {
                var item = CreateTableAndGetRow();

                var id = db.Insert(item, selectIdentity: true);

                var row = db.LoadSingleById<ComputeTest>(id);

                Assert.That(row.FirstName, Is.EqualTo("FirstName"));
                Assert.That(row.FullName, Is.EqualTo("FirstName LastName"));

                row.LastName = "Updated LastName";
                db.Update(row);

                row = db.LoadSingleById<ComputeTest>(id);

                Assert.That(row.FirstName, Is.EqualTo("FirstName"));
                Assert.That(row.FullName, Is.EqualTo("FirstName Updated LastName"));
            }
        }

        [Test]
        public async Task Can_Insert_and_Update_table_with_Computed_Column_async()
        {
            using (var db = OpenDbConnection())
            {
                var item = CreateTableAndGetRow();

                var row = await Create(item);

                Assert.That(row.FirstName, Is.EqualTo("FirstName"));
                Assert.That(row.FullName, Is.EqualTo("FirstName LastName"));

                row.LastName = "Updated LastName";
                row = await Create(row);

                Assert.That(row.FirstName, Is.EqualTo("FirstName"));
                Assert.That(row.FullName, Is.EqualTo("FirstName Updated LastName"));
            }
        }

        public virtual async Task<T> Create<T>(T obj) where T : IHasId<int>
        {
            using (var db = OpenDbConnection())
            {
                // if there is an id then INSERTS otherwise UPDATES
                var id = obj.GetId().ConvertTo<long>();

                if (id > 0)
                    db.Update(obj);
                else
                    id = db.Insert(obj, true);

                // returns the object inserted or updated
                return await db.LoadSingleByIdAsync<T>(id);
            }
        }

        [Test]
        public async Task LoadSelect_can_query_and_orderBy()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<TestExpression>();

                db.Insert(new TestExpression {AccountName = "Foo", IsActive = true});
                db.Insert(new TestExpression {AccountName = "Bar", IsActive = false});

                var rows = (await db.LoadSelectAsync<TestExpression>(x => x.IsActive))
                    .OrderBy(x => x.AccountName)
                    .ToList();

                Assert.That(rows.Count, Is.EqualTo(1));
                Assert.That(rows[0].AccountName, Is.EqualTo("Foo"));

                rows = await db.LoadSelectAsync(db.From<TestExpression>()
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.AccountName));

                Assert.That(rows.Count, Is.EqualTo(1));
                Assert.That(rows[0].AccountName, Is.EqualTo("Foo"));
            }
        }
    }
}