using System;
using System.Runtime.Serialization;
using ServiceStack.DataAnnotations;

namespace ServiceStack.Examples.ServiceInterface.Types
{
	[DataContract]
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

		[DataMember]
		public DateTime CreatedDate { get; set; }
	}
}