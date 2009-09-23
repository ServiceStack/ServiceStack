using System.Collections.Generic;
using System.Runtime.Serialization;
using RemoteInfo.ServiceModel.Types;

namespace RemoteInfo.ServiceModel.Operations
{
	[DataContract(Namespace = "http://schemas.servicestack.net/remoteinfo")]
	public class GetDirectoryInfoResponse
	{
		public GetDirectoryInfoResponse()
		{
			Directories = new List<DirectoryResult>();
			Files = new List<FileResult>();
		}

		[DataMember]
		public List<DirectoryResult> Directories { get; set; }

		[DataMember]
		public List<FileResult> Files { get; set; }
	}
}