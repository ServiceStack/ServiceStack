using System;
using System.Collections.Generic;

namespace ServiceStack.ServiceHost.Tests.AppData
{
    public class Customers { }

    public class CustomersResponse : IHasResponseStatus
    {
        public CustomersResponse()
        {
            this.ResponseStatus = new ResponseStatus();
            this.Customers = new List<Customer>();
        }
        public List<Customer> Customers { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class CustomerDetails
    {
        public string Id { get; set; }
    }

    public class CustomerDetailsResponse : IHasResponseStatus
    {
        public CustomerDetailsResponse()
        {
            this.ResponseStatus = new ResponseStatus();
            this.CustomerOrders = new List<CustomerOrder>();
        }
        public Customer Customer { get; set; }
        public List<CustomerOrder> CustomerOrders { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class Orders
    {
        public int? Page { get; set; }
        public string CustomerId { get; set; }
    }

    public class OrdersResponse : IHasResponseStatus
    {
        public OrdersResponse()
        {
            this.ResponseStatus = new ResponseStatus();
            this.Results = new List<CustomerOrder>();
        }
        public List<CustomerOrder> Results { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class Category
    {
        public int Id { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }
    }

    public class Customer
    {
        public string Id { get; set; }
        public string CompanyName { get; set; }
        public string ContactName { get; set; }
        public string ContactTitle { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Region { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public string Phone { get; set; }
        public string Fax { get; set; }
        public string Email
        {
            get { return this.ContactName.Replace(" ", ".").ToLower() + "@gmail.com"; }
        }
    }

    public class CustomerCustomerDemo
    {
        public string Id { get; set; }
        public string CustomerTypeId { get; set; }
    }

    public class CustomerDemographic
    {
        public string Id { get; set; }
        public string CustomerDesc { get; set; }
    }

    public class CustomerOrder
    {
        public CustomerOrder()
        {
            this.OrderDetails = new List<OrderDetail>();
        }
        public Order Order { get; set; }
        public List<OrderDetail> OrderDetails { get; set; }
    }

    public class Employee
    {
        public int Id { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string Title { get; set; }
        public string TitleOfCourtesy { get; set; }
        public DateTime? BirthDate { get; set; }
        public DateTime? HireDate { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Region { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public string HomePhone { get; set; }
        public string Extension { get; set; }
        public byte[] Photo { get; set; }
        public string Notes { get; set; }
        public int? ReportsTo { get; set; }
        public string PhotoPath { get; set; }
    }

    public class EmployeeTerritory
    {
        public string Id { get { return this.EmployeeId + "/" + this.TerritoryId; } }
        public int EmployeeId { get; set; }
        public string TerritoryId { get; set; }
    }

    public class Order
    {
        public int Id { get; set; }
        public string CustomerId { get; set; }
        public int EmployeeId { get; set; }
        public DateTime? OrderDate { get; set; }
        public DateTime? RequiredDate { get; set; }
        public DateTime? ShippedDate { get; set; }
        public int? ShipVia { get; set; }
        public decimal Freight { get; set; }
        public string ShipName { get; set; }
        public string ShipAddress { get; set; }
        public string ShipCity { get; set; }
        public string ShipRegion { get; set; }
        public string ShipPostalCode { get; set; }
        public string ShipCountry { get; set; }
    }

    public class OrderDetail
    {
        public string Id { get { return this.OrderId + "/" + this.ProductId; } }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public decimal UnitPrice { get; set; }
        public short Quantity { get; set; }
        public double Discount { get; set; }
    }

    public class Product
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public int SupplierId { get; set; }
        public int CategoryId { get; set; }
        public string QuantityPerUnit { get; set; }
        public decimal UnitPrice { get; set; }
        public short UnitsInStock { get; set; }
        public short UnitsOnOrder { get; set; }
        public short ReorderLevel { get; set; }
        public bool Discontinued { get; set; }
    }

    public class Region
    {
        public int Id { get; set; }
        public string RegionDescription { get; set; }
    }

    public class Shipper
    {
        public int Id { get; set; }
        public string CompanyName { get; set; }
        public string Phone { get; set; }
    }

    public class Supplier
    {
        public int Id { get; set; }
        public string CompanyName { get; set; }
        public string ContactName { get; set; }
        public string ContactTitle { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Region { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public string Phone { get; set; }
        public string Fax { get; set; }
        public string HomePage { get; set; }
    }

    public class Territory
    {
        public string Id { get; set; }
        public string TerritoryDescription { get; set; }
        public int RegionId { get; set; }
    }
}