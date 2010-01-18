using System;
using Northwind.Common.DataModel;
using Northwind.Perf;
using ServiceStack.DataAccess;

namespace Northwind.Benchmarks.BasicPersistenceProviders
{
	public abstract class BasicPersistenceProviderScenarioBase
		: ScenarioBase
	{
		protected abstract IBasicPersistenceProvider CreateProvider();

		protected int Iteration;
		public bool IsFirstRun
		{
			get
			{
				return this.Iteration == 0;
			}
		}

		public override void Run()
		{
			var provider = CreateProvider();
			Run(provider);
			this.Iteration++;
		}

		public virtual void OnBeforeRun(IBasicPersistenceProvider provider) {}

		public void Run(IBasicPersistenceProvider provider)
		{
			if (this.IsFirstRun)
			{
				OnBeforeRun(provider);
			}
			else
			{
				provider.DeleteAll<Category>();
				provider.DeleteAll<Customer>();
				provider.DeleteAll<Employee>();
				provider.DeleteAll<Shipper>();
				provider.DeleteAll<Order>();
				provider.DeleteAll<OrderDetail>();
				provider.DeleteAll<CustomerCustomerDemo>();
				provider.DeleteAll<Region>();
				provider.DeleteAll<Territory>();
				provider.DeleteAll<EmployeeTerritory>();
			}

			provider.StoreAll(NorthwindData.Categories);
			provider.StoreAll(NorthwindData.Customers);
			provider.StoreAll(NorthwindData.Employees);
			provider.StoreAll(NorthwindData.Shippers);
			provider.StoreAll(NorthwindData.Orders);
			provider.StoreAll(NorthwindData.OrderDetails);
			provider.StoreAll(NorthwindData.CustomerCustomerDemos);
			provider.StoreAll(NorthwindData.Regions);
			provider.StoreAll(NorthwindData.Territories);
			provider.StoreAll(NorthwindData.EmployeeTerritories);
		}

	}
}