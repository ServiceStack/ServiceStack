using System;
using System.Collections.Generic;
using @ServiceNamespace@.DataAccess;
using DataModel = @ServiceNamespace@.DataAccess.DataModel;

namespace @ServiceNamespace@.Tests.Support
{
	public static class TestData
	{
		public const string ClientModulusBase64 = "egFNQyG9xGWI9PIGOpVi8cACvsQKDqFY1Gd8UlHNBFkCm6drjfydql49ZV0sG9iTOlH9KVnWeI5uM8AmdL/mtQ==";
		public const string @ModelName@Password = "password";

		public static DataModel.@ModelName@ New@ModelName@
		{
			get
			{
				return new DataModel.@ModelName@ {
					Id = 1,
				};
			}
		}

		public static List<DataModel.@ModelName@> Load@ModelName@s(@ServiceName@DataAccessProvider provider, int userCount)
		{
			var userGlobalIds = new List<int>();

			for (int i = 0; i < userCount; i++)
			{
				var user = TestData.New@ModelName@;
				userGlobalIds.Add(user.Id);
				provider.Store(user);
			}

			return provider.Get@ModelName@s(userGlobalIds);
		}
	}
}