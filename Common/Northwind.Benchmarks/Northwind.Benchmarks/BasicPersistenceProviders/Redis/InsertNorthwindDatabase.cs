using System;
using ServiceStack.DataAccess;
using ServiceStack.Redis;

namespace Northwind.Benchmarks.BasicPersistenceProviders.Redis
{
	public class InsertNorthwindDatabase
		: BasicPersistenceProviderScenarioBase
	{
		protected override IBasicPersistenceProvider CreateProvider()
		{
			return new RedisClient();
		}
	}
}