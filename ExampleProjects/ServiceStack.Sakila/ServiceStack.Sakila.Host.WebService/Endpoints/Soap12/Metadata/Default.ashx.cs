using System.Web.Services;
using Sakila.ServiceModel.Version100.Operations.SakilaService;
using ServiceStack.WebHost.Endpoints.Endpoints;

namespace ServiceStack.Sakila.Host.WebService.Endpoints.Soap12.Metadata
{
    /// <summary>
    /// Summary description for $codebehindclassname$
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    public class Soap12MetadataHandler : BaseSoap12MetadataHandler
    {
        public Soap12MetadataHandler()
        {
            base.ServiceOperationType = typeof(GetCustomers);
            base.ServiceName = "SakilaService";
			base.UsageExamplesBaseUri = App.Instance.Config.UsageExamplesBaseUri;
        }
    }
}
