using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Web;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.WebHost.Endpoints.Extensions;

namespace ServiceStack.WebHost.Endpoints.Support
{
	public abstract class EndpointHandlerBase 
		: IServiceStackHttpHandler, IHttpHandler
	{
		private static readonly Dictionary<byte[], byte[]> NetworkInterfaceIpv4Addresses = new Dictionary<byte[], byte[]>();
		private static readonly byte[][] NetworkInterfaceIpv6Addresses;

		public string RequestName { get; set; }

		static EndpointHandlerBase()
		{
			IPAddressExtensions.GetAllNetworkInterfaceIpv4Addresses().ForEach((x, y) => NetworkInterfaceIpv4Addresses[x.GetAddressBytes()] = y.GetAddressBytes());

			NetworkInterfaceIpv6Addresses = IPAddressExtensions.GetAllNetworkInterfaceIpv6Addresses().ConvertAll(x => x.GetAddressBytes()).ToArray();
		}

		public EndpointAttributes HandlerAttributes { get; set; }

		public bool IsReusable
		{
			get { return false; }
		}

		public abstract object CreateRequest(IHttpRequest request, string operationName);
		public abstract object GetResponse(IHttpRequest httpReq, object request);

		public virtual void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
		{
			throw new NotImplementedException();
		}

		public IContentTypeFilter ContentTypeFilter { get; set; }

		public IContentTypeFilter GetContentFilters()
		{
			return ContentTypeFilter != null
				? this.ContentTypeFilter
				: EndpointHost.Config.ContentTypeFilter;
		}

		protected object DeserializeContentType(Type operationType, IHttpRequest httpReq, string contentType)
		{
			var httpMethod = httpReq.HttpMethod;
			var queryString = httpReq.QueryString;

			if (httpMethod == "GET" || httpMethod == "OPTIONS")
			{
				try
				{
					return KeyValueDataContractDeserializer.Instance.Parse(queryString, operationType);
				}
				catch (Exception ex)
				{
					var log = EndpointHost.Config.LogFactory.GetLogger(typeof(EndpointHandlerBase));
					log.ErrorFormat("Could not deserialize '{0}' request using KeyValueDataContractDeserializer: '{1}'.\nError: '{2}'",
									operationType, queryString, ex);
					throw;
				}
			}

			var isFormData = httpReq.HasAnyOfContentTypes(ContentType.FormUrlEncoded, ContentType.MultiPartFormData);
			if (isFormData)
			{
				return KeyValueDataContractDeserializer.Instance.Parse(httpReq.FormData, operationType);
			}

			try
			{
				var deserializer = GetContentFilters().GetStreamDeserializer(contentType);
				return deserializer(operationType, httpReq.InputStream);
			}
			catch (Exception ex)
			{
				var log = EndpointHost.Config.LogFactory.GetLogger(typeof(EndpointHandlerBase));
				log.ErrorFormat("Could not deserialize '{0}' request using {1}'\nError: {2}",
								contentType, operationType, ex);
				throw;
			}
		}

		protected static bool DefaultHandledRequest(HttpListenerContext context)
		{
			if (context.Request.HttpMethod == HttpMethods.Options)
			{
				foreach (var globalResponseHeader in EndpointHost.Config.GlobalResponseHeaders)
				{
					context.Response.AddHeader(globalResponseHeader.Key, globalResponseHeader.Value);
				}

				return true;
			}

			return false;
		}

		protected static bool DefaultHandledRequest(HttpContext context)
		{
			if (context.Request.HttpMethod == HttpMethods.Options)
			{
				foreach (var globalResponseHeader in EndpointHost.Config.GlobalResponseHeaders)
				{
					context.Response.AddHeader(globalResponseHeader.Key, globalResponseHeader.Value);
				}

				return true;
			}

			return false;
		}

		public virtual void ProcessRequest(HttpContext context)
		{
			var operationName = this.RequestName ?? context.Request.GetOperationName();

			if (string.IsNullOrEmpty(operationName)) return;

			if (DefaultHandledRequest(context)) return;

			ProcessRequest(
				new HttpRequestWrapper(operationName, context.Request),
				new HttpResponseWrapper(context.Response),
				operationName);
		}

		public virtual void ProcessRequest(HttpListenerContext context)
		{
			var operationName = this.RequestName ?? context.Request.GetOperationName();

			if (string.IsNullOrEmpty(operationName)) return;

			if (DefaultHandledRequest(context)) return;

			ProcessRequest(
				new HttpListenerRequestWrapper(operationName, context.Request),
				new HttpListenerResponseWrapper(context.Response),
				operationName);
		}

		public static ServiceManager ServiceManager { get; set; }

		public static Type GetOperationType(string operationName)
		{
			return ServiceManager != null
				? ServiceManager.ServiceOperations.GetOperationType(operationName)
				: EndpointHost.ServiceOperations.GetOperationType(operationName);
		}

		protected static object ExecuteService(object request, EndpointAttributes endpointAttributes, IHttpRequest httpReq)
		{
			return EndpointHost.ExecuteService(request, endpointAttributes, httpReq);
		}

		public EndpointAttributes GetEndpointAttributes(System.ServiceModel.OperationContext operationContext)
		{
			if (!EndpointHost.Config.EnableAccessRestrictions) return default(EndpointAttributes);

			var portRestrictions = default(EndpointAttributes);
			var ipAddress = GetIpAddress(operationContext);

			portRestrictions |= GetIpAddressEndpointAttributes(ipAddress);

			//TODO: work out if the request was over a secure channel			
			//portRestrictions |= request.IsSecureConnection ? PortRestriction.Secure : PortRestriction.InSecure;

			return portRestrictions;
		}

		public static IPAddress GetIpAddress(System.ServiceModel.OperationContext context)
		{
#if !MONO
			var prop = context.IncomingMessageProperties;
			if (context.IncomingMessageProperties.ContainsKey(System.ServiceModel.Channels.RemoteEndpointMessageProperty.Name))
			{
				var endpoint = prop[System.ServiceModel.Channels.RemoteEndpointMessageProperty.Name]
					as System.ServiceModel.Channels.RemoteEndpointMessageProperty;
				if (endpoint != null)
				{
					return IPAddress.Parse(endpoint.Address);
				}
			}
#endif
			return null;
		}

		public EndpointAttributes GetEndpointAttributes(IHttpRequest request)
		{
			var portRestrictions = EndpointAttributes.None;

			portRestrictions |= HttpMethods.GetEndpointAttribute(request.HttpMethod);
			portRestrictions |= request.IsSecureConnection ? EndpointAttributes.Secure : EndpointAttributes.InSecure;

			if (request.UserHostAddress != null)
			{
				var isIpv4Address = request.UserHostAddress.IndexOf('.') != -1;
				var ipAddressNumber = isIpv4Address
                    ? request.UserHostAddress.Split(':')[0]
					: request.UserHostAddress;

				var ipAddress = IPAddress.Parse(ipAddressNumber);

				portRestrictions |= GetIpAddressEndpointAttributes(ipAddress);
			}

			return portRestrictions;
		}

		private static EndpointAttributes GetIpAddressEndpointAttributes(IPAddress ipAddress)
		{
			if (IPAddress.IsLoopback(ipAddress))
				return EndpointAttributes.Localhost;

			return IsInLocalSubnet(ipAddress)
				? EndpointAttributes.LocalSubnet
				: EndpointAttributes.External;
		}

		private static bool IsInLocalSubnet(IPAddress ipAddress)
		{
			var ipAddressBytes = ipAddress.GetAddressBytes();
			switch (ipAddress.AddressFamily)
			{
				case AddressFamily.InterNetwork:
					foreach (var localIpv4AddressAndMask in NetworkInterfaceIpv4Addresses)
					{
						if (ipAddressBytes.IsInSameIpv4Subnet(localIpv4AddressAndMask.Key, localIpv4AddressAndMask.Value))
						{
							return true;
						}
					}
					break;

				case AddressFamily.InterNetworkV6:
					foreach (var localIpv6Address in NetworkInterfaceIpv6Addresses)
					{
						if (ipAddressBytes.IsInSameIpv6Subnet(localIpv6Address))
						{
							return true;
						}
					}
					break;
			}

			return false;
		}

		protected static void AssertOperationExists(string operationName, Type type)
		{
			if (type == null)
			{
				throw new NotImplementedException(
					string.Format("The operation '{0}' does not exist for this service", operationName));
			}
		}
	}
}