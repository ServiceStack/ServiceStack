using System.Runtime.Serialization;
using Northwind.Common.ServiceModel;
using Platform.Text;

namespace Northwind.Common.ComplexModel
{
	[TextRecord]
	[DataContract]
	public class MultiCustomerProperties
	{
		[TextField]
		[DataMember]
		public CustomerDto Customer1 { get; set; }

		[TextField]
		[DataMember]
		public CustomerDto Customer2 { get; set; }

		[TextField]
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