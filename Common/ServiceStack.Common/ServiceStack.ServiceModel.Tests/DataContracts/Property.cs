using System.Runtime.Serialization;

namespace ServiceStack.ServiceModel.Tests.DataContracts
{
	[DataContract(Namespace = "http://schemas.servicestack.net/types/")]
	public class Property
	{
		public string Name { get; set; }
		public string Value { get; set; }
	}
}