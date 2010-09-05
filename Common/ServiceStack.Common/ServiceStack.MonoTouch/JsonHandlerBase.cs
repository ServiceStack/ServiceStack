using System;
using System.Collections.Specialized;
using System.IO;
using System.Web;
using ServiceStack.ServiceModel.Serialization;

namespace ServiceStack.WebHost.Endpoints.Support
{
	public abstract class JsonHandlerBase : EndpointHandlerBase, IHttpHandler
    {
		protected static object CreateRequest(HttpRequest request, string operationName)
		{
			return CreateRequest(operationName,
				request.HttpMethod,
				request.QueryString,
				request.Form,
				request.InputStream);
		}

		public static object CreateRequest(string operationName, string httpMethod, NameValueCollection queryString, NameValueCollection requestForm, Stream inputStream)
		{
			var operationType = EndpointHost.ServiceOperations.GetOperationType(operationName);
			AssertOperationExists(operationName, operationType);
			if (httpMethod == "GET" || httpMethod == "OPTIONS")
			{
				try
				{
					return KeyValueDataContractDeserializer.Instance.Parse(queryString, operationType);
				}
				catch (Exception ex)
				{
					var log = EndpointHost.Config.LogFactory.GetLogger(typeof(JsonHandlerBase));
					log.ErrorFormat("Could not deserialize '{0}' request using KeyValueDataContractDeserializer: '{1}' '{2}'",
						operationType, queryString, ex);
					throw;
				}
            }

            var formData = new StreamReader(inputStream).ReadToEnd();
			var isJson = formData.StartsWith("{");

			try
			{
				return isJson ? JsonDataContractDeserializer.Instance.Parse(formData, operationType)
							  : KeyValueDataContractDeserializer.Instance.Parse(requestForm, operationType);
			}
			catch (Exception ex)
			{
				var log = EndpointHost.Config.LogFactory.GetLogger(typeof(JsonHandlerBase));
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