using System.IO;
using System.Web;
using ServiceStack.ServiceModel.Serialization;

namespace ServiceStack.WebHost.Endpoints.Support
{
	public abstract class JsonHandlerBase : EndpointHandlerBase, IHttpHandler
    {
		protected static object CreateRequest(HttpRequest request, string typeName)
        {
            var operationType = EndpointHost.GetOperationType(typeName);
            if (request.HttpMethod == "GET")
            {
                return KeyValueDataContractDeserializer.Instance.Parse(request.QueryString, operationType);
            }
            var formData = new StreamReader(request.InputStream).ReadToEnd();
			var isJson = formData.StartsWith("{");
			return isJson ? JsonDataContractDeserializer.Instance.Parse(formData, operationType) 
						  : KeyValueDataContractDeserializer.Instance.Parse(request.Form, operationType);
        }

		public abstract void ProcessRequest(HttpContext context);

		public bool IsReusable
        {
            get { return false; }
        }
    }
}