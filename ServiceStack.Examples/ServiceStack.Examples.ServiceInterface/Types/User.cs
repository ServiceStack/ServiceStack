using System.Runtime.Serialization;

namespace ServiceStack.Examples.ServiceInterface.Types
{
	[DataContract(Namespace = "http://schemas.sericestack.net/examples/types")]
	public class User
	{
		[DataMember]
		public long Id { get; set; }

		[DataMember]
		public string UserName { get; set; }

		[DataMember]
		public string Email { get; set; }

		[DataMember]
		public string Password { get; set; }
	}
}