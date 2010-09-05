using System;
using System.Collections.Specialized;
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
			return CreateRequest(operationName,
				request.HttpMethod,
				request.QueryString,
				request.InputStream);
		}

		public static object CreateRequest(string operationName, string httpMethod, NameValueCollection queryString, Stream inputStream)
		{
			var operationType = EndpointHost.ServiceOperations.GetOperationType(operationName);
			AssertOperationExists(operationName, operationType);
			if (httpMethod == "GET" || httpMethod == "OPTIONS")
			{
				try
				{
					return KeyValueDataContractDeserializer.Instance.Parse(queryString, operationType);
				}
				catch (System.Exception ex)
				{
					var log = EndpointHost.Config.LogFactory.GetLogger(typeof(XmlHandlerBase));
					log.ErrorFormat("Could not deserialize '{0}' request using KeyValueDataContractDeserializer: '{1}'.\nError: '{2}'",
					                operationType, queryString, ex);
					throw;
				}
			}

			var xml = new StreamReader(inputStream).ReadToEnd();

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