using System;
using System.Collections.Specialized;
using System.IO;
using System.Web;
using ServiceStack.Common.Web;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.WebHost.Endpoints.Extensions;

namespace ServiceStack.WebHost.Endpoints.Support
{
	public abstract class XmlHandlerBase : EndpointHandlerBase, IHttpHandler
	{
		public static string Serialize(object model)
		{
			return DataContractSerializer.Instance.Parse(model);
		}

		public override object CreateRequest(IHttpRequest request, string operationName)
		{
			return GetRequest(request, operationName);
		}

		public static object GetRequest(IHttpRequest httpReq, string operationName)
		{
			var operationType = GetOperationType(operationName);
			AssertOperationExists(operationName, operationType);
			
			var httpMethod = httpReq.HttpMethod;
			var queryString = httpReq.QueryString;

			if (httpMethod == "GET" || httpMethod == "OPTIONS")
			{
				try
				{
					return KeyValueDataContractDeserializer.Instance.Parse(queryString, operationType);
				}
				catch (Exception ex)
				{
					var log = EndpointHost.Config.LogFactory.GetLogger(typeof(XmlHandlerBase));
					log.ErrorFormat("Could not deserialize '{0}' request using KeyValueDataContractDeserializer: '{1}'.\nError: '{2}'",
					                operationType, queryString, ex);
					throw;
				}
			}

			var isFormData = httpReq.HasAnyOfContentTypes(ContentType.FormUrlEncoded, ContentType.MultiPartFormData);

			if (isFormData)
			{
				return KeyValueDataContractDeserializer.Instance.Parse(httpReq.FormData, operationType);
			}
			
			var xml = httpReq.GetRawBody();
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