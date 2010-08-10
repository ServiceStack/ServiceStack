using System.Runtime.Serialization;
using ServiceStack.Examples.ServiceInterface;

namespace ServiceStack.Examples.ServiceModel.Operations
{
	[DataContract(Namespace = ExampleConfig.DefaultNamespace)]
	public class Greet
	{
		[DataMember] public string Name { get; set; }
	}

	[DataContract(Namespace = ExampleConfig.DefaultNamespace)]
	public class GreetResponse
	{
		[DataMember] public string Result { get; set; }
	}
}