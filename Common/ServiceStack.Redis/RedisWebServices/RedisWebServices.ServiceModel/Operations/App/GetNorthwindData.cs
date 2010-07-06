using System.Collections.Generic;
using System.Runtime.Serialization;
using RedisWebServices.DataSource.Northwind;
using ServiceStack.ServiceInterface.ServiceModel;

namespace RedisWebServices.ServiceModel.Operations.App
{
	[DataContract]
	public class GetNorthwindData
	{
	}

	[DataContract]
	public class GetNorthwindDataResponse
	{
		public GetNorthwindDataResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public List<CategoryDto> Categories { get; set; }

		[DataMember]
		public List<CustomerDto> Customers { get; set; }
	
		[DataMember]
		public List<EmployeeDto> Employees { get; set; }
		
		[DataMember]
		public List<ShipperDto> Shippers { get; set; }
		
		[DataMember]
		public List<SupplierDto> Suppliers { get; set; }
		
		[DataMember]
		public List<OrderDto> Orders { get; set; }
		
		[DataMember]
		public List<ProductDto> Products { get; set; }
		
		[DataMember]
		public List<OrderDetailDto> OrderDetails { get; set; }
		
		[DataMember]
		public List<RegionDto> Regions { get; set; }
		
		[DataMember]
		public List<TerritoryDto> Territories { get; set; }
		
		[DataMember]
		public List<EmployeeTerritoryDto> EmployeeTerritories { get; set; }


		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}