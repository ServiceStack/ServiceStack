
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RemoteInfo.ServiceModel.Operations
{
	[DataContract(Namespace = "http://schemas.servicestack.net/remoteinfo")]
	public class GetTextFile
	{
		[DataMember]
		public string AtPath { get; set; }
	}
	
	
	[DataContract(Namespace = "http://schemas.servicestack.net/remoteinfo")]
	public class GetTextFileResponse
	{
		[DataMember]
		public string Contents { get; set; }

		[DataMember]
		public DateTime CreatedDate { get; set; }

		[DataMember]
		public DateTime LastModifiedDate { get; set; }
	}	
}
