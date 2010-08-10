using System;
using System.Runtime.Serialization;
using ServiceStack.DataAnnotations;
using ServiceStack.Examples.ServiceInterface;

namespace ServiceStack.Examples.ServiceModel.Types
{
	[DataContract(Namespace = ExampleConfig.DefaultNamespace)]
	public class User
	{
		[AutoIncrement]
		[DataMember]
		public int Id { get; set; }

		[DataMember]
		public string UserName { get; set; }

		[DataMember]
		public string Email { get; set; }

		[DataMember]
		public string Password { get; set; }

		[DataMember]
		public Guid GlobalId { get; set; }
	}
}