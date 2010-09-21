using System.Runtime.Serialization;

namespace ServiceStack.Examples.ServiceInterface.Types
{
	/// <summary>
	/// Use Plain old DataContract's Define your 'Service Interface'
	/// </summary>
	[DataContract]
	public class GetFibonacciNumbers
	{
		[DataMember]
		public long? Skip { get; set; }

		[DataMember]
		public long? Take { get; set; }
	}

	[DataContract]
	public class GetFibonacciNumbersResponse
	{
		[DataMember]
		public ArrayOfLong Results { get; set; }
	}
}