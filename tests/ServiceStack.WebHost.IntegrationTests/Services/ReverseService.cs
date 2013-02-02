using System;
using System.Runtime.Serialization;
using ServiceStack.ServiceInterface;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
	[DataContract]
	public class Reverse
	{
		[DataMember]
		public string Value { get; set; }
	}

	[DataContract]
	public class ReverseResponse
	{
		[DataMember]
		public string Result { get; set; }
	}

	public class ReverseService 
		: ServiceInterface.Service
	{
		public object Any(Reverse request)
		{
			return new ReverseResponse { Result = Execute(request.Value) };
		}

		public static string Execute(string value)
		{
			var valueBytes = value.ToCharArray();
			Array.Reverse(valueBytes);
			return new string(valueBytes);
		}
	}

}