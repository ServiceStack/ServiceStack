using System;
using System.ComponentModel.DataAnnotations;
using ServiceStack.DataAnnotations;
using ServiceStack.DesignPatterns.Model;

namespace RedisWebServices.DataSource.Northwind
{
	[Alias("Employees")]
	public class Employee
		: IHasIntId
	{
		[AutoIncrement]
		[Alias("EmployeeID")]
		public int Id { get; set; }

		[Index]
		[Required]
		[StringLength(20)]
		public string LastName { get; set; }

		[Required]
		[StringLength(10)]
		public string FirstName { get; set; }

		[StringLength(30)]
		public string Title { get; set; }

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

		[References(typeof(Employee))]
		public int? ReportsTo { get; set; }

		[StringLength(255)]
		public string PhotoPath { get; set; }
	}

	[Alias("Categories")]
	public class Category
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

	[Alias("Customers")]
	public class Customer
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

	[Alias("Shippers")]
	public class Shipper
		: IHasIntId
	{
		[AutoIncrement]
		[Alias("ShipperID")]
		public int Id { get; set; }

		[Required]
		[Index(Unique = true)]
		[StringLength(40)]
		public string CompanyName { get; set; }

		[StringLength(24)]
		public string Phone { get; set; }
	}

	[Alias("Suppliers")]
	public class Supplier
		: IHasIntId
	{
		[AutoIncrement]
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

	[Alias("Orders")]
	public class Order
		: IHasIntId
	{
		[AutoIncrement]
		[Alias("OrderID")]
		public int Id { get; set; }

		[Index]
		[References(typeof(Customer))]
		[Alias("CustomerID")]
		[StringLength(5)]
		public string CustomerId { get; set; }

		[Index]
		[References(typeof(Customer))]
		[Alias("EmployeeID")]
		public int EmployeeId { get; set; }

		[Index]
		public DateTime? OrderDate { get; set; }

		public DateTime? RequiredDate { get; set; }

		[Index]
		public DateTime? ShippedDate { get; set; }

		[Index]
		[References(typeof(Shipper))]
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

	[Alias("Products")]
	public class Product
		: IHasIntId
	{
		[AutoIncrement]
		[Alias("ProductID")]
		public int Id { get; set; }

		[Index]
		[Required]
		[StringLength(40)]
		public string ProductName { get; set; }

		[Index]
		[Alias("SupplierID")]
		[References(typeof(Supplier))]		
		public int SupplierId { get; set; }

		[Index]
		[Alias("CategoryID")]
		[References(typeof(Category))]
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
	public class OrderDetail
		: IHasStringId
	{
		public string Id { get { return this.OrderId + "/" + this.ProductId; } }

		[Index]
		[Alias("OrderID")]
		[References(typeof(Order))]
		public int OrderId { get; set; }

		[Index]
		[Alias("ProductID")]
		[References(typeof(Product))]
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

	[Alias("CustomerDemographics")]
	public class CustomerDemographic
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

	[Alias("Territories")]
	public class Territory
		: IHasStringId
	{
		[StringLength(20)]
		[Alias("TerritoryID")]
		public string Id { get; set; }

		[Required]
		[StringLength(50)]
		public string TerritoryDescription { get; set; }

		[Alias("RegionID")]
		[References(typeof(Region))]
		public int RegionId { get; set; }
	}

	[Alias("EmployeeTerritories")]
	public class EmployeeTerritory
		: IHasStringId
	{
		public string Id { get { return this.EmployeeId + "/" + this.TerritoryId; } }

		[Alias("EmployeeID")]
		public int EmployeeId { get; set; }

		[Required]
		[StringLength(20)]
		[Alias("TerritoryID")]
		public string TerritoryId { get; set; }
	}
}