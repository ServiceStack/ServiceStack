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
        HttpPatch = 1 << 10,
        HttpOptions = 1 << 11,
		//Future 12

		//Call Styles
		AsyncOneWay = 1 << 13,
		SyncReply = 1 << 14,

		//Different endpoints
		Soap11 = 1 << 15,
		Soap12 = 1 << 16,
		//POX
		Xml = 1 << 17,
		//Javascript
		Json = 1 << 18,
		//Jsv i.e. TypeSerializer
		Jsv = 1 << 19,
		//e.g. protobuf-net
		ProtoBuf = 1 << 20,
		//e.g. text/csv
		Csv = 1 << 21,

		Html = 1 << 22,
		Yaml = 1 << 23,
	}

}