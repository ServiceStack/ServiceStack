using System;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Model;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Async.Issues
{
    public class AsyncProject : IHasId<int>
    {
        [Alias("ProjectId")]
        [Index(Unique = true)]
        [AutoIncrement]
        public int Id { get; set; }

        [Required]
        [References(typeof(AsyncDepartment))]
        public int DepartmentId { get; set; }
        [Reference]
        public AsyncDepartment AsyncDepartment { get; set; }

        [Required]
        public string ProjectName { get; set; }
        [Required]
        public bool IsArchived { get; set; }
        [Required]
        public DateTime CreatedOn { get; set; }
    }

    public class AsyncDepartment
    {
        [Alias("DepartmentId")]
        [Index(Unique = true)]
        [AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public class AsyncProjectTask : IHasId<int>
    {
        [Alias("ProjectTaskId")]
        [Index(Unique = true)]
        [AutoIncrement]
        public int Id { get; set; }

        [References(typeof(AsyncProject))]
        public int ProjectId { get; set; }
        [Reference]
        public AsyncProject AsyncProject { get; set; }

        [Required]
        public string Description { get; set; }
        [Required]
        public DateTime CreatedOn { get; set; }
        public DateTime? FinishedOn { get; set; }
        [Required]
        public int EstimatedHours { get; set; }

        [References(typeof(AsyncProjectTaskStatus))]
        public int? ProjectTaskStatusId { get; set; }
        [Reference]
        public AsyncProjectTaskStatus AsyncProjectTaskStatus { get; set; }

        [Required]
        public int Priority { get; set; }
        [Required]
        public int Order { get; set; }
    }

    public class AsyncProjectTaskStatus : IHasId<int>
    {
        [Alias("ProjectTaskStatusId")]
        [Index(Unique = true)]
        [AutoIncrement]
        public int Id { get; set; }
        [Required]
        public string Description { get; set; }
    }
    
    [TestFixtureOrmLite]
    public class LoadSelectAmbiguousColumnIssue : OrmLiteProvidersTestBase
    {
        public LoadSelectAmbiguousColumnIssue(DialectContext context) : base(context) {}

        public class AsyncDeptEmployee //Ref of External Table
        {
            [PrimaryKey]
            public int Id { get; set; }
        }


        [Test]
        public async Task Can_select_columns_with_LoadSelectAsync()
        {
            using (var db = OpenDbConnection())
            {
                db.DropTable<AsyncDeptEmployee>();

                db.DropTable<AsyncProjectTask>();
                db.DropTable<AsyncProject>();
                db.DropTable<AsyncProjectTaskStatus>();
                db.DropTable<AsyncDepartment>();

                db.CreateTable<AsyncDepartment>();
                db.CreateTable<AsyncProjectTaskStatus>();
                db.CreateTable<AsyncProject>();
                db.CreateTable<AsyncProjectTask>();

                int departmentId = 1;
                int statusId = 1;

                var q = db.From<AsyncProjectTask>()
                          .Join<AsyncProjectTask, AsyncProject>((pt, p) => pt.ProjectId == p.Id)
                          .Where<AsyncProject>(p => p.DepartmentId == departmentId || departmentId == 0)
                          .And<AsyncProjectTask>(pt => pt.ProjectTaskStatusId == statusId || statusId == 0);

                var tasks = await db.LoadSelectAsync(q);

                tasks.PrintDump();
            }
        }
    }
}