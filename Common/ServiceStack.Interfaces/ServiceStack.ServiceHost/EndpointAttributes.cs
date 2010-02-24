using System;

namespace ServiceStack.ServiceHost
{
	[Flags]
	public enum EndpointAttributes
	{
		None = 0,

		All = AllNetworkAccessTypes | AllSecurityModes | AllHttpMethods | AllCallStyles | AllEndpointTypes,
		AllNetworkAccessTypes = External | Localhost | LocalSubnet,
		AllSecurityModes = Secure | InSecure,
		AllHttpMethods = HttpHead | HttpGet | HttpPost | HttpPut | HttpDelete,
		AllCallStyles = AsyncOneWay | SyncReply,
		AllEndpointTypes = Soap11 | Soap12 | Xml | Json | Jsv | ProtoBuf | Csv,
		
		InternalNetworkAccess = Localhost | LocalSubnet,

		//Whether it came from an Internal or External address
		Localhost = 1 << 0,
		LocalSubnet = 1 << 1,
		External = 1 << 2,

		//Called over a secure or insecure channel
		Secure = 1 << 3,
		InSecure = 1 << 4,

		//HTTP request type
		HttpHead = 1 << 5,
		HttpGet = 1 << 6,
		HttpPost = 1 << 7,
		HttpPut = 1 << 8,
		HttpDelete = 1 << 9,

		//Call Styles
		AsyncOneWay = 1 << 10,
		SyncReply = 1 << 11,

		//Different endpoints
		Soap11 = 1 << 12,
		Soap12 = 1 << 13,
		//POX
		Xml = 1 << 14,
		//Javascript
		Json = 1 << 15,
		//Jsv i.e. TypeSerializer
		Jsv = 1 << 16,
		//e.g. protobuf-net
		ProtoBuf = 1 << 17,
		//e.g. text/csv
		Csv = 1 << 18,
	}

}