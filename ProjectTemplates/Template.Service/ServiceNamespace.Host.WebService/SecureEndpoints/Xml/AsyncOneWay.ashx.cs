using System.IO;
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
	public class XmlAsyncOneWayHandler : IHttpHandler
	{
		public void ProcessRequest(HttpContext context)
		{
			if (string.IsNullOrEmpty(context.Request.PathInfo)) return;

			var operationName = context.Request.PathInfo.Substring("/".Length);
			var operationType = @ServiceModelNamespace@.ModelInfo.Instance.GetDtoTypeFromOperation(operationName);
			var json = new StreamReader(context.Request.InputStream).ReadToEnd();
			var request = DataContractDeserializer.Instance.Parse(json, operationType);


			App.Instance.ExecuteService(request);
		}

		public bool IsReusable
		{
			get { return false; }
		}
	}
}