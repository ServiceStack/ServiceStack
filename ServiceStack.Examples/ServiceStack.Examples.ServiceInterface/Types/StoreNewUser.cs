using System.Runtime.Serialization;

namespace ServiceStack.Examples.ServiceInterface.Types
{
	/// <summary>
	/// Use Plain old DataContract's Define your 'Service Interface'
	/// 
	/// This example introduces the concept of a generic 'ResponseStatus' that 
	/// your service client can use to assert that the request was successful.
	/// The ResponseStatus DTO also enables you to serialize an exception in your service.
	/// </summary>
	[DataContract(Namespace = "http://schemas.sericestack.net/examples/types")]
	public class StoreNewUser
	{
		[DataMember]
		public string UserName { get; set; }

		[DataMember]
		public string Email { get; set; }

		[DataMember]
		public string Password { get; set; }
	}

	[DataContract(Namespace = "http://schemas.sericestack.net/examples/types")]
	public class StoreNewUserResponse
	{
		public StoreNewUserResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public long UserId { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}