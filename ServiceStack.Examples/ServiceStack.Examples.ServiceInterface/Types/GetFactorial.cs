using System.Runtime.Serialization;

namespace ServiceStack.Examples.ServiceInterface.Types
{
	/// <summary>
	/// Use Plain old DataContract's Define your 'Service Interface'.
	/// 
	/// The purpose of this example is to show the minimum number and detail of classes 
	/// required in order to implement a simple service.
	/// </summary>
	[DataContract(Namespace = ExampleConfig.DefaultNamespace)]
	public class GetFactorial
	{
		[DataMember]
		public long ForNumber { get; set; }
	}

	[DataContract(Namespace = ExampleConfig.DefaultNamespace)]
	public class GetFactorialResponse
	{
		[DataMember]
		public long Result { get; set; }
	}
}