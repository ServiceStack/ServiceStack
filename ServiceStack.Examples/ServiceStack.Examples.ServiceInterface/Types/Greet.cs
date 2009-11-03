using System.Runtime.Serialization;

namespace ServiceStack.Examples.ServiceInterface.Types
{
	[DataContract]
	public class Greet
	{
		[DataMember] public string Name { get; set; }
	}

	[DataContract]
	public class GreetResponse
	{
		[DataMember] public string Result { get; set; }
	}
}