using System.Web.Services;
using Ddn.Common.Host.Endpoints;

namespace @ServiceNamespace@.Host.WebService.Endpoints.Xml.Metadata
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
            base.ServiceOperationType = typeof(@ServiceModelNamespace@.Version100.Operations.@ServiceName@.Get@ModelName@s);
            base.ServiceName = "@ServiceName@";
			base.UsageExamplesBaseUri = App.Instance.Config.UsageExamplesBaseUri;
        }
    }
}