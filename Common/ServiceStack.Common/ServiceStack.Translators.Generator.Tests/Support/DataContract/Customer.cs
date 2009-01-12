using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServiceStack.Translators.Generator.Tests.Support.DataContract
{
	[TranslateModel(typeof(Model.Customer))]
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
	}
}