using System.Linq;
using Funq;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.Issues
{
    [Route("/query/employees/{Id}", "GET")]
    public class QueryEmployee : QueryDb<Employee>
    {
        public string Id { get; set; }
    }

    [Route("/query/employees", "GET")]
    public class QueryEmployees : QueryDb<Employee>, IJoin<Employee, Department> { }

    public class Employee
    {
        [PrimaryKey]
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        [References(typeof(Department))]
        public int DepartmentId { get; set; }

        [DataAnnotations.Ignore]
        public Department Department { get; set; }
    }

    public class Department
    {
        [PrimaryKey]
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class AutoQueryJoinReferenceId
    {
        private static readonly Department[] SeedDepartments = new[]
        {
            new Department { Id = 10, Name = "Dept 1" },
            new Department { Id = 20, Name = "Dept 2" },
            new Department { Id = 30, Name = "Dept 3" },
        };

        public static Employee[] SeedEmployees = new[]
        {
            new Employee { Id = 1, DepartmentId = 10, FirstName = "First 1", LastName = "Last 1" },
            new Employee { Id = 2, DepartmentId = 20, FirstName = "First 2", LastName = "Last 2" },
            new Employee { Id = 3, DepartmentId = 30, FirstName = "First 3", LastName = "Last 3" },
        };

        class AppHost : AppSelfHostBase
        {
            public AppHost()
                : base(typeof(ClientMemoryLeak).Name, typeof(AutoQueryJoinReferenceId).Assembly) {}

            public override void Configure(Container container)
            {
                var dbFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);
                container.Register<IDbConnectionFactory>(dbFactory);

                Plugins.Add(new AutoQueryFeature { MaxLimit = 100 });

                using (var db = container.Resolve<IDbConnectionFactory>().Open())
                {
                    db.DropTable<Employee>();
                    db.DropTable<Department>();
                    db.CreateTable<Department>();
                    db.CreateTable<Employee>();

                    db.InsertAll(SeedDepartments);
                    db.InsertAll(SeedEmployees);
                }
            }
        }

        public IServiceClient client;
        private readonly ServiceStackHost appHost;
        public AutoQueryJoinReferenceId()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);

            client = new JsonServiceClient(Config.ListeningOn);
        }

        [TestFixtureTearDown]
        public void TearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Does_only_populate_selected_fields()
        {
            QueryResponse<Employee> response;
            response = client.Get(new QueryEmployees { Fields = "id,departmentid" });
            response.PrintDump();
            Assert.That(response.Results.All(x => x.Id > 0 && x.Id < 10));
            Assert.That(response.Results.All(x => x.DepartmentId >= 10));

            response = client.Get(new QueryEmployees { Fields = "departmentid" });
            response.PrintDump();
            Assert.That(response.Results.All(x => x.Id == 0));
            Assert.That(response.Results.All(x => x.DepartmentId >= 10));
        }
    }
}