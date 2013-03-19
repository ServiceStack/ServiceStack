using System.Collections.Generic;

namespace ServiceStack.Tests.Html.Support.Types
{
	class Person
	{
		public string First { get; set; }
		public string Last { get; set; }

		public Address Work { get; set; }
		public Address Home { get; set; }
	}

	class Address
	{
		public string Street { get; set; }
		public string StreetNo { get; set; }
		public string ZIP { get; set; }
		public string City { get; set; }
		public string State { get; set; }
	}
}
