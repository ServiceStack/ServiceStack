using System;
using System.Data;
using Northwind.Common.DataModel;
using Northwind.Perf;

namespace ServiceStack.OrmLite.TestsPerf.Scenarios.Northwind
{
	public class InsertNorthwindOrderScenario
		: DatabaseScenarioBase
	{
        protected override void Run(IDbConnection db)
		{
			if (this.IsFirstRun)
			{
				db.CreateTable<Order>(true);
			}

			db.Insert(NorthwindFactory.Order(this.Iteration, "VINET", 5, new DateTime(1996, 7, 4), new DateTime(1996, 1, 8), new DateTime(1996, 7, 16), 3, 32.38m, "Vins et alcools Chevalier", "59 rue de l'Abbaye", "Reims", null, "51100", "France"));
		}
	}

	public class InsertNorthwindCustomerScenario
		: DatabaseScenarioBase
	{
        protected override void Run(IDbConnection db)
		{
			if (this.IsFirstRun)
			{
				db.CreateTable<Customer>(true);
			}

			db.Insert(NorthwindFactory.Customer(this.Iteration.ToString("x"), "Alfreds Futterkiste", "Maria Anders", "Sales Representative", "Obere Str. 57", "Berlin", null, "12209", "Germany", "030-0074321", "030-0076545", null));
		}
	}

	public class InsertNorthwindSupplierScenario
		: DatabaseScenarioBase
	{
        protected override void Run(IDbConnection db)
		{
			if (this.IsFirstRun)
			{
				db.CreateTable<Supplier>(true);
			}

			db.Insert(NorthwindFactory.Supplier(this.Iteration, "Exotic Liquids", "Charlotte Cooper", "Purchasing Manager", "49 Gilbert St.", "London", null, "EC1 4SD", "UK", "(171) 555-2222", null, null));
		}
	}
}