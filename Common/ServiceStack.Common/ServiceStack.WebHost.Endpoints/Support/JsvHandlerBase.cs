using System;
using System.IO;
using System.Web;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Support
{
	public abstract class JsvHandlerBase : EndpointHandlerBase, IHttpHandler
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
					var log = EndpointHost.Config.LogFactory.GetLogger(typeof(JsvHandlerBase));
					log.ErrorFormat("Could not deserialize '{0}' request using KeyValueDataContractDeserializer: '{1}' '{2}'",
						operationType, request.QueryString, ex);
					throw;
				}
            }

            var formData = new StreamReader(request.InputStream).ReadToEnd();
			var isJsv = formData.StartsWith("{");

			try
			{
				return isJsv ? TypeSerializer.DeserializeFromString(formData, operationType)
							  : KeyValueDataContractDeserializer.Instance.Parse(request.Form, operationType);
			}
			catch (Exception ex)
			{
				var log = EndpointHost.Config.LogFactory.GetLogger(typeof(JsvHandlerBase));
				var deserializer = isJsv ? "TypeSerializer" : "KeyValueDataContractDeserializer";
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