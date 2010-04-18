using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using ServiceStack.DesignPatterns.Model;

namespace ServiceStack.Examples.ServiceInterface.Types
{
	[DataContract]
	public class CustomerOrders
	{
		public CustomerOrders()
		{
			this.Orders = new List<Order>();
		}

		[DataMember]
		public Customer Customer { get; set; }

		[DataMember]
		public List<Order> Orders { get; set; }

		public override bool Equals(object obj)
		{
			var other = obj as CustomerOrders;
			if (other == null) return false;

			var i = 0;
			return this.Customer.Equals(other.Customer)
				   && this.Orders.All(x => x.Equals(other.Orders[i++]));
		}
	}

	[DataContract]
	public class Customer
		: IHasStringId
	{
		public Customer()
		{
		}

		public Customer(string customerId, string companyName, string contactName, string contactTitle,
			string address, string city, string region, string postalCode, string country,
			string phoneNo, string faxNo,
			byte[] picture)
		{
			Id = customerId;
			CompanyName = companyName;
			ContactName = contactName;
			ContactTitle = contactTitle;
			Address = address;
			City = city;
			Region = region;
			PostalCode = postalCode;
			Country = country;
			Phone = phoneNo;
			Fax = faxNo;
			Picture = picture;
		}

		[DataMember]
		public string Id { get; set; }

		[DataMember]
		public string CompanyName { get; set; }

		[DataMember]
		public string ContactName { get; set; }

		[DataMember]
		public string ContactTitle { get; set; }

		[DataMember]
		public string Address { get; set; }

		[DataMember]
		public string City { get; set; }

		[DataMember]
		public string Region { get; set; }

		[DataMember]
		public string PostalCode { get; set; }

		[DataMember]
		public string Country { get; set; }

		[DataMember]
		public string Phone { get; set; }

		[DataMember]
		public string Fax { get; set; }

		//[TextField]
		//[DataMember]
		public byte[] Picture { get; set; }

		public override bool Equals(object obj)
		{
			var other = obj as Customer;
			if (other == null) return false;

			return this.Address == other.Address
				   && this.City == other.City
				   && this.CompanyName == other.CompanyName
				   && this.ContactName == other.ContactName
				   && this.ContactTitle == other.ContactTitle
				   && this.Country == other.Country
				   && this.Fax == other.Fax
				   && this.Id == other.Id
				   && this.Phone == other.Phone
				   && this.Picture == other.Picture
				   && this.PostalCode == other.PostalCode
				   && this.Region == other.Region;
		}
	}


	[DataContract]
	public class Order
	{
		public Order()
		{
			this.OrderDetails = new List<OrderDetail>();
		}

		[DataMember]
		public OrderHeader OrderHeader { get; set; }

		[DataMember]
		public List<OrderDetail> OrderDetails { get; set; }

		public override bool Equals(object obj)
		{
			var other = obj as Order;
			if (other == null) return false;

			var i = 0;
			return this.OrderHeader.Equals(other.OrderHeader)
				   && this.OrderDetails.All(x => x.Equals(other.OrderDetails[i++]));
		}
	}

	[DataContract]
	public class OrderHeader
		: IHasIntId
	{
		public OrderHeader()
		{
		}

		public OrderHeader(
			int orderId, string customerId, int employeeId, DateTime? orderDate, DateTime? requiredDate,
			DateTime? shippedDate, int shipVia, decimal freight, string shipName,
			string address, string city, string region, string postalCode, string country)
		{
			Id = orderId;
			CustomerId = customerId;
			EmployeeId = employeeId;
			OrderDate = orderDate;
			RequiredDate = requiredDate;
			ShippedDate = shippedDate;
			ShipVia = shipVia;
			Freight = freight;
			ShipName = shipName;
			ShipAddress = address;
			ShipCity = city;
			ShipRegion = region;
			ShipPostalCode = postalCode;
			ShipCountry = country;
		}

		[DataMember]
		public int Id { get; set; }

		[DataMember]
		public string CustomerId { get; set; }

		[DataMember]
		public int EmployeeId { get; set; }

		[DataMember]
		public DateTime? OrderDate { get; set; }

		[DataMember]
		public DateTime? RequiredDate { get; set; }

		[DataMember]
		public DateTime? ShippedDate { get; set; }

		[DataMember]
		public int? ShipVia { get; set; }

		[DataMember]
		public decimal Freight { get; set; }

		[DataMember]
		public string ShipName { get; set; }

		[DataMember]
		public string ShipAddress { get; set; }

		[DataMember]
		public string ShipCity { get; set; }

		[DataMember]
		public string ShipRegion { get; set; }

		[DataMember]
		public string ShipPostalCode { get; set; }

		[DataMember]
		public string ShipCountry { get; set; }

		public override bool Equals(object obj)
		{
			var other = obj as OrderHeader;
			if (other == null) return false;

			return this.Id == other.Id
				   && this.CustomerId == other.CustomerId
				   && this.EmployeeId == other.EmployeeId
				   && this.OrderDate == other.OrderDate
				   && this.RequiredDate == other.RequiredDate
				   && this.ShippedDate == other.ShippedDate
				   && this.ShipVia == other.ShipVia
				   && this.Freight == other.Freight
				   && this.ShipName == other.ShipName
				   && this.ShipAddress == other.ShipAddress
				   && this.ShipCity == other.ShipCity
				   && this.ShipRegion == other.ShipRegion
				   && this.ShipPostalCode == other.ShipPostalCode
				   && this.ShipCountry == other.ShipCountry;
		}
	}

	[DataContract]
	public class OrderDetail
		: IHasStringId
	{
		public OrderDetail()
		{
		}

		public OrderDetail(
			int orderId, int productId, decimal unitPrice, short quantity, double discount)
		{
			OrderId = orderId;
			ProductId = productId;
			UnitPrice = unitPrice;
			Quantity = quantity;
			Discount = discount;
		}

		public string Id { get { return this.OrderId + "/" + this.ProductId; } }

		[DataMember]
		public int OrderId { get; set; }

		[DataMember]
		public int ProductId { get; set; }

		[DataMember]
		public decimal UnitPrice { get; set; }

		[DataMember]
		public short Quantity { get; set; }

		[DataMember]
		public double Discount { get; set; }

		public override bool Equals(object obj)
		{
			var other = obj as OrderDetail;
			if (other == null) return false;

			return this.Id == other.Id
				   && this.OrderId == other.OrderId
				   && this.ProductId == other.ProductId
				   && this.UnitPrice == other.UnitPrice
				   && this.Quantity == other.Quantity
				   && this.Discount == other.Discount;
		}
	}

}