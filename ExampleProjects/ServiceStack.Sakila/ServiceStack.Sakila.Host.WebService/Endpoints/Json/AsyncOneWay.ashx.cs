using System.IO;
using System.Web;
using System.Web.Services;
using Sakila.ServiceModel;
using ServiceStack.ServiceModel.Serialization;

namespace ServiceStack.Sakila.Host.WebService.Endpoints.Json
{
    /// <summary>
    /// Summary description for $codebehindclassname$
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    public class JsonAsyncOneWayHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            if (string.IsNullOrEmpty(context.Request.PathInfo)) return;

            var operationName = context.Request.PathInfo.Substring("/".Length);
        	var operationType = ModelInfo.Instance.GetDtoTypeFromOperation(operationName);
            var formData = new StreamReader(context.Request.InputStream).ReadToEnd();
        	var isJson = formData.StartsWith("{");
        	var request = isJson ? JsonDataContractDeserializer.Instance.Parse(formData, operationType)
        	                 	 : KeyValueDataContractDeserializer.Instance.Parse(context.Request.Form, operationType);

			App.Instance.ExecuteService(request);
        }

        public bool IsReusable
        {
            get { return false; }
        }

    }
}