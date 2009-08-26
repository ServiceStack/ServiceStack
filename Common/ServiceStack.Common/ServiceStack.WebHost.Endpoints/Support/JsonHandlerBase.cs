using System;
using System.IO;
using System.Web;
using ServiceStack.ServiceModel.Serialization;

namespace ServiceStack.WebHost.Endpoints.Support
{
	public abstract class JsonHandlerBase : EndpointHandlerBase, IHttpHandler
    {
		protected static object CreateRequest(HttpRequest request, string typeName)
        {
			var operationType = EndpointHost.ServiceOperations.GetOperationType(typeName);
			if (request.HttpMethod == "GET" || request.HttpMethod == "OPTIONS")
			{
				try
				{
					return KeyValueDataContractDeserializer.Instance.Parse(request.QueryString, operationType);
				}
				catch (Exception ex)
				{
					var log = EndpointHost.Config.LogFactory.GetLogger(typeof(XmlHandlerBase));
					log.ErrorFormat("Could not deserialize '{0}' request using KeyValueDataContractDeserializer: '{1}' '{2}'",
						operationType, request.QueryString, ex);
					throw;
				}
            }

            var formData = new StreamReader(request.InputStream).ReadToEnd();
			var isJson = formData.StartsWith("{");

			try
			{
				return isJson ? JsonDataContractDeserializer.Instance.Parse(formData, operationType)
							  : KeyValueDataContractDeserializer.Instance.Parse(request.Form, operationType);
			}
			catch (Exception ex)
			{
				var log = EndpointHost.Config.LogFactory.GetLogger(typeof(XmlHandlerBase));
				var deserializer = isJson ? "JsonDataContractDeserializer" : "KeyValueDataContractDeserializer";
				log.ErrorFormat("Could not deserialize '{0}' request using {1}: '{2}'\nError: {3}",
					operationType, deserializer, formData, ex);
				throw;
			}

        }

		public abstract void ProcessRequest(HttpContext context);

		public bool IsReusable
        {
            get { return false; }
        }
    }
}