using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Sakila.ServiceModel.Version100.Types
{
	[DataContract(Namespace = "http://schemas.servicestack.net/types/")]
	public class Customer 
	{
		public int Id { get; set; }
		public int StoreId { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string Email { get; set; }
		public Address Address { get; set; }
	}
}