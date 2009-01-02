using System;
using System.Web;
using ServiceStack.CacheAccess;
using ServiceStack.Configuration;
using ServiceStack.LogicFacade;
using ServiceStack.Logging;

namespace ServiceStack.ServiceInterface
{

	public class OperationContext : IOperationContext
	{
		public static OperationContext Instance { get; private set; }

		public static void SetInstanceContext(OperationContext operationContext)
		{
			if (OperationContext.Instance != null)
			{
				throw new NotSupportedException("Cannot set the singleton instance once it has already been set");
			}
			OperationContext.Instance = operationContext;
		}

		[ThreadStatic]
		public static OperationContext current;
		public static OperationContext Current
		{
			get { return current; }
			set { current = value; }
		}

		public ILogFactory LogFactory { get; set; }

		public ICacheClient Cache { get; set; }

		public IResourceManager Resources { get; set; }

		public IFactoryProvider Factory { get; set; }

		public string IpAddress
		{
			get
			{
				if (HttpContext.Current != null)
				{
					return HttpContext.Current.Request.UserHostAddress;
				}

				var context = System.ServiceModel.OperationContext.Current;
				if (context == null) return null;
				var prop = context.IncomingMessageProperties;
				if (context.IncomingMessageProperties.ContainsKey(System.ServiceModel.Channels.RemoteEndpointMessageProperty.Name))
				{
					var endpoint = prop[System.ServiceModel.Channels.RemoteEndpointMessageProperty.Name]
						as System.ServiceModel.Channels.RemoteEndpointMessageProperty;
					if (endpoint != null)
					{
						return endpoint.Address;
					}
				}
				return null;
			}
		}
	}
}