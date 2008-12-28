/*
// $Id: App.cs 678 2008-12-22 19:23:55Z DDNGLOBAL\Demis $
//
// Revision      : $Revision: 678 $
// Modified Date : $LastChangedDate: 2008-12-22 19:23:55 +0000 (Mon, 22 Dec 2008) $ 
// Modified By   : $LastChangedBy: DDNGLOBAL\Demis $
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using System.Collections.Generic;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Web;
using Ddn.CacheAccess.Memcached;
using Ddn.Common.Services.Crypto;
using Ddn.Common.Services.Service;
using Ddn.DataAccess;
using Ddn.DataAccess.NHibernateProvider;
using Ddn.Logging;
using Ddn.Logging.Log4Net;
using Enyim.Caching;
using Utopia.Common.Resources;
using Utopia.Common.Service;
using @ServiceNamespace@.Host.WebService.AppSupport;
using @ServiceNamespace@.Logic;
using @ServiceNamespace@.Logic.LogicInterface;
using @ServiceNamespace@.ServiceInterface;
using RequestContext=Ddn.Common.Services.Service.RequestContext;

namespace @ServiceNamespace@.Host.WebService
{
	public class App
	{
		public static App Instance = new App();

		public AppConfig Config { get; private set; }
		public RsaPrivateKey ServerPrivateKey { get; private set; }
		public IStringResourceManager StringManager { get; private set; }
		public ILogFactory LogFactory { get; private set; }
		public IPersistenceProviderManagerFactory ProviderManagerFactory { get; private set; }
		public IPersistenceProviderManager DefaultProviderManager { get; private set; }
		public ServiceController ServiceController { get; private set; }
		public AppContext AppContext { get; private set; }

		public App()
		{
			Config = new AppConfig();
			LogFactory = new Log4NetFactory(true);
			ServerPrivateKey = new RsaPrivateKey(LogFactory, Config.ServerPrivateKey);
			StringManager = new StringResourceManager(LogFactory);
			ProviderManagerFactory = CreateNHibernateProviderManagerFactory();
			DefaultProviderManager = ProviderManagerFactory.CreateProviderManager(Config.ConnectionString);

			// Load string resources from file
			string path = Path.Combine(HttpContext.Current.Server.MapPath("~/bin/"), AppConfig.StringResourcesFile);
			StringManager.LoadTextFile(path);

			var cacheClient = new DdnMemcachedClient(new MemcachedClient());

			// Create the AppContext injected with the static service implementations
			this.AppContext = new AppContext(this.ServerPrivateKey, this.StringManager, this.LogFactory, cacheClient);

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
			// Retrieve the client IP address
			string clientIPAddress = App.GetIpAddress();

			// Create a facade around a provider connection
			I@ServiceName@Facade facade = new @ServiceName@Facade(this.AppContext, this.DefaultProviderManager, clientIPAddress);

			// Populate the request context
			RequestContext requestContext = new RequestContext(requestDto, facade);

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

			var xmlMappingAssemblyNames = new List<string>
			                              {
			                              	"@ServiceNamespace@.DataAccess",
			                              };

			return new NHibernateProviderManagerFactory(LogFactory)
			       {
			       	StaticConfigPropertyTable = propertyTable,
			       	XmlMappingAssemblyNames = xmlMappingAssemblyNames,
			       };
		}

		private static string GetIpAddress()
		{
			if (HttpContext.Current != null)
			{
				return HttpContext.Current.Request.@ModelName@HostAddress;
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