using System.Web.Services;
using Sakila.ServiceModel.Version100.Operations.SakilaService;
using ServiceStack.WebHost.Endpoints.Endpoints;

namespace ServiceStack.Sakila.Host.WebService.Endpoints.Xml.Metadata
{
    /// <summary>
    /// Summary description for $codebehindclassname$
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    public class XmlMetadataHandler : BaseXmlMetadataHandler
    {
        public XmlMetadataHandler()
        {
            base.ServiceOperationType = typeof(GetCustomers);
            base.ServiceName = "SakilaService";
			base.UsageExamplesBaseUri = App.Instance.Config.UsageExamplesBaseUri;
        }
    }
}