using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Northwind.Common.ServiceModel;
using ProtoBuf;

namespace Northwind.Common.ComplexModel
{
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, InferTagFromName = true)]
	[DataContract]
	public class CustomerOrderListDto
	{
		public CustomerOrderListDto()
		{
			this.Orders = new List<FullOrderDto>();
		}

		[DataMember]
		public CustomerDto Customer { get; set; }

		[DataMember]
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