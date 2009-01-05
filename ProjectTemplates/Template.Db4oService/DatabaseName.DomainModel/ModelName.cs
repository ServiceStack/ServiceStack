using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace @DomainModelNamespace@
{
	public class @ModelName@ : Entity 
	{
		public int StoreId { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string Email { get; set; }
		public Address Address { get; set; }
	}
}
