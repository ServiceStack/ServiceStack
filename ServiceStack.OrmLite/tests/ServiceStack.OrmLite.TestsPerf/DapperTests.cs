using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.SqlServer;

namespace ServiceStack.OrmLite.TestsPerf
{
    [TestFixture]
    public class DapperTests
    {
        private class PerformanceTests
        {
            private class Test
            {
                public static Test Create(Action<int> iteration, string name)
                {
                    return new Test {Iteration = iteration, Name = name};
                }

                public Action<int> Iteration { get; set; }
                public string Name { get; set; }
                public Stopwatch Watch { get; set; }
            }

            private class Tests : List<Test>
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
            
            public void Run(int iterations)
            {
                OrmLiteConfig.DialectProvider = SqlServerOrmLiteDialectProvider.Instance; //Using SQL Server
                IDbConnection ormLiteCmd = GetOpenConnection();

                var tests = new Tests();
                //tests.Add(id => ormLiteCmd.GetById<Post>(id), "OrmLite Query GetById");
                //tests.Add(id => ormLiteCmd.First<Post>("SELECT * FROM Posts WHERE Id = {0}", id), "OrmLite Query First<SQL>");
                //tests.Add(id => ormLiteCmd.QuerySingle<Post>("Id", id), "OrmLite QuerySingle");
                tests.Add(id => ormLiteCmd.SingleById<Post>(id), "OrmLite Query QueryById");

                tests.Run(iterations);
            }
        }

        [Alias("Posts")]
        //[Soma.Core.Table(Name = "Posts")]
        class Post
        {
            //[Soma.Core.Id(Soma.Core.IdKind.Identity)]
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

		//
		//public static readonly string connectionString = "Data Source=.;Initial Catalog=tempdb;Integrated Security=True";
		public static readonly string connectionString = @"Data Source=.\SQLEXPRESS;AttachDbFilename=|DataDirectory|\App_Data\Dapper.mdf;Integrated Security=True;Connect Timeout=30;User Instance=True";

        private IDbCommand dbCmd;
        public static SqlConnection GetOpenConnection()
        {
            var connection = new SqlConnection(connectionString);
            connection.Open();
            return connection;
        }

        private static void EnsureDBSetup()
        {
            using (var cnn = GetOpenConnection())
            {
                var cmd = cnn.CreateCommand();
                cmd.CommandText = @"
if (OBJECT_ID('Posts') is null)
begin
	create table Posts
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

	while @i <= 5001
	begin 
		
		insert Posts ([Text],CreationDate, LastChangeDate) values (replicate('x', 2000), GETDATE(), GETDATE())
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
        public void RunPerformanceTests()
        {
            EnsureDBSetup();
            var test = new PerformanceTests();
            const int iterations = 500;
            Console.WriteLine("Running {0} iterations that load up a post entity", iterations);

            Console.WriteLine("\n\n Run 1:\n");
            test.Run(iterations);
            Console.WriteLine("\n\n Run 2:\n");
            test.Run(iterations);
            Console.WriteLine("\n\n Run 3:\n");
            test.Run(iterations);
        }


    }
}