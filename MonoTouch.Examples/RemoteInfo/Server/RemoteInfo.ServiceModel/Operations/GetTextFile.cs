using System.Runtime.Serialization;

namespace RemoteInfo.ServiceModel.Operations
{
	[DataContract(Namespace = "http://schemas.servicestack.net/remoteinfo")]
	public class GetTextFile
	{
		[DataMember]
		public string AtPath { get; set; }
	}
}