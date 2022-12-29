using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Northwind.Common.ServiceModel;
using ProtoBuf;

namespace Northwind.Common.ComplexModel
{
	[DataContract]
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, InferTagFromName = true)]
	public class FullOrderDto
	{
		public FullOrderDto()
		{
			this.OrderDetails = new List<OrderDetailDto>();
		}

		[DataMember]
		public OrderDto Order { get; set; }

		[DataMember]
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