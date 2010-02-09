using System.Runtime.Serialization;
using ServiceStack.DataAnnotations;

namespace ServiceStack.Examples.ServiceInterface.Types
{
	[DataContract(Namespace = "http://schemas.sericestack.net/examples/types")]
	public class User
	{
		[AutoIncrement]
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