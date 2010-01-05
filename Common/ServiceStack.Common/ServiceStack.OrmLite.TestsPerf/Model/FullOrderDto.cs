using System.Collections.Generic;
using System.Linq;
using ServiceStack.OrmLite.Tests.Models;

namespace ServiceStack.OrmLite.TestsPerf.Model
{
	public class FullOrderDto
	{
		public FullOrderDto()
		{
			this.OrderDetails = new List<OrderDetailDto>();
		}

		public OrderDto Order { get; set; }

		public List<OrderDetailDto> OrderDetails { get; set; }

		public override bool Equals(object obj)
		{
			var other = obj as FullOrderDto;
			if (other == null) return false;

			var i = 0;
			return this.Order.Equals(other.Order)
			       && this.OrderDetails.All(x => x.Equals(other.OrderDetails[i++]));
		}
	}
}