using System.Runtime.Serialization;

namespace ServiceStack.Examples.ServiceInterface.Types
{
	/// <summary>
	/// Use Plain old DataContract's Define your 'Service Interface'
	/// </summary>
	[DataContract]
	public class GetFactorial
	{
		[DataMember]
		public long ForNumber { get; set; }
	}

	[DataContract]
	public class GetFactorialResponse
	{
		[DataMember]
		public long Result { get; set; }
	}
}