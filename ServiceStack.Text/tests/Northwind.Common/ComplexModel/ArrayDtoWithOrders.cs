using System;
using System.Runtime.Serialization;
using Northwind.Common.ServiceModel;
using ProtoBuf;

namespace Northwind.Common.ComplexModel
{
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, InferTagFromName = true)]
	[DataContract]
	public class ArrayDtoWithOrders
	{
		public ArrayDtoWithOrders()
		{
			Orders = new OrderDto[0];
		}

		[DataMember]
		public Guid Id { get; set; }

		[DataMember]
		public CustomerDto Customer { get; set; }

		[DataMember]
		public SupplierDto Supplier { get; set; }

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