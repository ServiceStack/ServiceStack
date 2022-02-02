using System;
using System.Collections.Generic;
using System.Linq;
using Northwind.Common.DataModel;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.Text.Tests.JsvTests
{
    [TestFixture]
#if NETCORE
    [Ignore("Fix Northwind.dll")]
#endif
    public class TypeSerializerToStringDictionaryTests
    {
        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            NorthwindData.LoadData(false);
        }

        [Test]
        public void Can_serialize_ModelWithFieldsOfDifferentTypes_to_StringDictionary()
        {
            var model = new ModelWithFieldsOfDifferentTypes
            {
                Id = 1,
                Name = "Name1",
                LongId = 1000,
                Guid = new Guid("{7da74353-a40c-468e-93aa-7ff51f4f0e84}"),
                Bool = false,
                DateTime = new DateTime(2010, 12, 20),
                Double = 2.11d,
            };

            Console.WriteLine(model.Dump());
            /* Prints out:
            {
                Id: 1,
                Name: Name1,
                LongId: 1000,
                Guid: 7da74353a40c468e93aa7ff51f4f0e84,
                Bool: False,
                DateTime: 2010-12-20,
                Double: 2.11
            }
            */

            Dictionary<string, string> map = model.ToStringDictionary();
            Assert.That(map.EquivalentTo(
                new Dictionary<string, string>
                {
                    {"Id","1"},
                    {"Name","Name1"},
                    {"LongId","1000"},
                    {"Guid","7da74353a40c468e93aa7ff51f4f0e84"},
                    {"Bool","False"},
                    {"DateTime","2010-12-20"},
                    {"Double","2.11"},
                }));
        }

        [Test]
        public void Can_serialize_Category_to_StringDictionary()
        {
            Assert.That(NorthwindData.Categories[0].ToStringDictionary().EquivalentTo(
                new Dictionary<string, string>
                {
                    {"Id","1"},
                    {"CategoryName","Beverages"},
                    {"Description","Soft drinks, coffees, teas, beers, and ales"},
                }));
        }

        [Test]
        public void Can_serialize_Customer_to_StringDictionary()
        {
            Assert.That(NorthwindData.Customers[0].ToStringDictionary().EquivalentTo(
                new Dictionary<string, string>
                {
                    {"Id","ALFKI"},
                    {"CompanyName","Alfreds Futterkiste"},
                    {"ContactName","Maria Anders"},
                    {"ContactTitle","Sales Representative"},
                    {"Address","Obere Str. 57"},
                    {"City","Berlin"},
                    {"PostalCode","12209"},
                    {"Country","Germany"},
                    {"Phone","030-0074321"},
                    {"Fax","030-0076545"},
                }));
        }

        [Test]
        public void Can_serialize_Employee_to_StringDictionary()
        {
            var actual = NorthwindData.Employees
                .First(x => x.LastName == "Davolio")
                .ToStringDictionary();

            Assert.That(actual.EquivalentTo(
                new Dictionary<string, string>
                {
                    {"Id","2"},
                    {"LastName","Davolio"},
                    {"FirstName","Nancy"},
                    {"Title","Sales Representative"},
                    {"TitleOfCourtesy","Ms."},
                    {"BirthDate","1948-12-08"},
                    {"HireDate","1992-05-01"},
                    {"Address","507 - 20th Ave. E. Apt. 2A"},
                    {"City","Seattle"},
                    {"Region","WA"},
                    {"PostalCode","98122"},
                    {"Country","USA"},
                    {"HomePhone","(206) 555-9857"},
                    {"Extension","5467"},
                    {"Notes","Education includes a BA in psychology from Colorado State University in 1970.  She also completed 'The Art of the Cold Call.'  Nancy is a member of Toastmasters International."},
                    {"ReportsTo","1"},
                    {"PhotoPath","http://accweb/emmployees/davolio.bmp"},
                }));
        }

        [Test]
        public void Can_serialize_EmployeeTerritory_to_StringDictionary()
        {
            Assert.That(NorthwindData.EmployeeTerritories[0].ToStringDictionary().EquivalentTo(
                new Dictionary<string, string>
                {
                    {"Id","1/06897"},
                    {"EmployeeId","1"},
                    {"TerritoryId","06897"},
                }));
        }

        [Test]
        public void Can_serialize_OrderDetail_to_StringDictionary()
        {
            Assert.That(NorthwindData.OrderDetails[0].ToStringDictionary().EquivalentTo(
                new Dictionary<string, string>
                {
                    {"Id","10248/11"},
                    {"OrderId","10248"},
                    {"ProductId","11"},
                    {"UnitPrice","14"},
                    {"Quantity","12"},
                    {"Discount","0"},
                }));
        }

        [Test]
        public void Can_serialize_Order_to_StringDictionary()
        {
            Assert.That(NorthwindData.Orders[0].ToStringDictionary().EquivalentTo(
                new Dictionary<string, string>
                {
                    {"Id","10248"},
                    {"CustomerId","VINET"},
                    {"EmployeeId","5"},
                    {"OrderDate","1996-07-04"},
                    {"RequiredDate","1996-08-01"},
                    {"ShippedDate","1996-07-16"},
                    {"ShipVia","3"},
                    {"Freight","32.38"},
                    {"ShipName","Vins et alcools Chevalier"},
                    {"ShipAddress","59 rue de l'Abbaye"},
                    {"ShipCity","Reims"},
                    {"ShipPostalCode","51100"},
                    {"ShipCountry","France"},
                }));
        }

        [Test]
        public void Can_serialize_Product_to_StringDictionary()
        {
            Console.WriteLine(NorthwindData.Products[0].ToStringDictionary());
            Assert.That(NorthwindData.Products[0].ToStringDictionary().EquivalentTo(
                new Dictionary<string, string>
                {
                    {"Id","1"},
                    {"ProductName","Chai"},
                    {"SupplierId","1"},
                    {"CategoryId","1"},
                    {"QuantityPerUnit","10 boxes x 20 bags"},
                    {"UnitPrice","18"},
                    {"UnitsInStock","39"},
                    {"UnitsOnOrder","0"},
                    {"ReorderLevel","10"},
                    {"Discontinued","False"},
                }));
        }

        [Test]
        public void Can_serialize_Region_to_StringDictionary()
        {
            Assert.That(NorthwindData.Regions[0].ToStringDictionary().EquivalentTo(
                new Dictionary<string, string>
                {
                    {"Id","1"},
                    {"RegionDescription","Eastern"},
                }));
        }

        [Test]
        public void Can_serialize_Shipper_to_StringDictionary()
        {
            Assert.That(NorthwindData.Shippers[0].ToStringDictionary().EquivalentTo(
                new Dictionary<string, string>
                {
                    {"Id","1"},
                    {"CompanyName","Speedy Express"},
                    {"Phone","(503) 555-9831"},
                }));
        }

        [Test]
        public void Can_serialize_Supplier_to_StringDictionary()
        {
            Assert.That(NorthwindData.Suppliers[0].ToStringDictionary().EquivalentTo(
                new Dictionary<string, string>
                {
                    {"Id","1"},
                    {"CompanyName","Exotic Liquids"},
                    {"ContactName","Charlotte Cooper"},
                    {"ContactTitle","Purchasing Manager"},
                    {"Address","49 Gilbert St."},
                    {"City","London"},
                    {"PostalCode","EC1 4SD"},
                    {"Country","UK"},
                    {"Phone","(171) 555-2222"},
                }));
        }

        [Test]
        public void Can_serialize_Territory_to_StringDictionary()
        {
            Assert.That(NorthwindData.Territories[0].ToStringDictionary().EquivalentTo(
                new Dictionary<string, string>
                {
                    {"Id","01581"},
                    {"TerritoryDescription","Westboro"},
                    {"RegionId","1"},
                }));
        }
    }

}