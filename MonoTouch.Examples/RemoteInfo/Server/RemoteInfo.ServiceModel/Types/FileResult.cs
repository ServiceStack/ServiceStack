using System.Runtime.Serialization;

namespace RemoteInfo.ServiceModel.Types
{
	[DataContract(Namespace = "http://schemas.servicestack.net/remoteinfo")]
	public class FileResult
	{
		[DataMember]
		public string Name { get; set; }

		[DataMember]
		public string Extension { get; set; }

		[DataMember]
		public long FileSizeBytes { get; set; }

		[DataMember]
		public bool IsTextFile { get; set; }
	}
}