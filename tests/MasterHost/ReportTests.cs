using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.ServiceInterface.Testing;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Support;

namespace MasterHost
{
	[TestFixture]
	public class ReportTests
	{
		//IIS7.0
		//private readonly AppConfig Config = new AppConfig
		//{
		//    RunOnBaseUrl = "http://localhost",
		//    TestPaths = "/,/metadata,/metadata/,/hello,/hello/,/hello/world,/hello/world/1,/hello/world/2,/hello/world/2/3".To<List<string>>(),
		//    HandlerHosts = "/ApiPath35/api,/CustomPath35/api,/CustomPath40/api,/RootPath35,/RootPath40,:82,:83,:5001/api,:5002/api,:5003,:5004".To<List<string>>(),
		//    HandlerHostNames = "IIS7+3.5,IIS7+3.5,IIS7+4.0,IIS7+3.5,IIS7+4.0,ConsoleApp,WindowsService,WebServer20,WebServer40,WebServer20,WebServer40".To<List<string>>(),
		//};

		//MONO+FastCGI + xsp :8080 + ConsoleApp + :82
		//private readonly AppConfig Config = new AppConfig
		//{
		//    RunOnBaseUrl = "http://servicestack.net",
		//    TestPaths = "/,/metadata,/metadata/,/hello,/hello/,/hello/world,/hello/world/1,/hello/world/2,/hello/world/2/3".To<List<string>>(),
		//    HandlerHosts = "/ApiPath35/api,/CustomPath35/api,/CustomPath40/api,/RootPath35,/RootPath40,:82,:8080/ApiPath35/api,:8080/CustomPath35/api,:8080/CustomPath40/api,:8080/RootPath35,:8080/RootPath40".To<List<string>>(),
		//    HandlerHostNames = "Nginx/FastCGI,Nginx/FastCGI,Nginx/FastCGI,Nginx/FastCGI,Nginx/FastCGI,ConsoleApp,xsp2,xsp4,xsp2,xsp4".To<List<string>>(),
		//};

		//MONO + Apache+mod_mono
		private readonly AppConfig Config = new AppConfig
		{
			RunOnBaseUrl = "http://api.servicestack.net",
			TestPaths = "/,/metadata,/metadata/,/hello,/hello/,/hello/world,/hello/world/1,/hello/world/2,/hello/world/2/3".To<List<string>>(),
			HandlerHosts = "/ApiPath35/api,/CustomPath35/api,/CustomPath40/api,/RootPath35,/RootPath40".To<List<string>>(),
			HandlerHostNames = "Apache+mod_mono2,Apache+mod_mono2,Apache+mod_mono2,Apache+mod_mono2,Apache+mod_mono2".To<List<string>>(),
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

		//[Test]
		//public void Run_RunReportsService()
		//{
		//    var appHost = new BasicAppHost();
		//    appHost.Container.Register(c => new ReportsService { DbFactory = DbFactory });
		//    var service = new RunReportsService
		//    {
		//        Config = Config,
		//        DbFactory = DbFactory,
		//        AppHost = appHost,
		//    };

		//    var response = service.Get(new RunReports { RunType = "all" });

		//    Console.WriteLine(response.Dump());
		//}
	}
}