using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Common.Utils;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Support;

namespace MasterHost
{
	[TestFixture]
	public class ReportTests
	{
		private readonly AppConfig Config = new AppConfig
		{
			RunOnBaseUrl = "http://localhost",
			TestPaths = TypeSerializer.DeserializeFromString<List<string>>("/,/metadata,/metadata/,/hello,/hello/,/hello/world,/hello/world/1,/hello/world/2,/hello/world/2/3"),
			HandlerHosts = TypeSerializer.DeserializeFromString<List<string>>("/CustomPath35/api,/CustomPath40/api,/RootPath35,/RootPath40,:82,:83,:5001/api,:5002/api,:5003,:5004"),
			HandlerHostNames = TypeSerializer.DeserializeFromString<List<string>>("IIS7+3.5,IIS7+4.0,IIS7+3.5,IIS7+4.0,ConsoleApp,WindowsService,WebServer20,WebServer40,WebServer20,WebServer40"),
		};

		private const string DbPath = @"C:\src\ServiceStack\tests\MasterHost\\reports.sqlite";

		private readonly IDbConnectionFactory DbFactory = new OrmLiteConnectionFactory(
			DbPath, SqliteOrmLiteDialectProvider.Instance);

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			DbFactory.Exec(dbCmd =>
			{
				dbCmd.CreateTable<Report>(false);
				dbCmd.CreateTable<RequestInfoResponse>(false);
			});
		}

		[Test]
		public void Run_RunReportsService()
		{
			var service = new RunReportsService
			{				
				Config = Config,
				DbFactory = DbFactory,								
			};

			var response = service.Get(new RunReports { RunType = "pathsonly" });

			Console.WriteLine(response.Dump());
		}
	}
}