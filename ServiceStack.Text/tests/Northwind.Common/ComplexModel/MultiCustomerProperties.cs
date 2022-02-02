using System.Runtime.Serialization;
using Northwind.Common.ServiceModel;
using ProtoBuf;

namespace Northwind.Common.ComplexModel
{
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, InferTagFromName = true)]
	[DataContract]
	public class MultiCustomerProperties
	{
		[DataMember]
		public CustomerDto Customer1 { get; set; }

		[DataMember]
		public CustomerDto Customer2 { get; set; }

		[DataMember]
		public CustomerDto Customer3 { get; set; }

		public override bool Equals(object obj)
		{
			var other = obj as MultiCustomerProperties;
			if (other == null) return false;

			return this.Customer1.Equals(other.Customer1)
			       && this.Customer2.Equals(other.Customer2)
			       && this.Customer3.Equals(other.Customer3);
		}
	}
}