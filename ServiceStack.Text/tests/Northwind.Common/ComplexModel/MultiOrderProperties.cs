using System.Runtime.Serialization;
using Northwind.Common.ServiceModel;
using ProtoBuf;

namespace Northwind.Common.ComplexModel
{
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, InferTagFromName = true)]
	[DataContract]
	public class MultiOrderProperties
	{
		[DataMember]
		public OrderDto Orders1 { get; set; }

		[DataMember]
		public OrderDto Orders2 { get; set; }

		[DataMember]
		public OrderDto Orders3 { get; set; }

		[DataMember]
		public OrderDto Orders4 { get; set; }

		public override bool Equals(object obj)
		{
			var other = obj as MultiOrderProperties;
			if (other == null) return false;

			return this.Orders1.Equals(other.Orders1)
			       && this.Orders2.Equals(other.Orders2)
			       && this.Orders3.Equals(other.Orders3)
			       && this.Orders4.Equals(other.Orders4);
		}
	}
}