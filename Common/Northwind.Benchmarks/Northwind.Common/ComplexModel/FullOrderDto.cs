using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Northwind.Common.ServiceModel;
using Platform.Text;
using ProtoBuf;

namespace Northwind.Common.ComplexModel
{
	[DataContract]
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, InferTagFromName = true)]
	[TextRecord]
	public class FullOrderDto
	{
		public FullOrderDto()
		{
			this.OrderDetails = new List<OrderDetailDto>();
		}

		[DataMember]
		[TextField]
		public OrderDto Order { get; set; }

		[DataMember]
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