using System;
using System.Runtime.Serialization;
using Platform.Text;
using ProtoBuf;
using ServiceStack.DesignPatterns.Model;

namespace Northwind.Common.ServiceModel
{

	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, InferTagFromName = true)]
	[TextRecord]
	[DataContract]
	[Serializable]
	public class EmployeeDto
		: IHasIntId
	{
		[TextField]
		[DataMember]
		public int Id { get; set; }

		[TextField]
		[DataMember]
		public string LastName { get; set; }

		[TextField]
		[DataMember]
		public string FirstName { get; set; }

		[TextField]
		[DataMember]
		public string Title { get; set; }

		[TextField]
		[DataMember]
		public string TitleOfCourtesy { get; set; }

		[TextField]
		[DataMember]
		public DateTime? BirthDate { get; set; }

		[TextField]
		[DataMember]
		public DateTime? HireDate { get; set; }

		[TextField]
		[DataMember]
		public string Address { get; set; }

		[TextField]
		[DataMember]
		public string City { get; set; }

		[TextField]
		[DataMember]
		public string Region { get; set; }

		[TextField]
		[DataMember]
		public string PostalCode { get; set; }

		[TextField]
		[DataMember]
		public string Country { get; set; }

		[TextField]
		[DataMember]
		public string HomePhone { get; set; }

		[TextField]
		[DataMember]
		public string Extension { get; set; }

		//[TextField]
		[DataMember]
		public byte[] Photo { get; set; }

		[TextField]
		[DataMember]
		public string Notes { get; set; }

		[TextField]
		[DataMember]
		public int? ReportsTo { get; set; }

		[TextField]
		[DataMember]
		public string PhotoPath { get; set; }
	}

	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, InferTagFromName = true)]
	[TextRecord]
	[DataContract]
	[Serializable]
	public class CategoryDto
		: IHasIntId
	{
		[TextField]
		[DataMember]
		public int Id { get; set; }

		[TextField]
		[DataMember]
		public string CategoryName { get; set; }

		[TextField]
		[DataMember]
		public string Description { get; set; }

		//[TextField]
		[DataMember]
		public byte[] Picture { get; set; }
	}

	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, InferTagFromName = true)]
	[TextRecord]
	[DataContract]
	[Serializable]
	public class CustomerDto
		: IHasStringId
	{
		[TextField]
		[DataMember]
		public string Id { get; set; }

		[TextField]
		[DataMember]
		public string CompanyName { get; set; }

		[TextField]
		[DataMember]
		public string ContactName { get; set; }

		[TextField]
		[DataMember]
		public string ContactTitle { get; set; }

		[TextField]
		[DataMember]
		public string Address { get; set; }

		[TextField]
		[DataMember]
		public string City { get; set; }

		[TextField]
		[DataMember]
		public string Region { get; set; }

		[TextField]
		[DataMember]
		public string PostalCode { get; set; }

		[TextField]
		[DataMember]
		public string Country { get; set; }

		[TextField]
		[DataMember]
		public string Phone { get; set; }

		[TextField]
		[DataMember]
		public string Fax { get; set; }

		//[TextField]
		[DataMember]
		public byte[] Picture { get; set; }

		public override bool Equals(object obj)
		{
			var other = obj as CustomerDto;
			if (other == null) return false;

			return this.Address == other.Address
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

	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, InferTagFromName = true)]
	[TextRecord]
	[DataContract]
	[Serializable]
	public class ShipperDto
		: IHasIntId
	{
		[TextField]
		[DataMember]
		public int Id { get; set; }

		[TextField]
		[DataMember]
		public string CompanyName { get; set; }

		[TextField]
		[DataMember]
		public string Phone { get; set; }
	}

	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, InferTagFromName = true)]
	[TextRecord]
	[DataContract]
	[Serializable]
	public class SupplierDto
		: IHasIntId
	{
		[TextField]
		[DataMember]
		public int Id { get; set; }

		[TextField]
		[DataMember]
		public string CompanyName { get; set; }

		[TextField]
		[DataMember]
		public string ContactName { get; set; }

		[TextField]
		[DataMember]
		public string ContactTitle { get; set; }

		[TextField]
		[DataMember]
		public string Address { get; set; }

		[TextField]
		[DataMember]
		public string City { get; set; }

		[TextField]
		[DataMember]
		public string Region { get; set; }

		[TextField]
		[DataMember]
		public string PostalCode { get; set; }

		[TextField]
		[DataMember]
		public string Country { get; set; }

		[TextField]
		[DataMember]
		public string Phone { get; set; }

		[TextField]
		[DataMember]
		public string Fax { get; set; }

		[TextField]
		[DataMember]
		public string HomePage { get; set; }

		public override bool Equals(object obj)
		{
			var other = obj as SupplierDto;
			if (other == null) return false;

			return this.Id == other.Id
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

	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, InferTagFromName = true)]
	[TextRecord]
	[DataContract]
	[Serializable]
	public class OrderDto
		: IHasIntId
	{
		[TextField]
		[DataMember]
		public int Id { get; set; }

		[TextLiteral]
		[TextField]
		[DataMember]
		public string CustomerId { get; set; }

		[TextField]
		[DataMember]
		public int EmployeeId { get; set; }

		[TextField]
		[DataMember]
		public DateTime? OrderDate { get; set; }

		[TextField]
		[DataMember]
		public DateTime? RequiredDate { get; set; }

		[TextField]
		[DataMember]
		public DateTime? ShippedDate { get; set; }

		[TextField]
		[DataMember]
		public int? ShipVia { get; set; }

		[TextField]
		[DataMember]
		public decimal Freight { get; set; }

		[TextLiteral]
		[TextField]
		[DataMember]
		public string ShipName { get; set; }

		[TextLiteral]
		[TextField]
		[DataMember]
		public string ShipAddress { get; set; }

		[TextLiteral]
		[TextField]
		[DataMember]
		public string ShipCity { get; set; }

		[TextLiteral]
		[TextField]
		[DataMember]
		public string ShipRegion { get; set; }

		[TextLiteral]
		[TextField]
		[DataMember]
		public string ShipPostalCode { get; set; }

		[TextLiteral]
		[TextField]
		[DataMember]
		public string ShipCountry { get; set; }

		public override bool Equals(object obj)
		{
			var other = obj as OrderDto;
			if (other == null) return false;

			return this.Id == other.Id
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

	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, InferTagFromName = true)]
	[TextRecord]
	[DataContract]
	[Serializable]
	public class ProductDto
		: IHasIntId
	{
		[TextField]
		[DataMember]
		public int Id { get; set; }

		[TextField]
		[DataMember]
		public string ProductName { get; set; }

		[TextField]
		[DataMember]
		public int SupplierId { get; set; }

		[TextField]
		[DataMember]
		public int CategoryId { get; set; }

		[TextField]
		[DataMember]
		public string QuantityPerUnit { get; set; }

		[TextField]
		[DataMember]
		public decimal UnitPrice { get; set; }

		[TextField]
		[DataMember]
		public short UnitsInStock { get; set; }

		[TextField]
		[DataMember]
		public short UnitsOnOrder { get; set; }

		[TextField]
		[DataMember]
		public short ReorderLevel { get; set; }

		[TextField]
		[DataMember]
		public bool Discontinued { get; set; }
	}

	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, InferTagFromName = true)]
	[TextRecord]
	[DataContract]
	[Serializable]
	public class OrderDetailDto
		: IHasStringId
	{
		public string Id { get { return this.OrderId + "/" + this.ProductId; } }

		[TextField]
		[DataMember]
		public int OrderId { get; set; }

		[TextField]
		[DataMember]
		public int ProductId { get; set; }

		[TextField]
		[DataMember]
		public decimal UnitPrice { get; set; }

		[TextField]
		[DataMember]
		public short Quantity { get; set; }

		[TextField]
		[DataMember]
		public double Discount { get; set; }

		public override bool Equals(object obj)
		{
			var other = obj as OrderDetailDto;
			if (other == null) return false;

			return this.Id == other.Id
				   && this.OrderId == other.OrderId
				   && this.ProductId == other.ProductId
				   && this.UnitPrice == other.UnitPrice
				   && this.Quantity == other.Quantity
				   && this.Discount == other.Discount;
		}
	}

	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, InferTagFromName = true)]
	[TextRecord]
	[DataContract]
	[Serializable]
	public class CustomerCustomerDemoDto
		: IHasStringId
	{
		[TextField]
		[DataMember]
		public string Id { get; set; }

		[TextField]
		[DataMember]
		public string CustomerTypeId { get; set; }
	}

	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, InferTagFromName = true)]
	[TextRecord]
	[DataContract]
	[Serializable]
	public class CustomerDemographicDto
		: IHasStringId
	{
		[TextField]
		[DataMember]
		public string Id { get; set; }

		[TextField]
		[DataMember]
		public string CustomerDesc { get; set; }
	}

	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, InferTagFromName = true)]
	[TextRecord]
	[DataContract]
	[Serializable]
	public class RegionDto
		: IHasIntId
	{
		[TextField]
		[DataMember]
		public int Id { get; set; }

		[TextField]
		[DataMember]
		public string RegionDescription { get; set; }
	}

	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, InferTagFromName = true)]
	[TextRecord]
	[DataContract]
	[Serializable]
	public class TerritoryDto
		: IHasStringId
	{
		[TextField]
		[DataMember]
		public string Id { get; set; }

		[TextField]
		[DataMember]
		public string TerritoryDescription { get; set; }

		[TextField]
		[DataMember]
		public int RegionId { get; set; }
	}

	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, InferTagFromName = true)]
	[TextRecord]
	[DataContract]
	[Serializable]
	public class EmployeeTerritoryDto
		: IHasStringId
	{
		[TextField]
		public string Id { get { return this.EmployeeId + "/" + this.TerritoryId; } }

		[TextField]
		[DataMember]
		public int EmployeeId { get; set; }

		[TextField]
		[DataMember]
		public string TerritoryId { get; set; }
	}
}