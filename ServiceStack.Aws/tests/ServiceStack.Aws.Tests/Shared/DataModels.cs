using System.Collections.Generic;
using System.Linq;
using ServiceStack.DataAnnotations;

namespace ServiceStack.Server.Tests.Shared
{
    public class Customer
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }

        [Reference]
        public CustomerAddress PrimaryAddress { get; set; }

        [Reference]
        public List<Order> Orders { get; set; }

        protected bool Equals(Customer other)
        {
            return Id == other.Id &&
                string.Equals(Name, other.Name) &&
                Equals(PrimaryAddress, other.PrimaryAddress) &&
                Orders.EquivalentTo(other.Orders);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Customer)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (PrimaryAddress != null ? PrimaryAddress.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Orders != null ? Orders.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public class CustomerAddress
    {
        [AutoIncrement]
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }

        protected bool Equals(CustomerAddress other)
        {
            return Id == other.Id &&
                CustomerId == other.CustomerId &&
                string.Equals(AddressLine1, other.AddressLine1) &&
                string.Equals(AddressLine2, other.AddressLine2) &&
                string.Equals(City, other.City) &&
                string.Equals(State, other.State) &&
                string.Equals(Country, other.Country);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CustomerAddress)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode * 397) ^ CustomerId;
                hashCode = (hashCode * 397) ^ (AddressLine1 != null ? AddressLine1.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (AddressLine2 != null ? AddressLine2.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (City != null ? City.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (State != null ? State.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Country != null ? Country.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public class Order
    {
        [AutoIncrement]
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string LineItem { get; set; }
        public int Qty { get; set; }
        public decimal Cost { get; set; }

        protected bool Equals(Order other)
        {
            return Id == other.Id &&
                CustomerId == other.CustomerId &&
                string.Equals(LineItem, other.LineItem) &&
                Qty == other.Qty &&
                Cost == other.Cost;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Order)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode * 397) ^ CustomerId;
                hashCode = (hashCode * 397) ^ (LineItem != null ? LineItem.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Qty;
                hashCode = (hashCode * 397) ^ Cost.GetHashCode();
                return hashCode;
            }
        }
    }

    public class Country
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string CountryName { get; set; }
        public string CountryCode { get; set; }

        protected bool Equals(Country other)
        {
            return Id == other.Id
                && string.Equals(CountryName, other.CountryName)
                && string.Equals(CountryCode, other.CountryCode);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Country)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode * 397) ^ (CountryName != null ? CountryName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (CountryCode != null ? CountryCode.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public class Node
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<Node> Children { get; set; }

        public Node() {}

        public Node(int id, string name, IEnumerable<Node> children = null)
        {
            Id = id;
            Name = name;
            if (children != null)
                Children = children.ToList();
        }

        protected bool Equals(Node other)
        {
            return Id == other.Id &&
                string.Equals(Name, other.Name) &&
                Children.EquivalentTo(other.Children);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Node)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Children != null ? Children.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}