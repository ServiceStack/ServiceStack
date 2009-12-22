using System;
using System.ComponentModel.DataAnnotations;
using ServiceStack.DataAnnotations;
using ServiceStack.DesignPatterns.Model;

namespace ServiceStack.OrmLite.Tests.Models
{
	public class Employees
		: IHasIntId
	{
		[Alias("EmployeeID")]
		public int Id { get; set; }

		[Index]
		[Required]
		[StringLength(20)]
		public string LastName { get; set; }

		[Required]
		[StringLength(10)]
		public string FirstName { get; set; }

		[StringLength(25)]
		public string TitleOfCourtesy { get; set; }

		public DateTime? BirthDate { get; set; }

		public DateTime? HireDate { get; set; }

		[StringLength(60)]
		public string Address { get; set; }

		[StringLength(15)]
		public string City { get; set; }

		[StringLength(15)]
		public string Region { get; set; }

		[Index]
		[StringLength(10)]
		public string PostalCode { get; set; }

		[StringLength(15)]
		public string Country { get; set; }

		[StringLength(24)]
		public string HomePhone { get; set; }

		[StringLength(4)]
		public string Extension { get; set; }

		public byte[] Photo { get; set; }

		public string Notes { get; set; }

		[References(typeof(Employees))]
		public int ReportsTo { get; set; }

		[StringLength(255)]
		public string PhotoPath { get; set; }
	}

	public class Categories
		: IHasIntId
	{
		[Alias("CategoryID")]
		public int Id { get; set; }

		[Index]
		[Required]
		[StringLength(15)]
		public string CategoryName { get; set; }

		[StringLength(10)]
		public string Description { get; set; }

		public byte[] Picture { get; set; }
	}

	public class Customers
		: IHasStringId
	{
		[Required]
		[StringLength(5)]
		[Alias("CustomerID")]
		public string Id { get; set; }

		[Index]
		[Required]
		[StringLength(40)]
		public string CompanyName { get; set; }

		[StringLength(30)]
		public string ContactName { get; set; }

		[StringLength(30)]
		public string ContactTitle { get; set; }

		[StringLength(60)]
		public string Address { get; set; }

		[Index]
		[StringLength(15)]
		public string City { get; set; }

		[Index]
		[StringLength(15)]
		public string Region { get; set; }

		[Index]
		[StringLength(10)]
		public string PostalCode { get; set; }

		[StringLength(15)]
		public string Country { get; set; }

		[StringLength(24)]
		public string Phone { get; set; }

		[StringLength(24)]
		public string Fax { get; set; }

		public byte[] Picture { get; set; }
	}

	public class Shippers
		: IHasIntId
	{
		[Alias("ShipperID")]
		public int Id { get; set; }

		[Required]
		[StringLength(40)]
		public string CompanyName { get; set; }

		[StringLength(24)]
		public string Phone { get; set; }
	}

	public class Suppliers
		: IHasIntId
	{
		[Alias("SupplierID")]
		public int Id { get; set; }

		[Index]
		[Required]
		[StringLength(40)]
		public string CompanyName { get; set; }

		[StringLength(30)]
		public string ContactName { get; set; }

		[StringLength(30)]
		public string ContactTitle { get; set; }

		[StringLength(60)]
		public string Address { get; set; }

		[StringLength(15)]
		public string City { get; set; }

		[StringLength(15)]
		public string Region { get; set; }

		[Index]
		[StringLength(10)]
		public string PostalCode { get; set; }

		[StringLength(15)]
		public string Country { get; set; }

		[StringLength(24)]
		public string Phone { get; set; }

		[StringLength(24)]
		public string Fax { get; set; }

		public string HomePage { get; set; }
	}

	public class Orders
		: IHasIntId
	{
		[Alias("OrderID")]
		public int Id { get; set; }

		[Index]
		[References(typeof(Customers))]
		[Alias("CustomerID")]
		[StringLength(5)]
		public string CustomerId { get; set; }

		[Index]
		[References(typeof(Customers))]
		[Alias("EmployeeID")]
		public int EmployeeId { get; set; }

		[Index]
		public DateTime? OrderDate { get; set; }

		public DateTime? RequiredDate { get; set; }

		[Index]
		public DateTime? ShippedDate { get; set; }

		[Index]
		[References(typeof(Shippers))]
		public int? ShipVia { get; set; }

		public decimal Freight { get; set; }

		[StringLength(40)]
		public string ShipName { get; set; }

		[StringLength(60)]
		public string ShipAddress { get; set; }

		[StringLength(15)]
		public string ShipCity { get; set; }

		[StringLength(15)]
		public string ShipRegion { get; set; }

		[Index]
		[StringLength(10)]
		public string ShipPostalCode { get; set; }

		[StringLength(15)]
		public string ShipCountry { get; set; }
	}

	public class Products
		: IHasIntId
	{
		[Alias("ProductID")]
		public int Id { get; set; }

		[Index]
		[Required]
		[StringLength(40)]
		public string ProductName { get; set; }

		[Index]
		[Alias("SupplierID")]
		[References(typeof(Suppliers))]		
		public int SupplierId { get; set; }

		[Index]
		[Alias("CategoryID")]
		[References(typeof(Categories))]
		public int CategoryId { get; set; }

		[StringLength(20)]
		public string QuantityPerUnit { get; set; }

		[Range(0, double.MaxValue)]
		public decimal UnitPrice { get; set; }

		[Range(0, double.MaxValue)]
		public short UnitsInStock { get; set; }

		[Range(0, double.MaxValue)]
		public short UnitsOnOrder { get; set; }

		[Range(0, double.MaxValue)]
		public short ReorderLevel { get; set; }

		public bool Discontinued { get; set; }
	}

	[Alias("Order Details")]
	public class OrderDetails
		: IHasIntId
	{
		[Alias("OrderID")]
		public int Id { get; set; }

		[Index]
		[Alias("ProductID")]
		[References(typeof(Products))]
		public int ProductId { get; set; }

		[Range(0, double.MaxValue)]
		public decimal UnitPrice { get; set; }

		[Range(0, double.MaxValue)]
		public short Quantity { get; set; }

		[Range(0, double.MaxValue)]
		public double Discount { get; set; }
	}

	public class CustomerCustomerDemo
		: IHasStringId
	{
		[StringLength(5)]
		[Alias("CustomerID")]
		public string Id { get; set; }

		[StringLength(10)]
		[Alias("CustomerTypeID")]
		public string CustomerTypeId { get; set; }
	}

	public class CustomerDemographics
		: IHasStringId
	{
		[StringLength(10)]
		[Alias("CustomerTypeID")]
		public string Id { get; set; }

		public string CustomerDesc { get; set; }
	}

	public class Region
		: IHasIntId
	{
		[Alias("RegionID")]
		public int Id { get; set; }

		[Required]
		[StringLength(50)]
		public string RegionDescription { get; set; }
	}

	public class Territories
		: IHasStringId
	{
		[StringLength(20)]
		[Alias("TerritoryID")]
		public string Id { get; set; }

		[Required]
		[StringLength(50)]
		public string TerritoryDescription { get; set; }

		[Alias("RegionID")]
		public int RegionId { get; set; }
	}

	public class EmployeeTerritories
		: IHasIntId
	{
		[Alias("EmployeeID")]
		public int Id { get; set; }

		[Required]
		[StringLength(20)]
		[Alias("TerritoryID")]
		public string TerritoryId { get; set; }
	}
}