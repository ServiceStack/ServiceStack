using System.IO;
using System.Web;
using ServiceStack.ServiceModel.Serialization;

namespace ServiceStack.WebHost.Endpoints.Support
{
	public abstract class XmlHandlerBase : EndpointHandlerBase, IHttpHandler
	{
		public abstract void ProcessRequest(HttpContext context);

		protected static object CreateRequest(HttpRequest request, string operationName)
		{
			var operationType = EndpointHost.ServiceOperations.GetOperationType(operationName);
			AssertOperationExists(operationName, operationType);
			if (request.HttpMethod == "GET")
			{
				return KeyValueDataContractDeserializer.Instance.Parse(request.QueryString, operationType);
			}
			var xml = new StreamReader(request.InputStream).ReadToEnd();
			return DataContractDeserializer.Instance.Parse(xml, operationType);
		}

		public bool IsReusable
		{
			get { return false; }
		}
	}
}