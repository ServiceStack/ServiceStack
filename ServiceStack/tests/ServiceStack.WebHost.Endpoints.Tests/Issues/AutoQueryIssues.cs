using System.Linq;
using Funq;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;
using ServiceStack.Script;
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
                : base(nameof(ClientMemoryLeak), typeof(AutoQueryJoinReferenceId).Assembly) {}

            public override void Configure(Container container)
            {
                ScriptContext.ScriptMethods.AddRange(new ScriptMethods[] {
                    new DbScriptsAsync(),
                    new MyValidators(), 
                });

                SetConfig(new HostConfig {
                    UseCamelCase = true,
                });
                var dbFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);
                //var dbFactory = new OrmLiteConnectionFactory(Tests.Config.SqlServerConnString, SqlServerDialect.Provider);
                container.Register<IDbConnectionFactory>(dbFactory);

                Plugins.Add(new AutoQueryFeature { MaxLimit = 100 });

                using var db = container.Resolve<IDbConnectionFactory>().Open();
                db.DropTable<Employee>();
                db.DropTable<Department>();
                db.CreateTable<Department>();
                db.CreateTable<Employee>();

                db.InsertAll(SeedDepartments);
                db.InsertAll(SeedEmployees);
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

        [OneTimeTearDown]
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

        public partial class CustomFields
        {
            [Required]
            [PrimaryKey]
            [CustomField("CHAR(20)")]
            public string StringId { get; set; }
            [Required]
            [CustomField("TINYINT")]
            public byte Byte { get; set; }
        }

        [Route("/Queries/CustomFields", "GET")]
        public partial class CustomFieldsQuery
            : QueryDb<CustomFields>, IReturn<QueryResponse<CustomFields>>
        {
            public CustomFieldsQuery()
            {
                StringIdBetween = new string[] { };
                StringIdIn = new string[] { };
                ByteBetween = new byte[] { };
                ByteIn = new byte[] { };
            }

            public virtual string StringId { get; set; }
            public virtual string StringIdStartsWith { get; set; }
            public virtual string StringIdEndsWith { get; set; }
            public virtual string StringIdContains { get; set; }
            public virtual string StringIdLike { get; set; }
            public virtual string[] StringIdBetween { get; set; }
            public virtual string[] StringIdIn { get; set; }
            public virtual byte? Byte { get; set; }
            public virtual byte? ByteGreaterThanOrEqualTo { get; set; }
            public virtual byte? ByteGreaterThan { get; set; }
            public virtual byte? ByteLessThan { get; set; }
            public virtual byte? ByteLessThanOrEqualTo { get; set; }
            public virtual byte? ByteNotEqualTo { get; set; }
            public virtual byte[] ByteBetween { get; set; }
            public virtual byte[] ByteIn { get; set; }
        }

        [Test]
        public void Can_query_Table_with_Byte_property()
        {
            using (var db = appHost.Resolve<IDbConnectionFactory>().Open())
            {
                db.DropAndCreateTable<CustomFields>();
                db.Insert(new CustomFields { StringId = "1001", Byte = 1 });
                db.Insert(new CustomFields { StringId = "1002", Byte = 2 });
            }

            var response = client.Get(new CustomFieldsQuery
            {
                StringIdIn = new[] { "1001", "1002" },
            });
            Assert.That(response.Results.Map(x => x.Byte), Is.EquivalentTo(new[] { 1, 2 }));

            response = client.Get(new CustomFieldsQuery
            {
                StringIdIn = new[] { "1001", "1002" },
                Byte = 2
            });
            Assert.That(response.Results.Map(x => x.Byte), Is.EquivalentTo(new[] { (byte)2 }));

            response = client.Get(new CustomFieldsQuery
            {
                ByteIn = new byte[] { 1, 2 }
            });
            Assert.That(response.Results.Map(x => x.Byte), Is.EquivalentTo(new[] { 1, 2 }));
        }
        
    }
}