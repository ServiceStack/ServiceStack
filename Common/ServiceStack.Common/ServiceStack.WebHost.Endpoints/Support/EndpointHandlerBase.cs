using System;
using System.Collections.Generic;
using System.Net;
using System.Web;
using ServiceStack.Common.Extensions;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Support
{
	public class EndpointHandlerBase
	{
		private readonly Dictionary<IPAddress, IPAddress> networkInterfaceIpv4Addresses;

		public EndpointHandlerBase()
		{
			this.networkInterfaceIpv4Addresses = IPAddressExtensions.GetAllNetworkInterfaceIpv4Addresses();
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

			if (IPAddress.IsLoopback(ipAddress))
			{
				portRestrictions |= EndpointAttributes.Internal | EndpointAttributes.Localhost;
			}
			else if (IsInLocalSubnet(ipAddress))
			{
				portRestrictions |= EndpointAttributes.Internal | EndpointAttributes.LocalSubnet;
			}
			else
			{
				portRestrictions |= EndpointAttributes.External;
			}
			
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

			if (IPAddress.IsLoopback(ipAddress))
			{
				portRestrictions |= EndpointAttributes.Internal | EndpointAttributes.Localhost;
			}
			else if (IsInLocalSubnet(ipAddress))
			{
				portRestrictions |= EndpointAttributes.Internal | EndpointAttributes.LocalSubnet;
			}
			else
			{
				portRestrictions |= EndpointAttributes.External;
			}

			return portRestrictions;
		}

		private bool IsInLocalSubnet(IPAddress ipAddress)
		{
			foreach (var ipv4AddressAndMask in networkInterfaceIpv4Addresses)
			{
				if (ipAddress.IsInSameSubnet(ipv4AddressAndMask.Key, ipv4AddressAndMask.Value))
				{
					return true;
				}
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