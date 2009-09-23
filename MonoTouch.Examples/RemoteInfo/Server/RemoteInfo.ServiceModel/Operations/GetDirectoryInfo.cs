using System.Runtime.Serialization;

namespace RemoteInfo.ServiceModel.Operations
{
	[DataContract(Namespace = "http://schemas.servicestack.net/remoteinfo")]
	public class GetDirectoryInfo
	{
		[DataMember]
		public string ForPath { get; set; }
	}
}