using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServiceStack.Translators.Generator.Tests.Support.DataContract
{
	[Translate(typeof(Model.Customer))]
	[DataContract]
	public partial class Customer
	{
		[DataMember]
		public int Id { get; set; }

		[DataMember]
		public string Name { get; set; }
	
		[DataMember]
		public Address BillingAddress { get; set; }

		[DataMember]
		public List<PhoneNumber> PhoneNumbers { get; set; }

		public string ModelReadOnly { get; set; }
		public string ModelWriteOnly { get; set; }
		public string DtoReadOnly { get; protected set; }
		public string DtoWriteOnly { protected get; set; }
	}
}