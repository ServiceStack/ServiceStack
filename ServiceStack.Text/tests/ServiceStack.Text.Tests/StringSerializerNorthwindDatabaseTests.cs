using System;
using System.Collections.Generic;
using Northwind.Common.DataModel;
using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
    [TestFixture]
#if NETCORE
    [Ignore("Fix Northwind.dll")]
#endif
    public class StringSerializerNorthwindDatabaseTests
        : TestBase
    {
        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            NorthwindData.LoadData(false);
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