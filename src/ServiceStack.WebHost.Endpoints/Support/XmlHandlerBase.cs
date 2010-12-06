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

		public static string Serialize(object model)
		{
			return DataContractSerializer.Instance.Parse(model);
		}

		protected object CreateRequest(HttpRequest request, string operationName)
		{
			return CreateRequest(operationName,
				request.HttpMethod,
				request.QueryString,
				null,
				request.InputStream);
		}

		public override object CreateRequest(string operationName, string httpMethod,
			NameValueCollection queryString, NameValueCollection requestForm, Stream inputStream)
		{
			return GetRequest(operationName, httpMethod, queryString, requestForm, inputStream);
		}

		public static object GetRequest(string operationName, string httpMethod,
			NameValueCollection queryString, NameValueCollection requestForm, Stream inputStream)
		{
			var operationType = GetOperationType(operationName);
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