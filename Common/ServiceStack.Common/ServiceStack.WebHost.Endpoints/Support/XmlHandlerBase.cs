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
				try
				{
					return KeyValueDataContractDeserializer.Instance.Parse(request.QueryString, operationType);
				}
				catch (System.Exception ex)
				{
					var log = EndpointHost.Config.LogFactory.GetLogger(typeof(XmlHandlerBase));
					log.ErrorFormat("Could not deserialize '{0}' request using KeyValueDataContractDeserializer: '{1}'.\nError: '{2}'",
						operationType, request.QueryString, ex);
					throw;
				}
			}

			var xml = new StreamReader(request.InputStream).ReadToEnd();

			try
			{
				return DataContractDeserializer.Instance.Parse(xml, operationType);
			}
			catch (System.Exception ex)
			{
				var log = EndpointHost.Config.LogFactory.GetLogger(typeof(XmlHandlerBase));
				log.ErrorFormat("Could not deserialize '{0}' request using DataContractDeserializer: '{1}'.\nError: '{2}'",
					operationType, xml, ex);
				throw;
			}
		}

		public bool IsReusable
		{
			get { return false; }
		}
	}
}