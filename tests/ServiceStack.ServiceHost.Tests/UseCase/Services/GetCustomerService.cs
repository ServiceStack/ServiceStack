using System;
using System.Data;
using ServiceStack.CacheAccess;
using ServiceStack.Configuration;
using ServiceStack.OrmLite;
using ServiceStack.ServiceHost.Tests.Support;
using ServiceStack.ServiceHost.Tests.UseCase.Operations;

namespace ServiceStack.ServiceHost.Tests.UseCase.Services
{
	public class GetCustomerService
		: IService<GetCustomer>
	{
		private static readonly string CacheKey = typeof (GetCustomer).Name;

		private readonly IDbConnection db;
		private readonly CustomerUseCaseConfig config;

		public GetCustomerService(IDbConnection dbConn, CustomerUseCaseConfig config)
		{
			this.db = dbConn;
			this.config = config;
		}

		public ICacheClient CacheClient { get; set; }

		public object Execute(GetCustomer request)
		{
			if (config.UseCache)
			{
				var inCache = this.CacheClient.Get<GetCustomerResponse>(CacheKey);
				if (inCache != null) return inCache;
			}

			var response = new GetCustomerResponse {
				Customer = db.GetById<Customer>(request.CustomerId)
			};

			if (config.UseCache) 
				this.CacheClient.Set(CacheKey, response);

			return response;
		}
	}
}