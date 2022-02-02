using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Dapper;

namespace ServiceStack.OrmLite.Tests.UseCase
{
    class Post
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastChangeDate { get; set; }
        public int? Counter1 { get; set; }
        public int? Counter2 { get; set; }
        public int? Counter3 { get; set; }
        public int? Counter4 { get; set; }
        public int? Counter5 { get; set; }
        public int? Counter6 { get; set; }
        public int? Counter7 { get; set; }
        public int? Counter8 { get; set; }
        public int? Counter9 { get; set; }

    }

    [TestFixtureOrmLiteDialects(Dialect.AnySqlServer), NUnit.Framework.Ignore("Integration Test")]
    public class PostPerfTests : OrmLiteProvidersTestBase
    {
        public PostPerfTests(DialectContext context) : base(context) {}
        
//        public PostPerfTests()
//        {
//            Dialect = Dialect.SqlServer2012;
//        }

        private void EnsureDBSetup()
        {
            using (var cnn = OpenDbConnection().ToDbConnection())
            {
                var cmd = cnn.CreateCommand();
                cmd.CommandText = @"
if (OBJECT_ID('Post') is null)
begin
	create table Post
	(
		Id int identity primary key, 
		[Text] varchar(max) not null, 
		CreationDate datetime not null, 
		LastChangeDate datetime not null,
		Counter1 int,
		Counter2 int,
		Counter3 int,
		Counter4 int,
		Counter5 int,
		Counter6 int,
		Counter7 int,
		Counter8 int,
		Counter9 int
	)
	   
	set nocount on 

	declare @i int
	declare @c int

	declare @id int

	set @i = 0

	while @i < 5000
	begin 
		
		insert Post ([Text],CreationDate, LastChangeDate) values (replicate('x', 2000), GETDATE(), GETDATE())
		set @id = @@IDENTITY
		
		set @i = @i + 1
	end
end
";
                cmd.Connection = cnn;
                cmd.ExecuteNonQuery();
            }
        }
        [Test]
        public void Run_single_select_Dapper()
        {
            var tester = new Tester();

            var db = OpenDbConnection();
            tester.Add(id => db.Query<Post>("select * from Post where Id = @Id", new { Id = id }).ToList(), "Mapper Query");

            tester.Run(500);
        }

        [Test]
        public void Run_single_select_OrmLite()
        {
            var tester = new Tester();

            var db = OpenDbConnection();
            tester.Add(id => db.Select<Post>("select * from Post where Id = @id", new { id = id }), "OrmLite Query");

            tester.Run(500);
        }

        [Test]
        public void Run_multi_select_Dapper()
        {
            var tester = new Tester();

            var db = OpenDbConnection();
            tester.Add(id => db.Query<Post>("select top 1000 * from Post").ToList(), "Mapper Query");

            tester.Run(50);
        }

        [Test]
        public void Run_multi_select_OrmLite()
        {
            var tester = new Tester();

            var db = OpenDbConnection();
            tester.Add(id => db.Select<Post>("select top 1000 * from Post"), "OrmLite Query");

            tester.Run(50);
        }

        [Test]
        public void Run_multi_select_OrmLite_SqlExpression()
        {
            var tester = new Tester();

            var db = OpenDbConnection();
            tester.Add(id => db.Select(db.From<Post>().Limit(1000)), "OrmLite Query Expression");

            tester.Run(50);
        }
    }

    [TestFixtureOrmLiteDialects(Dialect.AnySqlServer), Explicit, NUnit.Framework.Ignore("Integration Test")]
    public class AdventureWorksPerfTests : OrmLiteProvidersTestBase
    {
        public AdventureWorksPerfTests(DialectContext context) : base(context) {}

        private IDbConnection db;

//        public AdventureWorksPerfTests()
//        {
//            var dbFactory = new OrmLiteConnectionFactory(
//                "data source=localhost;initial catalog=AdventureWorks;integrated security=SSPI;persist security info=False;packet size=4096",
//                SqlServer2012Dialect.Provider);
//
//            db = dbFactory.Open();
//        }

        [SetUp]
        public void Setup()
        {
            db = OpenDbConnection();
        }

        [TearDown]
        public void TearDown()
        {
            db.Dispose();
        }

        private static string SqlSelectCommandText = @"SELECT [SalesOrderID],[RevisionNumber],[OrderDate],[DueDate],[ShipDate],[Status],[OnlineOrderFlag],[SalesOrderNumber],[PurchaseOrderNumber],[AccountNumber],[CustomerID],[SalesPersonID],[TerritoryID],[BillToAddressID],[ShipToAddressID],[ShipMethodID],[CreditCardID],[CreditCardApprovalCode],[CurrencyRateID],[SubTotal],[TaxAmt],[Freight],[TotalDue],[Comment],[rowguid],[ModifiedDate]	FROM [Sales].[SalesOrderHeader]";

        [Test]
        public void Select_all_SalesOrderHeader_Dapper()
        {
            db.Query<SalesOrderHeader>(SqlSelectCommandText).AsList();

            var tester = new Tester();

            tester.Add(id => db.Query<SalesOrderHeader>(SqlSelectCommandText).AsList(), "Dapper Query");

            tester.Run(10);
        }

        [Test]
        public void Select_all_SalesOrderHeader_OrmLite()
        {
            var tester = new Tester();

            tester.Add(id => db.SqlList<SalesOrderHeader>(SqlSelectCommandText), "OrmLite Query");

            tester.Run(10);
        }
    }

    [Schema("Sales")]
    public class SalesOrderHeader
    {
        public string AccountNumber { get; set; }
        public string Comment { get; set; }
        public string CreditCardApprovalCode { get; set; }
        public DateTime DueDate { get; set; }
        public decimal Freight { get; set; }
        public DateTime ModifiedDate { get; set; }
        public bool OnlineOrderFlag { get; set; }
        public DateTime OrderDate { get; set; }
        public string PurchaseOrderNumber { get; set; }
        public byte RevisionNumber { get; set; }
        public Guid Rowguid { get; set; }
        public int SalesOrderId { get; set; }
        public string SalesOrderNumber { get; set; }
        public DateTime? ShipDate { get; set; }
        public byte Status { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmt { get; set; }
        public decimal TotalDue { get; set; }

        public int CustomerID { get; set; }
        public int? SalesPersonID { get; set; }
        public int? TerritoryID { get; set; }
        public int BillToAddressID { get; set; }
        public int ShipToAddressID { get; set; }
        public int ShipMethodID { get; set; }
        public int? CreditCardID { get; set; }
        public int? CurrencyRateID { get; set; }
    }

    class Test
    {
        public static Test Create(Action<int> iteration, string name)
        {
            return new Test { Iteration = iteration, Name = name };
        }

        public Action<int> Iteration { get; set; }
        public string Name { get; set; }
        public Stopwatch Watch { get; set; }
    }

    class Tester : List<Test>
    {
        public void Add(Action<int> iteration, string name)
        {
            Add(Test.Create(iteration, name));
        }

        public void Run(int iterations)
        {
            // warmup 
            foreach (var test in this)
            {
                test.Iteration(iterations + 1);
                test.Watch = new Stopwatch();
                test.Watch.Reset();
            }

            var rand = new Random();
            for (int i = 1; i <= iterations; i++)
            {
                foreach (var test in this.OrderBy(ignore => rand.Next()))
                {
                    test.Watch.Start();
                    test.Iteration(i);
                    test.Watch.Stop();
                }
            }

            foreach (var test in this.OrderBy(t => t.Watch.ElapsedMilliseconds))
            {
                Console.WriteLine(test.Name + " took " + test.Watch.ElapsedMilliseconds + "ms");
            }
        }
    }

}