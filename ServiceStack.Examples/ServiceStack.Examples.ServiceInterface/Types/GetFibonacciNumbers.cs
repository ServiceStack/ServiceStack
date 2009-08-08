using System.Runtime.Serialization;

namespace ServiceStack.Examples.ServiceInterface.Types
{
	/// <summary>
	/// Use Plain old DataContract's Define your 'Service Interface'
	/// 
	/// This purpose of this example is how you would implement a more advanced
	/// web service returning a slightly more 'complex object'.
	/// </summary>
	[DataContract(Namespace = "http://schemas.sericestack.net/examples/types")]
	public class GetFibonacciNumbers
	{
		[DataMember]
		public long? Skip { get; set; }

		[DataMember]
		public long? Take { get; set; }
	}

	[DataContract(Namespace = "http://schemas.sericestack.net/examples/types")]
	public class GetFibonacciNumbersResponse
	{
		[DataMember]
		public ArrayOfLong Results { get; set; }
	}
}