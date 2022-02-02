using System.Data;
using ServiceStack.Caching;
using ServiceStack.OrmLite;
using ServiceStack.ServiceHost.Tests.Support;
using ServiceStack.ServiceHost.Tests.UseCase.Operations;

namespace ServiceStack.ServiceHost.Tests.UseCase.Services
{
    public class GetCustomerService : IService
    {
        private static readonly string CacheKey = typeof(GetCustomer).Name;

        private readonly IDbConnection db;
        private readonly CustomerUseCaseConfig config;

        public GetCustomerService(IDbConnection dbConn, CustomerUseCaseConfig config)
        {
            this.db = dbConn;
            this.config = config;
        }

        public ICacheClient CacheClient { get; set; }

        public object Any(GetCustomer request)
        {
            if (config.UseCache)
            {
                var inCache = this.CacheClient.Get<GetCustomerResponse>(CacheKey);
                if (inCache != null) return inCache;
            }

            var response = new GetCustomerResponse
            {
                Customer = db.SingleById<Customer>(request.CustomerId)
            };

            if (config.UseCache)
                this.CacheClient.Set(CacheKey, response);

            return response;
        }
    }
}