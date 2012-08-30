using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Web;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Extensions;

namespace ServiceStack.WebHost.Endpoints.Support
{
	public abstract class EndpointHandlerBase
		: IServiceStackHttpHandler, IHttpHandler
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(EndpointHandlerBase));
		private static readonly Dictionary<byte[], byte[]> NetworkInterfaceIpv4Addresses = new Dictionary<byte[], byte[]>();
		private static readonly byte[][] NetworkInterfaceIpv6Addresses = new byte[0][];

		public string RequestName { get; set; }

		static EndpointHandlerBase()
		{
			try
			{
				IPAddressExtensions.GetAllNetworkInterfaceIpv4Addresses().ForEach((x, y) => NetworkInterfaceIpv4Addresses[x.GetAddressBytes()] = y.GetAddressBytes());

				NetworkInterfaceIpv6Addresses = IPAddressExtensions.GetAllNetworkInterfaceIpv6Addresses().ConvertAll(x => x.GetAddressBytes()).ToArray();
			}
			catch (Exception ex)
			{
				Log.Warn("Failed to retrieve IP Addresses, some security restriction features may not work: " + ex.Message, ex);
			}
		}

		public EndpointAttributes HandlerAttributes { get; set; }

		public bool IsReusable
		{
			get { return false; }
		}

		public abstract object CreateRequest(IHttpRequest request, string operationName);
		public abstract object GetResponse(IHttpRequest httpReq, IHttpResponse httpRes, object request);

		public virtual void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
		{
			throw new NotImplementedException();
		}

	    public static object DeserializeHttpRequest(Type operationType, IHttpRequest httpReq, string contentType)
		{
			var httpMethod = httpReq.HttpMethod;
			var queryString = httpReq.QueryString;

			if (httpMethod == HttpMethods.Get || httpMethod == HttpMethods.Delete || httpMethod == HttpMethods.Options)
			{
				try
				{
					return KeyValueDataContractDeserializer.Instance.Parse(queryString, operationType);
				}
				catch (Exception ex)
				{
					var msg = "Could not deserialize '{0}' request using KeyValueDataContractDeserializer: '{1}'.\nError: '{2}'"
						.Fmt(operationType, queryString, ex);
					throw new SerializationException(msg);
				}
			}

			var isFormData = httpReq.HasAnyOfContentTypes(ContentType.FormUrlEncoded, ContentType.MultiPartFormData);
			if (isFormData)
			{
				try
				{
					return KeyValueDataContractDeserializer.Instance.Parse(httpReq.FormData, operationType);
				}
				catch (Exception ex)
				{
					throw new SerializationException("Error deserializing FormData: " + httpReq.FormData, ex);
				}
			}

			var request = CreateContentTypeRequest(httpReq, operationType, contentType);
			return request;
		}

		protected static object CreateContentTypeRequest(IHttpRequest httpReq, Type requestType, string contentType)
		{
			try
			{
				if (!string.IsNullOrEmpty(contentType) && httpReq.ContentLength > 0)
				{
					var deserializer = EndpointHost.AppHost.ContentTypeFilters.GetStreamDeserializer(contentType);
					if (deserializer != null)
					{
						return deserializer(requestType, httpReq.InputStream);
					}
				}
			}
			catch (Exception ex)
			{
				var msg = "Could not deserialize '{0}' request using {1}'\nError: {2}"
					.Fmt(contentType, requestType, ex);
				throw new SerializationException(msg);
			}
			return null;
		}

		protected static object GetCustomRequestFromBinder(IHttpRequest httpReq, Type requestType)
		{
			Func<IHttpRequest, object> requestFactoryFn;
			(ServiceManager ?? EndpointHost.ServiceManager).ServiceController.RequestTypeFactoryMap.TryGetValue(
				requestType, out requestFactoryFn);

			return requestFactoryFn != null ? requestFactoryFn(httpReq) : null;
		}

		protected static bool DefaultHandledRequest(HttpListenerContext context)
		{
			if (context.Request.HttpMethod == HttpMethods.Options)
			{
                context.Response.ApplyGlobalResponseHeaders();
				return true;
			}
			return false;
		}

		protected static bool DefaultHandledRequest(HttpContext context)
		{
			if (context.Request.HttpMethod == HttpMethods.Options)
			{
				context.Response.ApplyGlobalResponseHeaders();
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

		protected static object ExecuteService(object request, EndpointAttributes endpointAttributes,
			IHttpRequest httpReq, IHttpResponse httpRes)
		{
			return EndpointHost.ExecuteService(request, endpointAttributes, httpReq, httpRes);
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
				var isIpv4Address = request.UserHostAddress.IndexOf('.') != -1 &&
					request.UserHostAddress.IndexOf("::", StringComparison.InvariantCulture) == -1;
				var pieces = request.UserHostAddress.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
				var ipAddressNumber = isIpv4Address
					? pieces[0]
					: (request.UserHostAddress.Contains("]:")
						? request.UserHostAddress.Substring(0, request.UserHostAddress.LastIndexOf(':'))
						: request.UserHostAddress);

                try
                {
                    ipAddressNumber = ipAddressNumber.SplitOnFirst(',')[0];
                    var ipAddress = ipAddressNumber.StartsWith("::1")
                        ? IPAddress.IPv6Loopback
                        : IPAddress.Parse(ipAddressNumber);
                    portRestrictions |= GetIpAddressEndpointAttributes(ipAddress);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException("Could not parse Ipv{0} Address: {1} / {2}"
                        .Fmt((isIpv4Address ? 4 : 6), request.UserHostAddress, ipAddressNumber), ex);
                }
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

		protected void HandleException(IHttpRequest httpReq, IHttpResponse httpRes, string operationName, Exception ex)
		{
            var errorMessage = string.Format("Error occured while Processing Request: {0}", ex.Message);
            Log.Error(errorMessage, ex);

            try {
                EndpointHost.ExceptionHandler(httpReq, httpRes, operationName, ex);
            } catch (Exception writeErrorEx) {
                //Exception in writing to response should not hide the original exception
                Log.Info("Failed to write error to response: {0}", writeErrorEx);
                //rethrow the original exception
                throw ex;
            } finally {
                httpRes.EndServiceStackRequest(skipHeaders: true);
            }
		}

	}
}