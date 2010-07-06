using System;
using System.Runtime.Serialization;
using ServiceStack.DesignPatterns.Model;

namespace RedisWebServices.DataSource.Northwind
{
	[DataContract]
	public class EmployeeDto
		: IHasIntId
	{
		[DataMember]
		public int Id { get; set; }
		[DataMember]
		public string LastName { get; set; }
		[DataMember]
		public string FirstName { get; set; }
		[DataMember]
		public string Title { get; set; }
		[DataMember]
		public string TitleOfCourtesy { get; set; }
		[DataMember]
		public DateTime? BirthDate { get; set; }
		[DataMember]
		public DateTime? HireDate { get; set; }
		[DataMember]
		public string Address { get; set; }
		[DataMember]
		public string City { get; set; }
		[DataMember]
		public string Region { get; set; }
		[DataMember]
		public string PostalCode { get; set; }
		[DataMember]
		public string Country { get; set; }
		[DataMember]
		public string HomePhone { get; set; }
		[DataMember]
		public string Extension { get; set; }		//Some serializers can't handle bytes so disabling for all
		//
		//[DataMember]
		public byte[] Photo { get; set; }
		[DataMember]
		public string Notes { get; set; }
		[DataMember]
		public int? ReportsTo { get; set; }
		[DataMember]
		public string PhotoPath { get; set; }
	}

	[DataContract]
	public class CategoryDto
		: IHasIntId
	{
		[DataMember]
		public int Id { get; set; }
		[DataMember]
		public string CategoryName { get; set; }
		[DataMember]
		public string Description { get; set; }		//
		//[DataMember]
		public byte[] Picture { get; set; }
	}

	[DataContract]
	public class CustomerDto
		: IHasStringId
	{
		[DataMember]
		public string Id { get; set; }
		[DataMember]
		public string CompanyName { get; set; }
		[DataMember]
		public string ContactName { get; set; }
		[DataMember]
		public string ContactTitle { get; set; }
		[DataMember]
		public string Address { get; set; }
		[DataMember]
		public string City { get; set; }
		[DataMember]
		public string Region { get; set; }
		[DataMember]
		public string PostalCode { get; set; }
		[DataMember]
		public string Country { get; set; }
		[DataMember]
		public string Phone { get; set; }
		[DataMember]
		public string Fax { get; set; }		//
		//[DataMember]
		public byte[] Picture { get; set; }
		public override bool Equals(object obj)
		{
			var other = obj as CustomerDto;
			if (other == null) return false; return this.Address == other.Address
													&& this.City == other.City
													&& this.CompanyName == other.CompanyName
													&& this.ContactName == other.ContactName
													&& this.ContactTitle == other.ContactTitle
													&& this.Country == other.Country
													&& this.Fax == other.Fax
													&& this.Id == other.Id
													&& this.Phone == other.Phone
													&& this.Picture == other.Picture
													&& this.PostalCode == other.PostalCode
													&& this.Region == other.Region;
		}
	}

	[DataContract]
	public class ShipperDto
		: IHasIntId
	{
		[DataMember]
		public int Id { get; set; }
		[DataMember]
		public string CompanyName { get; set; }
		[DataMember]
		public string Phone { get; set; }
	}

	[DataContract]
	public class SupplierDto
		: IHasIntId
	{
		[DataMember]
		public int Id { get; set; }
		[DataMember]
		public string CompanyName { get; set; }
		[DataMember]
		public string ContactName { get; set; }
		[DataMember]
		public string ContactTitle { get; set; }
		[DataMember]
		public string Address { get; set; }
		[DataMember]
		public string City { get; set; }
		[DataMember]
		public string Region { get; set; }
		[DataMember]
		public string PostalCode { get; set; }
		[DataMember]
		public string Country { get; set; }
		[DataMember]
		public string Phone { get; set; }
		[DataMember]
		public string Fax { get; set; }
		[DataMember]
		public string HomePage { get; set; }
		public override bool Equals(object obj)
		{
			var other = obj as SupplierDto;
			if (other == null) return false; return this.Id == other.Id
													&& this.CompanyName == other.CompanyName
													&& this.ContactName == other.ContactName
													&& this.ContactTitle == other.ContactTitle
													&& this.Address == other.Address
													&& this.City == other.City
													&& this.Region == other.Region
													&& this.PostalCode == other.PostalCode
													&& this.Country == other.Country
													&& this.Phone == other.Phone
													&& this.Fax == other.Fax
													&& this.HomePage == other.HomePage;
		}
	}

	[DataContract]
	public class OrderDto
		: IHasIntId
	{
		[DataMember]
		public int Id { get; set; }
		[DataMember]
		public string CustomerId { get; set; }
		[DataMember]
		public int EmployeeId { get; set; }
		[DataMember]
		public DateTime? OrderDate { get; set; }
		[DataMember]
		public DateTime? RequiredDate { get; set; }
		[DataMember]
		public DateTime? ShippedDate { get; set; }
		[DataMember]
		public int? ShipVia { get; set; }
		[DataMember]
		public decimal Freight { get; set; }
		[DataMember]
		public string ShipName { get; set; }
		[DataMember]
		public string ShipAddress { get; set; }
		[DataMember]
		public string ShipCity { get; set; }
		[DataMember]
		public string ShipRegion { get; set; }
		[DataMember]
		public string ShipPostalCode { get; set; }
		[DataMember]
		public string ShipCountry { get; set; }
		public override bool Equals(object obj)
		{
			var other = obj as OrderDto;
			if (other == null) return false; return this.Id == other.Id
													&& this.CustomerId == other.CustomerId
													&& this.EmployeeId == other.EmployeeId
													&& this.OrderDate == other.OrderDate
													&& this.RequiredDate == other.RequiredDate
													&& this.ShippedDate == other.ShippedDate
													&& this.ShipVia == other.ShipVia
													&& this.Freight == other.Freight
													&& this.ShipName == other.ShipName
													&& this.ShipAddress == other.ShipAddress
													&& this.ShipCity == other.ShipCity
													&& this.ShipRegion == other.ShipRegion
													&& this.ShipPostalCode == other.ShipPostalCode
													&& this.ShipCountry == other.ShipCountry;
		}
	}

	[DataContract]
	public class ProductDto
		: IHasIntId
	{
		[DataMember]
		public int Id { get; set; }
		[DataMember]
		public string ProductName { get; set; }
		[DataMember]
		public int SupplierId { get; set; }
		[DataMember]
		public int CategoryId { get; set; }
		[DataMember]
		public string QuantityPerUnit { get; set; }
		[DataMember]
		public decimal UnitPrice { get; set; }
		[DataMember]
		public short UnitsInStock { get; set; }
		[DataMember]
		public short UnitsOnOrder { get; set; }
		[DataMember]
		public short ReorderLevel { get; set; }
		[DataMember]
		public bool Discontinued { get; set; }
	}

	[DataContract]
	public class OrderDetailDto
		: IHasStringId
	{
		public string Id { get { return this.OrderId + "/" + this.ProductId; } }

		[DataMember]
		public int OrderId { get; set; }

		[DataMember]
		public int ProductId { get; set; }

		[DataMember]
		public decimal UnitPrice { get; set; }

		[DataMember]
		public short Quantity { get; set; }

		[DataMember]
		public double Discount { get; set; }

		public override bool Equals(object obj)
		{
			var other = obj as OrderDetailDto;
			if (other == null) return false; return this.Id == other.Id
													&& this.OrderId == other.OrderId
													&& this.ProductId == other.ProductId
													&& this.UnitPrice == other.UnitPrice
													&& this.Quantity == other.Quantity
													&& this.Discount == other.Discount;
		}
	}

	[DataContract]
	public class CustomerCustomerDemoDto
		: IHasStringId
	{
		[DataMember]
		public string Id { get; set; }
		[DataMember]
		public string CustomerTypeId { get; set; }
	}

	[DataContract]
	public class CustomerDemographicDto
		: IHasStringId
	{
		[DataMember]
		public string Id { get; set; }
		[DataMember]
		public string CustomerDesc { get; set; }
	}

	[DataContract]
	public class RegionDto
		: IHasIntId
	{
		[DataMember]
		public int Id { get; set; }
		[DataMember]
		public string RegionDescription { get; set; }
	}

	[DataContract]
	public class TerritoryDto
		: IHasStringId
	{
		[DataMember]
		public string Id { get; set; }
		[DataMember]
		public string TerritoryDescription { get; set; }
		[DataMember]
		public int RegionId { get; set; }
	}

	[DataContract]
	public class EmployeeTerritoryDto
		: IHasStringId
	{
		public string Id { get { return this.EmployeeId + "/" + this.TerritoryId; } }
		[DataMember]
		public int EmployeeId { get; set; }
		[DataMember]
		public string TerritoryId { get; set; }
	}
}