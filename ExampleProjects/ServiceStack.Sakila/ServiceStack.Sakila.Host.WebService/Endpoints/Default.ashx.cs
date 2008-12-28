using System.Web.Services;
using Sakila.ServiceModel.Version100.Operations.SakilaService;
using ServiceStack.WebHost.Endpoints.Endpoints;

namespace ServiceStack.Sakila.Host.WebService.Endpoints
{
    /// <summary>
    /// Summary description for $codebehindclassname$
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    public class EndpointMetadataHandler : BaseIndexMetadataHandler
    {
        public EndpointMetadataHandler()
        {
            base.ServiceOperationType = typeof(GetCustomers);
            base.ServiceName = "SakilaService";
            base.UsageExamplesBaseUri = App.Instance.Config.UsageExamplesBaseUri;
        }
    }
}