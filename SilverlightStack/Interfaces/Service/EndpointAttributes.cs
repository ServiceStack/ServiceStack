using System;

namespace ServiceStack.Service
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
		
		//HTTP request type
		HttpGet = 256,
		HttpPost = 512,
		
		//Whether it came from an Internal or External address
		Internal = 1024,
		External = 2048,
	}
}