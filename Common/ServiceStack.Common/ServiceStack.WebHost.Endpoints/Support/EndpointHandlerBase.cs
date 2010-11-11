using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.ServiceModel.Channels;
using System.Web;
using System.Xml;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Web;
using ServiceStack.Messaging;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceModel.Serialization;
using DataContractSerializer = ServiceStack.ServiceModel.Serialization.DataContractSerializer;

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

		protected Message ExecuteMessage(Message requestMsg, EndpointAttributes endpointAttributes)
		{
			string requestXml;
			using (var reader = requestMsg.GetReaderAtBodyContents())
			{
				requestXml = reader.ReadOuterXml();
			}

			var requestType = GetRequestType(requestMsg, requestXml);
			try
			{
				var request = DataContractDeserializer.Instance.Parse(requestXml, requestType);
				var response = ExecuteService(request, endpointAttributes);

				return requestMsg.Headers.Action == null 
					? Message.CreateMessage(requestMsg.Version, null, response) 
					: Message.CreateMessage(requestMsg.Version, requestType.Name + "Response", response);
			}
			catch (Exception ex)
			{
				throw new SerializationException("3) Error trying to deserialize requestType: " 
					+ requestType
					+ ", xml body: " + requestXml, ex);
			}
		}

		protected static Message GetRequestMessage(HttpContext context)
		{
			using (var sr = new StreamReader(context.Request.InputStream))
			{
				var requestXml = sr.ReadToEnd();

				var doc = new XmlDocument();
				doc.LoadXml(requestXml);

				var msg = Message.CreateMessage(new XmlNodeReader(doc), requestXml.Length,
					MessageVersion.Soap12WSAddressingAugust2004);

				return msg;
			}
		}

		protected Type GetRequestType(Message requestMsg, string xml)
		{
			var action = GetAction(requestMsg, xml);

			var operationType = EndpointHost.ServiceOperations.GetOperationType(action);
			AssertOperationExists(action, operationType);

			return operationType;
		}

		protected string GetAction(Message requestMsg, string xml)
		{
			var action = GetActionFromHttpContext();
			if (action != null) return action;

			if (requestMsg.Headers.Action != null)
			{
				return requestMsg.Headers.Action;
			}

			if (xml.StartsWith("<"))
			{
				return xml.Substring(1, xml.IndexOf(" "));
			}

			return null;
		}

		protected static string GetActionFromHttpContext()
		{
			var context = HttpContext.Current;
			return GetAction(context);
		}

		private static string GetAction(HttpContext context)
		{
			if (context != null)
			{
				var contentType = context.Request.ContentType;
				return GetOperationName(contentType);
			}

			return null;
		}

		private static string GetOperationName(string contentType)
		{
			var urlActionPos = contentType.IndexOf("action=\"");
			if (urlActionPos != -1)
			{
				var startIndex = urlActionPos + "action=\"".Length;
				var urlAction = contentType.Substring(
					startIndex,
					contentType.IndexOf('"', startIndex) - startIndex);

				var parts = urlAction.Split('/');
				var operationName = parts.Last();
				return operationName;
			}

			return null;
		}

		public string GetSoapContentType(HttpContext context)
		{
			var requestOperationName = GetAction(context);
			return requestOperationName != null
			       	? context.Request.ContentType.Replace(requestOperationName, requestOperationName + "Response")
			       	: ContentType.Soap12;
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
		protected static string ExecuteXmlService(string xmlRequest, Type requestType, EndpointAttributes endpointAttributes)
		{
			return EndpointHost.ExecuteXmlService(xmlRequest, requestType, endpointAttributes);
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