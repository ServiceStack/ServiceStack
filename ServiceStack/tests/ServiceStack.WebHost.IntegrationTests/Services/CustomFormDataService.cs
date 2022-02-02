using System.Runtime.Serialization;
using ServiceStack.Web;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
	[Route("/customformdata")]
	[DataContract]
	public class CustomFormData { }

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

	public class CustomFormDataService : Service
	{
		//Parsing: &first-name=tom&item-0=blah&item-1-delete=1
		public object Post(CustomFormData request)
		{
			return new CustomFormDataResponse
			{
				FirstName = Request.FormData["first-name"],
                Item0 = Request.FormData["item-0"],
                Item1Delete = Request.FormData["item-1-delete"]
			};
		}
	}
}