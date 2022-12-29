using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Northwind.Common.DataModel
{
    public static class NorthwindFactory
    {
        public static readonly List<Type> ModelTypes = new List<Type> {
            typeof(Employee),
            typeof(Category),
            typeof(Customer),
            typeof(Shipper),
            typeof(Supplier),
            typeof(Order),
            typeof(Product),
            typeof(OrderDetail),
            typeof(CustomerCustomerDemo),
            typeof(Category),
            typeof(CustomerDemographic),
            typeof(Region),
            typeof(Territory),
            typeof(EmployeeTerritory),
        };

        public static Category Category(int id, string categoryName, string description, byte[] picture)
        {
            return new Category
            {
                Id = id,
                CategoryName = categoryName,
                Description = description,
                Picture = picture
            };
        }

        public static Customer Customer(
            string customerId, string companyName, string contactName, string contactTitle,
            string address, string city, string region, string postalCode, string country,
            string phoneNo, string faxNo,
            byte[] picture)
        {
            return new Customer
            {
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

        public static Employee Employee(
            int employeeId, string lastName, string firstName, string title,
            string titleOfCourtesy, DateTime? birthDate, DateTime? hireDate,
            string address, string city, string region, string postalCode, string country,
            string homePhone, string extension,
            byte[] photo,
            string notes, int? reportsTo, string photoPath)
        {
            return new Employee
            {
                Id = employeeId,
                LastName = lastName,
                FirstName = firstName,
                Title = title,
                TitleOfCourtesy = titleOfCourtesy,
                BirthDate = birthDate,
                HireDate = hireDate,
                Address = address,
                City = city,
                Region = region,
                PostalCode = postalCode,
                Country = country,
                HomePhone = homePhone,
                Extension = extension,
                Photo = photo,
                Notes = notes,
                ReportsTo = reportsTo,
                PhotoPath = photoPath,
            };
        }

        public static Shipper Shipper(int id, string companyName, string phoneNo)
        {
            return new Shipper
            {
                Id = id,
                CompanyName = companyName,
                Phone = phoneNo,
            };
        }

        public static Supplier Supplier(
            int supplierId, string companyName, string contactName, string contactTitle,
            string address, string city, string region, string postalCode, string country,
            string phoneNo, string faxNo,
            string homePage)
        {
            return new Supplier
            {
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

        public static Order Order(
            int orderId, string customerId, int employeeId, DateTime? orderDate, DateTime? requiredDate,
            DateTime? shippedDate, int shipVia, decimal freight, string shipName,
            string address, string city, string region, string postalCode, string country)
        {
            return new Order
            {
                Id = orderId,
                CustomerId = customerId,
                EmployeeId = employeeId,
                OrderDate = orderDate,
                RequiredDate = requiredDate,
                ShippedDate = shippedDate,
                ShipVia = shipVia,
                Freight = freight,
                ShipName = shipName,
                ShipAddress = address,
                ShipCity = city,
                ShipRegion = region,
                ShipPostalCode = postalCode,
                ShipCountry = country,
            };
        }

        public static Product Product(
            int productId, string productName, int supplierId, int categoryId,
            string qtyPerUnit, decimal unitPrice, short unitsInStock,
            short unitsOnOrder, short reorderLevel, bool discontinued)
        {
            return new Product
            {
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

        public static OrderDetail OrderDetail(
            int orderId, int productId, decimal unitPrice, short quantity, double discount)
        {
            return new OrderDetail
            {
                OrderId = orderId,
                ProductId = productId,
                UnitPrice = unitPrice,
                Quantity = quantity,
                Discount = discount,
            };
        }

        public static CustomerCustomerDemo CustomerCustomerDemo(
            string customerId, string customerTypeId)
        {
            return new CustomerCustomerDemo
            {
                Id = customerId,
                CustomerTypeId = customerTypeId,
            };
        }

        public static Region Region(
            int regionId, string regionDescription)
        {
            return new Region
            {
                Id = regionId,
                RegionDescription = regionDescription,
            };
        }

        public static Territory Territory(
            string territoryId, string territoryDescription, int regionId)
        {
            return new Territory
            {
                Id = territoryId,
                TerritoryDescription = territoryDescription,
                RegionId = regionId,
            };
        }

        public static EmployeeTerritory EmployeeTerritory(
            int employeeId, string territoryId)
        {
            return new EmployeeTerritory
            {
                EmployeeId = employeeId,
                TerritoryId = territoryId,
            };
        }

    }
}