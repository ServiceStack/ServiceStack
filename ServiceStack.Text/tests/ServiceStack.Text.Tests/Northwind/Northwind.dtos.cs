using System;
using System.Runtime.Serialization;
using ProtoBuf;
using ServiceStack.Model;

namespace ServiceStack.Text.Tests.Northwind;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, InferTagFromName = true)]
[DataContract]
[Serializable]
public class EmployeeDto
    : IHasIntId, IEquatable<EmployeeDto>
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
    public string Extension { get; set; }

    //Some serializers can't handle bytes so disabling for all
    //
    //[DataMember]
    public byte[] Photo { get; set; }

    [DataMember]
    public string Notes { get; set; }

    [DataMember]
    public int? ReportsTo { get; set; }

    [DataMember]
    public string PhotoPath { get; set; }

    public bool Equals(EmployeeDto other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id && string.Equals(LastName, other.LastName) && string.Equals(FirstName, other.FirstName) && string.Equals(Title, other.Title) && string.Equals(TitleOfCourtesy, other.TitleOfCourtesy) && BirthDate.Equals(other.BirthDate) && HireDate.Equals(other.HireDate) && string.Equals(Address, other.Address) && string.Equals(City, other.City) && string.Equals(Region, other.Region) && string.Equals(PostalCode, other.PostalCode) && string.Equals(Country, other.Country) && string.Equals(HomePhone, other.HomePhone) && string.Equals(Extension, other.Extension) && Equals(Photo, other.Photo) && string.Equals(Notes, other.Notes) && ReportsTo == other.ReportsTo && string.Equals(PhotoPath, other.PhotoPath);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((EmployeeDto)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Id;
            hashCode = (hashCode * 397) ^ (LastName != null ? LastName.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (FirstName != null ? FirstName.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Title != null ? Title.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (TitleOfCourtesy != null ? TitleOfCourtesy.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ BirthDate.GetHashCode();
            hashCode = (hashCode * 397) ^ HireDate.GetHashCode();
            hashCode = (hashCode * 397) ^ (Address != null ? Address.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (City != null ? City.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Region != null ? Region.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (PostalCode != null ? PostalCode.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Country != null ? Country.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (HomePhone != null ? HomePhone.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Extension != null ? Extension.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Photo != null ? Photo.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Notes != null ? Notes.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ ReportsTo.GetHashCode();
            hashCode = (hashCode * 397) ^ (PhotoPath != null ? PhotoPath.GetHashCode() : 0);
            return hashCode;
        }
    }
}

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, InferTagFromName = true)]

[DataContract]
[Serializable]
public class CategoryDto : IHasIntId, IEquatable<CategoryDto>
{
    [DataMember]
    public int Id { get; set; }

    [DataMember]
    public string CategoryName { get; set; }

    [DataMember]
    public string Description { get; set; }

    //[DataMember]
    public byte[] Picture { get; set; }

    public bool Equals(CategoryDto other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id && string.Equals(CategoryName, other.CategoryName) && string.Equals(Description, other.Description) && Equals(Picture, other.Picture);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((CategoryDto) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Id;
            hashCode = (hashCode*397) ^ (CategoryName != null ? CategoryName.GetHashCode() : 0);
            hashCode = (hashCode*397) ^ (Description != null ? Description.GetHashCode() : 0);
            hashCode = (hashCode*397) ^ (Picture != null ? Picture.GetHashCode() : 0);
            return hashCode;
        }
    }
}

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, InferTagFromName = true)]
[DataContract]
[Serializable]
public class CustomerDto
    : IHasStringId, IEquatable<CustomerDto>
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
    public string Fax { get; set; }

    //[DataMember]
    public byte[] Picture { get; set; }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((CustomerDto) obj);
    }

    public bool Equals(CustomerDto other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Id, other.Id) && string.Equals(CompanyName, other.CompanyName) && string.Equals(ContactName, other.ContactName) && string.Equals(ContactTitle, other.ContactTitle) && string.Equals(Address, other.Address) && string.Equals(City, other.City) && string.Equals(Region, other.Region) && string.Equals(PostalCode, other.PostalCode) && string.Equals(Country, other.Country) && string.Equals(Phone, other.Phone) && string.Equals(Fax, other.Fax) && Equals(Picture, other.Picture);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (Id != null ? Id.GetHashCode() : 0);
            hashCode = (hashCode*397) ^ (CompanyName != null ? CompanyName.GetHashCode() : 0);
            hashCode = (hashCode*397) ^ (ContactName != null ? ContactName.GetHashCode() : 0);
            hashCode = (hashCode*397) ^ (ContactTitle != null ? ContactTitle.GetHashCode() : 0);
            hashCode = (hashCode*397) ^ (Address != null ? Address.GetHashCode() : 0);
            hashCode = (hashCode*397) ^ (City != null ? City.GetHashCode() : 0);
            hashCode = (hashCode*397) ^ (Region != null ? Region.GetHashCode() : 0);
            hashCode = (hashCode*397) ^ (PostalCode != null ? PostalCode.GetHashCode() : 0);
            hashCode = (hashCode*397) ^ (Country != null ? Country.GetHashCode() : 0);
            hashCode = (hashCode*397) ^ (Phone != null ? Phone.GetHashCode() : 0);
            hashCode = (hashCode*397) ^ (Fax != null ? Fax.GetHashCode() : 0);
            hashCode = (hashCode*397) ^ (Picture != null ? Picture.GetHashCode() : 0);
            return hashCode;
        }
    }
}

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, InferTagFromName = true)]

[DataContract]
[Serializable]
public class ShipperDto : IHasIntId, IEquatable<ShipperDto>
{
    [DataMember]
    public int Id { get; set; }

    [DataMember]
    public string CompanyName { get; set; }

    [DataMember]
    public string Phone { get; set; }

    public bool Equals(ShipperDto other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id && string.Equals(CompanyName, other.CompanyName) && string.Equals(Phone, other.Phone);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((ShipperDto) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Id;
            hashCode = (hashCode*397) ^ (CompanyName != null ? CompanyName.GetHashCode() : 0);
            hashCode = (hashCode*397) ^ (Phone != null ? Phone.GetHashCode() : 0);
            return hashCode;
        }
    }
}

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, InferTagFromName = true)]

[DataContract]
[Serializable]
public class SupplierDto : IHasIntId
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

[DataContract]
[Serializable]
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
[DataContract]
[Serializable]
public class ProductDto : IHasIntId, IEquatable<ProductDto>
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

    public bool Equals(ProductDto other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id && string.Equals(ProductName, other.ProductName) && SupplierId == other.SupplierId && CategoryId == other.CategoryId && string.Equals(QuantityPerUnit, other.QuantityPerUnit) && UnitPrice == other.UnitPrice && UnitsInStock == other.UnitsInStock && UnitsOnOrder == other.UnitsOnOrder && ReorderLevel == other.ReorderLevel && Discontinued == other.Discontinued;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((ProductDto) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Id;
            hashCode = (hashCode*397) ^ (ProductName != null ? ProductName.GetHashCode() : 0);
            hashCode = (hashCode*397) ^ SupplierId;
            hashCode = (hashCode*397) ^ CategoryId;
            hashCode = (hashCode*397) ^ (QuantityPerUnit != null ? QuantityPerUnit.GetHashCode() : 0);
            hashCode = (hashCode*397) ^ UnitPrice.GetHashCode();
            hashCode = (hashCode*397) ^ UnitsInStock.GetHashCode();
            hashCode = (hashCode*397) ^ UnitsOnOrder.GetHashCode();
            hashCode = (hashCode*397) ^ ReorderLevel.GetHashCode();
            hashCode = (hashCode*397) ^ Discontinued.GetHashCode();
            return hashCode;
        }
    }
}

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, InferTagFromName = true)]
[DataContract]
[Serializable]
public class OrderDetailDto
    : IHasStringId
{
    [DataMember]
    public string Id { get; set; }
    //public string Id { get { return this.OrderId + "/" + this.ProductId; } } //Protobuf no like

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

[DataContract]
[Serializable]
public class CustomerCustomerDemoDto : IHasStringId, IEquatable<CustomerCustomerDemoDto>
{
    [DataMember]
    public string Id { get; set; }

    [DataMember]
    public string CustomerTypeId { get; set; }

    public bool Equals(CustomerCustomerDemoDto other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Id, other.Id) && string.Equals(CustomerTypeId, other.CustomerTypeId);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((CustomerCustomerDemoDto) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((Id != null ? Id.GetHashCode() : 0)*397) ^ (CustomerTypeId != null ? CustomerTypeId.GetHashCode() : 0);
        }
    }
}

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, InferTagFromName = true)]

[DataContract]
[Serializable]
public class CustomerDemographicDto : IHasStringId, IEquatable<CustomerDemographicDto>
{
    [DataMember]
    public string Id { get; set; }

    [DataMember]
    public string CustomerDesc { get; set; }

    public bool Equals(CustomerDemographicDto other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Id, other.Id) && string.Equals(CustomerDesc, other.CustomerDesc);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((CustomerDemographicDto) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((Id != null ? Id.GetHashCode() : 0)*397) ^ (CustomerDesc != null ? CustomerDesc.GetHashCode() : 0);
        }
    }
}

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, InferTagFromName = true)]

[DataContract]
[Serializable]
public class RegionDto : IHasIntId, IEquatable<RegionDto>
{
    [DataMember]
    public int Id { get; set; }

    [DataMember]
    public string RegionDescription { get; set; }

    public bool Equals(RegionDto other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id && string.Equals(RegionDescription, other.RegionDescription);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((RegionDto) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (Id*397) ^ (RegionDescription != null ? RegionDescription.GetHashCode() : 0);
        }
    }
}

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, InferTagFromName = true)]
[DataContract]
[Serializable]
public class TerritoryDto : IHasStringId, IEquatable<TerritoryDto>
{
    [DataMember]
    public string Id { get; set; }

    [DataMember]
    public string TerritoryDescription { get; set; }

    [DataMember]
    public int RegionId { get; set; }

    public bool Equals(TerritoryDto other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Id, other.Id) && string.Equals(TerritoryDescription, other.TerritoryDescription) && RegionId == other.RegionId;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((TerritoryDto) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (Id != null ? Id.GetHashCode() : 0);
            hashCode = (hashCode*397) ^ (TerritoryDescription != null ? TerritoryDescription.GetHashCode() : 0);
            hashCode = (hashCode*397) ^ RegionId;
            return hashCode;
        }
    }
}

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, InferTagFromName = true)]
[DataContract]
[Serializable]
public class EmployeeTerritoryDto : IHasStringId, IEquatable<EmployeeTerritoryDto>
{
    [DataMember]
    public string Id { get; set; }
    //public string Id { get { return this.EmployeeId + "/" + this.TerritoryId; } } //Protobuf no like

    [DataMember]
    public int EmployeeId { get; set; }

    [DataMember]
    public string TerritoryId { get; set; }

    public bool Equals(EmployeeTerritoryDto other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Id, other.Id) && EmployeeId == other.EmployeeId && string.Equals(TerritoryId, other.TerritoryId);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((EmployeeTerritoryDto) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (Id != null ? Id.GetHashCode() : 0);
            hashCode = (hashCode*397) ^ EmployeeId;
            hashCode = (hashCode*397) ^ (TerritoryId != null ? TerritoryId.GetHashCode() : 0);
            return hashCode;
        }
    }
}