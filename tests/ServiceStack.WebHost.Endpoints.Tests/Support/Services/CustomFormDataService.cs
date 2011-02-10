using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.WebHost.Endpoints.Tests.Support.Operations;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
	public class CustomFormDataService
		: RestServiceBase<CustomFormData> // which inherits from IRequiresRequestContext
	{
		//Parsing: &first-name=tom&item-0=blah&item-1-delete=1
		public override object OnPost(CustomFormData request)
		{
			var httpReq = base.RequestContext.Get<IHttpRequest>();

			return new CustomFormDataResponse
			{
				FirstName = httpReq.FormData["first-name"],
				Item0 = httpReq.FormData["item-0"],
				Item1Delete = httpReq.FormData["item-1-delete"]
			};
		}
	}

}