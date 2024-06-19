using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class Customer
    {
        public string CustomerId { get; set; }
        public string CompanyName { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Region { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public string Phone { get; set; }
        public string Fax { get; set; }
        public List<Order> Orders { get; set; }

        public override string ToString() =>
            $"Customer(customerId='{CustomerId}', companyName='{CompanyName}', orders='{Orders.Count}')";
    }

    public class Order
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public double Total { get; set; }
    }
    
    public class Product
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string Category { get; set; }
        public double UnitPrice { get; set; }
        public int UnitsInStock { get; set; }

        public Product() {}
        public Product(int productId, string productName, string category, double unitPrice, int unitsInStock)
        {
            ProductId = productId;
            ProductName = productName;
            Category = category;
            UnitPrice = unitPrice;
            UnitsInStock = unitsInStock;
        }
    }
    
    public class QueryData
    {
        public static Product[] Products = 
        {
            new(1, "Chai", "Beverages", 18.000, 39),
            new(2, "Chang", "Beverages", 19.000, 17),
            new(3, "Aniseed Syrup", "Condiments", 10.000, 13),
            new(4, "Chef Anton's Cajun Seasoning", "Condiments", 22.000, 53),
            new(5, "Chef Anton's Gumbo Mix", "Condiments", 21.350, 0),
            new(6, "Grandma's Boysenberry Spread", "Condiments", 25.000, 120),
            new(7, "Uncle Bob's Organic Dried Pears", "Produce", 30.000, 15),
            new(8, "Northwoods Cranberry Sauce", "Condiments", 40.000, 6),
            new(9, "Mishi Kobe Niku", "Meat/Poultry", 97.000, 29),
            new(10, "Ikura", "Seafood", 31.000, 31),
            new(11, "Queso Cabrales", "Dairy Products", 21.000, 22),
            new(12, "Queso Manchego La Pastora", "Dairy Products", 38.000, 86),
            new(13, "Konbu", "Seafood", 6.000, 24),
            new(14, "Tofu", "Produce", 23.250, 35),
            new(15, "Genen Shouyu", "Condiments", 15.500, 39),
            new(16, "Pavlova", "Confections", 17.450, 29),
            new(17, "Alice Mutton", "Meat/Poultry", 39.000, 0),
            new(18, "Carnarvon Tigers", "Seafood", 62.500, 42),
            new(19, "Teatime Chocolate Biscuits", "Confections", 9.200, 25),
            new(20, "Sir Rodney's Marmalade", "Confections", 81.000, 40),
            new(21, "Sir Rodney's Scones", "Confections", 10.000, 3),
            new(22, "Gustaf's Kn\u00e4ckebr\u00f6d", "Grains/Cereals", 21.000, 104),
            new(23, "Tunnbr\u00f6d", "Grains/Cereals", 9.000, 61),
            new(24, "Guaran\u00e1 Fant\u00e1stica", "Beverages", 4.500, 20),
            new(25, "NuNuCa Nu\u00df-Nougat-Creme", "Confections", 14.000, 76),
            new(26, "Gumb\u00e4r Gummib\u00e4rchen", "Confections", 31.230, 15),
            new(27, "Schoggi Schokolade", "Confections", 43.900, 49),
            new(28, "R\u00f6ssle Sauerkraut", "Produce", 45.600, 26),
            new(29, "Th\u00fcringer Rostbratwurst", "Meat/Poultry", 123.790, 0),
            new(30, "Nord-Ost Matjeshering", "Seafood", 25.890, 10),
            new(31, "Gorgonzola Telino", "Dairy Products", 12.500, 0),
            new(32, "Mascarpone Fabioli", "Dairy Products", 32.000, 9),
            new(33, "Geitost", "Dairy Products", 2.500, 112),
            new(34, "Sasquatch Ale", "Beverages", 14.000, 111),
            new(35, "Steeleye Stout", "Beverages", 18.000, 20),
            new(36, "Inlagd Sill", "Seafood", 19.000, 112),
            new(37, "Gravad lax", "Seafood", 26.000, 11),
            new(38, "C\u00f4te de Blaye", "Beverages", 263.500, 17),
            new(39, "Chartreuse verte", "Beverages", 18.000, 69),
            new(40, "Boston Crab Meat", "Seafood", 18.400, 123),
            new(41, "Jack's New England Clam Chowder", "Seafood", 9.650, 85),
            new(42, "Singaporean Hokkien Fried Mee", "Grains/Cereals", 14.000, 26),
            new(43, "Ipoh Coffee", "Beverages", 46.000, 17),
            new(44, "Gula Malacca", "Condiments", 19.450, 27),
            new(45, "Rogede sild", "Seafood", 9.500, 5),
            new(46, "Spegesild", "Seafood", 12.000, 95),
            new(47, "Zaanse koeken", "Confections", 9.500, 36),
            new(48, "Chocolade", "Confections", 12.750, 15),
            new(49, "Maxilaku", "Confections", 20.000, 10),
            new(50, "Valkoinen suklaa", "Confections", 16.250, 65),
            new(51, "Manjimup Dried Apples", "Produce", 53.000, 20),
            new(52, "Filo Mix", "Grains/Cereals", 7.000, 38),
            new(53, "Perth Pasties", "Meat/Poultry", 32.800, 0),
            new(54, "Tourti\u00e8re", "Meat/Poultry", 7.450, 21),
            new(55, "P\u00e2t\u00e9 chinois", "Meat/Poultry", 24.000, 115),
            new(56, "Gnocchi di nonna Alice", "Grains/Cereals", 38.000, 21),
            new(57, "Ravioli Angelo", "Grains/Cereals", 19.500, 36),
            new(58, "Escargots de Bourgogne", "Seafood", 13.250, 62),
            new(59, "Raclette Courdavault", "Dairy Products", 55.000, 79),
            new(60, "Camembert Pierrot", "Dairy Products", 34.000, 19),
            new(61, "Sirop d'\u00e9rable", "Condiments", 28.500, 113),
            new(62, "Tarte au sucre", "Confections", 49.300, 17),
            new(63, "Vegie-spread", "Condiments", 43.900, 24),
            new(64, "Wimmers gute Semmelkn\u00f6del", "Grains/Cereals", 33.250, 22),
            new(65, "Louisiana Fiery Hot Pepper Sauce", "Condiments", 21.050, 76),
            new(66, "Louisiana Hot Spiced Okra", "Condiments", 17.000, 4),
            new(67, "Laughing Lumberjack Lager", "Beverages", 14.000, 52),
            new(68, "Scottish Longbreads", "Confections", 12.500, 6),
            new(69, "Gudbrandsdalsost", "Dairy Products", 36.000, 26),
            new(70, "Outback Lager", "Beverages", 15.000, 15),
            new(71, "Flotemysost", "Dairy Products", 21.500, 26),
            new(72, "Mozzarella di Giovanni", "Dairy Products", 34.800, 14),
            new(73, "R\u00f6d Kaviar", "Seafood", 15.000, 101),
            new(74, "Longlife Tofu", "Produce", 10.000, 4),
            new(75, "Rh\u00f6nbr\u00e4u Klosterbier", "Beverages", 7.750, 125),
            new(76, "Lakkalik\u00f6\u00f6ri", "Beverages", 18.000, 57),
            new(77, "Original Frankfurter gr\u00fcne So\u00dfe", "Condiments", 13.000, 32)        
        };

        private static List<Customer> customers;
        public static List<Customer> Customers 
        {
            get
            {
                if (customers != null)
                    return customers;

                var path = "~/App_Data/customers.json".MapAbsolutePath();
                var json = File.ReadAllText(path);
                customers = json.FromJson<List<Customer>>();
                return customers;
            }
        }

        public static Customer GetCustomer(string id) => Customers.FirstOrDefault(x => x.CustomerId == id);
    }
}