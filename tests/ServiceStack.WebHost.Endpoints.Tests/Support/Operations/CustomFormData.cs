using System;
using System.Runtime.Serialization;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Operations
{
	[Route("/customformdata")]
	[DataContract]
	public class CustomFormData {}

	[DataContract]
	public class CustomFormDataResponse : IHasResponseStatus
	{
		[DataMember]
		public string FirstName { get; set; }

		[DataMember]
		public string Item0 { get; set; }

		[DataMember]
		public string Item1Delete { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}
