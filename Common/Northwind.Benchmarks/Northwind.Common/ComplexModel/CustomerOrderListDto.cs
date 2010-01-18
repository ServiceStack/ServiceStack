using System.Collections.Generic;
using System.Linq;
using Northwind.Common.ServiceModel;
using Platform.Text;

namespace Northwind.Common.ComplexModel
{
	[TextRecord]
	public class CustomerOrderListDto
	{
		public CustomerOrderListDto()
		{
			this.Orders = new List<FullOrderDto>();
		}

		[TextField]
		public CustomerDto Customer { get; set; }

		[TextField]
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