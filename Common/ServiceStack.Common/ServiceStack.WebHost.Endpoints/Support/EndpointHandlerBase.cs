using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Web;
using ServiceStack.Common.Extensions;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Support
{
	public class EndpointHandlerBase
	{
		private readonly Dictionary<byte[], byte[]> networkInterfaceIpv4Addresses = new Dictionary<byte[], byte[]>();
		private readonly byte[][] networkInterfaceIpv6Addresses;

		public EndpointHandlerBase()
		{
			IPAddressExtensions.GetAllNetworkInterfaceIpv4Addresses().ForEach((x, y) => networkInterfaceIpv4Addresses[x.GetAddressBytes()] = y.GetAddressBytes());

			this.networkInterfaceIpv6Addresses = IPAddressExtensions.GetAllNetworkInterfaceIpv6Addresses().ConvertAll(x => x.GetAddressBytes()).ToArray();
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

			portRestrictions |= request.RequestType == "GET" ? EndpointAttributes.HttpGet : EndpointAttributes.HttpPost;
			portRestrictions |= request.IsSecureConnection ? EndpointAttributes.Secure : EndpointAttributes.InSecure;

			var ipAddress = IPAddress.Parse(request.UserHostAddress);

			portRestrictions |= GetIpAddressEndpointAttributes(ipAddress);

			return portRestrictions;
		}

		private EndpointAttributes GetIpAddressEndpointAttributes(IPAddress ipAddress)
		{
			if (IPAddress.IsLoopback(ipAddress))
			{
				return EndpointAttributes.Internal | EndpointAttributes.Localhost;
			}
			if (IsInLocalSubnet(ipAddress))
			{
				return EndpointAttributes.Internal | EndpointAttributes.LocalSubnet;
			}
			return EndpointAttributes.External;
		}

		private bool IsInLocalSubnet(IPAddress ipAddress)
		{
			var ipAddressBytes = ipAddress.GetAddressBytes();
			switch (ipAddress.AddressFamily)
			{
				case AddressFamily.InterNetwork:
					foreach (var localIpv4AddressAndMask in networkInterfaceIpv4Addresses)
					{
						if (ipAddressBytes.IsInSameIpv4Subnet(localIpv4AddressAndMask.Key, localIpv4AddressAndMask.Value))
						{
							return true;
						}
					}
					break;

				case AddressFamily.InterNetworkV6:
					foreach (var localIpv6Address in networkInterfaceIpv6Addresses)
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