using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using ServiceStack.DataAnnotations;
using ServiceStack.DesignPatterns.Model;

namespace Northwind.Common.DataModel
{
	public class OrderBlob
		: IHasIntId
	{
		public OrderBlob()
		{
			this.OrderDetails = new List<OrderDetailBlob>();
		}

		[AutoIncrement]
		public int Id { get; set; }

		public Customer Customer { get; set; }

		public Employee Employee { get; set; }

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

		public List<OrderDetailBlob> OrderDetails { get; set; }

		public List<int> IntIds { get; set; }

		public Dictionary<int, string> CharMap { get; set; }

		public static OrderBlob Create(int orderId)
		{
			return new OrderBlob {
             	Id = orderId,
             	Customer = NorthwindFactory.Customer("ALFKI", "Alfreds Futterkiste", "Maria Anders", "Sales Representative", "Obere Str. 57", "Berlin", null, "12209", "Germany", "030-0074321", "030-0076545", null),
             	Employee = NorthwindFactory.Employee(1, "Davolio", "Nancy", "Sales Representative", "Ms.", NorthwindData.ToDateTime("12/08/1948"), NorthwindData.ToDateTime("05/01/1992"), "507 - 20th Ave. E. Apt. 2A", "Seattle", "WA", "98122", "USA", "(206) 555-9857", "5467", null, "Education includes a BA in psychology from Colorado State University in 1970.  She also completed 'The Art of the Cold Call.'  Nancy is a member of Toastmasters International.", 2, "http://accweb/emmployees/davolio.bmp"),
             	OrderDate = NorthwindData.ToDateTime("7/4/1996"),
             	RequiredDate = NorthwindData.ToDateTime("8/1/1996"),
             	ShippedDate = NorthwindData.ToDateTime("7/16/1996"),
             	ShipVia = 5,
             	Freight = 32.38m,
             	ShipName = "Vins et alcools Chevalier",
             	ShipAddress = "59 rue de l'Abbaye",
             	ShipCity = "Reims",
             	ShipRegion = null,
             	ShipPostalCode = "51100",
             	ShipCountry = "France",
             	OrderDetails = new List<OrderDetailBlob> {
             		new OrderDetailBlob { ProductId = 11, UnitPrice = 11, Quantity = 14, Discount = 0},
             		new OrderDetailBlob { ProductId = 42, UnitPrice = 9.8m, Quantity = 10, Discount = 0},
             		new OrderDetailBlob { ProductId = 72, UnitPrice = 34.8m, Quantity = 5, Discount = 0},
				 },
             	IntIds = new List<int> { 10, 20, 30 },
             	CharMap = new Dictionary<int, string>
 	          	{
 	          		{1,"A"},
 	          		{2,"B"},
 	          		{3,"C"},
 	          	}
             };
		}
	}
}