using System;
using System.Runtime.Serialization;
using Northwind.Common.ServiceModel;
using Platform.Text;

namespace Northwind.Common.ComplexModel
{
	[TextRecord]
	[DataContract]
	public class ArrayDtoWithOrders
	{
		public ArrayDtoWithOrders()
		{
			Orders = new OrderDto[0];
		}

		[TextField]
		[DataMember]
		public Guid Id { get; set; }

		[TextField]
		[DataMember]
		public CustomerDto Customer { get; set; }

		[TextField]
		[DataMember]
		public SupplierDto Supplier { get; set; }

		[TextField]
		[DataMember]
		public OrderDto[] Orders { get; set; }

		public override bool Equals(object obj)
		{
			var other = obj as ArrayDtoWithOrders;
			if (other == null) return false;

			return this.Id == other.Id
			       && this.Customer.Equals(other.Customer)
			       && this.Supplier.Equals(other.Supplier)
			       && this.Orders.Length == other.Orders.Length;
		}
	}
}