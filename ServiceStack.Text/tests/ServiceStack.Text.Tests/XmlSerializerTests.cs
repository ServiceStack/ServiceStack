using System;
using Northwind.Common.DataModel;
using NUnit.Framework;
using ServiceStack.Text.Tests.Support;

namespace ServiceStack.Text.Tests
{
    [TestFixture]
#if !NETFRAMEWORK
    [Ignore("Fix Northwind.dll")]
#endif
    public class XmlSerializerTests
    {
        static XmlSerializerTests()
        {
            NorthwindData.LoadData(false);
        }

        public void Serialize<T>(T data)
        {
            //TODO: implement serializer and test properly
            var xml = XmlSerializer.SerializeToString(data);
            Console.WriteLine(xml);
        }

        [Test]
        public void Can_Serialize_Movie()
        {
            Serialize(MoviesData.Movies[0]);
        }

        [Test]
        public void Can_Serialize_Movies()
        {
            Serialize(MoviesData.Movies);
        }

        [Test]
        public void Can_Serialize_MovieResponse_Dto()
        {
            Serialize(new MovieResponse { Movie = MoviesData.Movies[0] });
        }

        [Test]
        public void serialize_Category()
        {
            Serialize(NorthwindData.Categories[0]);
        }

        [Test]
        public void serialize_Categories()
        {
            Serialize(NorthwindData.Categories);
        }

        [Test]
        public void serialize_Customer()
        {
            Serialize(NorthwindData.Customers[0]);
        }

        [Test]
        public void serialize_Customers()
        {
            Serialize(NorthwindData.Customers);
        }

        [Test]
        public void serialize_Employee()
        {
            Serialize(NorthwindData.Employees[0]);
        }

        [Test]
        public void serialize_Employees()
        {
            Serialize(NorthwindData.Employees);
        }

        [Test]
        public void serialize_EmployeeTerritory()
        {
            Serialize(NorthwindData.EmployeeTerritories[0]);
        }

        [Test]
        public void serialize_EmployeeTerritories()
        {
            Serialize(NorthwindData.EmployeeTerritories);
        }

        [Test]
        public void serialize_OrderDetail()
        {
            Serialize(NorthwindData.OrderDetails[0]);
        }

        [Test]
        public void serialize_OrderDetails()
        {
            Serialize(NorthwindData.OrderDetails);
        }

        [Test]
        public void serialize_Order()
        {
            Serialize(NorthwindData.Orders[0]);
        }

        [Test]
        public void serialize_Orders()
        {
            Serialize(NorthwindData.Orders);
        }

        [Test]
        public void serialize_Product()
        {
            Serialize(NorthwindData.Products[0]);
        }

        [Test]
        public void serialize_Products()
        {
            Serialize(NorthwindData.Products);
        }

        [Test]
        public void serialize_Region()
        {
            Serialize(NorthwindData.Regions[0]);
        }

        [Test]
        public void serialize_Regions()
        {
            Serialize(NorthwindData.Regions);
        }

        [Test]
        public void serialize_Shipper()
        {
            Serialize(NorthwindData.Shippers[0]);
        }

        [Test]
        public void serialize_Shippers()
        {
            Serialize(NorthwindData.Shippers);
        }

        [Test]
        public void serialize_Supplier()
        {
            Serialize(NorthwindData.Suppliers[0]);
        }

        [Test]
        public void serialize_Suppliers()
        {
            Serialize(NorthwindData.Suppliers);
        }

        [Test]
        public void serialize_Territory()
        {
            Serialize(NorthwindData.Territories[0]);
        }

        [Test]
        public void serialize_Territories()
        {
            Serialize(NorthwindData.Territories);
        }

    }
}