using System;
using System.Runtime.Serialization;
using System.Web;
using ServiceStack.CacheAccess;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Extensions;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
    [Route("/req-items")]
    public class RequestItems
    {
    }

    public class RequestItemsResponse : IHasResponseStatus
    {
        public string Result { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class RequestItemsService : ServiceBase<RequestItems>
    {
        protected override object Run(RequestItems request)
        {
            if (!Request.Items.ContainsKey("_DataSetAtPreRequestFilters"))
                throw new InvalidOperationException("DataSetAtPreRequestFilters missing.");

            if (!Request.Items.ContainsKey("_DataSetAtRequestFilters"))
                throw new InvalidOperationException("DataSetAtRequestFilters data missing.");

            return new RequestItemsResponse { Result = "MissionSuccess" };
        }
    }
}