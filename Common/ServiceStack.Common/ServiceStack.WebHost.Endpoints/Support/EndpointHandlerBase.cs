using System;
using System.Net;
using System.Web;
using ServiceStack.Logging;
using ServiceStack.Service;

namespace ServiceStack.WebHost.Endpoints.Support
{
	public class EndpointHandlerBase
	{
		protected static object ExecuteService(object request, EndpointAttributes endpointAttributes)
		{			
			return EndpointHost.ExecuteService(request, endpointAttributes);
		}

		//Execute SOAP requests
		protected static string ExecuteXmlService(string xmlRequest, EndpointAttributes endpointAttributes)
		{
			return EndpointHost.ExecuteXmlService(xmlRequest, endpointAttributes);
		}

		public static bool IsPrivateNetworkAddress(IPAddress address)
		{
			var isLoopback = IPAddress.IsLoopback(address);

			//Log.DebugFormat("Request IPAddress: '{0}', loopback = '{1}'", address, isLoopback);

			if (isLoopback)
			{
				return true;
			}

			var addressBytes = address.GetAddressBytes();

			if (addressBytes.Length == 16)
			{
				if (addressBytes[0] == 0xfe && addressBytes[1] == 0x80)
				{
					return true;
				}
			}
			else if (addressBytes.Length != 4)
			{
				throw new NotSupportedException("IPV6 addresses not supported: " + address);
			}

			var ipv4Address = (addressBytes[0] << 24 | addressBytes[1] << 16 | addressBytes[2] << 8 | addressBytes[3] << 0);

			switch (addressBytes[0])
			{
				case 10:
					// 10.0.0.0/8
					return true;
				case 172:
					// 172.0.0.0/12
					return (ipv4Address & 0xfff00000) != 0;
				case 192:
					// 192.0.0.0/16
					return (ipv4Address & 0xffff0000) != 0;
				default:
					return false;
			}
		}

		public static EndpointAttributes GetEndpointAttributes(System.ServiceModel.OperationContext operationContext)
		{
			if (!EndpointHost.Config.EnablePortRestrictions) return default(EndpointAttributes);

			var portRestrictions = default(EndpointAttributes);
			var ipAddress = GetIpAddress(operationContext);

			portRestrictions |= IsPrivateNetworkAddress(ipAddress) ? EndpointAttributes.Internal : EndpointAttributes.External;
			//TODO: work out if the request was over a secure channel
			//portRestrictions |= request.IsSecureConnection ? PortRestriction.Secure : PortRestriction.InSecure;
			
			return portRestrictions;
		}

		public static IPAddress GetIpAddress(System.ServiceModel.OperationContext context)
		{
//			var prop = context.IncomingMessageProperties;
//			if (context.IncomingMessageProperties.ContainsKey(System.ServiceModel.Channels.RemoteEndpointMessageProperty.Name))
//			{
//				var endpoint = prop[System.ServiceModel.Channels.RemoteEndpointMessageProperty.Name]
//					as System.ServiceModel.Channels.RemoteEndpointMessageProperty;
//				if (endpoint != null)
//				{
//					return IPAddress.Parse(endpoint.Address);
//				}
//			}
			return null;
		}

		public static EndpointAttributes GetEndpointAttributes(HttpRequest request)
		{
			if (!EndpointHost.Config.EnablePortRestrictions) return default(EndpointAttributes);

			var portRestrictions = default(EndpointAttributes);

			portRestrictions |= request.RequestType == "GET" ? EndpointAttributes.HttpGet : EndpointAttributes.HttpPost;
			portRestrictions |= request.IsSecureConnection ? EndpointAttributes.Secure : EndpointAttributes.InSecure;

			var ipAddress = IPAddress.Parse(request.UserHostAddress);

			portRestrictions |= IsPrivateNetworkAddress(ipAddress) ? EndpointAttributes.Internal : EndpointAttributes.External;

			return portRestrictions;
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