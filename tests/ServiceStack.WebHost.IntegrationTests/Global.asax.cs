using System;
using Funq;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Web;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.Endpoints.Extensions;
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
						"~/App_Data/db.sqlite".MapHostAbsolutePath(),
						SqliteOrmLiteDialectProvider.Instance));

				var dbFactory = this.Container.Resolve<IDbConnectionFactory>();
				dbFactory.Exec(dbCmd => dbCmd.CreateTable<Movie>(true));

				var resetMovies = this.Container.Resolve<ResetMoviesService>();
				resetMovies.Post(null);
			}
		}

		protected void Application_Start(object sender, EventArgs e)
		{
			var appHost = new AppHost();
			appHost.Init();
		}

	}
}