using System.IO;
using System.Reflection;
using System.Web;
using System.Web.Services;
using Ddn.Common.Services.Serialization;

namespace @ServiceNamespace@.Host.WebService.SecureEndpoints.Xml
{
	/// <summary>
	/// Summary description for $codebehindclassname$
	/// </summary>
	[WebService(Namespace = "http://tempuri.org/")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	public class XmlSyncReplyHandler : IHttpHandler
	{
		readonly Assembly serviceModelAssembly = typeof(@ServiceModelNamespace@.ModelInfo).Assembly;

		public void ProcessRequest(HttpContext context)
		{
			if (string.IsNullOrEmpty(context.Request.PathInfo)) return;

			var operationName = context.Request.PathInfo.Substring("/".Length);
			var typeName = string.Format("@ServiceModelNamespace@.Version100.Operations.@ServiceName@.{0}", operationName);

			var request = CreateRequest(context.Request, typeName);

			var response = App.Instance.ExecuteService(request);
			if (response == null) return;

			var responseJson = DataContractSerializer.Instance.Parse(response);
			context.Response.ContentType = "application/xml";
			context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
			context.Response.Write(responseJson);
			context.Response.End();
		}

		private object CreateRequest(HttpRequest request, string typeName)
		{
			var operationType = this.serviceModelAssembly.GetType(typeName);
			if (request.HttpMethod == "GET")
			{
				return KeyValueDataContractDeserializer.Instance.Parse(request.QueryString, operationType);
			}
			var json = new StreamReader(request.InputStream).ReadToEnd();
			return DataContractDeserializer.Instance.Parse(json, operationType);
		}

		public bool IsReusable
		{
			get { return false; }
		}
	}
}