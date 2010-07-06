using RedisWebServices.DataSource.Northwind;
using RedisWebServices.ServiceModel.Operations.App;

namespace RedisWebServices.ServiceInterface.App
{
	public class GetNorthwindDataService
		: RedisServiceBase<GetNorthwindData>
	{
		static GetNorthwindDataService()
		{
			NorthwindDtoData.LoadData();
		}

		protected override object Run(GetNorthwindData request)
		{
			return new GetNorthwindDataResponse
			{
				Categories = NorthwindDtoData.Instance.Categories,
				Customers = NorthwindDtoData.Instance.Customers,
				Employees = NorthwindDtoData.Instance.Employees,
				Shippers = NorthwindDtoData.Instance.Shippers,
				Suppliers = NorthwindDtoData.Instance.Suppliers,
				Orders = NorthwindDtoData.Instance.Orders,
				Products = NorthwindDtoData.Instance.Products,
				OrderDetails = NorthwindDtoData.Instance.OrderDetails,
				Regions = NorthwindDtoData.Instance.Regions,
				Territories = NorthwindDtoData.Instance.Territories,
				EmployeeTerritories = NorthwindDtoData.Instance.EmployeeTerritories,
			};
		}
	}
}