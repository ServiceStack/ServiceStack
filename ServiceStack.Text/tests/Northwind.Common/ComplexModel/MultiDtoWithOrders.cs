using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Northwind.Common.ServiceModel;
using ProtoBuf;

namespace Northwind.Common.ComplexModel
{
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, InferTagFromName = true)]
	[DataContract]
	public class MultiDtoWithOrders
	{
		public MultiDtoWithOrders()
		{
			Orders = new List<OrderDto>();
		}

		[DataMember]
		public Guid Id { get; set; }

		[DataMember]
		public CustomerDto Customer { get; set; }

		[DataMember]
		public SupplierDto Supplier { get; set; }

		[DataMember]
		public List<OrderDto> Orders { get; set; }

		public override bool Equals(object obj)
		{
			var other = obj as MultiDtoWithOrders;
			if (other == null) return false;

			return this.Id == other.Id
			       && this.Customer.Equals(other.Customer)
			       && this.Supplier.Equals(other.Supplier)
			       && this.Orders.Count == other.Orders.Count;
		}
	}
}