using System;

namespace ServiceStack.Text.Tests.Northwind;

public static class NorthwindDtoFactory
{
    public static CategoryDto Category(int id, string categoryName, string description, byte[] picture)
    {
        return new CategoryDto
        {
            Id = id,
            CategoryName = categoryName,
            Description = description,
            Picture = picture
        };
    }

    public static CustomerDto Customer(
        string customerId, string companyName, string contactName, string contactTitle,
        string address, string city, string region, string postalCode, string country,
        string phoneNo, string faxNo,
        byte[] picture)
    {
        return new CustomerDto
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

    public static EmployeeDto Employee(
        int employeeId, string lastName, string firstName, string title,
        string titleOfCourtesy, DateTime? birthDate, DateTime? hireDate,
        string address, string city, string region, string postalCode, string country,
        string homePhone, string extension,
        byte[] photo,
        string notes, int? reportsTo, string photoPath)
    {
        return new EmployeeDto
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

    public static ShipperDto Shipper(int id, string companyName, string phoneNo)
    {
        return new ShipperDto
        {
            Id = id,
            CompanyName = companyName,
            Phone = phoneNo,
        };
    }

    public static SupplierDto Supplier(
        int supplierId, string companyName, string contactName, string contactTitle,
        string address, string city, string region, string postalCode, string country,
        string phoneNo, string faxNo,
        string homePage)
    {
        return new SupplierDto
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

    public static OrderDto Order(
        int orderId, string customerId, int employeeId, DateTime? orderDate, DateTime? requiredDate,
        DateTime? shippedDate, int shipVia, decimal freight, string shipName,
        string address, string city, string region, string postalCode, string country)
    {
        return new OrderDto
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

    public static ProductDto Product(
        int productId, string productName, int supplierId, int categoryId,
        string qtyPerUnit, decimal unitPrice, short unitsInStock,
        short unitsOnOrder, short reorderLevel, bool discontinued)
    {
        return new ProductDto
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

    public static OrderDetailDto OrderDetail(
        int orderId, int productId, decimal unitPrice, short quantity, double discount)
    {
        return new OrderDetailDto
        {
            OrderId = orderId,
            ProductId = productId,
            UnitPrice = unitPrice,
            Quantity = quantity,
            Discount = discount,
        };
    }

    public static CustomerCustomerDemoDto CustomerCustomerDemo(
        string customerId, string customerTypeId)
    {
        return new CustomerCustomerDemoDto
        {
            Id = customerId,
            CustomerTypeId = customerTypeId,
        };
    }

    public static RegionDto Region(
        int regionId, string regionDescription)
    {
        return new RegionDto
        {
            Id = regionId,
            RegionDescription = regionDescription,
        };
    }

    public static TerritoryDto Territory(
        string territoryId, string territoryDescription, int regionId)
    {
        return new TerritoryDto
        {
            Id = territoryId,
            TerritoryDescription = territoryDescription,
            RegionId = regionId,
        };
    }

    public static EmployeeTerritoryDto EmployeeTerritory(
        int employeeId, string territoryId)
    {
        return new EmployeeTerritoryDto
        {
            EmployeeId = employeeId,
            TerritoryId = territoryId,
        };
    }

}