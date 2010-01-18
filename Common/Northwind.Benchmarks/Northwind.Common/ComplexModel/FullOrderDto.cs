using System.Collections.Generic;
using System.Linq;
using Northwind.Common.ServiceModel;
using Platform.Text;

namespace Northwind.Common.ComplexModel
{
	[TextRecord]
	public class FullOrderDto
	{
		public FullOrderDto()
		{
			this.OrderDetails = new List<OrderDetailDto>();
		}

		[TextField]
		public OrderDto Order { get; set; }

		[TextField]
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