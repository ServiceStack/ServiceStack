using System;
using System.Runtime.Serialization;

namespace ServiceStack.ServiceModel.Version100
{
	[DataContract(Namespace = "http://schemas.ubixar.com/types")]
	public class ResponseStatus
	{
		[DataMember]
		public string ErrorCode { get; set; }

		[DataMember]
		public string ErrorMessage { get; set; }
		
		[DataMember]
		public string StackTrace { get; set; }

		public bool IsSuccess
		{
			get
			{
				return ErrorCode == null;
			}
		}
	}
}