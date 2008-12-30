using System.Runtime.Serialization;

namespace ServiceStack.ServiceModel.Tests.DataContracts
{
	[DataContract(Namespace = "http://schemas.servicestack.net/types/")]
	public class ResponseError
	{
		[DataMember]
		public string ErrorCode { get; set; }
		[DataMember]
		public string FieldName { get; set; }
		[DataMember]
		public string Message { get; set; }
	}
}