using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Web;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Support
{
	public class EndpointHandlerBase
	{
		private static readonly Dictionary<byte[], byte[]> NetworkInterfaceIpv4Addresses = new Dictionary<byte[], byte[]>();
		private static readonly byte[][] NetworkInterfaceIpv6Addresses;

		static EndpointHandlerBase()
		{
			IPAddressExtensions.GetAllNetworkInterfaceIpv4Addresses().ForEach((x, y) => NetworkInterfaceIpv4Addresses[x.GetAddressBytes()] = y.GetAddressBytes());

			NetworkInterfaceIpv6Addresses = IPAddressExtensions.GetAllNetworkInterfaceIpv6Addresses().ConvertAll(x => x.GetAddressBytes()).ToArray();
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

		protected static object ExecuteService(object request, EndpointAttributes endpointAttributes)
		{
			return EndpointHost.ExecuteService(request, endpointAttributes);
		}

		//Execute SOAP requests
		protected static string ExecuteXmlService(string xmlRequest, EndpointAttributes endpointAttributes)
		{
			return EndpointHost.ExecuteXmlService(xmlRequest, endpointAttributes);
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

		public EndpointAttributes GetEndpointAttributes(HttpRequest request)
		{
			var portRestrictions = EndpointAttributes.None;

			portRestrictions |= HttpMethods.GetEndpointAttribute(request.RequestType);
			portRestrictions |= request.IsSecureConnection ? EndpointAttributes.Secure : EndpointAttributes.InSecure;

			var ipAddress = IPAddress.Parse(request.UserHostAddress);

			portRestrictions |= GetIpAddressEndpointAttributes(ipAddress);

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