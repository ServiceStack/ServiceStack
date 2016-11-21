using ServiceStack.WebHost.Endpoints.Tests.Support.Operations;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
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