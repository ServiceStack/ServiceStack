using System;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;

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

    public class RequestItemsService : ServiceInterface.Service
    {
        public object Any(RequestItems request)
        {
            if (!Request.Items.ContainsKey("_DataSetAtPreRequestFilters"))
                throw new InvalidOperationException("DataSetAtPreRequestFilters missing.");

            if (!Request.Items.ContainsKey("_DataSetAtRequestFilters"))
                throw new InvalidOperationException("DataSetAtRequestFilters data missing.");

            return new RequestItemsResponse { Result = "MissionSuccess" };
        }
    }
}