using System;

namespace ServiceStack.ServiceHost
{
	[Flags]
	public enum EndpointAttributes
	{
		None = 0,
		
		//Whether 
		AsyncOneWay = 1,
		SyncReply = 2,
		
		//Called over a secure or insecure channel
		Secure = 4,
		InSecure = 8,
		
		//Different endpoints
		Soap11 = 16,
		Soap12 = 32,
		//POX
		Xml = 64,
		//Javascript
		Json = 128,
		
		//TypeSerializer, Csv
		Text = 256,
		
		//protobuf-net
		Binary = 512,

		//HTTP request type
		HttpGet = 1024,
		HttpPost = 2048,
		
		//Whether it came from an Internal or External address
		Localhost = 4096,
		LocalSubnet = 16384,

		//will be set if its either Localhost or LocalSubnet
		Internal = 32768,

		External = 65536,
	}
}