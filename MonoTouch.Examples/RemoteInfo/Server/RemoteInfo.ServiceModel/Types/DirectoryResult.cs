using System.Runtime.Serialization;

namespace RemoteInfo.ServiceModel.Types
{
	[DataContract(Namespace = "http://schemas.servicestack.net/remoteinfo")]
	public class DirectoryResult
	{
		[DataMember]
		public string Name { get; set; }

		[DataMember]
		public int FileCount { get; set; }
	}
}