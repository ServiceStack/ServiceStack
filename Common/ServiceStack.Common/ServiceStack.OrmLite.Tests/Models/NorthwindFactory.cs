using System;

namespace ServiceStack.OrmLite.Tests.Models
{
	public static class NorthwindFactory
	{
		public static Categories Category(int id, string categoryName, string description, byte[] picture)
		{
			return new Categories {
				Id = id,
				CategoryName = categoryName,
				Description = description,
				Picture = picture
			};
		}

		public static Customers Customer(
			string customerId, string companyName, string contactName, string contactTitle,
			string address, string city, string region, string postalCode, string country,
			string phoneNo, string faxNo, 
			byte[] picture)
		{
			return new Customers {
				Id = customerId,
                CompanyName = companyName,
				ContactName = contactName,
				ContactTitle = contactTitle,
				Address = address,
				City = city,
				Region = region,
				PostalCode = postalCode,
				Country = country,
				Phone = phoneNo,
				Fax = faxNo,
				Picture = picture
			};
		}

		public static Shippers Shipper(int id, string companyName, string phoneNo)
		{
			return new Shippers {
				Id = id,
				CompanyName = companyName,
				Phone = phoneNo,
			};
		}

		public static Suppliers Supplier(
			int supplierId, string companyName, string contactName, string contactTitle,
			string address, string city, string region, string postalCode, string country,
			string phoneNo, string faxNo,
			string homePage)
		{
			return new Suppliers {
				Id = supplierId,
				CompanyName = companyName,
				ContactName = contactName,
				ContactTitle = contactTitle,
				Address = address,
				City = city,
				Region = region,
				PostalCode = postalCode,
				Country = country,
				Phone = phoneNo,
				Fax = faxNo,
				HomePage = homePage
			};
		}

		public static Orders Order(
			int supplierId, int employeeId, DateTime? orderDate, DateTime? requiredDate,
			DateTime? shippedDate, int shipVia, decimal freight,
			string address, string city, string region, string postalCode, string country)
		{
			return new Orders {
				Id = supplierId,
				EmployeeId = employeeId,
				OrderDate = orderDate,
				RequiredDate = requiredDate,
				ShippedDate = shippedDate,
				ShipVia = shipVia,
				Freight = freight,
				ShipAddress = address,
				ShipCity = city,
				ShipRegion = region,
				ShipPostalCode = postalCode,
				ShipCountry = country,
			};
		}

		public static Products Product(
			int productId, string productName, int supplierId, int categoryId,
			string qtyPerUnit, decimal unitPrice, short unitsInStock,
			short unitsOnOrder, short reorderLevel, bool discontinued)
		{
			return new Products {
				Id = productId,
				ProductName = productName,
				SupplierId = supplierId,
				CategoryId = categoryId,
				QuantityPerUnit = qtyPerUnit,
				UnitPrice = unitPrice,
				UnitsInStock = unitsInStock,
				UnitsOnOrder = unitsOnOrder,
				ReorderLevel = reorderLevel,
				Discontinued = discontinued,
			};
		}

		public static OrderDetails OrderDetail(
			int orderId, int productId, decimal unitPrice, short quantity, double discount)
		{
			return new OrderDetails {
				Id = orderId,
				ProductId = productId,
				UnitPrice = unitPrice,
				Quantity = quantity,
				Discount = discount,
			};
		}

		public static CustomerCustomerDemo CustomerCustomerDemo(
			string customerId, string customerTypeId)
		{
			return new CustomerCustomerDemo {
				Id = customerId,
				CustomerTypeId = customerTypeId,
			};
		}

		public static Region Region(
			int regionId, string regionDescription)
		{
			return new Region {
				Id = regionId,
				RegionDescription = regionDescription,
			};
		}

		public static Territories Territory(
			string territoryId, string territoryDescription, int regionId)
		{
			return new Territories {
				Id = territoryId,
				TerritoryDescription = territoryDescription,
				RegionId = regionId,
			};
		}

		public static EmployeeTerritories EmployeeTerritory(
			int employeeId, string territoryId)
		{
			return new EmployeeTerritories {
				Id = employeeId,
				TerritoryId = territoryId,
			};
		}

	}
}