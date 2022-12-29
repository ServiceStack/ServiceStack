using System.Collections.Generic;
using System.Runtime.Serialization;
using Northwind.Common.ServiceModel;

namespace Northwind.Common.DataModel
{
    [DataContract]
    public class NorthwindDtoData
    {
        public static NorthwindDtoData Instance = new NorthwindDtoData();

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
        public List<CustomerCustomerDemoDto> CustomerCustomerDemos { get; set; }
        [DataMember]
        public List<RegionDto> Regions { get; set; }
        [DataMember]
        public List<TerritoryDto> Territories { get; set; }
        [DataMember]
        public List<EmployeeTerritoryDto> EmployeeTerritories { get; set; }

        public static void LoadData(bool loadImages)
        {
            NorthwindData.LoadData(loadImages);

            Instance = new NorthwindDtoData
            {
                Categories = NorthwindData.Categories.ConvertAll(x => ToCategoryDto(x)),
                Customers = NorthwindData.Customers.ConvertAll(x => ToCustomerDto(x)),
                Employees = NorthwindData.Employees.ConvertAll(x => ToEmployeeDto(x)),
                Shippers = NorthwindData.Shippers.ConvertAll(x => ToShipperDto(x)),
                Suppliers = NorthwindData.Suppliers.ConvertAll(x => ToSupplierDto(x)),
                Orders = NorthwindData.Orders.ConvertAll(x => ToOrderDto(x)),
                Products = NorthwindData.Products.ConvertAll(x => ToProduct(x)),
                OrderDetails = NorthwindData.OrderDetails.ConvertAll(x => ToOrderDetailDto(x)),
                CustomerCustomerDemos = NorthwindData.CustomerCustomerDemos.ConvertAll(x => ToCustomerCustomerDemoDto(x)),
                Regions = NorthwindData.Regions.ConvertAll(x => ToRegionDto(x)),
                Territories = NorthwindData.Territories.ConvertAll(x => ToTerritoryDto(x)),
                EmployeeTerritories = NorthwindData.EmployeeTerritories.ConvertAll(x => ToEmployeeTerritoryDto(x)),
            };
        }

        public static CategoryDto ToCategoryDto(Category model)
        {
            return new CategoryDto
            {
                CategoryName = model.CategoryName,
                Description = model.Description,
                Id = model.Id,
                Picture = model.Picture,
            };
        }

        public static CustomerDto ToCustomerDto(Customer model)
        {
            return new CustomerDto
            {
                Id = model.Id,
                Picture = model.Picture,
                CompanyName = model.CompanyName,
                ContactName = model.ContactName,
                ContactTitle = model.ContactTitle,
                Fax = model.Fax,
                Phone = model.Phone,
                Address = model.Address,
                City = model.City,
                Country = model.Country,
                PostalCode = model.PostalCode,
                Region = model.Region,
            };
        }

        public static EmployeeDto ToEmployeeDto(Employee model)
        {
            return new EmployeeDto
            {
                Id = model.Id,
                Address = model.Address,
                City = model.City,
                Country = model.Country,
                PostalCode = model.PostalCode,
                Region = model.Region,
                BirthDate = model.BirthDate,
                Extension = model.Extension,
                FirstName = model.FirstName,
                HireDate = model.HireDate,
                HomePhone = model.HomePhone,
                LastName = model.LastName,
                Notes = model.Notes,
                Photo = model.Photo,
                PhotoPath = model.PhotoPath,
                ReportsTo = model.ReportsTo,
                Title = model.Title,
                TitleOfCourtesy = model.TitleOfCourtesy,
            };
        }

        public static ShipperDto ToShipperDto(Shipper model)
        {
            return new ShipperDto
            {
                Id = model.Id,
                CompanyName = model.CompanyName,
                Phone = model.Phone,
            };
        }

        public static SupplierDto ToSupplierDto(Supplier model)
        {
            return new SupplierDto
            {
                Id = model.Id,
                CompanyName = model.CompanyName,
                ContactName = model.ContactName,
                ContactTitle = model.ContactTitle,
                Fax = model.Fax,
                Phone = model.Phone,
                Address = model.Address,
                City = model.City,
                Country = model.Country,
                PostalCode = model.PostalCode,
                Region = model.Region,
                HomePage = model.HomePage,
            };
        }

        public static OrderDto ToOrderDto(Order model)
        {
            return new OrderDto
            {
                Id = model.Id,
                CustomerId = model.CustomerId,
                EmployeeId = model.EmployeeId,
                Freight = model.Freight,
                OrderDate = model.OrderDate,
                RequiredDate = model.RequiredDate,
                ShipAddress = model.ShipAddress,
                ShipCity = model.ShipCity,
                ShipCountry = model.ShipCountry,
                ShipName = model.ShipName,
                ShippedDate = model.ShippedDate,
                ShipPostalCode = model.ShipPostalCode,
                ShipRegion = model.ShipRegion,
                ShipVia = model.ShipVia,
            };
        }

        public static ProductDto ToProduct(Product model)
        {
            return new ProductDto
            {
                Id = model.Id,
                CategoryId = model.CategoryId,
                Discontinued = model.Discontinued,
                ProductName = model.ProductName,
                QuantityPerUnit = model.QuantityPerUnit,
                ReorderLevel = model.ReorderLevel,
                SupplierId = model.SupplierId,
                UnitPrice = model.UnitPrice,
                UnitsInStock = model.UnitsInStock,
                UnitsOnOrder = model.UnitsOnOrder,
            };
        }

        public static OrderDetailDto ToOrderDetailDto(OrderDetail model)
        {
            return new OrderDetailDto
            {
                Discount = model.Discount,
                OrderId = model.OrderId,
                ProductId = model.ProductId,
                Quantity = model.Quantity,
                UnitPrice = model.UnitPrice,
            };
        }

        public static CustomerCustomerDemoDto ToCustomerCustomerDemoDto(CustomerCustomerDemo model)
        {
            return new CustomerCustomerDemoDto
            {
                Id = model.Id,
                CustomerTypeId = model.CustomerTypeId,
            };
        }

        public static RegionDto ToRegionDto(Region model)
        {
            return new RegionDto
            {
                Id = model.Id,
                RegionDescription = model.RegionDescription,
            };
        }

        public static TerritoryDto ToTerritoryDto(Territory model)
        {
            return new TerritoryDto
            {
                Id = model.Id,
                RegionId = model.RegionId,
                TerritoryDescription = model.TerritoryDescription,
            };
        }

        public static EmployeeTerritoryDto ToEmployeeTerritoryDto(EmployeeTerritory model)
        {
            return new EmployeeTerritoryDto
            {
                EmployeeId = model.EmployeeId,
                TerritoryId = model.TerritoryId,
            };
        }

    }
}