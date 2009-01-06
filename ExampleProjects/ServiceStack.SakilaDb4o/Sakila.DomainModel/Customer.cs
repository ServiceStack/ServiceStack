using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sakila.DomainModel
{
	public class Customer : Entity 
	{
		public int StoreId { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string Email { get; set; }
		public Address Address { get; set; }
	}
}
