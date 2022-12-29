using System;
using ServiceStack.DataAnnotations;
using ServiceStack.Model;

namespace Northwind.Common.DataModel
{
    [Alias("Employees")]
    public class Employee
        : IHasIntId, IEquatable<Employee>
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

        [StringLength(8000)]
        public string Notes { get; set; }

        [References(typeof(Employee))]
        public int? ReportsTo { get; set; }

        [StringLength(255)]
        public string PhotoPath { get; set; }

        public bool Equals(Employee other)
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
            return Equals((Employee)obj);
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

    [Alias("Categories")]
    public class Category
        : IHasIntId, IEquatable<Category>
    {
        [Alias("CategoryID")]
        public int Id { get; set; }

        [Index]
        [Required]
        [StringLength(15)]
        public string CategoryName { get; set; }

        [StringLength(100)]
        public string Description { get; set; }

        public byte[] Picture { get; set; }

        public bool Equals(Category other)
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
            return Equals((Category)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode * 397) ^ (CategoryName != null ? CategoryName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Description != null ? Description.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Picture != null ? Picture.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    [Alias("Customers")]
    public class Customer
        : IHasStringId, IEquatable<Customer>
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

        public bool Equals(Customer other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Id, other.Id) && string.Equals(CompanyName, other.CompanyName) && string.Equals(ContactName, other.ContactName) && string.Equals(ContactTitle, other.ContactTitle) && string.Equals(Address, other.Address) && string.Equals(City, other.City) && string.Equals(Region, other.Region) && string.Equals(PostalCode, other.PostalCode) && string.Equals(Country, other.Country) && string.Equals(Phone, other.Phone) && string.Equals(Fax, other.Fax) && Equals(Picture, other.Picture);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Customer)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Id != null ? Id.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (CompanyName != null ? CompanyName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ContactName != null ? ContactName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ContactTitle != null ? ContactTitle.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Address != null ? Address.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (City != null ? City.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Region != null ? Region.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (PostalCode != null ? PostalCode.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Country != null ? Country.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Phone != null ? Phone.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Fax != null ? Fax.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Picture != null ? Picture.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    [Alias("Shippers")]
    public class Shipper
        : IHasIntId, IEquatable<Shipper>
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

        public bool Equals(Shipper other)
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
            return Equals((Shipper)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode * 397) ^ (CompanyName != null ? CompanyName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Phone != null ? Phone.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    [Alias("Suppliers")]
    public class Supplier
        : IHasIntId, IEquatable<Supplier>
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

        public bool Equals(Supplier other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id && string.Equals(CompanyName, other.CompanyName) && string.Equals(ContactName, other.ContactName) && string.Equals(ContactTitle, other.ContactTitle) && string.Equals(Address, other.Address) && string.Equals(City, other.City) && string.Equals(Region, other.Region) && string.Equals(PostalCode, other.PostalCode) && string.Equals(Country, other.Country) && string.Equals(Phone, other.Phone) && string.Equals(Fax, other.Fax) && string.Equals(HomePage, other.HomePage);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Supplier)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode * 397) ^ (CompanyName != null ? CompanyName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ContactName != null ? ContactName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ContactTitle != null ? ContactTitle.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Address != null ? Address.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (City != null ? City.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Region != null ? Region.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (PostalCode != null ? PostalCode.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Country != null ? Country.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Phone != null ? Phone.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Fax != null ? Fax.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (HomePage != null ? HomePage.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    [Alias("Orders")]
    public class Order
        : IHasIntId, IEquatable<Order>
    {
        //[AutoIncrement]
        [Alias("OrderID")]
        public int Id { get; set; }

        [Index]
        [References(typeof(Customer))]
        [Alias("CustomerID")]
        [StringLength(5)]
        public string CustomerId { get; set; }

        [Index]
        [References(typeof(Employee))]
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

        public bool Equals(Order other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id && string.Equals(CustomerId, other.CustomerId) && EmployeeId == other.EmployeeId && OrderDate.Equals(other.OrderDate) && RequiredDate.Equals(other.RequiredDate) && ShippedDate.Equals(other.ShippedDate) && ShipVia == other.ShipVia && Freight == other.Freight && string.Equals(ShipName, other.ShipName) && string.Equals(ShipAddress, other.ShipAddress) && string.Equals(ShipCity, other.ShipCity) && string.Equals(ShipRegion, other.ShipRegion) && string.Equals(ShipPostalCode, other.ShipPostalCode) && string.Equals(ShipCountry, other.ShipCountry);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Order) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode*397) ^ (CustomerId != null ? CustomerId.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ EmployeeId;
                hashCode = (hashCode*397) ^ OrderDate.GetHashCode();
                hashCode = (hashCode*397) ^ RequiredDate.GetHashCode();
                hashCode = (hashCode*397) ^ ShippedDate.GetHashCode();
                hashCode = (hashCode*397) ^ ShipVia.GetHashCode();
                hashCode = (hashCode*397) ^ Freight.GetHashCode();
                hashCode = (hashCode*397) ^ (ShipName != null ? ShipName.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (ShipAddress != null ? ShipAddress.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (ShipCity != null ? ShipCity.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (ShipRegion != null ? ShipRegion.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (ShipPostalCode != null ? ShipPostalCode.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (ShipCountry != null ? ShipCountry.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    [Alias("Products")]
    public class Product
        : IHasIntId, IEquatable<Product>
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

        public bool Equals(Product other)
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
            return Equals((Product) obj);
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

    [Alias("Order Details")]
    public class OrderDetail
        : IHasStringId, IEquatable<OrderDetail>
    {
        public string Id => this.OrderId + "/" + this.ProductId;

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

        public bool Equals(OrderDetail other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return OrderId == other.OrderId && ProductId == other.ProductId && UnitPrice == other.UnitPrice && Quantity == other.Quantity && Discount.Equals(other.Discount);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((OrderDetail) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = OrderId;
                hashCode = (hashCode*397) ^ ProductId;
                hashCode = (hashCode*397) ^ UnitPrice.GetHashCode();
                hashCode = (hashCode*397) ^ Quantity.GetHashCode();
                hashCode = (hashCode*397) ^ Discount.GetHashCode();
                return hashCode;
            }
        }
    }

    public class CustomerCustomerDemo
        : IHasStringId, IEquatable<CustomerCustomerDemo>
    {
        [StringLength(5)]
        [Alias("CustomerID")]
        public string Id { get; set; }

        [StringLength(10)]
        [Alias("CustomerTypeID")]
        public string CustomerTypeId { get; set; }

        public bool Equals(CustomerCustomerDemo other)
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
            return Equals((CustomerCustomerDemo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Id != null ? Id.GetHashCode() : 0)*397) ^ (CustomerTypeId != null ? CustomerTypeId.GetHashCode() : 0);
            }
        }
    }

    [Alias("CustomerDemographics")]
    public class CustomerDemographic
        : IHasStringId, IEquatable<CustomerDemographic>
    {
        [StringLength(10)]
        [Alias("CustomerTypeID")]
        public string Id { get; set; }

        public string CustomerDesc { get; set; }

        public bool Equals(CustomerDemographic other)
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
            return Equals((CustomerDemographic) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Id != null ? Id.GetHashCode() : 0)*397) ^ (CustomerDesc != null ? CustomerDesc.GetHashCode() : 0);
            }
        }
    }

    public class Region
        : IHasIntId, IEquatable<Region>
    {
        [Alias("RegionID")]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string RegionDescription { get; set; }

        public bool Equals(Region other)
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
            return Equals((Region) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Id*397) ^ (RegionDescription != null ? RegionDescription.GetHashCode() : 0);
            }
        }
    }

    [Alias("Territories")]
    public class Territory
        : IHasStringId, IEquatable<Territory>
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

        public bool Equals(Territory other)
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
            return Equals((Territory) obj);
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

    [Alias("EmployeeTerritories")]
    public class EmployeeTerritory
        : IHasStringId, IEquatable<EmployeeTerritory>
    {
        public string Id { get { return this.EmployeeId + "/" + this.TerritoryId; } }

        [Alias("EmployeeID")]
        public int EmployeeId { get; set; }

        [Required]
        [StringLength(20)]
        [Alias("TerritoryID")]
        public string TerritoryId { get; set; }

        public bool Equals(EmployeeTerritory other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return EmployeeId == other.EmployeeId && string.Equals(TerritoryId, other.TerritoryId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((EmployeeTerritory) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (EmployeeId*397) ^ (TerritoryId != null ? TerritoryId.GetHashCode() : 0);
            }
        }
    }
}