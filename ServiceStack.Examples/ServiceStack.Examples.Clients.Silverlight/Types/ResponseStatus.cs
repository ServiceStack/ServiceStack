using System.Runtime.Serialization;
using ServiceStack.Service;

namespace ServiceStack.Examples.ServiceInterface.Types
{
	[DataContract]
	public class ResponseStatus : IResponseStatus
	{
		[DataMember]
		public string ErrorCode { get; set; }

		[DataMember]
		public string ErrorMessage { get; set; }

		[DataMember]
		public string StackTrace { get; set; }

		public bool IsSuccess { get { return ErrorCode == null; } }
	}
}