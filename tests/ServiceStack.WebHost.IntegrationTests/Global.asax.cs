using System;
using Funq;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests
{
	public class Global : System.Web.HttpApplication
	{
		public class AppHost
			: AppHostBase
		{
			public AppHost()
				: base("ServiceStack WebHost IntegrationTests", typeof(Reverse).Assembly)
			{
			}

			public override void Configure(Container container)
			{
				this.Container.Register<IDbConnectionFactory>(c =>
					new OrmLiteConnectionFactory(
						":memory:", false,
						SqliteOrmLiteDialectProvider.Instance));

				var dbFactory = this.Container.Resolve<IDbConnectionFactory>();
				dbFactory.Exec(dbCmd => dbCmd.CreateTable<Movie>(true));
			}
		}               

		protected void Application_Start(object sender, EventArgs e)
		{
			var appHost = new AppHost();
			appHost.Init();
		}

		protected void Session_Start(object sender, EventArgs e)
		{

		}

		protected void Application_BeginRequest(object sender, EventArgs e)
		{

		}

		protected void Application_AuthenticateRequest(object sender, EventArgs e)
		{

		}

		protected void Application_Error(object sender, EventArgs e)
		{

		}

		protected void Session_End(object sender, EventArgs e)
		{

		}

		protected void Application_End(object sender, EventArgs e)
		{

		}
	}
}