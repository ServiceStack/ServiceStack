using System.Runtime.Serialization;

namespace ServiceStack.ServiceModel.Tests.DataContracts
{
	[DataContract(Namespace = "http://schemas.servicestack.net/types/")]
	public class Customer 
	{
		public int Id { get; set; }
		public int StoreId { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string Email { get; set; }
	}
}