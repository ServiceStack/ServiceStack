using System.Collections.Generic;
using System.Linq;
using ServiceStack.OrmLite.Tests.Models;

namespace ServiceStack.OrmLite.TestsPerf.Model
{
	public class CustomerOrderListDto
	{
		public CustomerOrderListDto()
		{
			this.Orders = new List<FullOrderDto>();
		}

		public CustomerDto Customer { get; set; }

		public List<FullOrderDto> Orders { get; set; }

		public override bool Equals(object obj)
		{
			var other = obj as CustomerOrderListDto;
			if (other == null) return false;

			var i = 0;
			return this.Customer.Equals(other.Customer)
			       && this.Orders.All(x => x.Equals(other.Orders[i++]));
		}
	}
}