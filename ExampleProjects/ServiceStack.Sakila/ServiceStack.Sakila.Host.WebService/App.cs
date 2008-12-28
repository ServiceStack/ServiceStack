using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Web;
using Enyim.Caching;
using ServiceStack.CacheAccess.Memcached;
using ServiceStack.Common.Utils;
using ServiceStack.Configuration;
using ServiceStack.DataAccess;
using ServiceStack.DataAccess.NHibernateProvider;
using ServiceStack.Logging;
using ServiceStack.Logging.Log4Net;
using ServiceStack.Sakila.Host.WebService.AppSupport;
using ServiceStack.Sakila.Logic;
using ServiceStack.Sakila.Logic.LogicInterface;
using ServiceStack.Sakila.ServiceInterface;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceModel;
using RequestContext = ServiceStack.ServiceInterface.RequestContext;

namespace ServiceStack.Sakila.Host.WebService
{
	public class App
	{
		public static App Instance = new App();

		public AppConfig Config { get; private set; }
		public IResourceManager StringManager { get; private set; }
		public ILogFactory LogFactory { get; private set; }
		public IPersistenceProviderManagerFactory ProviderManagerFactory { get; private set; }
		public IPersistenceProviderManager DefaultProviderManager { get; private set; }
		public ServiceController ServiceController { get; private set; }
		public AppContext AppContext { get; private set; }

		public App()
		{
			Config = new AppConfig();
			LogFactory = new Log4NetFactory(true);
			ProviderManagerFactory = CreateNHibernateProviderManagerFactory();
			DefaultProviderManager = ProviderManagerFactory.CreateProviderManager(Config.ConnectionString);

			// Create the AppContext injected with the static service implementations
			this.AppContext = new AppContext {
				LogFactory = this.LogFactory,
				Cache = new ServiceStackMemcachedClient(new MemcachedClient()),
			};

			// Create the service controller
			this.ServiceController = new ServiceController(new ServiceResolver());
		}

		public object ExecuteService(object requestDto)
		{
			using (CallContext context = CreateCallContext(requestDto))
			{
				return this.ServiceController.Execute(context);
			}
		}

		public string ExecuteXmlService(string xml, ServiceModelInfo serviceModelInfo)
		{
			// Create a xml request DTO which the service controller will parse and reassign the call
			// context request DTO to a object expected by the relevant port
			XmlRequestDto requestDto = new XmlRequestDto(xml, serviceModelInfo);

			using (CallContext context = CreateCallContext(requestDto))
			{
				return this.ServiceController.ExecuteXml(context);
			}
		}

		private CallContext CreateCallContext(object requestDto)
		{
			// Retrieve the client IP Address
			string clientIPAddress = App.GetIpAddress();

			// Create a facade around a provider connection
			ISakilaServiceFacade facade = new SakilaServiceFacade(this.AppContext, this.DefaultProviderManager);

			// Populate the request context
			var requestContext = new RequestContext(requestDto, facade);

			return new CallContext(this.AppContext, requestContext);
		}

		private NHibernateProviderManagerFactory CreateNHibernateProviderManagerFactory()
		{
			var propertyTable = new Dictionary<string, string>
			                    {
			                    	{"connection.provider", "NHibernate.Connection.DriverConnectionProvider"},
			                    	{"dialect", "NHibernate.Dialect.MySQLDialect"},
			                    	{"connection.driver_class", "NHibernate.Driver.MySqlDataDriver"},
			                    };

			var xmlMappingAssemblyNames = new List<string> {
			                              	"ServiceStack.Sakila.DataAccess",
			                              };

			return new NHibernateProviderManagerFactory(LogFactory) {
				StaticConfigPropertyTable = propertyTable,
				XmlMappingAssemblyNames = xmlMappingAssemblyNames,
			};
		}

		private static string GetIpAddress()
		{
			if (HttpContext.Current != null)
			{
				return HttpContext.Current.Request.UserHostAddress;
			}

			var context = OperationContext.Current;
			if (context == null) return null;
			var prop = context.IncomingMessageProperties;
			if (context.IncomingMessageProperties.ContainsKey(RemoteEndpointMessageProperty.Name))
			{
				var endpoint = prop[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
				if (endpoint != null)
				{
					return endpoint.Address;
				}
			}
			return null;
		}
	}
}