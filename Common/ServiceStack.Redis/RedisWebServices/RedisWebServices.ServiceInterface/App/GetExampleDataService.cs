using RedisWebServices.DataSource.Northwind;
using RedisWebServices.ServiceModel.Operations.App;

namespace RedisWebServices.ServiceInterface.App
{
	public class GetExampleDataService
		: RedisServiceBase<GetExampleData>
	{
		static GetExampleDataService()
		{
			NorthwindData.LoadData();
		}

		protected override object Run(GetExampleData request)
		{
			return new GetExampleDataResponse();
		}
	}
}