using System.Web.Services;
using Ddn.Common.Host.Endpoints;

namespace @ServiceNamespace@.Host.WebService.Endpoints
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
            base.ServiceOperationType = typeof(@ServiceModelNamespace@.Version100.Operations.@ServiceName@.Get@ModelName@s);
            base.ServiceName = "@ServiceName@";
            base.UsageExamplesBaseUri = App.Instance.Config.UsageExamplesBaseUri;
        }
    }
}