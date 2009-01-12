using System.Collections.Generic;

namespace ServiceStack.Translators.Generator.Tests.Support.Model
{
	public class Customer
	{
		public Customer()
		{
			this.PhoneNumbers = new List<PhoneNumber>();
		}

		public int Id { get; set; }
		public string Name { get; set; }
		public Address BillingAddress { get; set; }
		public List<PhoneNumber> PhoneNumbers { get; set; }
	}
}