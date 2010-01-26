using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using ServiceStack.IntegrationTests.ServiceModel;
using ServiceStack.ServiceHost;

namespace ServiceStack.IntegrationTests.ServiceInterface
{
	public class RequestInfoService
		: IService<RequestInfo>, IRequiresRequestContext
	{
		public IRequestContext RequestContext { get; set; }

		public object Execute(RequestInfo request)
		{
			var response = new RequestInfoResponse {
				EnpointAttributes = RequestContext.EndpointAttributes.ToString().Split(',').ToList().ConvertAll(x => x.Trim()),
				IpAddress = RequestContext.IpAddress,
				NetworkLog = GetNetworkLog(),
				NetworkAttributes = GetIpv4Addresses(),
			};

			var requestAttr = RequestContext.RequestAttributes;
			if (requestAttr.AcceptsDeflate)
				response.RequestAttributes.Add(requestAttr.AcceptsDeflate.ToString());

			if (requestAttr.AcceptsGzip)
				response.RequestAttributes.Add(requestAttr.AcceptsGzip.ToString());

			return response;
		}

		public Dictionary<string, string> GetIpv4Addresses()
		{
			var map = new Dictionary<string, string>();
			foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
			{
				foreach (var uipi in ni.GetIPProperties().UnicastAddresses)
				{
					if (uipi.Address.AddressFamily == AddressFamily.InterNetwork)
					{
						if (uipi.IPv4Mask == null)
						{
							continue;
						}

						map[uipi.Address.ToString()] = uipi.IPv4Mask.ToString();
					}
				}
			}
			return map;
		}

		public string GetNetworkLog()
		{
			var sb = new StringBuilder();
			foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
			{
				sb.AppendLine(ni.Name);
				sb.AppendFormat("Operational? {0}\n", ni.OperationalStatus == OperationalStatus.Up);
				sb.AppendFormat("MAC: {0}\n", ni.GetPhysicalAddress());
				sb.AppendLine("Gateways:");
				foreach (var gipi in ni.GetIPProperties().GatewayAddresses)
				{
					sb.AppendFormat("\t{0}\n", gipi.Address);
				}
				sb.AppendLine("IP Addresses:");
				foreach (var uipi in ni.GetIPProperties().UnicastAddresses)
				{					
					sb.AppendFormat("\t{0} / {1} [{2}, {3}, {4}]\n", 
						uipi.Address, uipi.IPv4Mask,
						uipi.Address.AddressFamily,
						uipi.PrefixOrigin, uipi.SuffixOrigin);
				}

				sb.AppendLine();
			}
			
			return sb.ToString();
		}

	}
}