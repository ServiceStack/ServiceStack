using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using Ddn.Common.Host.Endpoints;

namespace @ServiceNamespace@.Host.WebService.Endpoints.Json.Metadata
{
    /// <summary>
    /// Summary description for $codebehindclassname$
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    public class JsonMetadataHandler : BaseJsonMetadataHandler
    {
        public JsonMetadataHandler()
        {
            base.ServiceOperationType = typeof(@ServiceModelNamespace@.Version100.Operations.@ServiceName@.Get@ModelName@s);
            base.ServiceName = "@ServiceName@";
			base.UsageExamplesBaseUri = App.Instance.Config.UsageExamplesBaseUri;
        }
    }
}
