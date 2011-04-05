using System;
using Funq;
using ServiceStack.CacheAccess;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.Common;
using ServiceStack.Common.Utils;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.ServiceHost;
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
				this.RequestFilters.Add((req, res, dto) =>
				{
					var requestFilter = dto as RequestFilter;
					if (requestFilter != null)
					{
						res.StatusCode = requestFilter.StatusCode;
						if (!requestFilter.HeaderName.IsNullOrEmpty())
						{
							res.AddHeader(requestFilter.HeaderName, requestFilter.HeaderValue);
						}
						res.Close();
					}

					var secureRequests = dto as IRequiresSession;
					if (secureRequests != null)
					{
						res.ReturnAuthRequired();
					}
				});

				this.Container.Register<IDbConnectionFactory>(c =>
					new OrmLiteConnectionFactory(
						"~/App_Data/db.sqlite".MapHostAbsolutePath(),
						SqliteOrmLiteDialectProvider.Instance));

				this.Container.Register<ICacheClient>(new MemoryCacheClient());

				var dbFactory = this.Container.Resolve<IDbConnectionFactory>();
				dbFactory.Exec(dbCmd => dbCmd.CreateTable<Movie>(true));

				Routes
					.Add<Movies>("/custom-movies", "GET")
					.Add<Movies>("/custom-movies/genres/{Genre}")
					.Add<Movie>("/custom-movies", "POST,PUT")
					.Add<Movie>("/custom-movies/{Id}");

				var resetMovies = this.Container.Resolve<ResetMoviesService>();
				resetMovies.Post(null);

				//var partialFeatures = Feature.All.Remove(Feature.Soap11);
				//SetConfig(new EndpointHostConfig { EnableFeatures = partialFeatures });
			}
		}

		protected void Application_Start(object sender, EventArgs e)
		{
			var appHost = new AppHost();
			appHost.Init();
		}

	}
}