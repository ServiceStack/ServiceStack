using System;
using System.Collections.Generic;
using ServiceStack.Sakila.DataAccess;
using DataModel = ServiceStack.Sakila.DataAccess.DataModel;

namespace ServiceStack.Sakila.Tests.Support
{
	public static class TestData
	{
		public const string ClientModulusBase64 = "egFNQyG9xGWI9PIGOpVi8cACvsQKDqFY1Gd8UlHNBFkCm6drjfydql49ZV0sG9iTOlH9KVnWeI5uM8AmdL/mtQ==";
		public const string CustomerPassword = "password";

		public static DataModel.Customer NewCustomer
		{
			get
			{
				return new DataModel.Customer {
					Id = 1,
					CreateDate = DateTime.Now,
					LastUpdate = DateTime.Now,
					LastName = "LastName",
					FirstName = "FirstName",
					Email = "user@host.com",					
				};
			}
		}

		public static List<DataModel.Customer> LoadCustomers(SakilaServiceDataAccessProvider provider, int userCount)
		{
			var userGlobalIds = new List<int>();

			for (int i = 0; i < userCount; i++)
			{
				var user = TestData.NewCustomer;
				userGlobalIds.Add(user.Id);
				provider.Store(user);
			}

			return provider.GetCustomers(userGlobalIds);
		}
	}
}